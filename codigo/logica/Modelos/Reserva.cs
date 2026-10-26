using Logica.Enumeraciones;

namespace Logica.Modelos;

// Cita de un cliente para un servicio, con un peluquero, en una fecha y hora.
public class Reserva
{
    private int id;
    private Cliente cliente;
    private Servicio servicio;
    private Peluquero peluquero;
    private DateOnly fecha;
    private TimeOnly horaInicio;
    private TimeOnly horaFin;
    private decimal precio;
    private EstadoReserva estado;
    private string? notas;
    private OrigenReserva origen;
    private bool? correoEnviado;   // null = la reserva no tiene correo (RN-20)
    private DateTime creadoEn;
    private Usuario? creadoPor;    // solo en reservas manuales

    // Reserva nueva: queda Confirmada, con el precio copiado del servicio (RN-13, RN-15).
    public Reserva(Cliente cliente, Servicio servicio, Peluquero peluquero, DateOnly fecha, TimeOnly horaInicio, OrigenReserva origen,
                   string? notas, bool hayCorreo, DateTime creadoEn, Usuario? creadoPor)
        : this(0, cliente, servicio, peluquero, fecha, horaInicio, horaInicio.AddMinutes(servicio.DuracionMin), servicio.Precio,
               EstadoReserva.Confirmada, notas, origen, hayCorreo ? false : null, creadoEn, creadoPor)
    {
    }

    // Reserva leída de la base de datos.
    public Reserva(int id, Cliente cliente, Servicio servicio, Peluquero peluquero, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin,
                   decimal precio, EstadoReserva estado, string? notas, OrigenReserva origen, bool? correoEnviado,
                   DateTime creadoEn, Usuario? creadoPor)
    {
        if (horaFin <= horaInicio) throw new ArgumentException("La hora de fin debe ser posterior a la de inicio.");
        this.id = id;
        this.cliente = cliente;
        this.servicio = servicio;
        this.peluquero = peluquero;
        this.fecha = fecha;
        this.horaInicio = horaInicio;
        this.horaFin = horaFin;
        this.precio = precio;
        this.estado = estado;
        this.notas = notas;
        this.origen = origen;
        this.correoEnviado = correoEnviado;
        this.creadoEn = creadoEn;
        this.creadoPor = creadoPor;
    }

    public int Id => id;
    public Cliente Cliente => cliente;
    public Servicio Servicio => servicio;
    public Peluquero Peluquero => peluquero;
    public DateOnly Fecha => fecha;
    public TimeOnly HoraInicio => horaInicio;
    public TimeOnly HoraFin => horaFin;
    public decimal Precio => precio;
    public EstadoReserva Estado => estado;
    public string? Notas => notas;
    public OrigenReserva Origen => origen;
    public bool? CorreoEnviado => correoEnviado;
    public DateTime CreadoEn => creadoEn;
    public Usuario? CreadoPor => creadoPor;

    public DateTime Inicio => fecha.ToDateTime(horaInicio);

    // RN-07: ocupan horario las Confirmadas y Atendidas.
    public bool OcupaHorario => estado == EstadoReserva.Confirmada || estado == EstadoReserva.Atendida;

    // RN-06: [inicio, fin) se superpone con esta reserva.
    public bool SeSuperponeCon(TimeOnly inicio, TimeOnly fin) => inicio < horaFin && fin > horaInicio;

    public void AsignarId(int nuevoId) => id = nuevoId;

    // RN-14: cancelar solo si la cita aún no empezó.
    public void Cancelar(DateTime ahora)
    {
        ExigirConfirmada();
        if (Inicio <= ahora) throw new InvalidOperationException("La cita ya empezó: márcala como Atendida o No asistió.");
        estado = EstadoReserva.Cancelada;
    }

    // RN-14: Atendida o No asistió solo si la cita ya empezó.
    public void MarcarAtendida(DateTime ahora) => CerrarCita(EstadoReserva.Atendida, ahora);
    public void MarcarNoAsistio(DateTime ahora) => CerrarCita(EstadoReserva.NoAsistio, ahora);

    public void Reprogramar(DateOnly nuevaFecha, TimeOnly nuevaHora, DateTime ahora)
    {
        ExigirConfirmada();
        if (Inicio <= ahora) throw new InvalidOperationException("La cita ya empezó.");
        fecha = nuevaFecha;
        horaInicio = nuevaHora;
        horaFin = nuevaHora.AddMinutes(servicio.DuracionMin);
    }

    public void RegistrarEnvioCorreo(bool enviado) => correoEnviado = enviado;

    private void CerrarCita(EstadoReserva nuevoEstado, DateTime ahora)
    {
        ExigirConfirmada();
        if (Inicio > ahora) throw new InvalidOperationException("La cita aún no empezó.");
        estado = nuevoEstado;
    }

    private void ExigirConfirmada()
    {
        if (estado != EstadoReserva.Confirmada) throw new InvalidOperationException("Solo se pueden modificar reservas confirmadas.");
    }
}
