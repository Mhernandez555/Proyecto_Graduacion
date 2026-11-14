namespace Controlex.Web.Models;

public record TareaResumen(string Codigo, string Nombre, string Proyecto, string Estado, DateOnly? FechaFin)
{
    public bool Vencida => Estado != "Completada" && FechaFin is DateOnly fechaFin && fechaFin < DateOnly.FromDateTime(DateTime.Today);
}

public record ProyectoResumen(string Codigo, string Nombre, string Cliente, string Estado, DateOnly? FechaFin);

public class InicioViewModel
{
    // Tareas del usuario que inició sesión
    public int MisPendientes { get; set; }
    public int MisEnProceso { get; set; }
    public int MisVencidas { get; set; }
    public List<TareaResumen> ProximasTareas { get; set; } = new();

    // Vista del equipo: solo para quien tiene acceso total a proyectos
    public bool VeEquipo { get; set; }
    public int ProyectosEnEjecucion { get; set; }
    public int TareasPendientesEquipo { get; set; }
    public List<ProyectoResumen> ProyectosActivos { get; set; } = new();
}
