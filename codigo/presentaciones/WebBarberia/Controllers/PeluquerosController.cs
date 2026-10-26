using Logica.Gestores;
using Logica.Modelos;
using Logica.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBarberia.Models;

namespace WebBarberia.Controllers;

// Panel: registrar, modificar, activar y desactivar peluqueros (RF-25).
[Authorize(Roles = "Administrador")]
public class PeluquerosController : Controller
{
    private readonly GestorPeluqueros gestorPeluqueros;
    private readonly RegistroErrores registro;

    public PeluquerosController(GestorPeluqueros gestorPeluqueros, RegistroErrores registro)
    {
        this.gestorPeluqueros = gestorPeluqueros;
        this.registro = registro;
    }

    public IActionResult Index()
    {
        try
        {
            return View(gestorPeluqueros.ListarTodos());
        }
        catch (Exception ex)
        {
            registro.Registrar("PeluquerosController.Index", ex);
            ViewBag.Error = "No se pudieron cargar los peluqueros. Intenta nuevamente.";
            return View(new List<Peluquero>());
        }
    }

    [HttpGet]
    public IActionResult Nuevo() => View("Formulario", new PeluqueroViewModel());

    [HttpPost]
    public IActionResult Nuevo(PeluqueroViewModel modelo)
    {
        try
        {
            // El constructor (Persona) valida el nombre y el apellido.
            gestorPeluqueros.Registrar(new Peluquero(0, modelo.Nombre ?? "", modelo.Apellido ?? "", modelo.Especialidad ?? ""));
            TempData["Exito"] = "Peluquero registrado: ya aparece en la reserva web.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            modelo.Errores.Add(ex.Message);
        }
        catch (Exception ex)
        {
            registro.Registrar("PeluquerosController.Nuevo", ex);
            modelo.Errores.Add("No se pudo registrar el peluquero. Intenta nuevamente.");
        }
        return View("Formulario", modelo);
    }

    [HttpGet]
    public IActionResult Editar(int id)
    {
        try
        {
            Peluquero? peluquero = gestorPeluqueros.ObtenerPorId(id);
            if (peluquero == null) return RedirectToAction(nameof(Index));
            return View("Formulario", PeluqueroViewModel.Desde(peluquero));
        }
        catch (Exception ex)
        {
            registro.Registrar("PeluquerosController.Editar", ex);
            TempData["Error"] = "No se pudo cargar el peluquero.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public IActionResult Editar(PeluqueroViewModel modelo)
    {
        try
        {
            Peluquero? peluquero = gestorPeluqueros.ObtenerPorId(modelo.Id);
            if (peluquero == null) return RedirectToAction(nameof(Index));
            peluquero.CambiarNombre(modelo.Nombre ?? "", modelo.Apellido ?? "");
            peluquero.CambiarEspecialidad(modelo.Especialidad);
            gestorPeluqueros.Modificar(peluquero);
            TempData["Exito"] = "Peluquero actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            modelo.Errores.Add(ex.Message);
        }
        catch (Exception ex)
        {
            registro.Registrar("PeluquerosController.Editar", ex);
            modelo.Errores.Add("No se pudo actualizar el peluquero. Intenta nuevamente.");
        }
        return View("Formulario", modelo);
    }

    // RN-16: los peluqueros no se eliminan (tienen reservas), se activan o desactivan.
    [HttpPost]
    public IActionResult CambiarEstado(int id, bool activo)
    {
        try
        {
            gestorPeluqueros.CambiarEstado(id, activo);
            TempData["Exito"] = activo ? "Peluquero activado." : "Peluquero desactivado: ya no aparece en la web. Sus reservas se conservan.";
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            registro.Registrar("PeluquerosController.CambiarEstado", ex);
            TempData["Error"] = "No se pudo cambiar el estado del peluquero.";
        }
        return RedirectToAction(nameof(Index));
    }
}
