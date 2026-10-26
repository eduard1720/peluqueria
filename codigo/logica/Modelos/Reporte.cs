using System.Text;
using Logica.Enumeraciones;

namespace Logica.Modelos;

// Resultado del algoritmo A5 (reporte por rango de fechas).
public class Reporte
{
    public Reporte(DateOnly desde, DateOnly hasta)
    {
        Desde = desde;
        Hasta = hasta;
        foreach (EstadoReserva e in Enum.GetValues<EstadoReserva>()) CitasPorEstado[e] = 0;
    }

    public DateOnly Desde { get; }
    public DateOnly Hasta { get; }
    public int Total { get; internal set; }
    public Dictionary<EstadoReserva, int> CitasPorEstado { get; } = new();
    public SortedDictionary<DateOnly, int> CitasPorDia { get; } = new();
    public List<ConteoServicio> ServiciosOrdenados { get; internal set; } = new();
    public SortedDictionary<string, int> CitasPorPeluquero { get; } = new();         // sin contar canceladas
    public SortedDictionary<string, decimal> IngresoPorPeluquero { get; } = new();   // solo atendidas
    public decimal IngresoReal { get; internal set; }
    public decimal IngresoEstimado { get; internal set; }
    public int SinMarcar { get; internal set; }

    // Texto del archivo reporte_aaaammdd_aaaammdd.txt (RF-20).
    public string ATexto()
    {
        var sb = new StringBuilder();
        sb.AppendLine("AS BAJO LA MANGA - REPORTE DE RESERVAS");
        sb.AppendLine($"Del {Desde:dd/MM/yyyy} al {Hasta:dd/MM/yyyy}");
        sb.AppendLine($"Total de citas: {Total}");
        sb.AppendLine();
        sb.AppendLine("Citas por estado:");
        foreach (var par in CitasPorEstado) sb.AppendLine($"  {par.Key,-12} {par.Value,5}");
        sb.AppendLine();
        sb.AppendLine("Citas por día:");
        foreach (var par in CitasPorDia) sb.AppendLine($"  {par.Key:dd/MM/yyyy} {par.Value,5}");
        sb.AppendLine();
        sb.AppendLine("Servicios más solicitados:");
        foreach (var c in ServiciosOrdenados) sb.AppendLine($"  {c.Servicio,-25} {c.Cantidad,5}");
        sb.AppendLine();
        sb.AppendLine("Por peluquero (citas / ingreso real):");
        foreach (var par in CitasPorPeluquero)
            sb.AppendLine($"  {par.Key,-25} {par.Value,5}   Bs {IngresoPorPeluquero.GetValueOrDefault(par.Key):0.00}");
        sb.AppendLine();
        sb.AppendLine($"Ingreso real (atendidas):      Bs {IngresoReal:0.00}");
        sb.AppendLine($"Ingreso estimado (por venir):  Bs {IngresoEstimado:0.00}");
        sb.AppendLine($"Citas pasadas sin marcar:      {SinMarcar}");
        return sb.ToString();
    }

    public string NombreArchivo => $"reporte_{Desde:yyyyMMdd}_{Hasta:yyyyMMdd}.txt";
}
