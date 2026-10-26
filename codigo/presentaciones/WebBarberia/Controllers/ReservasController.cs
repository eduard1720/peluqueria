using System.Security.Claims;
using Logica.Enumeraciones;
using Logica.Excepciones;
using Logica.Gestores;
using Logica.Modelos;
using Logica.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBarberia.Models;

namespace WebBarberia.Controllers;

// Panel: agenda por rango (RF-11), reserva manual (RF-12), cancelar, atendida, no asistió (RF-13, RF-14), reprogramar (RF-15). A2, A4.
[Authorize(Roles = "Administrador,Recepcionista")]
public class ReservasController : Controller
{
    private readonly GestorReservas gestorReservas;
    private readonly GestorServicios gestorServicios;
    private readonly GestorPeluqueros gestorPeluqueros;
    private readonly GestorClientes gestorClientes;
    private readonly GestorUsuarios gestorUsuarios;
    private readonly RegistroErrores registro;

    public ReservasController(GestorReservas gestorReservas, GestorServicios gestorServicios, GestorPeluqueros gestorPeluqueros,
                              GestorClientes gestorClientes,
                              GestorUsuarios gestorUsuarios, RegistroErrores registro)
    {
        this.gestorReservas = gestorReservas;
        this.gestorServicios = gestorServicios;
        this.gestorPeluqueros = gestorPeluqueros;
        this.gestorClientes = gestorClientes;
        this.gestorUsuarios = gestorUsuarios;
        this.registro = registro;
    }

    // RF-11: lista por rango de fechas, estado y peluquero (por defecto, la semana: de hoy a 7 días).
    public IActionResult Index(DateOnly? desde, DateOnly? hasta, EstadoReserva? estado, int? peluqueroId)
    {
        DateOnly hoy = Reloj.HoyBolivia();
        DateOnly fi = desde ?? hoy;
        DateOnly ff = hasta ?? hoy.AddDays(7);
        ViewBag.Desde = fi;
        ViewBag.Hasta = ff;
        ViewBag.Estado = estado;
        ViewBag.PeluqueroId = peluqueroId;
        ViewBag.Peluqueros = new List<Peluquero>();
        try
        {
            ViewBag.Peluqueros = gestorPeluqueros.ListarTodos();
            if (fi > ff) throw new ArgumentException("La fecha inicial no puede ser posterior a la final.");
            return View(gestorReservas.ObtenerAgenda(fi, ff, estado, peluqueroId));
        }
        catch (ArgumentException ex)
        {
            ViewBag.Error = ex.Message;
        }
        catch (Exception ex)
        {
            registro.Registrar("ReservasController.Index", ex);
            ViewBag.Error = "No se pudieron cargar las reservas. Intenta nuevamente.";
        }
        return View(new List<Reserva>());
    }

    // ---------- RF-12: reserva manual ----------

    // Elegir servicio y fecha recarga la página con los horarios libres (A1, origen Manual).
    [HttpGet]
    public IActionResult Nueva(int? servicioId, int? peluqueroId, DateOnly? fecha, int? clienteId)
    {
        var modelo = new ReservaManualViewModel { ServicioId = servicioId ?? 0, PeluqueroId = peluqueroId ?? 0, Fecha = fecha ?? Reloj.HoyBolivia(), ClienteId = clienteId };
        try
        {
            if (clienteId.HasValue)
            {
                Cliente? c = gestorClientes.ObtenerPorId(clienteId.Value);
                if (c != null)
                {
                    modelo.Nombre = c.Nombre; modelo.Apellido = c.Apellido; modelo.Telefono = c.Telefono; modelo.Correo = c.Correo;
                }
            }
        }
        catch (Exception ex)
        {
            registro.Registrar("ReservasController.Nueva", ex);
        }
        CargarHorarios(modelo);
        return View(modelo);
    }

    [HttpPost]
    public IActionResult Nueva(ReservaManualViewModel modelo)
    {
        if (!modelo.Hora.HasValue) modelo.Errores.Add("Elige un horario.");
        modelo.Errores.AddRange(Validador.ValidarDatosReserva(modelo.ADatosReserva(), OrigenReserva.Manual));
        if (modelo.Errores.Count == 0)
        {
            try
            {
                Usuario? sesion = gestorUsuarios.ObtenerPorId(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0"));
                Reserva res = gestorReservas.RegistrarReserva(modelo.ADatosReserva(), OrigenReserva.Manual, sesion);
                string aviso = res.CorreoEnviado == false ? " No se pudo enviar el correo de confirmación." : "";
                TempData["Exito"] = $"Reserva registrada: {res.Cliente.NombreCompleto}, {res.Fecha:dd/MM/yyyy} {res.HoraInicio:HH\\:mm}.{aviso}";
                return RedirectToAction("Index", "Agenda", new { fecha = res.Fecha.ToString("yyyy-MM-dd") });
            }
            catch (HorarioNoDisponibleException ex)
            {
                modelo.Errores.Add(ex.Message);
                modelo.Hora = null;
            }
            catch (ArgumentException ex)
            {
                modelo.Errores.Add(ex.Message);
            }
            catch (Exception ex)
            {
                registro.Registrar("ReservasController.Nueva", ex);
                modelo.Errores.Add("No se pudo registrar la reserva, intenta nuevamente.");
            }
        }
        CargarHorarios(modelo);
        return View(modelo);
    }

    // ---------- RF-15: reprogramar (A4) ----------

    [HttpGet]
    public IActionResult Reprogramar(int id, DateOnly? fecha)
    {
        try
        {
            Reserva res = gestorReservas.ObtenerReserva(id);
            if (res.Estado != EstadoReserva.Confirmada)
            {
                TempData["Error"] = "Solo se pueden reprogramar reservas confirmadas.";
                return RedirectToAction(nameof(Index));
            }
            DateOnly dia = fecha ?? res.Fecha;
            var modelo = new ReprogramarViewModel
            {
                Reserva = res,
                Fecha = dia,
                Hoy = Reloj.HoyBolivia(),
                Disponibles = gestorReservas.CalcularHorariosDisponibles(dia, res.Servicio, res.Peluquero.Id, res.Id, OrigenReserva.Manual),
                Error = TempData["Error"] as string,
            };
            return View(modelo);
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            registro.Registrar("ReservasController.Reprogramar", ex);
            TempData["Error"] = "No se pudo cargar la reserva.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Reprogramar(int id, DateOnly fecha, TimeOnly hora)
    {
        try
        {
            gestorReservas.Reprogramar(id, fecha, hora);
            TempData["Exito"] = $"Reserva reprogramada al {fecha:dd/MM/yyyy} a las {hora:HH\\:mm}.";
            return RedirectToAction("Index", "Agenda", new { fecha = fecha.ToString("yyyy-MM-dd") });
        }
        catch (HorarioNoDisponibleException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            registro.Registrar("ReservasController.Reprogramar", ex);
            TempData["Error"] = "No se pudo reprogramar la reserva.";
        }
        return RedirectToAction(nameof(Reprogramar), new { id, fecha = fecha.ToString("yyyy-MM-dd") });
    }

    // ---------- RF-13 y RF-14: cancelar, atendida, no asistió (A4) ----------

    [HttpPost]
    public IActionResult CambiarEstado(int id, EstadoReserva estado, DateOnly desde, DateOnly hasta, int? peluqueroId)
    {
        try
        {
            gestorReservas.CambiarEstado(id, estado);
            TempData["Exito"] = "Estado actualizado.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            registro.Registrar("ReservasController.CambiarEstado", ex);
            TempData["Error"] = "No se pudo cambiar el estado. Intenta nuevamente.";
        }
        return RedirectToAction(nameof(Index), new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd"), peluqueroId });
    }

    private void CargarHorarios(ReservaManualViewModel modelo)
    {
        modelo.Hoy = Reloj.HoyBolivia();
        try
        {
            modelo.Servicios = gestorServicios.ListarActivos();
            modelo.Peluqueros = gestorPeluqueros.ListarActivos();
            if (modelo.PeluqueroId == 0 && modelo.Peluqueros.Count == 1) modelo.PeluqueroId = modelo.Peluqueros[0].Id;
            Servicio? servicio = modelo.Servicios.Find(s => s.Id == modelo.ServicioId);
            Peluquero? peluquero = modelo.Peluqueros.Find(p => p.Id == modelo.PeluqueroId);
            if (servicio != null && peluquero != null)
                modelo.Disponibles = gestorReservas.CalcularHorariosDisponibles(modelo.Fecha, servicio, peluquero.Id, 0, OrigenReserva.Manual);
        }
        catch (Exception ex)
        {
            registro.Registrar("ReservasController.CargarHorarios", ex);
            modelo.Errores.Add("No se pudieron cargar los servicios, peluqueros u horarios.");
        }
    }
}
