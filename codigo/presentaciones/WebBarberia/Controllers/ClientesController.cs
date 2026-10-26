using Logica.Gestores;
using Logica.Modelos;
using Logica.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBarberia.Models;

namespace WebBarberia.Controllers;

// Panel: CRUD de clientes, búsqueda lineal e historial (RF-16, RF-17).
[Authorize(Roles = "Administrador,Recepcionista")]
public class ClientesController : Controller
{
    private readonly GestorClientes gestorClientes;
    private readonly RegistroErrores registro;

    public ClientesController(GestorClientes gestorClientes, RegistroErrores registro)
    {
        this.gestorClientes = gestorClientes;
        this.registro = registro;
    }

    // Lista completa o resultado de la búsqueda lineal por nombre o teléfono.
    public IActionResult Index(string? buscar)
    {
        ViewBag.Buscar = buscar;
        try
        {
            return View(string.IsNullOrWhiteSpace(buscar) ? gestorClientes.Listar() : gestorClientes.Buscar(buscar));
        }
        catch (Exception ex)
        {
            registro.Registrar("ClientesController.Index", ex);
            ViewBag.Error = "No se pudieron cargar los clientes. Intenta nuevamente.";
            return View(new List<Cliente>());
        }
    }

    [HttpGet]
    public IActionResult Nuevo() => View("Formulario", new ClienteViewModel());

    [HttpPost]
    public IActionResult Nuevo(ClienteViewModel modelo)
    {
        if (!Validar(modelo)) return View("Formulario", modelo);
        try
        {
            gestorClientes.Registrar(new Cliente(0, modelo.Nombre, modelo.Apellido, modelo.Telefono, modelo.Correo, Reloj.AhoraBolivia()));
            TempData["Exito"] = "Cliente registrado.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            modelo.Errores.Add(ex.Message);
        }
        catch (Exception ex)
        {
            registro.Registrar("ClientesController.Nuevo", ex);
            modelo.Errores.Add("No se pudo registrar el cliente. Intenta nuevamente.");
        }
        return View("Formulario", modelo);
    }

    [HttpGet]
    public IActionResult Editar(int id)
    {
        try
        {
            Cliente? cliente = gestorClientes.ObtenerPorId(id);
            if (cliente == null) return RedirectToAction(nameof(Index));
            return View("Formulario", ClienteViewModel.Desde(cliente));
        }
        catch (Exception ex)
        {
            registro.Registrar("ClientesController.Editar", ex);
            TempData["Error"] = "No se pudo cargar el cliente.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public IActionResult Editar(ClienteViewModel modelo)
    {
        if (!Validar(modelo)) return View("Formulario", modelo);
        try
        {
            Cliente? cliente = gestorClientes.ObtenerPorId(modelo.Id);
            if (cliente == null) return RedirectToAction(nameof(Index));
            cliente.CambiarNombre(modelo.Nombre, modelo.Apellido);
            cliente.CambiarTelefono(modelo.Telefono);
            cliente.CambiarCorreo(modelo.Correo);
            gestorClientes.Modificar(cliente);
            TempData["Exito"] = "Cliente actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            modelo.Errores.Add(ex.Message);
        }
        catch (Exception ex)
        {
            registro.Registrar("ClientesController.Editar", ex);
            modelo.Errores.Add("No se pudo actualizar el cliente. Intenta nuevamente.");
        }
        return View("Formulario", modelo);
    }

    // RN-17: solo se elimina si no tiene reservas.
    [HttpPost]
    public IActionResult Eliminar(int id)
    {
        try
        {
            gestorClientes.Eliminar(id);
            TempData["Exito"] = "Cliente eliminado.";
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
            registro.Registrar("ClientesController.Eliminar", ex);
            TempData["Error"] = "No se pudo eliminar el cliente.";
        }
        return RedirectToAction(nameof(Index));
    }

    // RF-17: todas las reservas del cliente.
    public IActionResult Historial(int id)
    {
        try
        {
            Cliente? cliente = gestorClientes.ObtenerPorId(id);
            if (cliente == null) return RedirectToAction(nameof(Index));
            ViewBag.Cliente = cliente;
            return View(gestorClientes.Historial(id));
        }
        catch (Exception ex)
        {
            registro.Registrar("ClientesController.Historial", ex);
            TempData["Error"] = "No se pudo cargar el historial.";
            return RedirectToAction(nameof(Index));
        }
    }

    private static bool Validar(ClienteViewModel m)
    {
        if (!Validador.EsNombreValido(m.Nombre)) m.Errores.Add("El nombre debe tener de 2 a 50 letras.");
        if (!Validador.EsNombreValido(m.Apellido)) m.Errores.Add("El apellido debe tener de 2 a 50 letras.");
        if (!Validador.EsTelefonoValido(m.Telefono)) m.Errores.Add("El teléfono debe tener 8 dígitos y empezar con 6 o 7.");
        if (!string.IsNullOrWhiteSpace(m.Correo) && !Validador.EsCorreoValido(m.Correo)) m.Errores.Add("El correo no tiene un formato válido.");
        return m.Errores.Count == 0;
    }
}
