using FondoNet.Domain.Enums;

namespace FondoNet.Domain.Entities;

public class MovimientoAhorro
{
    public int Id { get; set; }
    public int AhorradorId { get; set; }
    public DateOnly Fecha { get; set; }
    public TipoMovimientoAhorro Tipo { get; set; }

    /// <summary>Monto con signo: aportaciones y rendimientos positivos, retiros negativos.</summary>
    public decimal Monto { get; set; }

    public string Concepto { get; set; } = string.Empty;

    public Ahorrador? Ahorrador { get; set; }
}
