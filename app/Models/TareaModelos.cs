using System.ComponentModel.DataAnnotations;
namespace Controlex.Web.Models;

/// <summary>Formulario de crear y editar tarea (solo acceso total a TAREAS).</summary>
public class TareaViewModel
{
    public int IdTarea { get; set; }
    public string? Codigo { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre de la tarea."), StringLength(200)]
    [Display(Name = "Nombre de la tarea")]
    public string Nombre { get; set; } = "";

    [StringLength(1000)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el proyecto.")]
    [Display(Name = "Proyecto")]
    public int IdProyecto { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la categoría.")]
    [Display(Name = "Categoría")]
    public int IdCategoria { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el estado.")]
    [Display(Name = "Estado")]
    public int IdEstado { get; set; }

    [Display(Name = "Fecha de inicio")]
    public DateTime? FechaInicio { get; set; }

    [Display(Name = "Fecha de finalización")]
    public DateTime? FechaFin { get; set; }

    [Range(0, 9999, ErrorMessage = "Ingrese un tiempo estimado válido.")]
    [Display(Name = "Tiempo estimado (horas)")]
    public decimal TiempoEstimado { get; set; }

    [StringLength(1000)]
    [Display(Name = "Comentarios")]
    public string? Comentarios { get; set; }

    [Display(Name = "Usuarios asignados")]
    public List<int> IdsUsuarios { get; set; } = new();
}

public record OpcionUsuario(int Id, string Texto);

public record TareaListado(int IdTarea, string Codigo, string Nombre, string Proyecto, string Categoria, string Asignados,
    string Estado, DateOnly? FechaInicio, DateOnly? FechaFin, decimal Estimado, decimal Registrado)
{
    public bool Vencida => Estado != "Completada" && FechaFin is DateOnly fechaFin && fechaFin < DateOnly.FromDateTime(DateTime.Today);
}

public class TareasIndexViewModel
{
    public List<TareaListado> Filas { get; set; } = new();
    public bool Total { get; set; }
    public Paginacion Paginacion { get; set; } = new();
}

public record AsignadoItem(int IdUsuario, string Nombre, decimal Horas);

public record RegistroItem(int IdRegistro, DateOnly Fecha, int IdUsuario, string Usuario, decimal Horas,
    decimal CostoHora, string? Comentario, bool PuedeEliminar)
{
    public decimal Costo => Horas * CostoHora;
}

/// <summary>Ventana "Registro de tiempo y estado de tareas".</summary>
public class TareaDetalleViewModel
{
    public int IdTarea { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Proyecto { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Estado { get; set; } = "";
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public decimal Estimado { get; set; }
    public string? Comentarios { get; set; }
    public decimal TotalRegistrado { get; set; }
    public List<AsignadoItem> Asignados { get; set; } = new();
    public List<RegistroItem> Registros { get; set; } = new();
    public Paginacion Paginacion { get; set; } = new();

    public bool PuedeEditar { get; set; }      // acceso total a TAREAS
    public bool VerCostos { get; set; }        // el costo por hora solo lo ve quien tiene acceso total
    public bool ElegirUsuario { get; set; }    // el acceso total puede registrar tiempo a nombre de cualquier asignado
    public int IdUsuarioActual { get; set; }
    public bool EsAsignado { get; set; }
}
