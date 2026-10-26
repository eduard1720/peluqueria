namespace Logica.Enumeraciones;

// Estados de una reserva (RN-13). Solo una reserva Confirmada puede cambiar de estado.
public enum EstadoReserva
{
    Confirmada,
    Atendida,
    Cancelada,
    NoAsistio
}
