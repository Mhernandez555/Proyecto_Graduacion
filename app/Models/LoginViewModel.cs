using System.ComponentModel.DataAnnotations;
namespace Controlex.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese el nombre de usuario."), StringLength(50)]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = "";

    [Required(ErrorMessage = "Ingrese la contraseña."), DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = "";
}
