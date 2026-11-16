namespace FondoNet.Application.Cierre;

public sealed record CierreAhorradorDto(
    int AhorradorId,
    string Nombre,
    decimal Factor,
    decimal Rendimiento,
    /// <summary>Saldo total liquidado al 31/dic (ahorro + patronal + rendimiento), en positivo.</summary>
    decimal Retiro);

public sealed record CierreCajaDto(
    int CajaAhorroId,
    string NombreCaja,
    decimal Bolsa,
    decimal SumaFactores,
    decimal TotalRendimientos,
    decimal TotalRetiros,
    IReadOnlyList<CierreAhorradorDto> Ahorradores);

public sealed record ResultadoCierreAnualDto(
    int Anio,
    bool Simulacion,
    /// <summary>True si el año ya estaba cerrado: no se escribió nada.</summary>
    bool YaCerrado,
    decimal TotalBolsa,
    decimal TotalRendimientos,
    IReadOnlyList<CierreCajaDto> Cajas);
