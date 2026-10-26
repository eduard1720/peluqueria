using Logica.Modelos;

namespace WebBarberia.Models;

// Reserva manual desde el panel (RF-12): el personal elige servicio, peluquero, fecha y hora y escribe los datos del cliente.
public class ReservaManualViewModel
{
    public int ServicioId { get; set; }
    public int PeluqueroId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly? Hora { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string? Correo { get; set; }          // opcional en la reserva manual
    public string? Notas { get; set; }

    public int? ClienteId { get; set; }         // cliente elegido desde la pantalla Clientes (rellena sus datos)
    public List<Servicio> Servicios { get; set; } = new();
    public List<Peluquero> Peluqueros { get; set; } = new();
    public List<TimeOnly> Disponibles { get; set; } = new();
    public DateOnly Hoy { get; set; }
    public List<string> Errores { get; set; } = new();

    public DatosReserva ADatosReserva() => new()
    {
        ServicioId = ServicioId, PeluqueroId = PeluqueroId, Fecha = Fecha, Hora = Hora ?? default, Nombre = Nombre ?? "", Apellido = Apellido ?? "",
        Telefono = Telefono ?? "", Correo = Correo, Notas = Notas,
    };
}
