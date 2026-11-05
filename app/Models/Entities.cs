using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Controlex.Web.Models;

// Nombres de tablas y columnas = CONTROLEX_DB (Entregable 4, 4.3.2.4). Por ahora: seguridad y bitácoras.
[Table("roles")]
public class Rol
{
    [Key, Column("id_rol")] public int IdRol { get; set; }
    [Column("nombre_rol")] public string NombreRol { get; set; } = "";
    [Column("descripcion")] public string? Descripcion { get; set; }
    [Column("es_administrador")] public bool EsAdministrador { get; set; }
    [Column("estado")] public bool Estado { get; set; }
}

[Table("permisos")]
public class Permiso
{
    [Key, Column("id_permiso")] public int IdPermiso { get; set; }
    [Column("id_rol")] public int IdRol { get; set; }
    [Column("modulo")] public string Modulo { get; set; } = "";
    [Column("nivel_acceso")] public byte NivelAcceso { get; set; }
}

[Table("usuarios")]
public class Usuario
{
    [Key, Column("id_usuario")] public int IdUsuario { get; set; }
    [Column("cedula")] public string Cedula { get; set; } = "";
    [Column("nombre")] public string Nombre { get; set; } = "";
    [Column("apellido1")] public string Apellido1 { get; set; } = "";
    [Column("apellido2")] public string? Apellido2 { get; set; }
    [Column("correo")] public string Correo { get; set; } = "";
    [Column("id_rol")] public int IdRol { get; set; }
    [Column("nombre_usuario")] public string NombreUsuario { get; set; } = "";
    [Column("contrasena_hash")] public string ContrasenaHash { get; set; } = "";
    [Column("requiere_cambio_contrasena")] public bool RequiereCambioContrasena { get; set; }
    [Column("costo_hora")] public decimal CostoHora { get; set; }
    [Column("estado")] public bool Estado { get; set; }
    [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
    [ForeignKey(nameof(IdRol))] public Rol Rol { get; set; } = null!;
}

[Table("bitacora_ingresos")]
public class BitacoraIngreso
{
    [Key, Column("id_bitacora_ingreso")] public int IdBitacoraIngreso { get; set; }
    [Column("id_usuario")] public int IdUsuario { get; set; }
    [Column("direccion_ip")] public string DireccionIp { get; set; } = "";
    [Column("fecha_hora_entrada")] public DateTime FechaHoraEntrada { get; set; }
    [Column("fecha_hora_salida")] public DateTime? FechaHoraSalida { get; set; }
}

[Table("bitacora_movimientos")]
public class BitacoraMovimiento
{
    [Key, Column("id_movimiento")] public int IdMovimiento { get; set; }
    [Column("id_usuario")] public int IdUsuario { get; set; }
    [Column("direccion_ip")] public string DireccionIp { get; set; } = "";
    [Column("fecha_hora")] public DateTime FechaHora { get; set; }
    [Column("modulo")] public string Modulo { get; set; } = "";
    [Column("tipo_movimiento")] public string TipoMovimiento { get; set; } = "";
    [Column("detalle")] public string? Detalle { get; set; }
}
