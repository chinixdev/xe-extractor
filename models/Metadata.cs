#pragma warning disable CS8600, CS8601, CS8604, CS8605 // valores leídos de sys.*: las columnas usadas nunca son NULL

using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace XeExtractor.Models;

// ------------------------------------------------------
// metadata.json: estructura REAL (desde sys.*) de los objetos que aparecen en el caso.
// Solo lectura. Código de procedures/vistas/triggers va en metadata\codigo\*.sql
// ------------------------------------------------------

public class MetadataFile
{
    public string Case { get; set; } = "";

    public string Database { get; set; } = "";

    public string GeneratedAt { get; set; } = "";

    public List<MetaObject> Objects { get; set; } = new();

    // Nombres del trace que no existen como objeto en la BD (alias, temporales, etc.)
    public List<string> NotFound { get; set; } = new();
}

public class MetaObject
{
    public string Schema { get; set; } = "";

    public string Name { get; set; } = "";

    // USER_TABLE, VIEW, SQL_STORED_PROCEDURE, SQL_SCALAR_FUNCTION, ...
    public string Type { get; set; } = "";

    public List<MetaColumn> Columns { get; set; } = new();

    // Solo con --perfil: filas totales aproximadas y filas de la muestra analizada
    public long? Rows { get; set; }

    public int? SampleRows { get; set; }

    public List<string> PrimaryKey { get; set; } = new();

    public List<MetaIndex> Indexes { get; set; } = new();

    // Claves foráneas donde la tabla es origen (Out) o destino (In)
    public List<MetaForeignKey> ForeignKeys { get; set; } = new();

    public List<MetaTrigger> Triggers { get; set; } = new();

    // Procedures / vistas / funciones: archivo con la definición
    public string? CodeFile { get; set; }

    // Procedures / vistas / funciones: objetos que referencia según sys (lectura / escritura)
    public List<MetaDependency> Depends { get; set; } = new();
}

public class MetaColumn
{
    public string Name { get; set; } = "";

    public string Type { get; set; } = "";

    public bool Nullable { get; set; }

    public bool Identity { get; set; }

    public bool Computed { get; set; }

    // Perfil de datos (solo con --perfil), calculado sobre la muestra de la tabla.
    // Son conteos: no se guarda ningún valor de los datos.
    // % de filas con NULL
    public double? NullPct { get; set; }

    // % de filas con texto vacío o solo espacios (solo columnas de texto)
    public double? EmptyPct { get; set; }

    // valores distintos en la muestra (no se calcula en columnas LOB)
    public int? Distinct { get; set; }

    // Columna que guarda archivos o texto largo (image, text, ntext, xml, ...(max)).
    // No se trata como dato de negocio normal; el contenido nunca se lee.
    public bool Lob { get; set; }

    // Solo LOB con --perfil: tamaño promedio y máximo en bytes (sobre la muestra, solo filas no nulas)
    public long? AvgBytes { get; set; }

    public long? MaxBytes { get; set; }
}

public class MetaIndex
{
    public string Name { get; set; } = "";

    public bool Unique { get; set; }

    public string Columns { get; set; } = "";
}

public class MetaForeignKey
{
    public string Name { get; set; } = "";

    // "Out": esta tabla referencia a otra. "In": otra tabla referencia a esta.
    public string Direction { get; set; } = "";

    public string Table { get; set; } = "";

    public string Columns { get; set; } = "";

    public string RefTable { get; set; } = "";

    public string RefColumns { get; set; } = "";
}

public class MetaTrigger
{
    public string Name { get; set; } = "";

    public string Events { get; set; } = "";

    public bool Disabled { get; set; }

    public string? CodeFile { get; set; }
}

public class MetaDependency
{
    public string Object { get; set; } = "";

    public bool Reads { get; set; }

    // Pista de sys.dm_sql_referenced_entities (puede no reflejar INSERT/DELETE en todos los casos)
    public bool Writes { get; set; }
}


public static class MetadataExtractor
{
    public static async Task Generar(
        string connectionString,
        string caso,
        string database,
        IEnumerable<string> nombres,
        string carpetaCaso,
        JsonSerializerOptions opciones,
        int perfilFilas = 0)
    {
        var cs = new SqlConnectionStringBuilder(connectionString)
        {
            ApplicationIntent = ApplicationIntent.ReadOnly,
            ApplicationName = "XeExtractor-metadata"
        };

        if (string.IsNullOrEmpty(cs.InitialCatalog))
            cs.InitialCatalog = database;

        var lista = nombres
            .Select(n => n.ToUpperInvariant())
            .Distinct()
            .ToList();

        var meta = new MetadataFile
        {
            Case = caso,
            Database = cs.InitialCatalog,
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        string carpetaCodigo = Path.Combine(carpetaCaso, "metadata", "codigo");
        Directory.CreateDirectory(carpetaCodigo);

        await using var cn = new SqlConnection(cs.ConnectionString);
        await cn.OpenAsync();

        // 1. Resolver nombres -> objetos reales
        var porId = new Dictionary<int, MetaObject>();

        using (var cmd = cn.CreateCommand())
        {
            var parametros = lista.Select((_, i) => "@n" + i).ToList();

            for (int i = 0; i < lista.Count; i++)
                cmd.Parameters.AddWithValue(parametros[i], lista[i]);

            cmd.CommandText = $@"
                SELECT o.object_id, s.name, o.name, o.type_desc
                FROM sys.objects o
                JOIN sys.schemas s ON s.schema_id = o.schema_id
                WHERE o.is_ms_shipped = 0
                  AND o.type IN ('U','V','P','FN','IF','TF','TR')
                  AND UPPER(o.name) IN ({string.Join(",", parametros)})";

            await using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                porId[rd.GetInt32(0)] = new MetaObject
                {
                    Schema = rd.GetString(1),
                    Name = rd.GetString(2),
                    Type = rd.GetString(3)
                };
            }
        }

        var encontrados = porId.Values
            .Select(o => o.Name.ToUpperInvariant())
            .ToHashSet();

        meta.NotFound = lista.Where(n => !encontrados.Contains(n)).OrderBy(n => n).ToList();

        if (porId.Count == 0)
        {
            await Guardar(meta, carpetaCaso, opciones);
            return;
        }

        string ids = string.Join(",", porId.Keys);

        // 2. Columnas
        foreach (var fila in await Consultar(cn, $@"
            SELECT c.object_id, c.name, t.name, c.max_length, c.precision, c.scale,
                   c.is_nullable, c.is_identity, c.is_computed
            FROM sys.columns c
            JOIN sys.types t ON t.user_type_id = c.user_type_id
            WHERE c.object_id IN ({ids})
            ORDER BY c.object_id, c.column_id"))
        {
            string tipoColumna = FormatoTipo((string)fila[2], Convert.ToInt32(fila[3]), Convert.ToInt32(fila[4]), Convert.ToInt32(fila[5]));

            porId[(int)fila[0]].Columns.Add(new MetaColumn
            {
                Name = (string)fila[1],
                Type = tipoColumna,
                Nullable = (bool)fila[6],
                Identity = (bool)fila[7],
                Computed = (bool)fila[8],
                Lob = EsLob(tipoColumna)
            });
        }

        // 3. PK e índices
        var indices = await Consultar(cn, $@"
            SELECT i.object_id, i.name, i.is_unique, i.is_primary_key,
                   STUFF((SELECT ',' + col.name
                          FROM sys.index_columns ic
                          JOIN sys.columns col ON col.object_id = ic.object_id AND col.column_id = ic.column_id
                          WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
                          ORDER BY ic.key_ordinal
                          FOR XML PATH('')), 1, 1, '')
            FROM sys.indexes i
            WHERE i.object_id IN ({ids}) AND i.type > 0 AND i.is_hypothetical = 0");

        foreach (var fila in indices)
        {
            var obj = porId[(int)fila[0]];
            string columnas = fila[4] as string ?? "";

            if ((bool)fila[3])
            {
                obj.PrimaryKey = columnas.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                obj.Indexes.Add(new MetaIndex
                {
                    Name = (string)fila[1],
                    Unique = (bool)fila[2],
                    Columns = columnas
                });
            }
        }

        // 4. Claves foráneas (en ambos sentidos)
        var fks = await Consultar(cn, $@"
            SELECT fk.name, fk.parent_object_id, fk.referenced_object_id,
                   OBJECT_NAME(fk.parent_object_id), OBJECT_NAME(fk.referenced_object_id),
                   STUFF((SELECT ',' + pc.name
                          FROM sys.foreign_key_columns fc
                          JOIN sys.columns pc ON pc.object_id = fc.parent_object_id AND pc.column_id = fc.parent_column_id
                          WHERE fc.constraint_object_id = fk.object_id
                          ORDER BY fc.constraint_column_id
                          FOR XML PATH('')), 1, 1, ''),
                   STUFF((SELECT ',' + rc.name
                          FROM sys.foreign_key_columns fc
                          JOIN sys.columns rc ON rc.object_id = fc.referenced_object_id AND rc.column_id = fc.referenced_column_id
                          WHERE fc.constraint_object_id = fk.object_id
                          ORDER BY fc.constraint_column_id
                          FOR XML PATH('')), 1, 1, '')
            FROM sys.foreign_keys fk
            WHERE fk.parent_object_id IN ({ids}) OR fk.referenced_object_id IN ({ids})");

        foreach (var fila in fks)
        {
            var fk = new MetaForeignKey
            {
                Name = (string)fila[0],
                Table = (string)fila[3],
                RefTable = (string)fila[4],
                Columns = fila[5] as string ?? "",
                RefColumns = fila[6] as string ?? ""
            };

            if (porId.TryGetValue((int)fila[1], out var origen))
                origen.ForeignKeys.Add(new MetaForeignKey
                {
                    Name = fk.Name, Direction = "Out", Table = fk.Table,
                    Columns = fk.Columns, RefTable = fk.RefTable, RefColumns = fk.RefColumns
                });

            if (porId.TryGetValue((int)fila[2], out var destino) && (int)fila[1] != (int)fila[2])
                destino.ForeignKeys.Add(new MetaForeignKey
                {
                    Name = fk.Name, Direction = "In", Table = fk.Table,
                    Columns = fk.Columns, RefTable = fk.RefTable, RefColumns = fk.RefColumns
                });
        }

        // 5. Triggers de las tablas y vistas
        var triggers = await Consultar(cn, $@"
            SELECT tr.parent_id, tr.name, tr.is_disabled, sm.definition,
                   STUFF((SELECT ',' + te.type_desc
                          FROM sys.trigger_events te
                          WHERE te.object_id = tr.object_id
                          FOR XML PATH('')), 1, 1, '')
            FROM sys.triggers tr
            LEFT JOIN sys.sql_modules sm ON sm.object_id = tr.object_id
            WHERE tr.parent_id IN ({ids})");

        foreach (var fila in triggers)
        {
            string nombre = (string)fila[1];
            string? archivo = null;

            if (fila[3] is string definicion)
            {
                archivo = Path.Combine("metadata", "codigo", Sanear("TRIGGER." + nombre) + ".sql");
                File.WriteAllText(Path.Combine(carpetaCaso, archivo), definicion);
            }

            porId[(int)fila[0]].Triggers.Add(new MetaTrigger
            {
                Name = nombre,
                Events = fila[4] as string ?? "",
                Disabled = (bool)fila[2],
                CodeFile = archivo
            });
        }

        // 6. Código y dependencias de procedures, vistas y funciones
        var modulos = await Consultar(cn, $@"
            SELECT sm.object_id, sm.definition
            FROM sys.sql_modules sm
            JOIN sys.objects o ON o.object_id = sm.object_id
            WHERE sm.object_id IN ({ids}) AND o.type <> 'TR'");

        foreach (var fila in modulos)
        {
            var obj = porId[(int)fila[0]];

            if (fila[1] is string definicion)
            {
                obj.CodeFile = Path.Combine("metadata", "codigo", Sanear(obj.Schema + "." + obj.Name) + ".sql");
                File.WriteAllText(Path.Combine(carpetaCaso, obj.CodeFile), definicion);
            }

            try
            {
                var deps = await Consultar(cn, $@"
                    SELECT referenced_schema_name, referenced_entity_name,
                           MAX(CAST(is_selected AS int)), MAX(CAST(is_updated AS int))
                    FROM sys.dm_sql_referenced_entities('[{obj.Schema}].[{obj.Name}]', 'OBJECT')
                    WHERE referenced_entity_name IS NOT NULL
                    GROUP BY referenced_schema_name, referenced_entity_name");

                foreach (var d in deps)
                {
                    obj.Depends.Add(new MetaDependency
                    {
                        Object = string.IsNullOrEmpty(d[0] as string) ? (string)d[1] : d[0] + "." + d[1],
                        Reads = Convert.ToInt32(d[2]) == 1,
                        Writes = Convert.ToInt32(d[3]) == 1
                    });
                }
            }
            catch (SqlException)
            {
                // Hay objetos cuyas dependencias no se pueden resolver (SQL dinámico, nombres ausentes)
            }
        }

        // 7. Perfil de datos: qué tan vacía está cada columna (solo conteos, solo lectura)
        if (perfilFilas > 0)
        {
            foreach (var (id, obj) in porId.Where(p => p.Value.Type == "USER_TABLE" && p.Value.Columns.Count > 0))
            {
                try
                {
                    await PerfilarTabla(cn, id, obj, perfilFilas);
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"Perfil omitido en {obj.Name}: {ex.Message}");
                }
            }
        }

        meta.Objects = porId.Values
            .OrderBy(o => o.Type)
            .ThenBy(o => o.Name)
            .ToList();

        await Guardar(meta, carpetaCaso, opciones);
    }


    // Tipos donde no se puede (o no conviene) contar valores distintos
    static readonly HashSet<string> SinDistinct = new(StringComparer.OrdinalIgnoreCase)
    {
        "text", "ntext", "image", "xml", "geography", "geometry", "sql_variant"
    };

    static readonly HashSet<string> TiposTexto = new(StringComparer.OrdinalIgnoreCase)
    {
        "char", "varchar", "nchar", "nvarchar"
    };

    // Columnas que guardan archivos o texto largo (image, text, ntext, xml, ...(max))
    public static bool EsLob(string tipoFormateado)
    {
        string tipo = tipoFormateado.Split('(')[0];

        return tipo.Equals("image", StringComparison.OrdinalIgnoreCase)
            || tipo.Equals("text", StringComparison.OrdinalIgnoreCase)
            || tipo.Equals("ntext", StringComparison.OrdinalIgnoreCase)
            || tipo.Equals("xml", StringComparison.OrdinalIgnoreCase)
            || tipoFormateado.EndsWith("(max)", StringComparison.OrdinalIgnoreCase);
    }

    // Muestra reducida para tablas con columnas LOB (el contenido nunca se lee, pero cuesta más)
    const int MuestraConLob = 2000;

    static async Task PerfilarTabla(SqlConnection cn, int id, MetaObject obj, int filasMuestra)
    {
        string tabla = $"[{obj.Schema}].[{obj.Name}]";

        if (obj.Columns.Any(c => c.Lob))
            filasMuestra = Math.Min(filasMuestra, MuestraConLob);

        // Filas totales aproximadas (metadatos, no recorre la tabla)
        var total = await Consultar(cn, $@"
            SELECT SUM(row_count) FROM sys.dm_db_partition_stats
            WHERE object_id = {id} AND index_id IN (0, 1)");

        obj.Rows = total.Count > 0 && total[0][0] != null ? Convert.ToInt64(total[0][0]) : 0;

        // Muestra: las últimas N filas por PK (si hay PK). No es aleatoria.
        string orden = obj.PrimaryKey.Count > 0
            ? " ORDER BY " + string.Join(", ", obj.PrimaryKey.Select(c => $"[{c}] DESC"))
            : "";

        string origen = $"(SELECT TOP ({filasMuestra}) * FROM {tabla} WITH (NOLOCK){orden}) s";

        // Se procesa por bloques de columnas para no armar consultas enormes
        foreach (var bloque in obj.Columns.Chunk(40))
        {
            var partes = new List<string>();

            for (int i = 0; i < bloque.Length; i++)
            {
                string c = $"[{bloque[i].Name}]";
                string tipo = bloque[i].Type.Split('(')[0];
                bool lob = bloque[i].Lob;
                bool esTexto = TiposTexto.Contains(tipo) && !lob;
                bool sinDistinct = lob || SinDistinct.Contains(tipo);

                // COUNT(col) no admite image/text/ntext; IS NULL sí funciona con todos los tipos
                partes.Add($"SUM(CASE WHEN {c} IS NULL THEN 0 ELSE 1 END) AS nn{i}");
                partes.Add(sinDistinct ? $"CAST(NULL AS int) AS d{i}" : $"COUNT(DISTINCT {c}) AS d{i}");
                partes.Add(esTexto
                    ? $"SUM(CASE WHEN LTRIM(RTRIM({c})) = '' THEN 1 ELSE 0 END) AS e{i}"
                    : $"CAST(NULL AS int) AS e{i}");

                // LOB: solo el largo en bytes (DATALENGTH), nunca el contenido
                partes.Add(lob
                    ? $"AVG(CAST(DATALENGTH({c}) AS bigint)) AS a{i}"
                    : $"CAST(NULL AS bigint) AS a{i}");
                partes.Add(lob
                    ? $"MAX(CAST(DATALENGTH({c}) AS bigint)) AS m{i}"
                    : $"CAST(NULL AS bigint) AS m{i}");
            }

            var fila = (await Consultar(cn, $"SELECT COUNT(*) AS n, {string.Join(", ", partes)} FROM {origen}"))[0];

            int n = Convert.ToInt32(fila[0]);
            obj.SampleRows = n;

            if (n == 0)
                continue;

            for (int i = 0; i < bloque.Length; i++)
            {
                int baseIdx = 1 + i * 5;
                int noNulos = Convert.ToInt32(fila[baseIdx]);

                bloque[i].NullPct = Math.Round(100.0 * (n - noNulos) / n, 1);
                bloque[i].Distinct = fila[baseIdx + 1] == null ? null : Convert.ToInt32(fila[baseIdx + 1]);
                bloque[i].EmptyPct = fila[baseIdx + 2] == null ? null : Math.Round(100.0 * Convert.ToInt32(fila[baseIdx + 2]) / n, 1);
                bloque[i].AvgBytes = fila[baseIdx + 3] == null ? null : Convert.ToInt64(fila[baseIdx + 3]);
                bloque[i].MaxBytes = fila[baseIdx + 4] == null ? null : Convert.ToInt64(fila[baseIdx + 4]);
            }
        }
    }


    static async Task Guardar(MetadataFile meta, string carpetaCaso, JsonSerializerOptions opciones)
    {
        string ruta = Path.Combine(carpetaCaso, "metadata.json");

        await File.WriteAllTextAsync(ruta, JsonSerializer.Serialize(meta, opciones));

        Console.WriteLine($"Archivo generado: {ruta}");
        Console.WriteLine($"Objetos con metadata: {meta.Objects.Count}  |  no encontrados en la BD: {meta.NotFound.Count}");
    }


    static async Task<List<object?[]>> Consultar(SqlConnection cn, string sql)
    {
        var filas = new List<object?[]>();

        await using var cmd = cn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 60;

        await using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            var fila = new object?[rd.FieldCount];

            for (int i = 0; i < rd.FieldCount; i++)
                fila[i] = rd.IsDBNull(i) ? null : rd.GetValue(i);

            filas.Add(fila);
        }

        return filas;
    }


    static string FormatoTipo(string tipo, int longitud, int precision, int escala)
    {
        switch (tipo.ToLowerInvariant())
        {
            case "varchar":
            case "char":
            case "varbinary":
            case "binary":
                return longitud == -1 ? $"{tipo}(max)" : $"{tipo}({longitud})";

            case "nvarchar":
            case "nchar":
                return longitud == -1 ? $"{tipo}(max)" : $"{tipo}({longitud / 2})";

            case "decimal":
            case "numeric":
                return $"{tipo}({precision},{escala})";

            default:
                return tipo;
        }
    }


    static string Sanear(string nombre)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            nombre = nombre.Replace(c, '_');

        return nombre;
    }
}
