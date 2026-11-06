using System.ComponentModel.DataAnnotations;
namespace Controlex.Web.Models;

public class UsuarioViewModel
{
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "Ingrese la cédula."),
     RegularExpression(@"^\d{9,12}$", ErrorMessage = "La cédula debe contener solo números (9 a 12 dígitos).")]
    [Display(Name = "Número de cédula")]
    public string Cedula { get; set; } = "";

    [Required(ErrorMessage = "Ingrese el nombre."), StringLength(100),
     RegularExpression(@"^[\p{L} ]+$", ErrorMessage = "El nombre solo admite letras.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingrese el primer apellido."), StringLength(100),
     RegularExpression(@"^[\p{L} ]+$", ErrorMessage = "El apellido solo admite letras.")]
    [Display(Name = "Primer apellido")]
    public string Apellido1 { get; set; } = "";

    [StringLength(100), RegularExpression(@"^[\p{L} ]+$", ErrorMessage = "El apellido solo admite letras.")]
    [Display(Name = "Segundo apellido")]
    public string? Apellido2 { get; set; }

    [Required(ErrorMessage = "Ingrese el correo electrónico."), StringLength(200),
     EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [Display(Name = "Correo electrónico")]
    public string Correo { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol.")]
    [Display(Name = "Rol")]
    public int IdRol { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre de usuario."),
     RegularExpression(@"^[A-Za-z0-9_.]{4,50}$", ErrorMessage = "Use de 4 a 50 caracteres: letras, números, punto o guion bajo.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string? Contrasena { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Contrasena), ErrorMessage = "La confirmación no coincide con la contraseña.")]
    [Display(Name = "Confirmar contraseña")]
    public string? ConfirmarContrasena { get; set; }

    [Range(0, 99999999, ErrorMessage = "Ingrese un costo por hora válido.")]
    [Display(Name = "Costo por hora (₡)")]
    public decimal CostoHora { get; set; }

    [Display(Name = "Activo")]
    public bool Estado { get; set; } = true;
}
