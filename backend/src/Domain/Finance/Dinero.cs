namespace FondoNet.Domain.Finance;

/// <summary>Redondeo monetario único del sistema (equivalente a ROUND de SQL).</summary>
public static class Dinero
{
    public static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
