using Microsoft.AspNetCore.Routing;
namespace Controlex.Web.Models;

public class IngresosFiltro
{
    public int? IdUsuario { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Pagina { get; set; } = 1;
}

public class MovimientosFiltro
{
    public int? IdUsuario { get; set; }
    public string? TipoMovimiento { get; set; }
    public string? Modulo { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Pagina { get; set; } = 1;
}

public record FilaIngreso(int Id, string Ip, string Usuario, string Nombre, DateTime Entrada, DateTime? Salida);

public record FilaMovimiento(int Id, string Ip, string Usuario, string Nombre, DateTime FechaHora,
    string Modulo, string Tipo, string? Detalle);

/// <summary>Datos para los botones Anterior / Siguiente, conservando los filtros.</summary>
public class Paginacion
{
    public string Accion { get; set; } = "";
    public string Controlador { get; set; } = "Bitacoras";
    public int Pagina { get; set; } = 1;
    public int Tam { get; set; } = 50;
    public int Total { get; set; }
    public RouteValueDictionary Filtros { get; set; } = new();

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)Tam));
    public int Desde => Total == 0 ? 0 : (Pagina - 1) * Tam + 1;
    public int Hasta => Math.Min(Total, Pagina * Tam);

    public RouteValueDictionary Ruta(int pagina)
    {
        var r = new RouteValueDictionary(Filtros);
        r["Pagina"] = pagina;
        return r;
    }
}

public class IngresosViewModel
{
    public IngresosFiltro Filtro { get; set; } = new();
    public List<FilaIngreso> Filas { get; set; } = new();
    public Paginacion Paginacion { get; set; } = new();
}

public class MovimientosViewModel
{
    public MovimientosFiltro Filtro { get; set; } = new();
    public List<FilaMovimiento> Filas { get; set; } = new();
    public Paginacion Paginacion { get; set; } = new();
}
