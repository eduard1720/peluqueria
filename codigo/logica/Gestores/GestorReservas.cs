using System.Text;
using Logica.Datos;
using Logica.Enumeraciones;
using Logica.Excepciones;
using Logica.Integraciones;
using Logica.Modelos;
using Logica.Utilidades;
using Npgsql;

namespace Logica.Gestores;

public class GestorReservas
{
    private const string ErrorChoqueBD = "23P01";   // restricción de exclusión de PostgreSQL

    private readonly ConexionBD conexion;
    private readonly EnviadorCorreo enviadorCorreo;
    private readonly RegistroErrores registro;

    public GestorReservas(ConexionBD conexion, EnviadorCorreo enviadorCorreo, RegistroErrores registro)
    {
        this.conexion = conexion;
        this.enviadorCorreo = enviadorCorreo;
        this.registro = registro;
    }


    public List<TimeOnly> CalcularHorariosDisponibles(DateOnly fecha, Servicio servicio, int idPeluquero, int idExcluir, OrigenReserva origen)
    {
        List<Reserva> reservasDelDia = ObtenerReservasDelDia(fecha);
        return CalcularHorariosDisponibles(fecha, servicio, idPeluquero, reservasDelDia, idExcluir, origen, Reloj.AhoraBolivia());
    }

    public static List<TimeOnly> CalcularHorariosDisponibles(DateOnly fecha, Servicio servicio, int idPeluquero, List<Reserva> reservasDelDia,
                                                            int idExcluir, OrigenReserva origen, DateTime ahora)
    {
        var disponibles = new List<TimeOnly>();
        DateOnly hoy = DateOnly.FromDateTime(ahora);

        bool fechaValida = fecha >= hoy && fecha <= hoy.AddDays(HorarioAtencion.DiasMaximos) && HorarioAtencion.EsDiaLaborable(fecha);
        if (!fechaValida) return disponibles;

        int d = servicio.DuracionMin;
        DateTime limite = origen == OrigenReserva.Web
            ? ahora.AddMinutes(HorarioAtencion.MargenWebMinutos)
            : InicioDelBloqueEnCurso(ahora);   

        var R = new List<Reserva>();
        foreach (Reserva r in reservasDelDia)
        {
            if (r.Fecha == fecha && r.Peluquero.Id == idPeluquero && r.OcupaHorario && r.Id != idExcluir) R.Add(r);
        }

        foreach (Turno t in HorarioAtencion.Turnos)
        {
            TimeOnly h = t.Inicio;
            while (h.AddMinutes(d) <= t.Fin)
            {
                if (fecha.ToDateTime(h) >= limite)          
                {
                    bool choca = HayChoque(h, d, R);
                    if (!choca) disponibles.Add(h);
                }
                h = h.AddMinutes(HorarioAtencion.MinutosBloque);
            }
        }
        return disponibles;
    }


    public static bool HayChoque(TimeOnly h, int d, List<Reserva> R)
    {
        TimeOnly fin = h.AddMinutes(d);
        bool choca = false;
        int i = 0;
        while (i < R.Count && !choca)
        {
            if (h < R[i].HoraFin && fin > R[i].HoraInicio) choca = true;
            i++;
        }
        return choca;
    }

    private static DateTime InicioDelBloqueEnCurso(DateTime ahora) =>
        new DateTime(ahora.Year, ahora.Month, ahora.Day, ahora.Hour, ahora.Minute - ahora.Minute % HorarioAtencion.MinutosBloque, 0);


    public Reserva RegistrarReserva(DatosReserva datos, OrigenReserva origen, Usuario? usuarioSesion)
    {
        List<string> errores = Validador.ValidarDatosReserva(datos, origen);
        if (errores.Count > 0) throw new ArgumentException(string.Join(" ", errores));

        Servicio servicio = ObtenerServicio(datos.ServicioId);
        Peluquero peluquero = ObtenerPeluquero(datos.PeluqueroId);
        List<TimeOnly> disponibles = CalcularHorariosDisponibles(datos.Fecha, servicio, peluquero.Id, 0, origen);
        if (!disponibles.Contains(datos.Hora)) throw new HorarioNoDisponibleException();

        Reserva res;
        try
        {
            res = GuardarClienteYReserva(datos, servicio, peluquero, origen, usuarioSesion);   
        }
        catch (PostgresException ex) when (ex.SqlState == ErrorChoqueBD)
        {
            throw new HorarioNoDisponibleException("Ese horario ya no está disponible, elige otro.", ex);
        }

        if (!string.IsNullOrWhiteSpace(datos.Correo))
        {
            bool enviado = enviadorCorreo.EnviarConfirmacion(res, datos.Correo);   
            res.RegistrarEnvioCorreo(enviado);
            try
            {
                ActualizarCorreoEnviado(res);
            }
            catch (Exception ex)
            {
                registro.Registrar("GestorReservas.ActualizarCorreoEnviado", ex);
            }
        }
        return res;
    }


    public void CambiarEstado(int idReserva, EstadoReserva nuevoEstado)
    {
        Reserva res = ObtenerReserva(idReserva);
        DateTime ahora = Reloj.AhoraBolivia();
        switch (nuevoEstado)
        {
            case EstadoReserva.Cancelada: res.Cancelar(ahora); break;
            case EstadoReserva.Atendida: res.MarcarAtendida(ahora); break;
            case EstadoReserva.NoAsistio: res.MarcarNoAsistio(ahora); break;
            default: throw new ArgumentException("Acción no válida.");
        }
        GuardarCambios(res);
    }

    public void Reprogramar(int idReserva, DateOnly nuevaFecha, TimeOnly nuevaHora)
    {
        Reserva res = ObtenerReserva(idReserva);
        List<TimeOnly> disponibles = CalcularHorariosDisponibles(nuevaFecha, res.Servicio, res.Peluquero.Id, res.Id, OrigenReserva.Manual);
        if (!disponibles.Contains(nuevaHora)) throw new HorarioNoDisponibleException();
        res.Reprogramar(nuevaFecha, nuevaHora, Reloj.AhoraBolivia());
        try
        {
            GuardarCambios(res);
        }
        catch (PostgresException ex) when (ex.SqlState == ErrorChoqueBD)
        {
            throw new HorarioNoDisponibleException("Ese horario ya no está disponible.", ex);
        }
    }


    public List<Reserva> ObtenerAgenda(DateOnly desde, DateOnly hasta, EstadoReserva? estado, int? idPeluquero = null)
    {
        string filtro = "WHERE r.fecha BETWEEN @desde AND @hasta" + (estado.HasValue ? " AND r.estado = @estado" : "") +
                        (idPeluquero.HasValue ? " AND r.peluquero_id = @peluquero" : "");
        return ConsultarReservas(filtro, cmd =>
        {
            cmd.Parameters.AddWithValue("desde", desde);
            cmd.Parameters.AddWithValue("hasta", hasta);
            if (estado.HasValue) cmd.Parameters.AddWithValue("estado", estado.Value.ToString());
            if (idPeluquero.HasValue) cmd.Parameters.AddWithValue("peluquero", idPeluquero.Value);
        });
    }

    public List<Reserva> ObtenerPorCliente(int idCliente) =>
        ConsultarReservas("WHERE r.cliente_id = @id", cmd => cmd.Parameters.AddWithValue("id", idCliente));

    public static string AgendaATexto(DateOnly fecha, List<Reserva> reservas)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"AS BAJO LA MANGA - AGENDA DEL {fecha:dd/MM/yyyy}");
        foreach (Reserva r in reservas)
        {
            string correo = r.CorreoEnviado == false ? " [correo no enviado]" : "";
            sb.AppendLine($"{r.HoraInicio:HH\\:mm}-{r.HoraFin:HH\\:mm} | {r.Cliente.NombreCompleto} | {r.Cliente.Telefono} | " +
                          $"{r.Servicio.Nombre} | {r.Peluquero.NombreCompleto} | Bs {r.Precio:0.00} | {r.Estado}{correo}");
        }
        return sb.ToString();
    }


    private const string ConsultaReservas =
        "SELECT r.id, r.fecha, r.hora_inicio, r.hora_fin, r.precio, r.estado, r.notas, r.origen, r.correo_enviado, r.creado_en, " +
        "c.id, c.nombre, c.apellido, c.telefono, c.correo, c.fecha_registro, " + GestorServicios.Columnas + ", " + GestorPeluqueros.Columnas + " " +
        "FROM reservas r JOIN clientes c ON c.id = r.cliente_id JOIN servicios s ON s.id = r.servicio_id " +
        "JOIN peluqueros p ON p.id = r.peluquero_id ";

    private List<Reserva> ObtenerReservasDelDia(DateOnly fecha) =>
        ConsultarReservas("WHERE r.fecha = @fecha", cmd => cmd.Parameters.AddWithValue("fecha", fecha));

    private Servicio ObtenerServicio(int id)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand($"SELECT {GestorServicios.Columnas} FROM servicios s WHERE s.id = @id AND s.activo", con);
        cmd.Parameters.AddWithValue("id", id);
        using NpgsqlDataReader r = cmd.ExecuteReader();
        if (!r.Read()) throw new ArgumentException("El servicio no existe o no está disponible.");
        return GestorServicios.LeerServicio(r, 0);
    }

    private Peluquero ObtenerPeluquero(int id)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand($"SELECT {GestorPeluqueros.Columnas} FROM peluqueros p WHERE p.id = @id AND p.activo", con);
        cmd.Parameters.AddWithValue("id", id);
        using NpgsqlDataReader r = cmd.ExecuteReader();
        if (!r.Read()) throw new ArgumentException("El peluquero no existe o no está disponible.");
        return GestorPeluqueros.LeerPeluquero(r, 0);
    }

    public Reserva ObtenerReserva(int id)
    {
        List<Reserva> lista = ConsultarReservas("WHERE r.id = @id", cmd => cmd.Parameters.AddWithValue("id", id));
        if (lista.Count == 0) throw new ArgumentException("La reserva no existe.");
        return lista[0];
    }

    private Reserva GuardarClienteYReserva(DatosReserva datos, Servicio servicio, Peluquero peluquero, OrigenReserva origen, Usuario? usuarioSesion)
    {
        DateTime ahora = Reloj.AhoraBolivia();
        string telefono = Validador.NormalizarTelefono(datos.Telefono);
        using NpgsqlConnection con = conexion.Abrir();
        using NpgsqlTransaction tx = con.BeginTransaction();

        Cliente? cliente = null;
        using (var cmd = new NpgsqlCommand(
            "SELECT id, nombre, apellido, telefono, correo, fecha_registro FROM clientes WHERE telefono = @t FOR UPDATE", con, tx))
        {
            cmd.Parameters.AddWithValue("t", telefono);
            using NpgsqlDataReader r = cmd.ExecuteReader();
            if (r.Read())
                cliente = new Cliente(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3),
                                      r.IsDBNull(4) ? null : r.GetString(4), r.GetDateTime(5));
        }

        if (cliente == null)
        {
            cliente = new Cliente(0, datos.Nombre.Trim(), datos.Apellido.Trim(), telefono, datos.Correo, ahora);
            using var cmd = new NpgsqlCommand(
                "INSERT INTO clientes (nombre, apellido, telefono, correo, fecha_registro) VALUES (@n, @a, @t, @c, @f) RETURNING id", con, tx);
            cmd.Parameters.AddWithValue("n", cliente.Nombre);
            cmd.Parameters.AddWithValue("a", cliente.Apellido);
            cmd.Parameters.AddWithValue("t", cliente.Telefono);
            cmd.Parameters.AddWithValue("c", (object?)cliente.Correo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("f", ConexionBD.SinZona(ahora));
            cliente.AsignarId((int)cmd.ExecuteScalar()!);
        }
        else
        {
            if (origen == OrigenReserva.Web)
            {
                cliente.CompletarCorreo(datos.Correo);       
            }
            else
            {
                cliente.CambiarNombre(datos.Nombre, datos.Apellido);   
                if (!string.IsNullOrWhiteSpace(datos.Correo)) cliente.CambiarCorreo(datos.Correo);
            }
            using var cmd = new NpgsqlCommand("UPDATE clientes SET nombre = @n, apellido = @a, correo = @c WHERE id = @id", con, tx);
            cmd.Parameters.AddWithValue("n", cliente.Nombre);
            cmd.Parameters.AddWithValue("a", cliente.Apellido);
            cmd.Parameters.AddWithValue("c", (object?)cliente.Correo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("id", cliente.Id);
            cmd.ExecuteNonQuery();
        }

        string? notas = string.IsNullOrWhiteSpace(datos.Notas) ? null : datos.Notas.Trim();
        bool hayCorreo = !string.IsNullOrWhiteSpace(datos.Correo);
        var res = new Reserva(cliente, servicio, peluquero, datos.Fecha, datos.Hora, origen, notas, hayCorreo, ahora, usuarioSesion);
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO reservas (cliente_id, servicio_id, peluquero_id, fecha, hora_inicio, hora_fin, precio, estado, notas, origen, " +
            "correo_enviado, creado_en, creado_por) VALUES (@cl, @se, @pe, @fe, @hi, @hf, @pr, @es, @no, @or, @ce, @cr, @cp) RETURNING id", con, tx))
        {
            cmd.Parameters.AddWithValue("cl", cliente.Id);
            cmd.Parameters.AddWithValue("se", servicio.Id);
            cmd.Parameters.AddWithValue("pe", peluquero.Id);
            cmd.Parameters.AddWithValue("fe", res.Fecha);
            cmd.Parameters.AddWithValue("hi", res.HoraInicio);
            cmd.Parameters.AddWithValue("hf", res.HoraFin);
            cmd.Parameters.AddWithValue("pr", res.Precio);
            cmd.Parameters.AddWithValue("es", res.Estado.ToString());
            cmd.Parameters.AddWithValue("no", (object?)res.Notas ?? DBNull.Value);
            cmd.Parameters.AddWithValue("or", res.Origen.ToString());
            cmd.Parameters.AddWithValue("ce", (object?)res.CorreoEnviado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cr", ConexionBD.SinZona(res.CreadoEn));
            cmd.Parameters.AddWithValue("cp", (object?)usuarioSesion?.Id ?? DBNull.Value);
            res.AsignarId((int)cmd.ExecuteScalar()!);
        }
        tx.Commit();
        return res;
    }

    private void GuardarCambios(Reserva res)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand(
            "UPDATE reservas SET fecha = @fe, hora_inicio = @hi, hora_fin = @hf, estado = @es WHERE id = @id", con);
        cmd.Parameters.AddWithValue("fe", res.Fecha);
        cmd.Parameters.AddWithValue("hi", res.HoraInicio);
        cmd.Parameters.AddWithValue("hf", res.HoraFin);
        cmd.Parameters.AddWithValue("es", res.Estado.ToString());
        cmd.Parameters.AddWithValue("id", res.Id);
        cmd.ExecuteNonQuery();
    }

    private void ActualizarCorreoEnviado(Reserva res)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand("UPDATE reservas SET correo_enviado = @ce WHERE id = @id", con);
        cmd.Parameters.AddWithValue("ce", (object?)res.CorreoEnviado ?? DBNull.Value);
        cmd.Parameters.AddWithValue("id", res.Id);
        cmd.ExecuteNonQuery();
    }

    private List<Reserva> ConsultarReservas(string filtro, Action<NpgsqlCommand> parametros)
    {
        var lista = new List<Reserva>();
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand(ConsultaReservas + filtro + " ORDER BY r.fecha, r.hora_inicio", con);
        parametros(cmd);
        using NpgsqlDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            var cliente = new Cliente(r.GetInt32(10), r.GetString(11), r.GetString(12), r.GetString(13),
                                      r.IsDBNull(14) ? null : r.GetString(14), r.GetDateTime(15));
            Servicio servicio = GestorServicios.LeerServicio(r, 16);
            Peluquero peluquero = GestorPeluqueros.LeerPeluquero(r, 23);
            lista.Add(new Reserva(r.GetInt32(0), cliente, servicio, peluquero, r.GetFieldValue<DateOnly>(1), r.GetFieldValue<TimeOnly>(2),
                                  r.GetFieldValue<TimeOnly>(3), r.GetDecimal(4), Enum.Parse<EstadoReserva>(r.GetString(5)),
                                  r.IsDBNull(6) ? null : r.GetString(6), Enum.Parse<OrigenReserva>(r.GetString(7)),
                                  r.IsDBNull(8) ? null : r.GetBoolean(8), r.GetDateTime(9), null));
        }
        return lista;
    }
}
