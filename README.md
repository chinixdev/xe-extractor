# XE-Extractor

Herramienta de **ingeniería inversa de un ERP** basada en *Extended Events* de SQL Server. Registra lo que el ERP hace en la base de datos mientras un usuario ejecuta una operación de negocio y lo convierte en documentación legible: qué procedimientos almacenados se ejecutan, qué tablas se leen o escriben y qué columnas intervienen.

## Contenido

1. [Problema y objetivo](#problema-y-objetivo)
2. [Cómo funciona](#cómo-funciona)
3. [Estructura del repositorio](#estructura-del-repositorio)
4. [Requisitos](#requisitos)
5. [Procedimiento paso a paso](#procedimiento-paso-a-paso)
6. [Archivos que genera](#archivos-que-genera)
7. [Visor web](#visor-web)
8. [Por qué la carpeta `casos/` está vacía](#por-qué-la-carpeta-casos-está-vacía)
9. [Seguridad y privacidad](#seguridad-y-privacidad)
10. [Limitaciones](#limitaciones)

## Problema y objetivo

El ERP no tiene documentación y su código fuente no está disponible. La única fuente fiable de su comportamiento es lo que ejecuta contra SQL Server. El objetivo es obtener, para cada proceso de negocio (por ejemplo "generar una receta" o "despachar químicos"):

- La secuencia de procedimientos almacenados y sentencias SQL.
- Las tablas y columnas que se modifican.
- Las relaciones entre tablas (claves foráneas, triggers, dependencias).
- Un mapa navegable que sirva de base para reescribir o integrar el proceso.

## Cómo funciona

Cada proceso que se quiere estudiar es un **caso**. El flujo es:

```
 Usuario opera el ERP         SQL Server              XE-Extractor                Resultado
 (grabado en video)   --->  Sesión Extended   --->   Lee los .xel, filtra  --->  trace.json
                            Events (.xel)            y agrupa las sentencias     analysis_input.json
                                                     + consulta de metadata      metadata.json
                                                                                 resumen.md + visor
```

1. **Captura.** Se inicia una sesión de Extended Events en SSMS, se hace la operación en el ERP y se detiene la sesión. SQL Server deja uno o más archivos `.xel`.
2. **Extracción (C#).** `Program.cs` lee los `.xel` con `XEFileEventStreamer` y construye una línea de tiempo con cada evento: hora, sesión, base de datos, usuario, equipo, aplicación, SQL, procedimiento y métricas (duración, lecturas, escrituras, filas).
3. **Análisis.** Sobre esa línea de tiempo se aplican estos pasos:
   - Se descartan las consultas hechas desde SSMS (ruido, no son del ERP).
   - Se filtra por el equipo (`hostname`) donde se hizo la prueba.
   - Se desempaqueta el SQL (`sp_executesql`) para ver la sentencia real.
   - Se detecta, por regex, la operación (`INSERT`, `UPDATE`, `DELETE`, `SELECT`, `EXEC`) y el objeto que toca.
   - Se eliminan duplicados casi simultáneos (≤ 1 ms).
   - Se agrupan las sentencias que solo cambian en sus valores. Los `EXEC` se agrupan solo si son idénticos.
4. **Metadata (opcional).** Con `--metadata` o `--perfil` se conecta a la base de datos en **solo lectura** y extrae, de las vistas `sys.*`, columnas, tipos, índices, claves foráneas, triggers y dependencias de cada objeto detectado. Con `--perfil` calcula además el porcentaje de nulos y vacíos por columna sobre una muestra (por defecto, las últimas 20 000 filas). Solo guarda conteos, nunca valores de los datos.
5. **Documentación (Python).** `herramientas/generar.py` une los tres JSON de cada caso y produce un `resumen.md` por caso y los datos del visor web.

## Estructura del repositorio

| Ruta | Descripción |
|---|---|
| `Program.cs` | Punto de entrada: lectura de `.xel`, análisis, generación de JSON y conexión a la base de datos |
| `models/` | Modelos de datos: `TraceEvent`, `AnalysisInput` y `Metadata` (extracción desde `sys.*`) |
| `herramientas/generar.py` | Genera `resumen.md` por caso y `visor/datos.js` |
| `visor/` | Visor web estático (`index.html` + `datos.js`): mapa de procedimientos y tablas |
| `casos/` | Carpeta de trabajo local para los casos (vacía en el repositorio, ver [más abajo](#por-qué-la-carpeta-casos-está-vacía)) |
| `instrucciones.md` | Secuencia operativa para registrar un caso nuevo |
| `registros.md` | Registro de los procesos ya levantados y de quién los hizo |

## Requisitos

- .NET 8 SDK
- Python 3 (solo para `generar.py`)
- SQL Server Management Studio, para crear y detener la sesión de Extended Events
- Acceso de lectura a la base de datos, para la opción de metadata
- Paquetes NuGet (se restauran solos): `Microsoft.Data.SqlClient` y `Microsoft.SqlServer.XEvent.XELite`

## Procedimiento paso a paso

### 1. Capturar un caso

1. En SSMS, iniciar la sesión de Extended Events en el servidor.
2. Iniciar la grabación de pantalla (MP4).
3. Realizar la operación de negocio en el ERP.
4. Detener la sesión de Extended Events y la grabación.
5. Copiar los archivos `.xel` a `casos/<nombre_caso>/xel/` y el video a `casos/<nombre_caso>/`.

### 2. Procesar con el extractor

```bash
# Un caso. El segundo argumento es el hostname del equipo donde se hizo la prueba
dotnet run -- caso_001 SISTEMASPC2 --perfil

# Todos los casos nuevos (omite los que ya están actualizados)
dotnet run -- --todos SISTEMASPC2 --perfil

# Forzar el reprocesado de todos
dotnet run -- --todos SISTEMASPC2 --perfil --forzar
```

Opciones:

| Opción | Efecto |
|---|---|
| `--selects` | Incluye los `SELECT` en el detalle (por defecto se omiten) |
| `--metadata` | Genera `metadata.json` consultando la base de datos |
| `--perfil[=N]` | Lo anterior, más el perfil de datos sobre las últimas N filas (20 000 por defecto) |
| `--todos` | Procesa todos los casos pendientes |
| `--forzar` | Con `--todos`, reprocesa también los casos ya actualizados |

Para la conexión, definir la variable de entorno `XE_CONN` con la cadena de conexión. Si no existe, el programa pide servidor, base de datos, usuario y clave por consola. La clave no se muestra ni se guarda. Con `--todos` la conexión se pide una sola vez.

La carpeta base de los casos se resuelve en este orden: variable `ERP_CASOS`, la unidad compartida `Z:\casos` si existe, o `.\casos`.

### 3. Generar resúmenes y visor

```bash
python herramientas/generar.py
```

### 4. Revisar

Abrir `visor/index.html` en el navegador.

## Archivos que genera

Por cada caso, en `casos/<caso>/`:

| Archivo | Contenido |
|---|---|
| `trace.json` | Línea de tiempo completa, agrupada por sesión |
| `analysis_input.json` | Resumen por tabla o procedimiento (operaciones, columnas, quién lo llama) y detalle cronológico |
| `metadata.json` | Estructura de las tablas y perfil de datos (si se pidió) |
| `resumen.md` | Resumen legible: procedimientos, tablas, columnas usadas y no usadas |

## Visor web

`visor/index.html` es una página estática, sin servidor ni dependencias. Lee `visor/datos.js` y muestra el mapa **Procedures ↔ Tablas** de todos los casos, con el flujo paso a paso de cada operación. Se regenera con `generar.py`.

## Por qué la carpeta `casos/` está vacía

Los casos **no viven en este repositorio ni en la PC de desarrollo**. El motivo es técnico y de seguridad.

- Los archivos `.xel` los escribe SQL Server **en el disco del propio servidor**, el equipo conocido como **"la 10"**. Hay que sacarlos de ahí.
- Contienen el SQL real del ERP, con datos de clientes, pedidos y precios. No pueden publicarse.
- Pesan mucho y cambian en cada prueba, por lo que no es práctico versionarlos.

Por eso la carpeta compartida de "la 10" se monta como unidad **`Z:\casos`**, y el programa la usa automáticamente cuando existe. Así los `.xel` se leen y los JSON se escriben en el servidor, sin duplicar nada. En un equipo sin esa unidad, `casos/` es solo la carpeta local de trabajo y está vacía.

Para trabajar en otro entorno, apuntar a otra carpeta con la variable `ERP_CASOS`.

## Seguridad y privacidad

- El acceso a la base de datos es de **solo lectura** y solo consulta `sys.*` y conteos para el perfil.
- Nunca se leen ni se guardan valores de las columnas. En las columnas LOB (`image`, `text`, `xml`, `(max)`) solo se mide el tamaño.
- Las credenciales no se guardan en ningún archivo ni en el repositorio.
- `bin/` y `obj/` están excluidos con `.gitignore`.

## Limitaciones

- La detección de tablas y columnas es por **regex** (mejor esfuerzo). No es un parser SQL completo, y en los `JOIN` con alias las columnas pueden quedar aproximadas.
- Las dependencias de `sys.dm_sql_referenced_entities` pueden no reflejar todos los `INSERT` y `DELETE`.
- La calidad del resultado depende de que la operación grabada sea limpia: un caso por proceso, con el hostname correcto.

## Licencia

Ver [LICENSE](LICENSE).
