using Logica.Enumeraciones;
using Logica.Gestores;
using Logica.Modelos;
using Logica.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace WebBarberia.Controllers;

// Panel: agenda del día (de todos o de un peluquero), cambio de estado (A4) y exportación a .txt (RF-10, RF-13, RF-14, RF-21).
[Authorize(Roles = "Administrador,Recepcionista")]
public class AgendaController : Controller
{
    private readonly GestorReservas gestorReservas;
    private readonly GestorPeluqueros gestorPeluqueros;
    private readonly RegistroErrores registro;

    public AgendaController(GestorReservas gestorReservas, GestorPeluqueros gestorPeluqueros, RegistroErrores registro)
    {
        this.gestorReservas = gestorReservas;
        this.gestorPeluqueros = gestorPeluqueros;
        this.registro = registro;
    }

    public IActionResult Index(DateOnly? fecha, int? peluqueroId)
    {
        DateOnly dia = fecha ?? Reloj.HoyBolivia();
        ViewBag.Fecha = dia;
        ViewBag.PeluqueroId = peluqueroId;
        ViewBag.Peluqueros = new List<Peluquero>();
        try
        {
            ViewBag.Peluqueros = gestorPeluqueros.ListarTodos();
            return View(gestorReservas.ObtenerAgenda(dia, dia, null, peluqueroId));
        }
        catch (Exception ex)
        {
            registro.Registrar("AgendaController.Index", ex);
            ViewBag.Error = "No se pudo cargar la agenda. Intenta nuevamente.";
            return View(new List<Reserva>());
        }
    }

    // Cancelar, marcar Atendida o No asistió (RN-13, RN-14).
    [HttpPost]
    public IActionResult CambiarEstado(int id, EstadoReserva estado, DateOnly fecha, int? peluqueroId)
    {
        try
        {
            gestorReservas.CambiarEstado(id, estado);
            TempData["Exito"] = "Estado actualizado.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;      // regla del negocio no cumplida
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            registro.Registrar("AgendaController.CambiarEstado", ex);
            TempData["Error"] = "No se pudo cambiar el estado. Intenta nuevamente.";
        }
        return RedirectToAction(nameof(Index), new { fecha = fecha.ToString("yyyy-MM-dd"), peluqueroId });
    }

    // Archivo agenda_aaaammdd.txt armado en memoria y descargado (RF-21).
    public IActionResult Exportar(DateOnly fecha, int? peluqueroId)
    {
        try
        {
            string texto = GestorReservas.AgendaATexto(fecha, gestorReservas.ObtenerAgenda(fecha, fecha, null, peluqueroId));
            return File(Encoding.UTF8.GetBytes(texto), "text/plain", $"agenda_{fecha:yyyyMMdd}.txt");
        }
        catch (Exception ex)
        {
            registro.Registrar("AgendaController.Exportar", ex);
            TempData["Error"] = "No se pudo exportar la agenda.";
            return RedirectToAction(nameof(Index), new { fecha = fecha.ToString("yyyy-MM-dd"), peluqueroId });
        }
    }
}
