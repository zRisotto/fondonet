namespace FondoNet.Domain.Finance;

/// <summary>Renglón calculado de la tabla de amortización (sin persistencia).</summary>
public sealed record RenglonAmortizacion(
    int NumeroPago,
    DateOnly FechaVencimiento,
    decimal SaldoInicial,
    decimal Capital,
    decimal Interes,
    decimal Cuota,
    decimal SaldoFinal);

/// <summary>
/// Préstamo a cuota fija quincenal sobre saldo insoluto.
/// tasa quincenal = tasa mensual / 2; interés = ROUND(saldo × tasa quincenal, 2);
/// capital = cuota − interés; el último pago liquida el saldo exacto.
/// </summary>
public static class AmortizacionCalculator
{
    public const decimal TasaMinima = 0.01m;
    public const decimal TasaMaxima = 0.02m;

    public static decimal CalcularCuota(decimal monto, decimal tasaMensual, int plazoQuincenas)
    {
        Validar(monto, tasaMensual, plazoQuincenas);
        decimal r = tasaMensual / 2m;

        // (1 + r)^n en decimal (sin pasar por double).
        decimal factor = 1m;
        for (int i = 0; i < plazoQuincenas; i++)
            factor *= 1m + r;

        // r / (1 − (1 + r)^−n) = r × f / (f − 1)
        return Dinero.Redondear(monto * r * factor / (factor - 1m));
    }

    public static IReadOnlyList<RenglonAmortizacion> Generar(
        decimal monto, decimal tasaMensual, int plazoQuincenas, DateOnly fechaOtorgamiento)
    {
        decimal cuota = CalcularCuota(monto, tasaMensual, plazoQuincenas);
        decimal r = tasaMensual / 2m;

        var tabla = new List<RenglonAmortizacion>(plazoQuincenas);
        decimal saldo = monto;
        DateOnly fecha = PrimerVencimiento(fechaOtorgamiento);

        for (int n = 1; n <= plazoQuincenas; n++)
        {
            decimal interes = Dinero.Redondear(saldo * r);
            decimal capital = n == plazoQuincenas ? saldo : cuota - interes;
            tabla.Add(new RenglonAmortizacion(n, fecha, saldo, capital, interes, capital + interes, saldo - capital));
            saldo -= capital;
            fecha = SiguienteQuincena(fecha);
        }
        return tabla;
    }

    /// <summary>Próxima quincena (día 15 o último del mes) estrictamente posterior al otorgamiento.</summary>
    public static DateOnly PrimerVencimiento(DateOnly otorgamiento)
    {
        if (otorgamiento.Day < 15)
            return new DateOnly(otorgamiento.Year, otorgamiento.Month, 15);
        DateOnly ultimo = UltimoDia(otorgamiento);
        return otorgamiento < ultimo ? ultimo : ultimo.AddDays(15);
    }

    /// <summary>15 → último día del mes; último día → 15 del mes siguiente.</summary>
    public static DateOnly SiguienteQuincena(DateOnly fecha) =>
        fecha.Day == 15 ? UltimoDia(fecha) : UltimoDia(fecha).AddDays(15);

    private static DateOnly UltimoDia(DateOnly f) =>
        new(f.Year, f.Month, DateTime.DaysInMonth(f.Year, f.Month));

    private static void Validar(decimal monto, decimal tasaMensual, int plazoQuincenas)
    {
        if (monto <= 0) throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser mayor a 0.");
        if (tasaMensual < TasaMinima || tasaMensual > TasaMaxima)
            throw new ArgumentOutOfRangeException(nameof(tasaMensual), "La tasa mensual debe estar entre 1% y 2%.");
        if (plazoQuincenas <= 0) throw new ArgumentOutOfRangeException(nameof(plazoQuincenas), "El plazo debe ser mayor a 0.");
    }
}
