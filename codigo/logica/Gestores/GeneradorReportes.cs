using System.Text;
using Logica.Enumeraciones;
using Logica.Modelos;

namespace Logica.Gestores;

public class GeneradorReportes
{
    private readonly GestorReservas gestorReservas;  

    public GeneradorReportes(GestorReservas gestorReservas)
    {
        this.gestorReservas = gestorReservas;
    }

    public static Reporte Generar(List<Reserva> reservasDelRango, DateOnly fi, DateOnly ff, DateOnly hoy)
    {
        if (fi > ff) throw new ArgumentException("Rango de fechas no válido.");
        var reporte = new Reporte(fi, ff);
        var contServicio = new Dictionary<string, int>();

        foreach (Reserva r in reservasDelRango)
        {
            if (r.Fecha < fi || r.Fecha > ff) continue;  
            reporte.Total++;
            reporte.CitasPorEstado[r.Estado]++;
            reporte.CitasPorDia[r.Fecha] = reporte.CitasPorDia.GetValueOrDefault(r.Fecha) + 1;

            string peluquero = r.Peluquero.NombreCompleto;
            if (r.Estado == EstadoReserva.Atendida)
            {
                reporte.IngresoReal += r.Precio;
                reporte.IngresoPorPeluquero[peluquero] = reporte.IngresoPorPeluquero.GetValueOrDefault(peluquero) + r.Precio;
            }
            if (r.Estado == EstadoReserva.Confirmada)
            {
                if (r.Fecha >= hoy) reporte.IngresoEstimado += r.Precio;
                else reporte.SinMarcar++;
            }
            if (r.Estado != EstadoReserva.Cancelada)
            {
                contServicio[r.Servicio.Nombre] = contServicio.GetValueOrDefault(r.Servicio.Nombre) + 1;
                reporte.CitasPorPeluquero[peluquero] = reporte.CitasPorPeluquero.GetValueOrDefault(peluquero) + 1;
            }
        }

        var lista = new List<ConteoServicio>();
        foreach (var par in contServicio) lista.Add(new ConteoServicio(par.Key, par.Value));
        OrdenarBurbuja(lista);
        reporte.ServiciosOrdenados = lista;
        return reporte;
    }

    public static void OrdenarBurbuja(List<ConteoServicio> lista)
    {
        int k = lista.Count;
        for (int i = 0; i <= k - 2; i++)
        {
            for (int j = 0; j <= k - 2 - i; j++)
            {
                if (lista[j].Cantidad < lista[j + 1].Cantidad)
                {
                    ConteoServicio aux = lista[j];
                    lista[j] = lista[j + 1];
                    lista[j + 1] = aux;
                }
            }
        }
    }

    public static byte[] ExportarTxt(Reporte reporte) => Encoding.UTF8.GetBytes(reporte.ATexto());

    public Reporte Generar(DateOnly fi, DateOnly ff, DateOnly hoy)
    {
        if (fi > ff) throw new ArgumentException("La fecha inicial no puede ser posterior a la final.");
        return Generar(gestorReservas.ObtenerAgenda(fi, ff, null), fi, ff, hoy);
    }
}
