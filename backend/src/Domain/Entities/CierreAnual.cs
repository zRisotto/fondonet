namespace FondoNet.Domain.Entities;

public class CierreAnual
{
    public int Id { get; set; }
    public int CajaAhorroId { get; set; }
    public int Anio { get; set; }
    public DateTime FechaEjecucion { get; set; }
    public decimal Bolsa { get; set; }
    public decimal TotalRendimientos { get; set; }
    public decimal TotalRetiros { get; set; }

    public CajaAhorro? CajaAhorro { get; set; }
}
