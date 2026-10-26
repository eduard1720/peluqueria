using Logica.Gestores;
using Logica.Modelos;
using Logica.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebBarberia.Controllers;

// Panel: Reportes por rango de fechas y exportación .txt (RF-20, A5, A5.1).
[Authorize(Roles = "Administrador")]
public class ReportesController : Controller
{
    private readonly GeneradorReportes generador;
    private readonly RegistroErrores registro;

    public ReportesController(GeneradorReportes generador, RegistroErrores registro)
    {
        this.generador = generador;
        this.registro = registro;
    }

    // Por defecto: del primer día del mes actual hasta hoy.
    public IActionResult Index(DateOnly? desde, DateOnly? hasta)
    {
        DateOnly hoy = Reloj.HoyBolivia();
        DateOnly fi = desde ?? new DateOnly(hoy.Year, hoy.Month, 1);
        DateOnly ff = hasta ?? hoy;
        ViewBag.Desde = fi;
        ViewBag.Hasta = ff;
        try
        {
            return View(generador.Generar(fi, ff, hoy));
        }
        catch (ArgumentException ex)
        {
            ViewBag.Error = ex.Message;
        }
        catch (Exception ex)
        {
            registro.Registrar("ReportesController.Index", ex);
            ViewBag.Error = "No se pudo generar el reporte. Intenta nuevamente.";
        }
        return View((Reporte?)null);
    }

    // Archivo reporte_aaaammdd_aaaammdd.txt armado en memoria y descargado (RF-20).
    public IActionResult Exportar(DateOnly desde, DateOnly hasta)
    {
        try
        {
            Reporte reporte = generador.Generar(desde, hasta, Reloj.HoyBolivia());
            return File(GeneradorReportes.ExportarTxt(reporte), "text/plain", reporte.NombreArchivo);
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            registro.Registrar("ReportesController.Exportar", ex);
            TempData["Error"] = "No se pudo exportar el reporte.";
        }
        return RedirectToAction(nameof(Index), new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd") });
    }
}
