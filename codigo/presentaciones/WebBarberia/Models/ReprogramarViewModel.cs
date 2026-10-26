using Logica.Modelos;

namespace WebBarberia.Models;

// Reprogramar una reserva confirmada (RF-15, A4): nueva fecha y hora libres para el mismo servicio.
public class ReprogramarViewModel
{
    public Reserva Reserva { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public DateOnly Hoy { get; set; }
    public List<TimeOnly> Disponibles { get; set; } = new();
    public string? Error { get; set; }
}
