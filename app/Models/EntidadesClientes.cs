using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Controlex.Web.Models;

[Table("tipos_cliente")]
public class TipoCliente
{
    [Key, Column("id_tipo_cliente")] public int IdTipoCliente { get; set; }
    [Column("nombre")] public string Nombre { get; set; } = "";
}

[Table("clientes")]
public class Cliente
{
    [Key, Column("id_cliente")] public int IdCliente { get; set; }
    [Column("id_tipo_cliente")] public int IdTipoCliente { get; set; }
    [Column("identificacion")] public string Identificacion { get; set; } = "";
    [Column("nombre_cliente")] public string NombreCliente { get; set; } = "";
    [Column("contacto")] public string? Contacto { get; set; }
    [Column("telefono1")] public string Telefono1 { get; set; } = "";
    [Column("telefono2")] public string? Telefono2 { get; set; }
    [Column("correo")] public string? Correo { get; set; }
    [Column("direccion")] public string? Direccion { get; set; }
    [Column("estado")] public bool Estado { get; set; }
    [ForeignKey(nameof(IdTipoCliente))] public TipoCliente TipoCliente { get; set; } = null!;
}
