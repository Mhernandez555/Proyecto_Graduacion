using Controlex.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Services;

/// <summary>Consultas de los reportes. Las usan tanto la pantalla como la exportación a PDF.</summary>
public class ReporteService(ControlexDbContext contexto)
{
    // ---------- RF-005: proyectos y rentabilidad ----------
    public async Task<List<FilaRentabilidad>> RentabilidadAsync(RentabilidadFiltro filtro, int? pagina = null)
    {
        IQueryable<VistaRentabilidad> consulta = ConsultaRentabilidad(filtro).OrderBy(proyecto => proyecto.Codigo);
        if (pagina is int paginaActual)
            consulta = consulta.Skip((paginaActual - 1) * Paginacion.FilasPorPagina)
                .Take(Paginacion.FilasPorPagina);

        var proyectos = await consulta.ToListAsync();
        return proyectos.Select(proyecto => new FilaRentabilidad(proyecto.Codigo, proyecto.Proyecto, proyecto.NombreCliente, proyecto.Estado,
            proyecto.FechaInicio, proyecto.FechaFin, proyecto.PrecioVenta, proyecto.CostoAcumulado, proyecto.Rentabilidad)).ToList();
    }

    public Task<int> ContarRentabilidadAsync(RentabilidadFiltro filtro) =>
        ConsultaRentabilidad(filtro).CountAsync();

    public async Task<ResumenRentabilidad> ResumirRentabilidadAsync(RentabilidadFiltro filtro, int cantidad)
    {
        var consulta = ConsultaRentabilidad(filtro);
        var ventas = await consulta.SumAsync(proyecto => (decimal?)proyecto.PrecioVenta) ?? 0m;
        var costos = await consulta.SumAsync(proyecto => (decimal?)proyecto.CostoAcumulado) ?? 0m;
        var rentabilidad = await consulta.SumAsync(proyecto => (decimal?)proyecto.Rentabilidad) ?? 0m;
        return new ResumenRentabilidad(cantidad, ventas, costos, rentabilidad);
    }

    public async Task<string> DescribirAsync(RentabilidadFiltro filtro)
    {
        var proyecto = filtro.IdProyecto is int idProyecto
            ? (await contexto.Proyectos.Where(proyecto => proyecto.IdProyecto == idProyecto)
                .Select(proyecto => proyecto.Codigo + " " + proyecto.Nombre).FirstOrDefaultAsync() ?? "—")
            : "todos";
        return $"Proyecto: {proyecto}  |  Período (fecha de inicio): {Periodo(filtro.Desde, filtro.Hasta)}";
    }

    // ---------- RF-024: eficiencia de tareas y pendientes ----------
    public async Task<List<FilaTarea>> TareasAsync(TareasFiltro filtro, int? pagina = null)
    {
        var consultaOrdenada = ConsultaTareas(filtro);
        IQueryable<Tarea> consultaPagina = filtro.Tipo == "pendientes"
            ? consultaOrdenada.OrderBy(tarea => tarea.FechaFin == null).ThenBy(tarea => tarea.FechaFin).ThenBy(tarea => tarea.Codigo)
            : consultaOrdenada.OrderBy(tarea => tarea.Proyecto.Nombre).ThenBy(tarea => tarea.Codigo);
        if (pagina is int paginaActual)
            consultaPagina = consultaPagina.Skip((paginaActual - 1) * Paginacion.FilasPorPagina)
                .Take(Paginacion.FilasPorPagina);

        var tareas = await consultaPagina.Select(tarea => new
        {
            tarea.IdTarea, tarea.Codigo, tarea.Nombre,
            Proyecto = tarea.Proyecto.Nombre, Categoria = tarea.Categoria.Nombre, Estado = tarea.EstadoActual.Nombre,
            tarea.FechaInicio, tarea.FechaFin, tarea.TiempoEstimado
        }).ToListAsync();

        var identificadoresTarea = tareas.Select(tarea => tarea.IdTarea).ToList();
        var asignados = (await contexto.TareaUsuarios.AsNoTracking().Where(asignacion => identificadoresTarea.Contains(asignacion.IdTarea))
            .Select(asignacion => new { asignacion.IdTarea, Nombre = asignacion.Usuario.Nombre + " " + asignacion.Usuario.Apellido1 }).ToListAsync())
            .ToLookup(asignacion => asignacion.IdTarea, asignacion => asignacion.Nombre);
        var horasPorTarea = await contexto.RegistrosTiempo.AsNoTracking().Where(registro => identificadoresTarea.Contains(registro.IdTarea))
            .GroupBy(registro => registro.IdTarea)
            .Select(grupo => new { IdTarea = grupo.Key, Horas = grupo.Sum(registro => registro.Horas) })
            .ToDictionaryAsync(resumen => resumen.IdTarea, resumen => resumen.Horas);

        var filas = tareas.Select(tarea => new FilaTarea(tarea.Codigo, tarea.Nombre, tarea.Proyecto, tarea.Categoria,
            string.Join(", ", asignados[tarea.IdTarea]), tarea.Estado, tarea.FechaInicio, tarea.FechaFin,
            tarea.TiempoEstimado, horasPorTarea.GetValueOrDefault(tarea.IdTarea, 0m), tarea.Estado != "Completada"));

        return filas.ToList();
    }

    public Task<int> ContarTareasAsync(TareasFiltro filtro) =>
        ConsultaTareas(filtro).CountAsync();

    public async Task<ResumenTareas> ResumirTareasAsync(TareasFiltro filtro, int cantidad)
    {
        var consulta = ConsultaTareas(filtro);
        var identificadoresTareas = consulta.Select(tarea => tarea.IdTarea);
        var horasPorTarea = contexto.RegistrosTiempo.AsNoTracking()
            .Where(registro => identificadoresTareas.Contains(registro.IdTarea))
            .GroupBy(registro => registro.IdTarea)
            .Select(grupo => new { IdTarea = grupo.Key, Horas = grupo.Sum(registro => registro.Horas) });

        var estimado = await consulta.SumAsync(tarea => (decimal?)tarea.TiempoEstimado) ?? 0m;
        var identificadoresConTiempo = horasPorTarea.Where(resumen => resumen.Horas > 0)
            .Select(resumen => resumen.IdTarea);
        var estimadoConTiempo = await consulta.Where(tarea => identificadoresConTiempo.Contains(tarea.IdTarea))
            .SumAsync(tarea => (decimal?)tarea.TiempoEstimado) ?? 0m;
        var registrado = await horasPorTarea.SumAsync(resumen => (decimal?)resumen.Horas) ?? 0m;
        return new ResumenTareas(cantidad, estimado, registrado, estimadoConTiempo, registrado);
    }

    private IQueryable<VistaRentabilidad> ConsultaRentabilidad(RentabilidadFiltro filtro)
    {
        var consulta = contexto.RentabilidadProyectos.AsNoTracking().AsQueryable();
        if (filtro.IdProyecto is int idProyecto) consulta = consulta.Where(proyecto => proyecto.IdProyecto == idProyecto);
        if (filtro.Desde is DateTime fechaInicial)
        {
            var fechaDesde = DateOnly.FromDateTime(fechaInicial);
            consulta = consulta.Where(proyecto => proyecto.FechaInicio >= fechaDesde);
        }
        if (filtro.Hasta is DateTime fechaFinal)
        {
            var fechaHasta = DateOnly.FromDateTime(fechaFinal);
            consulta = consulta.Where(proyecto => proyecto.FechaInicio <= fechaHasta);
        }
        return consulta;
    }

    private IQueryable<Tarea> ConsultaTareas(TareasFiltro filtro)
    {
        var consulta = contexto.Tareas.AsNoTracking().AsQueryable();
        if (filtro.IdProyecto is int idProyecto) consulta = consulta.Where(tarea => tarea.IdProyecto == idProyecto);
        if (filtro.IdEstado is int idEstado) consulta = consulta.Where(tarea => tarea.IdEstado == idEstado);
        if (filtro.IdUsuario is int idUsuario)
            consulta = consulta.Where(tarea => contexto.TareaUsuarios.Any(asignacion =>
                asignacion.IdTarea == tarea.IdTarea && asignacion.IdUsuario == idUsuario));
        if (filtro.Desde is DateTime fechaInicial)
        {
            var fechaDesde = DateOnly.FromDateTime(fechaInicial);
            consulta = consulta.Where(tarea => (tarea.FechaFin ?? tarea.FechaInicio) >= fechaDesde);
        }
        if (filtro.Hasta is DateTime fechaFinal)
        {
            var fechaHasta = DateOnly.FromDateTime(fechaFinal);
            consulta = consulta.Where(tarea => (tarea.FechaFin ?? tarea.FechaInicio) <= fechaHasta);
        }
        if (filtro.Tipo == "pendientes") consulta = consulta.Where(tarea => tarea.EstadoActual.Nombre != "Completada");
        return consulta;
    }

    public async Task<string> DescribirAsync(TareasFiltro filtro)
    {
        var tipo = filtro.Tipo == "pendientes" ? "Tareas pendientes" : "Eficiencia de tareas";
        var proyecto = filtro.IdProyecto is int idProyecto
            ? (await contexto.Proyectos.Where(proyecto => proyecto.IdProyecto == idProyecto)
                .Select(proyecto => proyecto.Codigo + " " + proyecto.Nombre).FirstOrDefaultAsync() ?? "—")
            : "todos";
        var usuario = filtro.IdUsuario is int idUsuario
            ? (await contexto.Usuarios.Where(usuario => usuario.IdUsuario == idUsuario)
                .Select(usuario => usuario.Nombre + " " + usuario.Apellido1).FirstOrDefaultAsync() ?? "—")
            : "todos";
        var estado = filtro.IdEstado is int idEstado
            ? (await contexto.Estados.Where(estado => estado.IdEstado == idEstado)
                .Select(estado => estado.Nombre).FirstOrDefaultAsync() ?? "—")
            : "todos";
        return $"{tipo}  |  Proyecto: {proyecto}  |  Usuario: {usuario}  |  Estado: {estado}  |  Período: {Periodo(filtro.Desde, filtro.Hasta)}";
    }

    private static string Periodo(DateTime? desde, DateTime? hasta) => (desde, hasta) switch
    {
        (not null, not null) => $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}",
        (not null, null) => $"desde {desde:dd/MM/yyyy}",
        (null, not null) => $"hasta {hasta:dd/MM/yyyy}",
        _ => "sin límite"
    };
}
