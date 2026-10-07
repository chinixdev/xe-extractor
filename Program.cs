using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.XEvent.XELite;
using XeExtractor.Models;

// Carpeta base de los casos: variable ERP_CASOS, o Z:\casos (carpeta compartida de la 10) si existe, o .\casos
string carpetaCasos = Environment.GetEnvironmentVariable("ERP_CASOS") is { Length: > 0 } envCasos
    ? envCasos
    : Directory.Exists(@"Z:\casos") ? @"Z:\casos" : "casos";


if (args.Contains("--todos"))
{
    var opciones = args.Where(a => a != "--todos" && a != "--forzar").ToList();
    bool forzar = args.Contains("--forzar");
    bool pideMetadata = opciones.Any(a => a == "--metadata" || a.StartsWith("--perfil"));

    var pendientes = new List<string>();

    foreach (string dir in Directory.Exists(carpetaCasos) ? Directory.GetDirectories(carpetaCasos).OrderBy(d => d).ToArray() : Array.Empty<string>())
    {
        if (Path.GetFileName(dir).StartsWith('_'))
            continue;

        string xelDir = Path.Combine(dir, "xel");
        var xels = Directory.Exists(xelDir) ? Directory.GetFiles(xelDir, "*.xel") : Array.Empty<string>();

        if (xels.Length == 0)
            continue;

        string analisis = Path.Combine(dir, "analysis_input.json");
        bool actualizado = File.Exists(analisis) &&
            File.GetLastWriteTime(analisis) > xels.Max(File.GetLastWriteTime) &&
            (!pideMetadata || File.Exists(Path.Combine(dir, "metadata.json")));

        if (forzar || !actualizado)
            pendientes.Add(Path.GetFileName(dir));
    }

    if (pendientes.Count == 0)
    {
        Console.WriteLine("No hay casos nuevos. (Usa --forzar para reprocesar todos.)");
        return;
    }

    Console.WriteLine($"Carpeta de casos: {Path.GetFullPath(carpetaCasos)}");
    Console.WriteLine($"Casos a procesar ({pendientes.Count}): {string.Join(", ", pendientes)}");

    // La conexión se pide una sola vez y la heredan los procesos de cada caso
    if (pideMetadata && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("XE_CONN")))
    {
        string? conexionUnica = PedirConexion("");

        if (conexionUnica != null)
            Environment.SetEnvironmentVariable("XE_CONN", conexionUnica);
    }

    string exe = Environment.ProcessPath!;

    foreach (string nombre in pendientes)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(exe) { UseShellExecute = false };

        // Bajo "dotnet run" el proceso es dotnet.exe y la .dll va como primer argumento
        if (Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            psi.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);

        psi.ArgumentList.Add(nombre);

        foreach (string opcion in opciones)
            psi.ArgumentList.Add(opcion);

        using var proceso = System.Diagnostics.Process.Start(psi)!;
        proceso.WaitForExit();
    }

    return;
}

if (args.Length == 0)
{
    Console.WriteLine("Uso: dotnet run -- <nombre_caso> [hostname] [--selects] [--perfil]   (ej: caso_001_guia SISTEMASPC2)");
    Console.WriteLine("     dotnet run -- --todos [hostname] [--perfil]                      (todos los casos nuevos)");
    return;
}

string caso = args[0];
bool conSelects = args.Contains("--selects");
string? hostFiltro = args.Skip(1).FirstOrDefault(a => !a.StartsWith("--"));
string carpetaCaso = Path.Combine(carpetaCasos, caso);
string carpetaXel = Path.Combine(carpetaCaso, "xel");

if (!Directory.Exists(carpetaXel) ||
    Directory.GetFiles(carpetaXel, "*.xel").Length == 0)
{
    Console.WriteLine($"No hay archivos .xel en: {carpetaXel}");
    return;
}

string archivoTrace = Path.Combine(carpetaCaso, "trace.json");
string archivoAnalysis = Path.Combine(carpetaCaso, "analysis_input.json");

Console.WriteLine("======================================");
Console.WriteLine("     EXTRACTOR DE EXTENDED EVENTS");
Console.WriteLine($"     Caso: {caso}");
Console.WriteLine($"     Carpeta: {Path.GetFullPath(carpetaCasos)}");
Console.WriteLine("======================================");
Console.WriteLine();

var eventos = new List<TraceEvent>();

foreach (var archivo in Directory.GetFiles(carpetaXel, "*.xel").OrderBy(f => f))
{
    var xeStream = new XEFileEventStreamer(archivo);

    Console.WriteLine($"Leyendo: {Path.GetFileName(archivo)}");

    xeStream.ReadEventStream(
        () => Task.CompletedTask,
        xevent =>
        {
            var traceEvent = new TraceEvent
            {
                Timestamp = xevent.Timestamp,
                Event = xevent.Name,

                Database = GetActionValue(xevent, "database_name"),
                Username = GetActionValue(xevent, "username"),
                Hostname = GetActionValue(xevent, "client_hostname"),
                Application = GetActionValue(xevent, "client_app_name"),
                Sql = CleanSql(GetActionValue(xevent, "sql_text")),
                Statement = CleanSql(GetFieldText(xevent, "statement", "batch_text")),
                ProcName = GetFieldText(xevent, "object_name"),

                SessionId = GetIntActionValue(xevent, "session_id"),

                Metrics = new TraceMetrics
                {
                    Duration = GetLongFieldValue(xevent, "duration"),
                    CpuTime = GetLongFieldValue(xevent, "cpu_time"),
                    LogicalReads = GetLongFieldValue(xevent, "logical_reads"),
                    Writes = GetLongFieldValue(xevent, "writes"),
                    RowCount = GetLongFieldValue(xevent, "row_count")
                }
            };

            eventos.Add(traceEvent);

            return Task.CompletedTask;
        },
        CancellationToken.None
    ).Wait();
}

eventos = eventos.OrderBy(e => e.Timestamp).ToList();

Console.WriteLine();


// ======================================================
// 1. GUARDAR TRACE.JSON
// ======================================================

// Los datos de conexión (BD, usuario, PC, aplicación) van una sola vez por sesión
var traceFile = new TraceFile
{
    Case = caso,
    TotalEvents = eventos.Count,
    Sessions = eventos
        .GroupBy(e => new { e.SessionId, e.Database, e.Username, e.Hostname, e.Application })
        .Select(g => new TraceSession
        {
            SessionId = g.Key.SessionId,
            Database = g.Key.Database,
            Username = g.Key.Username,
            Hostname = g.Key.Hostname,
            Application = g.Key.Application,
            Events = g.Select(e => new TraceLine
            {
                Time = e.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Event = e.Event,
                Via = ProcPadre(e),
                Sql = TextoEvento(e),
                Duration = e.Metrics.Duration,
                Reads = e.Metrics.LogicalReads,
                Writes = e.Metrics.Writes,
                Rows = e.Metrics.RowCount
            }).ToList()
        })
        .OrderBy(s => s.Events[0].Time)
        .ToList()
};

var opcionesTrace = new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
};

string jsonTrace = JsonSerializer.Serialize(traceFile, opcionesTrace);

File.WriteAllText(archivoTrace, jsonTrace);

Console.WriteLine($"Eventos procesados: {eventos.Count}");
Console.WriteLine($"Archivo generado: {archivoTrace}");


// ======================================================
// 2. GENERAR ANALYSIS_INPUT.JSON
// ======================================================

var analysis = new AnalysisInput
{
    Case = caso
};

// Operaciones planas (una por sentencia y objeto), antes de agrupar para el detalle
var planas = new List<AnalysisOperation>();

foreach (var evento in eventos)
{
    // sql_batch_completed repite el texto de sus sentencias: se analiza cada sentencia
    if (evento.Event == "sql_batch_completed")
        continue;

    string texto = TextoEvento(evento);

    if (string.IsNullOrWhiteSpace(texto))
        continue;

    // Ruido: consultas hechas desde SSMS (no son del ERP)
    if (evento.Application.StartsWith("Microsoft SQL Server Management Studio",
            StringComparison.OrdinalIgnoreCase))
        continue;

    if (hostFiltro != null &&
        !evento.Hostname.Equals(hostFiltro, StringComparison.OrdinalIgnoreCase))
        continue;

    string sqlReal = DesempaquetarSql(texto);

    foreach (var resultado in DetectarObjetos(sqlReal))
    {
        if (analysis.Database == "")
        {
            analysis.Database = evento.Database;
            analysis.Username = evento.Username;
        }

        planas.Add(new AnalysisOperation
        {
            Timestamp = evento.Timestamp.ToLocalTime(),
            Operation = resultado.Operation,
            ObjectType = resultado.ObjectType,
            ObjectName = resultado.ObjectName,
            Via = ProcPadre(evento),
            Sql = sqlReal,
            SessionId = evento.SessionId
        });
    }
}

// Eliminar duplicados (se hace una sola vez, después de recorrer todos los eventos)
planas = planas
    .OrderBy(x => x.Timestamp)
    .GroupBy(x => new
    {
        x.SessionId,
        x.Operation,
        x.ObjectType,
        x.ObjectName,
        Sql = NormalizarSql(x.Sql)
    })
    .SelectMany(grupo =>
    {
        // Dentro del grupo (ya ordenado por Timestamp) se conserva el primer evento
        // y solo se descartan los que ocurren a <= 1 ms del último conservado.
        var conservados = new List<AnalysisOperation>();

        foreach (var op in grupo)
        {
            if (conservados.Count == 0 ||
                (op.Timestamp - conservados[^1].Timestamp).TotalMilliseconds > 1)
            {
                conservados.Add(op);
            }
        }

        return conservados;
    })
    .OrderBy(x => x.Timestamp)
    .ToList();

// Cabecera: una fila por tabla / procedure. Columnas por regex (mejor esfuerzo);
// en JOINs las columnas con alias pueden quedar aproximadas.
analysis.Summary = planas
    .GroupBy(x => new { x.ObjectType, x.ObjectName })
    .Select(g => new AnalysisObject
    {
        ObjectType = g.Key.ObjectType,
        ObjectName = g.Key.ObjectName,
        Operations = g.Select(x => x.Operation).Distinct().OrderBy(x => x).ToList(),
        Executions = g.Count(),
        Columns = g
            .SelectMany(x => ExtraerColumnas(x.Sql, x.Operation))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList(),
        UsedBy = g
            .Where(x => x.Via != null)
            .Select(x => x.Via!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList()
    })
    .OrderBy(x => x.ObjectType)
    .ThenBy(x => x.ObjectName)
    .ToList();

// Detalle: una fila por sentencia "de la misma forma" (mismos valores cambiados por ?),
// con las tablas que toca y cuántas veces se repitió. Se conserva el orden de la primera vez.
analysis.Operations = planas
    .Where(x => conSelects || x.Operation != "SELECT")
    // EXEC: los parámetros importan (otro DNI, otro código), así que solo se agrupan llamadas idénticas.
    // El resto de sentencias se agrupan por "forma" (mismos valores cambiados por ?).
    .GroupBy(x => new { x.SessionId, x.Via, x.Operation, Forma = x.Operation == "EXEC" ? NormalizarSql(x.Sql) : FormaSql(x.Sql) })
    .Select(g => new AnalysisStep
    {
        Timestamp = g.First().Timestamp,
        SessionId = g.Key.SessionId,
        Via = g.Key.Via,
        Operation = g.Key.Operation,
        Objects = g.Select(x => x.ObjectName).Distinct().OrderBy(x => x).ToList(),
        Count = g.Count() / Math.Max(1, g.Select(x => x.ObjectName).Distinct().Count()),
        Sql = Recortar(g.First().Sql, 400)
    })
    .OrderBy(x => x.Timestamp)
    .ToList();

string jsonAnalysis = JsonSerializer.Serialize(
    analysis,
    opcionesTrace
);

File.WriteAllText(archivoAnalysis, jsonAnalysis);

Console.WriteLine($"Archivo generado: {archivoAnalysis}");


// ======================================================
// 3. METADATA.JSON (opcional, solo lectura sobre la BD)
//    Requiere la variable de entorno XE_CONN con la cadena de conexión.
// ======================================================

// --perfil[=N]: además calcula % de vacíos por columna sobre las últimas N filas (20000 por defecto)
int perfilFilas = 0;
string? argPerfil = args.FirstOrDefault(a => a.StartsWith("--perfil"));

if (argPerfil != null)
{
    perfilFilas = argPerfil.Contains('=') && int.TryParse(argPerfil.Split('=')[1], out int n) && n > 0
        ? n
        : 20000;
}

if (args.Contains("--metadata") || perfilFilas > 0)
{
    // Si no existe XE_CONN, se pide la conexión por consola (la clave no se muestra ni se guarda)
    string? conexion = Environment.GetEnvironmentVariable("XE_CONN");

    if (string.IsNullOrWhiteSpace(conexion))
        conexion = PedirConexion(analysis.Database);

    if (string.IsNullOrWhiteSpace(conexion))
    {
        Console.WriteLine("Sin conexión: no se generó metadata.json.");
    }
    else
    {
        try
        {
            MetadataExtractor.Generar(
                conexion,
                caso,
                analysis.Database,
                analysis.Summary.Select(s => s.ObjectName),
                carpetaCaso,
                opcionesTrace,
                perfilFilas).Wait();
        }
        catch (AggregateException ex)
        {
            // Mensaje corto en vez del stack trace completo
            Console.WriteLine("No se pudo generar metadata.json: " + ex.GetBaseException().Message);
        }
    }
}


// Pide servidor, usuario y clave. Devuelve null si la consola no es interactiva.
static string? PedirConexion(string baseDatos)
{
    if (Console.IsInputRedirected)
    {
        Console.WriteLine("Define XE_CONN o ejecuta en una terminal interactiva para indicar la conexión.");
        return null;
    }

    Console.WriteLine();
    Console.WriteLine("--- Conexión a la base de datos (solo lectura) ---");

    Console.Write("Servidor: ");
    string servidor = Console.ReadLine()?.Trim() ?? "";

    Console.Write(baseDatos.Length > 0
        ? $"Base de datos [{baseDatos}]: "
        : "Base de datos (vacío = la que usó cada caso): ");
    string bd = Console.ReadLine()?.Trim() ?? "";

    Console.Write("Usuario (vacío = usuario de Windows): ");
    string usuario = Console.ReadLine()?.Trim() ?? "";

    if (servidor.Length == 0)
        return null;

    var cs = new System.Text.StringBuilder();
    string destino = bd.Length > 0 ? bd : baseDatos;

    cs.Append($"Server={servidor};TrustServerCertificate=true;");

    if (destino.Length > 0)
        cs.Append($"Database={destino};");

    if (usuario.Length == 0)
    {
        cs.Append("Integrated Security=true;");
    }
    else
    {
        Console.Write("Clave: ");
        var clave = new System.Text.StringBuilder();

        ConsoleKeyInfo tecla;
        while ((tecla = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (clave.Length > 0)
                    clave.Length--;
            }
            else if (!char.IsControl(tecla.KeyChar))
            {
                clave.Append(tecla.KeyChar);
            }
        }

        Console.WriteLine();

        // Comillas dobles alrededor de la clave: admite ; y espacios
        cs.Append($"User Id={usuario};Password=\"{clave.ToString().Replace("\"", "\"\"")}\";");
    }

    return cs.ToString();
}


// ======================================================
// MÉTODOS AUXILIARES
// ======================================================

static string CleanSql(string sql)
{
    return sql
        .Replace("\0", "")
        .Trim();
}


// Misma sentencia con otros valores (números y textos) => misma forma
static string FormaSql(string sql)
{
    string s = Regex.Replace(sql, @"'(?:[^']|'')*'", "?");
    s = Regex.Replace(s, @"\b\d+(?:\.\d+)?\b", "?");

    return NormalizarSql(s);
}


static string Recortar(string texto, int max)
{
    texto = Regex.Replace(texto, @"\s+", " ").Trim();

    return texto.Length <= max
        ? texto
        : texto[..max] + "...";
}


static string NormalizarSql(string sql)
{
    return Regex
        .Replace(sql, @"\s+", " ")
        .Trim()
        .ToUpperInvariant();
}


// Texto a analizar: la sentencia real si existe; si no, el sql_text.
static string TextoEvento(TraceEvent e)
{
    return e.Statement.Length > 0 ? e.Statement : e.Sql;
}


// Procedure dentro del cual corrió una sentencia. object_name no siempre viene en el
// evento; en sp_statement_completed el sql_text es el EXEC que originó la sentencia.
static string? ProcPadre(TraceEvent e)
{
    if (e.ProcName.Length > 0)
        return e.ProcName;

    if (e.Event != "sp_statement_completed")
        return null;

    foreach (var r in DetectarObjetos(DesempaquetarSql(e.Sql)))
    {
        if (r.Operation == "EXEC")
            return r.ObjectName;
    }

    return null;
}


// Primer campo (field) de la lista que tenga valor.
static string GetFieldText(IXEvent xevent, params string[] nombres)
{
    foreach (string nombre in nombres)
    {
        foreach (var field in xevent.Fields)
        {
            if (field.Key == nombre)
            {
                string valor = field.Value?.ToString() ?? "";

                if (valor.Length > 0)
                    return valor;
            }
        }
    }

    return "";
}


static string GetActionValue(IXEvent xevent, string name)
{
    foreach (var action in xevent.Actions)
    {
        if (action.Key == name)
            return action.Value?.ToString() ?? "";
    }

    return "";
}


static int GetIntActionValue(IXEvent xevent, string name)
{
    string value = GetActionValue(xevent, name);

    return int.TryParse(value, out int result)
        ? result
        : 0;
}


static long GetLongFieldValue(IXEvent xevent, string name)
{
    foreach (var field in xevent.Fields)
    {
        if (field.Key == name)
        {
            return long.TryParse(
                field.Value?.ToString(),
                out long result
            )
                ? result
                : 0;
        }
    }

    return 0;
}


// ======================================================
// DETECCIÓN DE OPERACIONES Y OBJETOS
// ======================================================

const string NombreObjeto =
    @"(?:\[?[A-Za-z0-9_]+\]?\.){0,2}(?:\[([^\]]+)\]|([A-Za-z0-9_#]+))";

static List<(string Operation, string ObjectType, string ObjectName)> DetectarObjetos(
    string sql)
{
    string texto = sql.Trim();
    var lista = new List<(string Operation, string ObjectType, string ObjectName)>();

    // EXEC / EXECUTE (se ignoran los procedimientos internos del driver)
    Match exec = Regex.Match(
        texto,
        @"^\s*EXEC(?:UTE)?\s+" + NombreObjeto,
        RegexOptions.IgnoreCase);

    if (exec.Success)
    {
        string nombre = NombreDe(exec);

        if (!nombre.StartsWith("sp_", StringComparison.OrdinalIgnoreCase))
            lista.Add(("EXEC", "PROCEDURE", nombre));

        return lista;
    }

    // Escrituras: se evalúan ANTES que SELECT, porque
    // "INSERT INTO x SELECT ... FROM y" contiene un FROM.
    AgregarCoincidencias(lista, texto, @"\bINSERT\s+(?:INTO\s+)?", "INSERT");
    AgregarCoincidencias(lista, texto, @"^\s*UPDATE\s+", "UPDATE");
    AgregarCoincidencias(lista, texto, @"\bDELETE\s+(?:FROM\s+)?", "DELETE");

    // Lecturas: todas las tablas de FROM y JOIN
    // (el "FROM" de un DELETE FROM ya quedó registrado como DELETE)
    var escritas = lista.Select(x => x.ObjectName).ToHashSet(StringComparer.OrdinalIgnoreCase);

    var lecturas = new List<(string Operation, string ObjectType, string ObjectName)>();
    AgregarCoincidencias(lecturas, texto, @"\b(?:FROM|JOIN)\s+", "SELECT");

    lista.AddRange(lecturas.Where(x => !escritas.Contains(x.ObjectName)));

    return lista.Distinct().ToList();
}

static void AgregarCoincidencias(
    List<(string Operation, string ObjectType, string ObjectName)> lista,
    string texto,
    string prefijo,
    string operacion)
{
    foreach (Match m in Regex.Matches(
        texto, prefijo + NombreObjeto, RegexOptions.IgnoreCase))
    {
        string nombre = NombreDe(m).ToUpperInvariant();

        // FETCH NEXT FROM <cursor> INTO ...: el nombre es un cursor, no una tabla
        if (Regex.IsMatch(
                texto[..m.Index],
                @"\bFETCH\s+(?:NEXT|PRIOR|FIRST|LAST)?\s*$",
                RegexOptions.IgnoreCase))
            continue;

        // Se descartan temporales, tablas especiales de triggers, alias de 1-2 letras
        // y variables de tabla (@nombre) que no son tablas reales
        if (nombre.Length <= 2 ||
            nombre.StartsWith('#') ||
            nombre is "INSERTED" or "DELETED" ||
            Regex.IsMatch(texto, @"@" + Regex.Escape(nombre) + @"\b", RegexOptions.IgnoreCase))
            continue;

        lista.Add((operacion, "TABLE", nombre));
    }
}

static string NombreDe(Match m)
{
    return m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
}


// ADO/ODBC envuelve el SQL real en sp_executesql / sp_prepexec: se extrae el statement.
static string DesempaquetarSql(string sql)
{
    if (!Regex.IsMatch(sql, @"\bsp_(executesql|prepexec|prepare|cursoropen|cursorprepexec|cursorprepare)\b",
            RegexOptions.IgnoreCase))
        return sql;

    // El statement es el primer literal N'...' que no sea la lista de parámetros (@P1 ...)
    foreach (Match m in Regex.Matches(sql, @"N'((?:[^']|'')*)'", RegexOptions.IgnoreCase))
    {
        string literal = m.Groups[1].Value.Replace("''", "'").Trim();

        if (literal.Length > 0 && !literal.StartsWith('@'))
            return literal;
    }

    return sql;
}


// ======================================================
// COLUMNAS (regex, mejor esfuerzo; no es un parser SQL)
// ======================================================

static bool EsPalabraReservada(string palabra)
{
    return palabra.ToUpperInvariant() is
        "AS" or "DISTINCT" or "TOP" or "NULL" or "ALL" or "CASE" or "WHEN" or "THEN"
        or "ELSE" or "END" or "AND" or "OR" or "NOT" or "IS" or "IN" or "LIKE"
        or "BETWEEN" or "SELECT" or "FROM" or "WHERE" or "ASC" or "DESC" or "ON"
        or "JOIN" or "INNER" or "LEFT" or "RIGHT" or "OUTER" or "GROUP" or "ORDER" or "BY";
}

static IEnumerable<string> ExtraerColumnas(string sql, string operacion)
{
    // Se quitan los literales de texto para no leer valores como columnas
    string s = Regex.Replace(sql, @"'(?:[^']|'')*'", "''");

    var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    void Agregar(string nombre)
    {
        nombre = nombre.Trim().Trim('[', ']');

        if (nombre.Length > 0 && !char.IsDigit(nombre[0]) && !EsPalabraReservada(nombre))
            columnas.Add(nombre);
    }

    if (operacion == "INSERT")
    {
        Match m = Regex.Match(s, @"\bINSERT\s+(?:INTO\s+)?" + NombreObjeto + @"\s*\(([^)]*)\)", RegexOptions.IgnoreCase);

        if (m.Success)
        {
            foreach (string c in m.Groups[3].Value.Split(','))
                Agregar(c);
        }
    }
    else if (operacion == "UPDATE")
    {
        Match m = Regex.Match(s, @"\bSET\b(.*?)(?:\bFROM\b|\bWHERE\b|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (m.Success)
        {
            foreach (Match c in Regex.Matches(m.Groups[1].Value, @"(?:^|,)\s*(?:\w+\.)?\[?(\w+)\]?\s*="))
                Agregar(c.Groups[1].Value);
        }
    }
    else if (operacion == "SELECT")
    {
        Match m = Regex.Match(s, @"^\s*SELECT\b(.*?)\bFROM\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (m.Success)
        {
            if (m.Groups[1].Value.Contains('*'))
                columnas.Add("*");

            foreach (Match c in Regex.Matches(m.Groups[1].Value, @"\b([A-Za-z_]\w*)\b(?!\s*\()"))
                Agregar(c.Groups[1].Value);
        }
    }

    // Columnas usadas en filtros / joins
    foreach (Match c in Regex.Matches(
        s,
        @"(?:\bWHERE\b|\bAND\b|\bOR\b|\bON\b)\s*\(*\s*(?:\w+\.)?\[?([A-Za-z_]\w*)\]?\s*(?:=|<>|!=|>=|<=|>|<|\bLIKE\b|\bIN\b|\bBETWEEN\b|\bIS\b)",
        RegexOptions.IgnoreCase))
    {
        Agregar(c.Groups[1].Value);
    }

    return columnas;
}
