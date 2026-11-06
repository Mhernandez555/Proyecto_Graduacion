using System.ComponentModel.DataAnnotations;
namespace Controlex.Web.Models;

public class CambiarContrasenaViewModel
{
    [Required(ErrorMessage = "Ingrese su contraseña actual."), DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string ContrasenaActual { get; set; } = "";

    [Required(ErrorMessage = "Ingrese la nueva contraseña."), DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string NuevaContrasena { get; set; } = "";

    [Required(ErrorMessage = "Confirme la nueva contraseña."), DataType(DataType.Password)]
    [Compare(nameof(NuevaContrasena), ErrorMessage = "La confirmación no coincide con la nueva contraseña.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarContrasena { get; set; } = "";
}

public class RecuperarContrasenaViewModel
{
    [Required(ErrorMessage = "Ingrese el nombre de usuario."), StringLength(50)]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = "";

    [Required(ErrorMessage = "Ingrese el número de cédula."),
     RegularExpression(@"^\d{9,12}$", ErrorMessage = "La cédula debe contener solo números (9 a 12 dígitos).")]
    [Display(Name = "Número de cédula")]
    public string Cedula { get; set; } = "";
}
