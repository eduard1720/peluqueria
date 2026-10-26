using System.Security.Claims;
using Logica.Gestores;
using Logica.Modelos;
using Logica.Utilidades;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using WebBarberia.Models;

namespace WebBarberia.Controllers;

// Inicio y cierre de sesión del personal (RF-08, RF-09, algoritmo A3).
public class CuentaController : Controller
{
    private readonly GestorUsuarios gestorUsuarios;
    private readonly RegistroErrores registro;

    public CuentaController(GestorUsuarios gestorUsuarios, RegistroErrores registro)
    {
        this.gestorUsuarios = gestorUsuarios;
        this.registro = registro;
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        try
        {
            Usuario? usuario = gestorUsuarios.IniciarSesion(modelo.Usuario, modelo.Contrasena);
            if (usuario == null)
            {
                ModelState.AddModelError("", GestorUsuarios.Mensaje);
                return View(modelo);
            }
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Name, usuario.NombreCompleto),
                new(ClaimTypes.Role, usuario.Rol.ToString()),
            };
            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));
            return RedirectToAction("Index", "Agenda");
        }
        catch (Exception ex)
        {
            registro.Registrar("CuentaController.Login", ex);
            ModelState.AddModelError("", "No se pudo iniciar sesión. Intenta nuevamente.");
            return View(modelo);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Reservar");
    }
}
