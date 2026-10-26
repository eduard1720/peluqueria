using System.Security.Claims;
using Logica.Gestores;
using Logica.Modelos;
using Logica.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBarberia.Models;

namespace WebBarberia.Controllers;

// Panel: Crear, modificar y desactivar usuarios del personal (RF-19, RN-19).
[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly GestorUsuarios gestorUsuarios;
    private readonly RegistroErrores registro;

    public UsuariosController(GestorUsuarios gestorUsuarios, RegistroErrores registro)
    {
        this.gestorUsuarios = gestorUsuarios;
        this.registro = registro;
    }

    private int IdSesion => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    public IActionResult Index()
    {
        ViewBag.IdSesion = IdSesion;
        try
        {
            return View(gestorUsuarios.Listar());
        }
        catch (Exception ex)
        {
            registro.Registrar("UsuariosController.Index", ex);
            ViewBag.Error = "No se pudieron cargar los usuarios. Intenta nuevamente.";
            return View(new List<Usuario>());
        }
    }

    [HttpGet]
    public IActionResult Nuevo() => View("Formulario", new UsuarioViewModel());

    [HttpPost]
    public IActionResult Nuevo(UsuarioViewModel modelo)
    {
        if (!Validar(modelo, true)) return View("Formulario", modelo);
        try
        {
            gestorUsuarios.Crear(modelo.Nombre, modelo.Apellido, modelo.NombreUsuario, modelo.Contrasena ?? "", modelo.Rol);
            TempData["Exito"] = "Usuario creado.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            modelo.Errores.Add(ex.Message);
        }
        catch (Exception ex)
        {
            registro.Registrar("UsuariosController.Nuevo", ex);
            modelo.Errores.Add("No se pudo crear el usuario. Intenta nuevamente.");
        }
        return View("Formulario", modelo);
    }

    [HttpGet]
    public IActionResult Editar(int id)
    {
        try
        {
            Usuario? usuario = gestorUsuarios.ObtenerPorId(id);
            if (usuario == null) return RedirectToAction(nameof(Index));
            return View("Formulario", UsuarioViewModel.Desde(usuario));
        }
        catch (Exception ex)
        {
            registro.Registrar("UsuariosController.Editar", ex);
            TempData["Error"] = "No se pudo cargar el usuario.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public IActionResult Editar(UsuarioViewModel modelo)
    {
        if (!Validar(modelo, false)) return View("Formulario", modelo);
        try
        {
            Usuario? usuario = gestorUsuarios.ObtenerPorId(modelo.Id);
            if (usuario == null) return RedirectToAction(nameof(Index));
            modelo.NombreUsuario = usuario.NombreUsuario;   // el nombre de usuario no se cambia

            string? motivo = GestorUsuarios.ValidarCambio(usuario, modelo.Rol, modelo.Activo, IdSesion,
                                                          gestorUsuarios.ContarAdministradoresActivos());
            if (motivo != null)
            {
                modelo.Errores.Add(motivo);
                return View("Formulario", modelo);
            }

            usuario.CambiarNombre(modelo.Nombre, modelo.Apellido);
            usuario.CambiarRol(modelo.Rol);
            if (modelo.Activo) usuario.Activar(); else usuario.Desactivar();
            if (!string.IsNullOrEmpty(modelo.Contrasena))
            {
                usuario.CambiarContrasena(modelo.Contrasena);
                usuario.ReiniciarIntentos();   // la nueva contraseña también desbloquea la cuenta
            }
            gestorUsuarios.Modificar(usuario);
            TempData["Exito"] = "Usuario actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            modelo.Errores.Add(ex.Message);
        }
        catch (Exception ex)
        {
            registro.Registrar("UsuariosController.Editar", ex);
            modelo.Errores.Add("No se pudo actualizar el usuario. Intenta nuevamente.");
        }
        return View("Formulario", modelo);
    }

    private static bool Validar(UsuarioViewModel m, bool esNuevo)
    {
        if (!Validador.EsNombreValido(m.Nombre)) m.Errores.Add("El nombre debe tener de 2 a 50 letras.");
        if (!Validador.EsNombreValido(m.Apellido)) m.Errores.Add("El apellido debe tener de 2 a 50 letras.");
        if (esNuevo && !Validador.EsNombreUsuarioValido(m.NombreUsuario))
            m.Errores.Add("El usuario debe tener de 4 a 20 caracteres (letras, números, punto o guion bajo).");
        bool hayContrasena = !string.IsNullOrEmpty(m.Contrasena);
        if ((esNuevo || hayContrasena) && !Validador.EsContrasenaSegura(m.Contrasena))
            m.Errores.Add("La contraseña debe tener al menos 8 caracteres, con letras y números.");
        return m.Errores.Count == 0;
    }
}
