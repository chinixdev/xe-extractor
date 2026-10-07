namespace XeExtractor.Models;

// Evento plano, tal como se lee del .xel (uso interno).
public class TraceEvent
{
    public DateTimeOffset Timestamp { get; set; }

    public string Event { get; set; } = "";

    public string Database { get; set; } = "";

    public string Username { get; set; } = "";

    public int SessionId { get; set; }

    public string Hostname { get; set; } = "";

    public string Application { get; set; } = "";

    // Texto completo (acción sql_text): el lote o el EXEC externo.
    public string Sql { get; set; } = "";

    // Sentencia real que se ejecutó (campo statement / batch_text).
    public string Statement { get; set; } = "";

    // Procedure dentro del cual corrió la sentencia (campo object_name).
    public string ProcName { get; set; } = "";

    public TraceMetrics Metrics { get; set; } = new();
}

public class TraceMetrics
{
    public long Duration { get; set; }

    public long CpuTime { get; set; }

    public long LogicalReads { get; set; }

    public long Writes { get; set; }

    public long RowCount { get; set; }
}


// ------------------------------------------------------
// Formato de salida de trace.json: los datos de conexión
// se escriben una sola vez por sesión, no por evento.
// ------------------------------------------------------

public class TraceFile
{
    public string Case { get; set; } = "";

    public int TotalEvents { get; set; }

    public List<TraceSession> Sessions { get; set; } = new();
}

public class TraceSession
{
    public int SessionId { get; set; }

    public string Database { get; set; } = "";

    public string Username { get; set; } = "";

    public string Hostname { get; set; } = "";

    public string Application { get; set; } = "";

    public List<TraceLine> Events { get; set; } = new();
}

public class TraceLine
{
    public string Time { get; set; } = "";

    public string Event { get; set; } = "";

    // Procedure dentro del cual corrió (null si fue directo).
    public string? Via { get; set; }

    public string Sql { get; set; } = "";

    // Los valores en 0 no se escriben.
    public long Duration { get; set; }

    public long Reads { get; set; }

    public long Writes { get; set; }

    public long Rows { get; set; }
}
