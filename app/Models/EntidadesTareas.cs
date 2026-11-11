using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Controlex.Web.Models;

[Table("tareas")]
public class Tarea
{
    [Key, Column("id_tarea")] public int IdTarea { get; set; }
    [Column("codigo"), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public string Codigo { get; set; } = "";
    [Column("nombre")] public string Nombre { get; set; } = "";
    [Column("descripcion")] public string? Descripcion { get; set; }
    [Column("id_proyecto")] public int IdProyecto { get; set; }
    [Column("id_categoria")] public int IdCategoria { get; set; }
    [Column("id_estado")] public int IdEstado { get; set; }
    [Column("fecha_inicio")] public DateOnly? FechaInicio { get; set; }
    [Column("fecha_fin")] public DateOnly? FechaFin { get; set; }
    [Column("tiempo_estimado")] public decimal TiempoEstimado { get; set; }
    [Column("comentarios")] public string? Comentarios { get; set; }
    [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
    [ForeignKey(nameof(IdProyecto))] public Proyecto Proyecto { get; set; } = null!;
    [ForeignKey(nameof(IdCategoria))] public Categoria Categoria { get; set; } = null!;
    [ForeignKey(nameof(IdEstado))] public EstadoCatalogo EstadoActual { get; set; } = null!;
}

/// <summary>Asignación de un usuario a una tarea (clave compuesta, se configura en el DbContext).</summary>
[Table("tarea_usuarios")]
public class TareaUsuario
{
    [Column("id_tarea")] public int IdTarea { get; set; }
    [Column("id_usuario")] public int IdUsuario { get; set; }
    [Column("fecha_asignacion")] public DateTime FechaAsignacion { get; set; }
    [ForeignKey(nameof(IdTarea))] public Tarea Tarea { get; set; } = null!;
    [ForeignKey(nameof(IdUsuario))] public Usuario Usuario { get; set; } = null!;
}

[Table("registros_tiempo")]
public class RegistroTiempo
{
    [Key, Column("id_registro")] public int IdRegistro { get; set; }
    [Column("id_tarea")] public int IdTarea { get; set; }
    [Column("id_usuario")] public int IdUsuario { get; set; }
    [Column("fecha_registro")] public DateOnly FechaRegistro { get; set; }
    [Column("horas")] public decimal Horas { get; set; }
    [Column("costo_hora")] public decimal CostoHora { get; set; }   // costo por hora vigente al registrar
    [Column("comentario")] public string? Comentario { get; set; }
    [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
}
