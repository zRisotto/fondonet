namespace FondoNet.Domain.Entities;

public class Empresa
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;
    public DateOnly FechaAlta { get; set; }

    public ICollection<CajaAhorro> CajasAhorro { get; set; } = new List<CajaAhorro>();
}
