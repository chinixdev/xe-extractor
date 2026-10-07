# Instrucciones: registrar un caso nuevo

Un **caso** es un proceso de negocio del ERP (por ejemplo, "generar una receta") capturado de principio a fin. Estos son los pasos para registrar y procesar uno.

## 1. Captura

1. Iniciar la sesión de Extended Events en SSMS.
2. Iniciar la grabación de pantalla (MP4).
3. Realizar las acciones de negocio en el ERP, sin mezclar otros procesos.
4. Detener la sesión de Extended Events en SSMS.
5. Detener la grabación.

## 2. Traslado de archivos

Copiar los archivos a la carpeta del caso, dentro de `casos/` (o `Z:\casos` si está montada la unidad compartida del servidor):

| Archivo | Destino |
|---|---|
| Archivos `.xel` del servidor | `casos/<nombre_caso>/xel/` |
| Grabación `.mp4` | `casos/<nombre_caso>/` |

Los nombres de caso siguen el formato `caso_001`, `caso_002`, etc. Las carpetas que empiezan con `_` se ignoran.

## 3. Procesamiento

El segundo argumento es el **hostname del equipo** donde se hizo la prueba. Solo se analizan los eventos de ese equipo.

```bash
# Un caso
dotnet run -- caso_001 <HOSTNAME> --perfil

# Todos los casos nuevos
dotnet run -- --todos <HOSTNAME> --perfil

# Reprocesar todos, aunque ya estén actualizados
dotnet run -- --todos <HOSTNAME> --perfil --forzar
```

Al usar `--perfil` (o `--metadata`), el programa pide los datos de conexión:

```
Servidor: <servidor>
Base de datos: <base>   (Enter para aceptar la sugerida)
Usuario (vacío = usuario de Windows): <usuario>
Clave: ********         (no se muestra al escribir)
```

Con `--todos` la conexión se pide **una sola vez** para todos los casos. Para no escribirla cada vez, definir la variable de entorno `XE_CONN` con la cadena de conexión.

## 4. Generar resúmenes y visor

```bash
python herramientas/generar.py
```

Crea `resumen.md` en cada caso y actualiza `visor/datos.js`. Para revisar el resultado, abrir `visor/index.html` en el navegador.

## 5. Registrar el caso

Anotar en [registros.md](registros.md) el proceso levantado y quién lo hizo.

## Resultado esperado por caso

```
casos/<nombre_caso>/
├── xel/                    archivos de Extended Events
├── <grabacion>.mp4
├── trace.json              línea de tiempo completa
├── analysis_input.json     resumen y detalle de operaciones
├── metadata.json           estructura y perfil de tablas (con --perfil)
└── resumen.md              resumen legible
```

## Problemas frecuentes

| Síntoma | Causa probable |
|---|---|
| `No hay archivos .xel en: ...` | Los `.xel` no están en `casos/<caso>/xel/` |
| `No hay casos nuevos` | Todos están actualizados. Usar `--forzar` |
| `analysis_input.json` vacío | El hostname no coincide con el del equipo de la prueba |
| No se genera `metadata.json` | Faltan `--metadata` o `--perfil`, o falló la conexión |
