using Logica.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebBarberia.Controllers;

// Panel: ver el registro de errores, leyendo App_Data/errores.txt (RF-23).
[Authorize(Roles = "Administrador")]
public class ErroresController : Controller
{
    private readonly RegistroErrores registro;

    public ErroresController(RegistroErrores registro)
    {
        this.registro = registro;
    }

    public IActionResult Index() => View(registro.LeerUltimas(100));
}
