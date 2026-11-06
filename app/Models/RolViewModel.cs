using System.ComponentModel.DataAnnotations;
namespace Controlex.Web.Models;

public class RolViewModel
{
    public int IdRol { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre del rol."), StringLength(50)]
    [Display(Name = "Nombre del rol")]
    public string NombreRol { get; set; } = "";

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Acceso administrativo total")]
    public bool EsAdministrador { get; set; }

    [Display(Name = "Activo")]
    public bool Estado { get; set; } = true;

    public List<PermisoItemViewModel> Permisos { get; set; } = new();
}

public class PermisoItemViewModel
{
    public string Modulo { get; set; } = "";
    public int Nivel { get; set; }   // 0 sin acceso, 1 parcial, 2 total
}
