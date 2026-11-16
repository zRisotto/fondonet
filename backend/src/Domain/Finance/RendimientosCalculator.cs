namespace FondoNet.Domain.Finance;

public sealed record MovimientoSaldo(DateOnly Fecha, decimal Monto);
public sealed record FactorAhorrador(int AhorradorId, decimal Factor);
public sealed record RendimientoAhorrador(int AhorradorId, decimal Monto);

/// <summary>
/// Reparto anual de la bolsa de intereses de UNA caja.
/// factor = Σ(saldo diario × días) / 365; rendimiento = factor / Σfactores × bolsa,
/// redondeado a 2 decimales; el residuo va al ahorrador de id mayor.
/// </summary>
public static class RendimientosCalculator
{
    /// <summary>
    /// Factor del periodo [inicio, fin]. Los movimientos anteriores a <paramref name="inicio"/>
    /// forman el saldo inicial; los posteriores a <paramref name="fin"/> se ignoran.
    /// </summary>
    public static decimal CalcularFactor(IEnumerable<MovimientoSaldo> movimientos, DateOnly inicio, DateOnly fin)
    {
        DateOnly finExclusivo = fin.AddDays(1);
        var porDia = movimientos
            .Where(m => m.Fecha <= fin)
            .GroupBy(m => m.Fecha)
            .Select(g => (Fecha: g.Key, Monto: g.Sum(m => m.Monto)))
            .OrderBy(x => x.Fecha)
            .ToList();

        decimal saldo = 0m, acumulado = 0m;
        for (int i = 0; i < porDia.Count; i++)
        {
            saldo += porDia[i].Monto;
            DateOnly desde = porDia[i].Fecha > inicio ? porDia[i].Fecha : inicio;
            DateOnly hasta = i + 1 < porDia.Count ? porDia[i + 1].Fecha : finExclusivo;
            int dias = hasta.DayNumber - desde.DayNumber;
            if (dias > 0) acumulado += saldo * dias;
        }
        return acumulado / 365m;
    }

    /// <summary>
    /// Reparte la bolsa de una caja. Solo participan factores &gt; 0.
    /// Bolsa ≤ 0 o Σfactores = 0 → lista vacía (no hay nada que repartir).
    /// </summary>
    public static IReadOnlyList<RendimientoAhorrador> Repartir(decimal bolsa, IEnumerable<FactorAhorrador> factores)
    {
        var participantes = factores.Where(f => f.Factor > 0).OrderBy(f => f.AhorradorId).ToList();
        decimal sumaFactores = participantes.Sum(f => f.Factor);
        if (bolsa <= 0 || sumaFactores <= 0 || participantes.Count == 0)
            return Array.Empty<RendimientoAhorrador>();

        var resultado = new List<RendimientoAhorrador>(participantes.Count);
        decimal asignado = 0m;
        for (int i = 0; i < participantes.Count - 1; i++)
        {
            decimal monto = Dinero.Redondear(participantes[i].Factor / sumaFactores * bolsa);
            resultado.Add(new RendimientoAhorrador(participantes[i].AhorradorId, monto));
            asignado += monto;
        }
        resultado.Add(new RendimientoAhorrador(participantes[^1].AhorradorId, bolsa - asignado));
        return resultado;
    }
}
