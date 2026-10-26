namespace Logica.Modelos;

// Servicio que ofrece la barbería (corte, barba, etc.).
public class Servicio
{
    private int id;
    private string nombre = "";
    private string descripcion = "";
    private int duracionMin;
    private decimal precio;
    private string imagen = "";
    private bool activo;

    public Servicio(int id, string nombre, string descripcion, int duracionMin, decimal precio, string imagen, bool activo = true)
    {
        this.id = id;
        Modificar(nombre, descripcion, duracionMin, precio, imagen);
        this.activo = activo;
    }

    public int Id => id;
    public string Nombre => nombre;
    public string Descripcion => descripcion;
    public int DuracionMin => duracionMin;
    public decimal Precio => precio;
    public string Imagen => imagen;
    public bool Activo => activo;

    public void AsignarId(int nuevoId) => id = nuevoId;

    // Valida RN-04: duración múltiplo de 30 entre 30 y 180; precio mayor que 0.
    public void Modificar(string nombre, string descripcion, int duracionMin, decimal precio, string imagen)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es obligatorio.");
        if (duracionMin < 30 || duracionMin > 180 || duracionMin % 30 != 0)
            throw new ArgumentException("La duración debe ser múltiplo de 30, entre 30 y 180 minutos.");
        if (precio <= 0) throw new ArgumentException("El precio debe ser mayor que 0.");
        this.nombre = nombre.Trim();
        this.descripcion = descripcion?.Trim() ?? "";
        this.duracionMin = duracionMin;
        this.precio = precio;
        this.imagen = imagen?.Trim() ?? "";
    }

    // Borrado lógico (RN-16).
    public void Activar() => activo = true;
    public void Desactivar() => activo = false;
}
