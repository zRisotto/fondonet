namespace FondoNet.Application.Layouts;

/// <summary>Archivo generado listo para descargar.</summary>
public sealed record ArchivoGenerado(string NombreArchivo, string ContentType, byte[] Contenido);

/// <summary>Renglón del layout de incidencias para nómina.</summary>
public sealed record IncidenciaNominaDto(
    string ClaveEmpleado,
    decimal DescuentoAhorroOrdinario,
    decimal DescuentoAhorroVoluntario,
    decimal CuotaAmortizacionPrestamo,
    decimal TotalDeduccion);

/// <summary>Renglón del layout de dispersión SPEI.</summary>
public sealed record DispersionSpeiDto(
    string CuentaRetiro,
    string ClabeDestino,
    decimal MontoTotal,
    string NombreBeneficiario,
    string Concepto);
