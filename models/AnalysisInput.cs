namespace XeExtractor.Models;

public class AnalysisInput
{
    public string Case { get; set; } = "";

    public string Database { get; set; } = "";

    public string Username { get; set; } = "";

    // Cabecera: una fila por tabla / procedure con lo que se hizo sobre él.
    public List<AnalysisObject> Summary { get; set; } = new();

    // Detalle: cada operación en orden cronológico.
    public List<AnalysisStep> Operations { get; set; } = new();
}

// Fila del detalle: una sentencia (o todas las que solo cambian en sus valores).
public class AnalysisStep
{
    // Hora de la primera vez
    public DateTimeOffset Timestamp { get; set; }

    public int SessionId { get; set; }

    // Procedure dentro del cual se ejecutó (null si fue directa)
    public string? Via { get; set; }

    public string Operation { get; set; } = "";

    public List<string> Objects { get; set; } = new();

    // Cuántas veces se repitió con otros valores
    public int Count { get; set; }

    public string Sql { get; set; } = "";
}

public class AnalysisObject
{
    public string ObjectType { get; set; } = "";

    public string ObjectName { get; set; } = "";

    // INSERT, UPDATE, DELETE, SELECT, EXEC
    public List<string> Operations { get; set; } = new();

    public int Executions { get; set; }

    // Columnas detectadas por regex (mejor esfuerzo, no es un parser SQL completo).
    public List<string> Columns { get; set; } = new();

    // Procedures que tocaron este objeto (cuando la sentencia corrió dentro de un SP).
    public List<string> UsedBy { get; set; } = new();
}

public class AnalysisOperation
{
    public DateTimeOffset Timestamp { get; set; }

    public int SessionId { get; set; }

    public string Operation { get; set; } = "";

    public string ObjectType { get; set; } = "";

    public string ObjectName { get; set; } = "";

    // Procedure dentro del cual se ejecutó la sentencia (null si fue directa).
    public string? Via { get; set; }

    public string Sql { get; set; } = "";
}
