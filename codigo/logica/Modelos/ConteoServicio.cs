namespace Logica.Modelos;

// Par (servicio, cantidad) que se ordena con burbuja en el reporte.
public class ConteoServicio
{
    private string servicio;
    private int cantidad;

    public ConteoServicio(string servicio, int cantidad)
    {
        this.servicio = servicio;
        this.cantidad = cantidad;
    }

    public string Servicio => servicio;
    public int Cantidad => cantidad;
}
