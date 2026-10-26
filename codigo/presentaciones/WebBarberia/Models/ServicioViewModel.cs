using System.Globalization;
using Logica.Modelos;

namespace WebBarberia.Models;

// Formulario de registro y modificación de servicios (RF-18).
public class ServicioViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int DuracionMin { get; set; } = 30;
    public string Precio { get; set; } = "";   // texto: se acepta "35.50" o "35,50"
    public string? Imagen { get; set; }

    public List<string> Errores { get; set; } = new();

    public static ServicioViewModel Desde(Servicio s) => new()
    {
        Id = s.Id, Nombre = s.Nombre, Descripcion = s.Descripcion, DuracionMin = s.DuracionMin,
        Precio = s.Precio.ToString("0.00", CultureInfo.InvariantCulture), Imagen = s.Imagen,
    };

    public bool TryLeerPrecio(out decimal precio) =>
        decimal.TryParse((Precio ?? "").Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out precio);
}
