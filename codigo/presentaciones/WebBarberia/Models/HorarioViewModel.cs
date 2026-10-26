using Logica.Modelos;

namespace WebBarberia.Models;

// Paso 3: todos los bloques del horario del peluquero; los que no están en Disponibles se muestran tachados.
public class HorarioViewModel
{
    public Servicio Servicio { get; set; } = null!;
    public Peluquero Peluquero { get; set; } = null!;
    public bool HayVariosPeluqueros { get; set; }
    public DateOnly Fecha { get; set; }
    public DateOnly Hoy { get; set; }
    public List<TimeOnly> Bloques { get; set; } = new();
    public List<TimeOnly> Disponibles { get; set; } = new();
    public string? Mensaje { get; set; }
}
