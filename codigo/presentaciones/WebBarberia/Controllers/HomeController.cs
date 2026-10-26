using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebBarberia.Models;

namespace WebBarberia.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Reservar");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
