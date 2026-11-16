using FondoNet.Domain.Finance;
using Xunit;

namespace FondoNet.Domain.Tests;

public class AmortizacionCalculatorTests
{
    // Préstamo 1 del seed: Ana, 10,000 al 1.5% mensual, 12 quincenas, otorgado 2025-06-30.
    // Valores tomados de la tabla `amortizaciones` (prestamo_id = 1).
    public static readonly TheoryData<int, string, decimal, decimal, decimal, decimal, decimal> TablaAna = new()
    {
        { 1,  "2025-07-15", 10000.00m, 799.51m, 75.00m, 874.51m, 9200.49m },
        { 2,  "2025-07-31",  9200.49m, 805.51m, 69.00m, 874.51m, 8394.98m },
        { 3,  "2025-08-15",  8394.98m, 811.55m, 62.96m, 874.51m, 7583.43m },
        { 4,  "2025-08-31",  7583.43m, 817.63m, 56.88m, 874.51m, 6765.80m },
        { 5,  "2025-09-15",  6765.80m, 823.77m, 50.74m, 874.51m, 5942.03m },
        { 6,  "2025-09-30",  5942.03m, 829.94m, 44.57m, 874.51m, 5112.09m },
        { 7,  "2025-10-15",  5112.09m, 836.17m, 38.34m, 874.51m, 4275.92m },
        { 8,  "2025-10-31",  4275.92m, 842.44m, 32.07m, 874.51m, 3433.48m },
        { 9,  "2025-11-15",  3433.48m, 848.76m, 25.75m, 874.51m, 2584.72m },
        { 10, "2025-11-30",  2584.72m, 855.12m, 19.39m, 874.51m, 1729.60m },
        { 11, "2025-12-15",  1729.60m, 861.54m, 12.97m, 874.51m,  868.06m },
        { 12, "2025-12-31",   868.06m, 868.06m,  6.51m, 874.57m,    0.00m },
    };

    private static readonly IReadOnlyList<RenglonAmortizacion> Ana =
        AmortizacionCalculator.Generar(10000m, 0.015m, 12, new DateOnly(2025, 6, 30));

    [Theory]
    [MemberData(nameof(TablaAna))]
    public void Reproduce_al_centavo_el_prestamo_de_Ana(
        int pago, string fecha, decimal saldoIni, decimal capital, decimal interes, decimal cuota, decimal saldoFin)
    {
        var r = Ana[pago - 1];
        Assert.Equal(pago, r.NumeroPago);
        Assert.Equal(DateOnly.Parse(fecha), r.FechaVencimiento);
        Assert.Equal(saldoIni, r.SaldoInicial);
        Assert.Equal(capital, r.Capital);
        Assert.Equal(interes, r.Interes);
        Assert.Equal(cuota, r.Cuota);
        Assert.Equal(saldoFin, r.SaldoFinal);
    }

    [Fact]
    public void Intereses_de_Ana_suman_la_bolsa_2025_de_la_caja_1()
    {
        Assert.Equal(12, Ana.Count);
        Assert.Equal(494.18m, Ana.Sum(r => r.Interes));
        Assert.Equal(10000.00m, Ana.Sum(r => r.Capital));
    }

    [Fact]
    public void Prestamo_de_Sofia_genera_la_bolsa_2025_de_la_caja_2()
    {
        // Préstamo 2 del seed: 5,000 al 1.2% mensual, 12 quincenas, otorgado 2025-05-15.
        var tabla = AmortizacionCalculator.Generar(5000m, 0.012m, 12, new DateOnly(2025, 5, 15));
        Assert.Equal(new DateOnly(2025, 5, 31), tabla[0].FechaVencimiento);
        Assert.Equal(197.13m, tabla.Sum(r => r.Interes));
        Assert.Equal(0m, tabla[^1].SaldoFinal);
    }

    [Fact]
    public void Cada_renglon_cumple_cuota_igual_capital_mas_interes()
    {
        var tabla = AmortizacionCalculator.Generar(6000m, 0.015m, 24, new DateOnly(2026, 3, 15));
        Assert.All(tabla, r =>
        {
            Assert.Equal(r.Capital + r.Interes, r.Cuota);
            Assert.Equal(r.SaldoInicial - r.Capital, r.SaldoFinal);
            Assert.True(r.SaldoFinal >= 0);
        });
        Assert.Equal(0m, tabla[^1].SaldoFinal);
    }

    [Theory]
    [InlineData("2026-03-10", "2026-03-15")]
    [InlineData("2026-03-15", "2026-03-31")]
    [InlineData("2026-03-31", "2026-04-15")]
    [InlineData("2026-02-20", "2026-02-28")]
    public void Primer_vencimiento_es_la_siguiente_quincena(string otorgamiento, string esperado)
    {
        Assert.Equal(DateOnly.Parse(esperado), AmortizacionCalculator.PrimerVencimiento(DateOnly.Parse(otorgamiento)));
    }

    [Theory]
    [InlineData(0, 0.015, 12)]
    [InlineData(1000, 0.009, 12)]
    [InlineData(1000, 0.021, 12)]
    [InlineData(1000, 0.015, 0)]
    public void Rechaza_parametros_invalidos(decimal monto, double tasa, int plazo)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AmortizacionCalculator.CalcularCuota(monto, (decimal)tasa, plazo));
    }

    [Fact]
    public void Redondeo_es_away_from_zero()
    {
        Assert.Equal(0.13m, Dinero.Redondear(0.125m));
        Assert.Equal(-0.13m, Dinero.Redondear(-0.125m));
    }
}
