using System.ComponentModel.DataAnnotations;
namespace Controlex.Web.Models;

public class ClienteViewModel
{
    public int IdCliente { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de cliente.")]
    [Display(Name = "Tipo de cliente")]
    public int IdTipoCliente { get; set; }

    [Required(ErrorMessage = "Ingrese la identificación."),
     RegularExpression(@"^\d{9,12}$", ErrorMessage = "La identificación debe contener solo números (9 a 12 dígitos).")]
    [Display(Name = "Identificación")]
    public string Identificacion { get; set; } = "";

    [Required(ErrorMessage = "Ingrese el nombre del cliente."), StringLength(200)]
    [Display(Name = "Nombre del cliente")]
    public string NombreCliente { get; set; } = "";

    [StringLength(100), RegularExpression(@"^[\p{L} ]+$", ErrorMessage = "El contacto solo admite letras.")]
    [Display(Name = "Contacto")]
    public string? Contacto { get; set; }

    [Required(ErrorMessage = "Ingrese el teléfono principal."),
     RegularExpression(@"^\d{8,20}$", ErrorMessage = "El teléfono debe contener solo números (mínimo 8 dígitos).")]
    [Display(Name = "Teléfono 1")]
    public string Telefono1 { get; set; } = "";

    [RegularExpression(@"^\d{8,20}$", ErrorMessage = "El teléfono debe contener solo números (mínimo 8 dígitos).")]
    [Display(Name = "Teléfono 2")]
    public string? Telefono2 { get; set; }

    [StringLength(200), EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [Display(Name = "Correo")]
    public string? Correo { get; set; }

    [StringLength(500)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [Display(Name = "Activo")]
    public bool Estado { get; set; } = true;
}
