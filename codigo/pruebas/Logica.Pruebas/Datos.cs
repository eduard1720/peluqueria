using Logica.Enumeraciones;
using Logica.Modelos;

namespace Logica.Pruebas;

// Objetos de ejemplo para las pruebas.
internal static class Datos
{
    public static readonly Servicio Corte = new(1, "Corte de cabello", "", 30, 35m, "");
    public static readonly Servicio CorteBarba = new(3, "Corte + barba", "", 60, 55m, "");
    public static readonly Servicio Tinte = new(5, "Tinte", "", 90, 120m, "");
    public static readonly Cliente Ana = new(1, "Ana", "Pérez", "71234567", "ana@correo.com", new DateTime(2026, 10, 1));
    public static readonly Peluquero Luis = new(1, "Luis", "Rojas", "Cortes clásicos");
    public static readonly Peluquero Marco = new(2, "Marco", "Vaca", "Degradados");

    // Sin peluquero indicado, la reserva es de Luis.
    public static Reserva Reserva(int id, DateOnly fecha, string inicio, Servicio servicio,
                                  EstadoReserva estado = EstadoReserva.Confirmada, Peluquero? peluquero = null)
    {
        TimeOnly h = TimeOnly.Parse(inicio);
        return new Reserva(id, Ana, servicio, peluquero ?? Luis, fecha, h, h.AddMinutes(servicio.DuracionMin), servicio.Precio, estado,
                           null, OrigenReserva.Web, false, new DateTime(2026, 10, 1), null);
    }

    public static List<string> Horas(List<TimeOnly> horas) => horas.Select(h => h.ToString("HH:mm")).ToList();
}
