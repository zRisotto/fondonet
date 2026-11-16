using FondoNet.Domain.Finance;
using Xunit;

namespace FondoNet.Domain.Tests;

public class RendimientosCalculatorTests
{
    private static readonly DateOnly Ini2025 = new(2025, 1, 1);
    private static readonly DateOnly Fin2025 = new(2025, 12, 31);

    /// <summary>Quincenas del seed (15 y fin de mes) desde 2025-01-15 hasta 2025-12-31.</summary>
    private static IEnumerable<DateOnly> Quincenas2025()
    {
        for (int mes = 1; mes <= 12; mes++)
        {
            yield return new DateOnly(2025, mes, 15);
            yield return new DateOnly(2025, mes, DateTime.DaysInMonth(2025, mes));
        }
    }

    /// <summary>Movimientos 2025 del seed: aportación quincenal = ordinario + patronal + voluntario.</summary>
    private static List<MovimientoSaldo> Aportaciones(decimal porQuincena, DateOnly ingreso) =>
        Quincenas2025().Where(q => q >= ingreso).Select(q => new MovimientoSaldo(q, porQuincena)).ToList();

    private static FactorAhorrador Factor(int id, decimal porQuincena, DateOnly? ingreso = null) =>
        new(id, RendimientosCalculator.CalcularFactor(Aportaciones(porQuincena, ingreso ?? Ini2025), Ini2025, Fin2025));

    // Caja 1 (8%): Ana 720+720+500, Carlos 880+880, Lucía 600+600+300 desde 2025-04-01.
    private static readonly FactorAhorrador[] FactoresCaja1 =
    {
        Factor(1, 1940m), Factor(2, 1760m), Factor(3, 1500m, new DateOnly(2025, 4, 1)),
    };

    // Caja 2 (10%): Jorge 1250+1250+1000, Sofía 800+800, Ricardo 700+700+200.
    private static readonly FactorAhorrador[] FactoresCaja2 =
    {
        Factor(4, 3500m), Factor(5, 1600m), Factor(6, 1600m),
    };

    [Fact]
    public void Reproduce_rendimientos_2025_de_la_caja_1()
    {
        var r = RendimientosCalculator.Repartir(494.18m, FactoresCaja1);
        Assert.Equal(new[] { new RendimientoAhorrador(1, 211.40m), new RendimientoAhorrador(2, 191.79m), new RendimientoAhorrador(3, 90.99m) }, r);
    }

    [Fact]
    public void Reproduce_rendimientos_2025_de_la_caja_2_con_residuo_al_id_mayor()
    {
        var r = RendimientosCalculator.Repartir(197.13m, FactoresCaja2);
        // Sofía y Ricardo tienen el mismo factor: 47.08 por redondeo y 47.07 por residuo.
        Assert.Equal(new[] { new RendimientoAhorrador(4, 102.98m), new RendimientoAhorrador(5, 47.08m), new RendimientoAhorrador(6, 47.07m) }, r);
    }

    [Fact]
    public void Total_2025_cuadra_exacto_con_las_bolsas()
    {
        var total = RendimientosCalculator.Repartir(494.18m, FactoresCaja1).Sum(x => x.Monto)
                  + RendimientosCalculator.Repartir(197.13m, FactoresCaja2).Sum(x => x.Monto);
        Assert.Equal(691.31m, total);
    }

    [Fact]
    public void Residuo_va_al_id_mayor_aunque_venga_desordenado()
    {
        var r = RendimientosCalculator.Repartir(100m, new[] { new FactorAhorrador(9, 1m), new FactorAhorrador(2, 1m), new FactorAhorrador(5, 1m) });
        Assert.Equal(new[] { 2, 5, 9 }, r.Select(x => x.AhorradorId));
        Assert.Equal(new[] { 33.33m, 33.33m, 33.34m }, r.Select(x => x.Monto));
        Assert.Equal(100m, r.Sum(x => x.Monto));
    }

    [Fact]
    public void Bolsa_cero_no_reparte()
    {
        Assert.Empty(RendimientosCalculator.Repartir(0m, FactoresCaja1));
    }

    [Fact]
    public void Suma_de_factores_cero_no_reparte()
    {
        Assert.Empty(RendimientosCalculator.Repartir(500m, new[] { new FactorAhorrador(1, 0m), new FactorAhorrador(2, 0m) }));
        Assert.Empty(RendimientosCalculator.Repartir(500m, Array.Empty<FactorAhorrador>()));
    }

    [Fact]
    public void Factor_cero_queda_fuera_del_reparto()
    {
        var r = RendimientosCalculator.Repartir(10m, new[] { new FactorAhorrador(1, 2m), new FactorAhorrador(7, 0m) });
        Assert.Equal(new[] { new RendimientoAhorrador(1, 10m) }, r);
    }

    [Fact]
    public void Factor_pondera_saldo_por_dias_y_respeta_saldo_inicial()
    {
        // 365 días con saldo 1,000 (aportado antes del periodo) -> factor = 1,000.
        var antes = new[] { new MovimientoSaldo(new DateOnly(2024, 12, 31), 1000m) };
        Assert.Equal(1000m, RendimientosCalculator.CalcularFactor(antes, Ini2025, Fin2025));

        // 730 a mitad de año: 1,000 × 181 + 1,730 × 184 = 499,320 -> / 365.
        var conAporte = antes.Append(new MovimientoSaldo(new DateOnly(2025, 7, 1), 730m));
        Assert.Equal(499320m / 365m, RendimientosCalculator.CalcularFactor(conAporte, Ini2025, Fin2025));

        // Movimientos posteriores al cierre se ignoran.
        var despues = antes.Append(new MovimientoSaldo(new DateOnly(2026, 1, 15), 5000m));
        Assert.Equal(1000m, RendimientosCalculator.CalcularFactor(despues, Ini2025, Fin2025));
    }
}
