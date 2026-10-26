using System.ComponentModel.DataAnnotations;

namespace WebBarberia.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Completa usuario y contraseña.")]
    public string Usuario { get; set; } = "";

    [Required(ErrorMessage = "Completa usuario y contraseña.")]
    [DataType(DataType.Password)]
    public string Contrasena { get; set; } = "";
}
