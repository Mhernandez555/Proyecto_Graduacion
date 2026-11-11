using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Models;

// Tablas de apoyo y proyectos de CONTROLEX_DB (Entregable 4, 4.3.2.4).
[Table("estados")]
public class EstadoCatalogo
{
    [Key, Column("id_estado")] public int IdEstado { get; set; }
    [Column("tipo_estado")] public string TipoEstado { get; set; } = "";   // PROYECTO o TAREA
    [Column("nombre")] public string Nombre { get; set; } = "";
}

[Table("categorias")]
public class Categoria
{
    [Key, Column("id_categoria")] public int IdCategoria { get; set; }
    [Column("nombre")] public string Nombre { get; set; } = "";
    [Column("descripcion")] public string? Descripcion { get; set; }
    [Column("estado")] public bool Estado { get; set; }
}

[Table("proyectos")]
public class Proyecto
{
    [Key, Column("id_proyecto")] public int IdProyecto { get; set; }
    [Column("codigo"), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public string Codigo { get; set; } = "";
    [Column("nombre")] public string Nombre { get; set; } = "";
    [Column("descripcion")] public string? Descripcion { get; set; }
    [Column("id_cliente")] public int IdCliente { get; set; }
    [Column("id_categoria")] public int IdCategoria { get; set; }
    [Column("id_estado")] public int IdEstado { get; set; }
    [Column("id_responsable")] public int IdResponsable { get; set; }
    [Column("fecha_inicio")] public DateOnly FechaInicio { get; set; }
    [Column("fecha_fin")] public DateOnly? FechaFin { get; set; }
    [Column("precio_venta")] public decimal PrecioVenta { get; set; }
    [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
    [ForeignKey(nameof(IdCliente))] public Cliente Cliente { get; set; } = null!;
    [ForeignKey(nameof(IdCategoria))] public Categoria Categoria { get; set; } = null!;
    [ForeignKey(nameof(IdEstado))] public EstadoCatalogo EstadoActual { get; set; } = null!;
    [ForeignKey(nameof(IdResponsable))] public Usuario Responsable { get; set; } = null!;
}

/// <summary>Vista vw_rentabilidad_proyectos: costo acumulado y rentabilidad de cada proyecto (fuente de RF-005).</summary>
[Keyless]
public class VistaRentabilidad
{
    [Column("id_proyecto")] public int IdProyecto { get; set; }
    [Column("codigo")] public string Codigo { get; set; } = "";
    [Column("proyecto")] public string Proyecto { get; set; } = "";
    [Column("nombre_cliente")] public string NombreCliente { get; set; } = "";
    [Column("estado")] public string Estado { get; set; } = "";
    [Column("fecha_inicio")] public DateOnly FechaInicio { get; set; }
    [Column("fecha_fin")] public DateOnly? FechaFin { get; set; }
    [Column("precio_venta")] public decimal PrecioVenta { get; set; }
    [Column("costo_acumulado")] public decimal CostoAcumulado { get; set; }
    [Column("rentabilidad")] public decimal Rentabilidad { get; set; }
}
