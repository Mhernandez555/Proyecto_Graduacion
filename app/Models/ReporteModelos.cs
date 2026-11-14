namespace Controlex.Web.Models;

public class RentabilidadFiltro
{
    public int? IdProyecto { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Pagina { get; set; } = 1;
}

public class TareasFiltro
{
    public string Tipo { get; set; } = "eficiencia";   // eficiencia | pendientes
    public int? IdProyecto { get; set; }
    public int? IdUsuario { get; set; }
    public int? IdEstado { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Pagina { get; set; } = 1;
}

public record FilaRentabilidad(string Codigo, string Proyecto, string Cliente, string Estado,
    DateOnly FechaInicio, DateOnly? FechaFin, decimal PrecioVenta, decimal Costo, decimal Rentabilidad)
{
    /// <summary>Rentabilidad sobre el precio de venta.</summary>
    public decimal? Margen => PrecioVenta > 0 ? Rentabilidad / PrecioVenta : null;
}

public record FilaTarea(string Codigo, string Nombre, string Proyecto, string Categoria, string Usuarios, string Estado,
    DateOnly? FechaInicio, DateOnly? FechaFin, decimal Estimado, decimal Registrado, bool Pendiente)
{
    /// <summary>Horas estimadas menos horas registradas (negativo = se excedió).</summary>
    public decimal Diferencia => Estimado - Registrado;

    /// <summary>Estimado / registrado. 100 % o más: dentro de lo estimado. Sin horas registradas no se calcula.</summary>
    public decimal? Eficiencia => Registrado > 0 ? Estimado / Registrado : null;

    public bool Vencida => Pendiente && FechaFin is DateOnly fechaFin && fechaFin < DateOnly.FromDateTime(DateTime.Today);
}

public record ResumenRentabilidad(int Cantidad, decimal Ventas, decimal Costos, decimal Rentabilidad);

public record ResumenTareas(int Cantidad, decimal Estimado, decimal Registrado,
    decimal EstimadoConTiempo, decimal RegistradoConTiempo);

public class RentabilidadViewModel
{
    public RentabilidadFiltro Filtro { get; set; } = new();
    public List<FilaRentabilidad> Filas { get; set; } = new();
    public bool Consultado { get; set; }
    public ResumenRentabilidad Resumen { get; set; } = new(0, 0m, 0m, 0m);
    public Paginacion Paginacion { get; set; } = new();
}

public class TareasReporteViewModel
{
    public TareasFiltro Filtro { get; set; } = new();
    public List<FilaTarea> Filas { get; set; } = new();
    public bool Consultado { get; set; }
    public bool SoloPropias { get; set; }
    public ResumenTareas Resumen { get; set; } = new(0, 0m, 0m, 0m, 0m);
    public Paginacion Paginacion { get; set; } = new();
}
