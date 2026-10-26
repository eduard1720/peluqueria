using Logica.Enumeraciones;
using Logica.Excepciones;
using Logica.Gestores;
using Logica.Modelos;
using Logica.Utilidades;
using Microsoft.AspNetCore.Mvc;
using WebBarberia.Models;

namespace WebBarberia.Controllers;

// Zona pública: reserva en 5 pasos (RF-01 a RF-07, RF-24; algoritmos A1 y A2).
public class ReservarController : Controller
{
    private readonly GestorServicios gestorServicios;
    private readonly GestorPeluqueros gestorPeluqueros;
    private readonly GestorReservas gestorReservas;
    private readonly RegistroErrores registro;

    public ReservarController(GestorServicios gestorServicios, GestorPeluqueros gestorPeluqueros, GestorReservas gestorReservas,
                              RegistroErrores registro)
    {
        this.gestorServicios = gestorServicios;
        this.gestorPeluqueros = gestorPeluqueros;
        this.gestorReservas = gestorReservas;
        this.registro = registro;
    }

    // Paso 1: servicios activos.
    public IActionResult Index()
    {
        try
        {
            return View(gestorServicios.ListarActivos());
        }
        catch (Exception ex)
        {
            registro.Registrar("ReservarController.Index", ex);
            ViewBag.Error = "No se pudieron cargar los servicios. Intenta nuevamente en unos minutos.";
            return View(new List<Servicio>());
        }
    }

    // Paso 2: elegir peluquero (RF-24). Si hay uno solo, se salta este paso.
    public IActionResult ElegirPeluquero(int servicioId)
    {
        try
        {
            Servicio? servicio = gestorServicios.ObtenerPorId(servicioId);
            if (servicio == null || !servicio.Activo) return RedirectToAction(nameof(Index));

            List<Peluquero> peluqueros = gestorPeluqueros.ListarActivos();
            if (peluqueros.Count == 1) return RedirectToAction(nameof(Horario), new { servicioId, peluqueroId = peluqueros[0].Id });

            ViewBag.Servicio = servicio;
            if (peluqueros.Count == 0) ViewBag.Error = "Por ahora no hay peluqueros disponibles.";
            return View(peluqueros);
        }
        catch (Exception ex)
        {
            registro.Registrar("ReservarController.ElegirPeluquero", ex);
            ViewBag.Error = "No se pudieron cargar los peluqueros. Intenta nuevamente.";
            return View("Index", new List<Servicio>());
        }
    }

    // Paso 3: fecha y hora del peluquero elegido. Muestra todos los bloques y tacha los que A1 no devuelve.
    public IActionResult Horario(int servicioId, int peluqueroId, DateOnly? fecha)
    {
        try
        {
            Servicio? servicio = gestorServicios.ObtenerPorId(servicioId);
            if (servicio == null || !servicio.Activo) return RedirectToAction(nameof(Index));
            Peluquero? peluquero = gestorPeluqueros.ObtenerPorId(peluqueroId);
            if (peluquero == null || !peluquero.Activo) return RedirectToAction(nameof(ElegirPeluquero), new { servicioId });

            DateOnly hoy = Reloj.HoyBolivia();
            var modelo = new HorarioViewModel
            {
                Servicio = servicio,
                Peluquero = peluquero,
                HayVariosPeluqueros = gestorPeluqueros.ListarActivos().Count > 1,
                Fecha = fecha ?? hoy,
                Hoy = hoy,
                Mensaje = TempData["Mensaje"] as string,
            };
            foreach (Turno t in HorarioAtencion.Turnos)
            {
                for (TimeOnly h = t.Inicio; h < t.Fin; h = h.AddMinutes(HorarioAtencion.MinutosBloque)) modelo.Bloques.Add(h);
            }
            modelo.Disponibles = gestorReservas.CalcularHorariosDisponibles(modelo.Fecha, servicio, peluquero.Id, 0, OrigenReserva.Web);
            if (modelo.Disponibles.Count == 0 && modelo.Mensaje == null) modelo.Mensaje = "No hay horarios disponibles para esta fecha.";
            return View(modelo);
        }
        catch (Exception ex)
        {
            registro.Registrar("ReservarController.Horario", ex);
            ViewBag.Error = "No se pudieron cargar los horarios. Intenta nuevamente.";
            return View("Index", new List<Servicio>());
        }
    }

    // Paso 4: datos del cliente.
    [HttpGet]
    public IActionResult Datos(int servicioId, int peluqueroId, DateOnly fecha, TimeOnly hora)
    {
        var modelo = new ReservaViewModel { ServicioId = servicioId, PeluqueroId = peluqueroId, Fecha = fecha, Hora = hora };
        return CargarServicioYPeluquero(modelo) ? View(modelo) : RedirectToAction(nameof(Index));
    }

    // Valida los datos (Validador) y muestra el resumen (paso 5).
    [HttpPost]
    public IActionResult Datos(ReservaViewModel modelo)
    {
        if (!CargarServicioYPeluquero(modelo)) return RedirectToAction(nameof(Index));
        modelo.Errores = Validador.ValidarDatosReserva(modelo.ADatosReserva(), OrigenReserva.Web);
        return modelo.Errores.Count > 0 ? View(modelo) : View("Resumen", modelo);
    }

    // Paso 5 → "Editar": vuelve al formulario con los datos escritos.
    [HttpPost]
    public IActionResult Editar(ReservaViewModel modelo)
    {
        if (!CargarServicioYPeluquero(modelo)) return RedirectToAction(nameof(Index));
        return View("Datos", modelo);
    }

    // Paso 5 → "Confirmar": A2 (revalida el horario del peluquero, guarda y envía el correo).
    [HttpPost]
    public IActionResult Confirmar(ReservaViewModel modelo)
    {
        try
        {
            Reserva res = gestorReservas.RegistrarReserva(modelo.ADatosReserva(), OrigenReserva.Web, null);
            TempData["Servicio"] = res.Servicio.Nombre;
            TempData["Peluquero"] = res.Peluquero.NombreCompleto;
            TempData["Fecha"] = res.Fecha.ToString("dd/MM/yyyy");
            TempData["Hora"] = $"{res.HoraInicio:HH\\:mm} a {res.HoraFin:HH\\:mm}";
            TempData["Precio"] = res.Precio.ToString("0.00");
            TempData["Cliente"] = res.Cliente.NombreCompleto;
            TempData["CorreoEnviado"] = res.CorreoEnviado == true;
            return RedirectToAction(nameof(Confirmada));
        }
        catch (HorarioNoDisponibleException)
        {
            TempData["Mensaje"] = "Ese horario ya no está disponible, elige otro.";
            return RedirectToAction(nameof(Horario), new
            {
                servicioId = modelo.ServicioId, peluqueroId = modelo.PeluqueroId, fecha = modelo.Fecha.ToString("yyyy-MM-dd"),
            });
        }
        catch (ArgumentException ex)
        {
            CargarServicioYPeluquero(modelo);
            modelo.Errores.Add(ex.Message);
            return View("Datos", modelo);
        }
        catch (Exception ex)
        {
            registro.Registrar("ReservarController.Confirmar", ex);
            CargarServicioYPeluquero(modelo);
            modelo.Errores.Add("No se pudo registrar la reserva, intenta nuevamente.");
            return View("Datos", modelo);
        }
    }

    public IActionResult Confirmada()
    {
        if (TempData["Servicio"] == null) return RedirectToAction(nameof(Index));
        return View();
    }

    private bool CargarServicioYPeluquero(ReservaViewModel modelo)
    {
        try
        {
            modelo.Servicio = gestorServicios.ObtenerPorId(modelo.ServicioId);
            modelo.Peluquero = gestorPeluqueros.ObtenerPorId(modelo.PeluqueroId);
            return modelo.Servicio != null && modelo.Servicio.Activo && modelo.Peluquero != null && modelo.Peluquero.Activo;
        }
        catch (Exception ex)
        {
            registro.Registrar("ReservarController.CargarServicioYPeluquero", ex);
            return false;
        }
    }
}
