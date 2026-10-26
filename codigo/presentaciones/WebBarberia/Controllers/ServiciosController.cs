using Logica.Gestores;
using Logica.Modelos;
using Logica.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBarberia.Models;

namespace WebBarberia.Controllers;

// Panel: Registrar, modificar, activar y desactivar servicios (RF-18).
[Authorize(Roles = "Administrador")]
public class ServiciosController : Controller
{
    private readonly GestorServicios gestorServicios;
    private readonly RegistroErrores registro;

    public ServiciosController(GestorServicios gestorServicios, RegistroErrores registro)
    {
        this.gestorServicios = gestorServicios;
        this.registro = registro;
    }

    public IActionResult Index()
    {
        try
        {
            return View(gestorServicios.ListarTodos());
        }
        catch (Exception ex)
        {
            registro.Registrar("ServiciosController.Index", ex);
            ViewBag.Error = "No se pudieron cargar los servicios. Intenta nuevamente.";
            return View(new List<Servicio>());
        }
    }

    [HttpGet]
    public IActionResult Nuevo() => View("Formulario", new ServicioViewModel());

    [HttpPost]
    public IActionResult Nuevo(ServicioViewModel modelo)
    {
        try
        {
            if (!modelo.TryLeerPrecio(out decimal precio)) throw new ArgumentException("El precio debe ser un número.");
            // El constructor de Servicio valida RN-04 (duración y precio).
            gestorServicios.Registrar(new Servicio(0, modelo.Nombre, modelo.Descripcion ?? "", modelo.DuracionMin, precio, modelo.Imagen ?? ""));
            TempData["Exito"] = "Servicio registrado.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            modelo.Errores.Add(ex.Message);
        }
        catch (Exception ex)
        {
            registro.Registrar("ServiciosController.Nuevo", ex);
            modelo.Errores.Add("No se pudo registrar el servicio. Intenta nuevamente.");
        }
        return View("Formulario", modelo);
    }

    [HttpGet]
    public IActionResult Editar(int id)
    {
        try
        {
            Servicio? servicio = gestorServicios.ObtenerPorId(id);
            if (servicio == null) return RedirectToAction(nameof(Index));
            return View("Formulario", ServicioViewModel.Desde(servicio));
        }
        catch (Exception ex)
        {
            registro.Registrar("ServiciosController.Editar", ex);
            TempData["Error"] = "No se pudo cargar el servicio.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public IActionResult Editar(ServicioViewModel modelo)
    {
        try
        {
            if (!modelo.TryLeerPrecio(out decimal precio)) throw new ArgumentException("El precio debe ser un número.");
            Servicio? servicio = gestorServicios.ObtenerPorId(modelo.Id);
            if (servicio == null) return RedirectToAction(nameof(Index));
            servicio.Modificar(modelo.Nombre, modelo.Descripcion ?? "", modelo.DuracionMin, precio, modelo.Imagen ?? "");
            gestorServicios.Modificar(servicio);
            TempData["Exito"] = "Servicio actualizado. Las reservas ya hechas conservan su precio.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            modelo.Errores.Add(ex.Message);
        }
        catch (Exception ex)
        {
            registro.Registrar("ServiciosController.Editar", ex);
            modelo.Errores.Add("No se pudo actualizar el servicio. Intenta nuevamente.");
        }
        return View("Formulario", modelo);
    }

    // RN-16: los servicios no se eliminan, se activan o desactivan.
    [HttpPost]
    public IActionResult CambiarEstado(int id, bool activo)
    {
        try
        {
            gestorServicios.CambiarEstado(id, activo);
            TempData["Exito"] = activo ? "Servicio activado." : "Servicio desactivado: ya no aparece en la web.";
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            registro.Registrar("ServiciosController.CambiarEstado", ex);
            TempData["Error"] = "No se pudo cambiar el estado del servicio.";
        }
        return RedirectToAction(nameof(Index));
    }
}
