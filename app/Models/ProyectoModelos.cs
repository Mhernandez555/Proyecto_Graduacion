using System.ComponentModel.DataAnnotations;
namespace Controlex.Web.Models;

/// <summary>Formulario de crear y editar proyecto (solo acceso total a PROYECTOS).</summary>
public class ProyectoViewModel
{
    public int IdProyecto { get; set; }
    public string? Codigo { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre del proyecto."), StringLength(200)]
    [Display(Name = "Nombre del proyecto")]
    public string Nombre { get; set; } = "";

    [StringLength(1000)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el cliente.")]
    [Display(Name = "Cliente")]
    public int IdCliente { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la categoría.")]
    [Display(Name = "Categoría")]
    public int IdCategoria { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el estado.")]
    [Display(Name = "Estado")]
    public int IdEstado { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el responsable.")]
    [Display(Name = "Responsable")]
    public int IdResponsable { get; set; }

    [Required(ErrorMessage = "Ingrese la fecha de inicio.")]
    [Display(Name = "Fecha de inicio")]
    public DateTime? FechaInicio { get; set; }

    [Display(Name = "Fecha de finalización prevista")]
    public DateTime? FechaFin { get; set; }

    [Range(0.0, 999999999999.99, ErrorMessage = "Ingrese un precio de venta válido.")]
    [Display(Name = "Precio de venta (₡)")]
    public decimal PrecioVenta { get; set; }
}

/// <summary>Fila de la lista. Los montos son null para quien no tiene acceso total.</summary>
public record ProyectoListado(int IdProyecto, string Codigo, string Nombre, string Cliente, string Categoria, string Estado,
    string Responsable, DateOnly FechaInicio, DateOnly? FechaFin, int TareasTotal, int TareasCompletadas,
    decimal? Precio, decimal? Costo, decimal? Rentabilidad);

public class ProyectosIndexViewModel
{
    public List<ProyectoListado> Filas { get; set; } = new();
    public bool Total { get; set; }
    public Paginacion Paginacion { get; set; } = new();
}

public class ProyectoDetalleViewModel
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Cliente { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Responsable { get; set; } = "";
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }

    // Solo acceso total
    public bool VerFinanzas { get; set; }
    public bool PuedeEditar { get; set; }
    public int TotalTareas { get; set; }
    public int TareasCompletadas { get; set; }
    public Paginacion Paginacion { get; set; } = new();
    public decimal Precio { get; set; }
    public decimal Costo { get; set; }
    public decimal Rentabilidad => Precio - Costo;
    public decimal? Margen => Precio > 0 ? Rentabilidad / Precio : null;

    public List<TareaListado> Tareas { get; set; } = new();
}
