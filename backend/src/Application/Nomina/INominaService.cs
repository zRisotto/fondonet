namespace FondoNet.Application.Nomina;

/// <summary>Lee .csv o .xlsx; los renglones inválidos se reportan sin detener el resto.</summary>
public interface INominaParser
{
    ResultadoParseoNomina Parsear(Stream archivo, string nombreArchivo);
}

public interface INominaService
{
    /// <summary>Aplica altas/bajas/incidencias a los ahorradores de la caja (ClaveEmpleado es única por caja).</summary>
    Task<ResultadoImportacionNomina> ImportarAsync(int cajaAhorroId, Stream archivo, string nombreArchivo, CancellationToken ct = default);
}
