using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

public class ReportesController(ControlexDbContext contexto, ReporteService reportes, BitacoraService bitacora) : Controller
{
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string NombreActual => User.FindFirst("nombre_completo")?.Value ?? "";

    /// <summary>Con acceso parcial a REP_TAREAS (Empleado) solo se consultan las tareas propias.</summary>
    private bool SoloPropias => !(int.TryParse(User.FindFirst("perm:REP_TAREAS")?.Value, out var nivel) && nivel >= 2);

    // ---------- RF-005: proyectos y rentabilidad ----------
    [RequiereModulo("REP_RENTABILIDAD", 1)]
    public async Task<IActionResult> Rentabilidad(RentabilidadFiltro filtro, string? consultar)
    {
        var modeloVista = new RentabilidadViewModel { Filtro = filtro };
        if (consultar is not null)
        {
            var total = await reportes.ContarRentabilidadAsync(filtro);
            modeloVista.Paginacion = Paginacion.Crear("Reportes", nameof(Rentabilidad), filtro.Pagina, total,
                RutaRentabilidad(filtro));
            filtro.Pagina = modeloVista.Paginacion.Pagina;
            modeloVista.Filas = await reportes.RentabilidadAsync(filtro, filtro.Pagina);
            modeloVista.Resumen = await reportes.ResumirRentabilidadAsync(filtro, total);
            modeloVista.Consultado = true;
            await bitacora.MovimientoAsync(IdActual, "REP_RENTABILIDAD", "CONSULTA",
                Recortar(await reportes.DescribirAsync(filtro)));
        }
        await CargarProyectos(filtro.IdProyecto);
        return View(modeloVista);
    }

    [RequiereModulo("REP_RENTABILIDAD", 1)]
    public async Task<IActionResult> RentabilidadPdf(RentabilidadFiltro filtro)
    {
        var filas = await reportes.RentabilidadAsync(filtro);
        var descripcion = await reportes.DescribirAsync(filtro);
        await bitacora.MovimientoAsync(IdActual, "REP_RENTABILIDAD", "EXPORTAR PDF", Recortar(descripcion));
        return Pdf(ReportePdf.Rentabilidad(filas, descripcion, NombreActual), "rentabilidad");
    }

    // ---------- RF-024: eficiencia de tareas y pendientes ----------
    [RequiereModulo("REP_TAREAS", 1)]
    public async Task<IActionResult> Tareas(TareasFiltro filtro, string? consultar)
    {
        Normalizar(filtro);
        var modeloVista = new TareasReporteViewModel { Filtro = filtro, SoloPropias = SoloPropias };
        if (consultar is not null)
        {
            var total = await reportes.ContarTareasAsync(filtro);
            modeloVista.Paginacion = Paginacion.Crear("Reportes", nameof(Tareas), filtro.Pagina, total,
                RutaTareas(filtro));
            filtro.Pagina = modeloVista.Paginacion.Pagina;
            modeloVista.Filas = await reportes.TareasAsync(filtro, filtro.Pagina);
            modeloVista.Resumen = await reportes.ResumirTareasAsync(filtro, total);
            modeloVista.Consultado = true;
            await bitacora.MovimientoAsync(IdActual, "REP_TAREAS", "CONSULTA",
                Recortar(await reportes.DescribirAsync(filtro)));
        }
        await CargarFiltrosTareas(filtro);
        return View(modeloVista);
    }

    [RequiereModulo("REP_TAREAS", 1)]
    public async Task<IActionResult> TareasPdf(TareasFiltro filtro)
    {
        Normalizar(filtro);
        var filas = await reportes.TareasAsync(filtro);
        var descripcion = await reportes.DescribirAsync(filtro);
        await bitacora.MovimientoAsync(IdActual, "REP_TAREAS", "EXPORTAR PDF", Recortar(descripcion));
        return Pdf(ReportePdf.Tareas(filas, filtro.Tipo != "pendientes", descripcion, NombreActual),
                   filtro.Tipo == "pendientes" ? "tareas_pendientes" : "eficiencia_tareas");
    }

    // ---------- apoyo ----------
    private void Normalizar(TareasFiltro filtro)
    {
        filtro.Tipo = filtro.Tipo == "pendientes" ? "pendientes" : "eficiencia";
        if (SoloPropias) filtro.IdUsuario = IdActual;
    }

    private FileContentResult Pdf(byte[] contenido, string prefijo)
    {
        Response.Headers.ContentDisposition = $"inline; filename=\"{prefijo}_{DateTime.Now:yyyyMMdd_HHmm}.pdf\"";
        return File(contenido, "application/pdf");
    }

    private static string Recortar(string descripcion) => descripcion.Length <= 450 ? descripcion : descripcion[..450];

    private static RouteValueDictionary RutaRentabilidad(RentabilidadFiltro filtro)
    {
        var valoresRuta = new RouteValueDictionary { ["consultar"] = "1" };
        if (filtro.IdProyecto is int idProyecto) valoresRuta["IdProyecto"] = idProyecto;
        if (filtro.Desde is DateTime fechaDesde) valoresRuta["Desde"] = fechaDesde.ToString("yyyy-MM-dd");
        if (filtro.Hasta is DateTime fechaHasta) valoresRuta["Hasta"] = fechaHasta.ToString("yyyy-MM-dd");
        return valoresRuta;
    }

    private static RouteValueDictionary RutaTareas(TareasFiltro filtro)
    {
        var valoresRuta = new RouteValueDictionary { ["consultar"] = "1", ["Tipo"] = filtro.Tipo };
        if (filtro.IdProyecto is int idProyecto) valoresRuta["IdProyecto"] = idProyecto;
        if (filtro.IdUsuario is int idUsuario) valoresRuta["IdUsuario"] = idUsuario;
        if (filtro.IdEstado is int idEstado) valoresRuta["IdEstado"] = idEstado;
        if (filtro.Desde is DateTime fechaDesde) valoresRuta["Desde"] = fechaDesde.ToString("yyyy-MM-dd");
        if (filtro.Hasta is DateTime fechaHasta) valoresRuta["Hasta"] = fechaHasta.ToString("yyyy-MM-dd");
        return valoresRuta;
    }

    private async Task CargarProyectos(int? idProyectoSeleccionado) =>
        ViewBag.Proyectos = new SelectList(
            await contexto.Proyectos.AsNoTracking().OrderBy(proyecto => proyecto.Codigo)
                .Select(proyecto => new { proyecto.IdProyecto, Texto = proyecto.Codigo + " · " + proyecto.Nombre }).ToListAsync(),
            "IdProyecto", "Texto", idProyectoSeleccionado);

    private async Task CargarFiltrosTareas(TareasFiltro filtro)
    {
        await CargarProyectos(filtro.IdProyecto);
        ViewBag.Usuarios = new SelectList(
            await contexto.Usuarios.AsNoTracking().Where(usuario => usuario.Estado).OrderBy(usuario => usuario.Nombre)
                .Select(usuario => new { usuario.IdUsuario, Texto = usuario.Nombre + " " + usuario.Apellido1 }).ToListAsync(),
            "IdUsuario", "Texto", filtro.IdUsuario);
        ViewBag.Estados = new SelectList(
            await contexto.Estados.AsNoTracking().Where(estado => estado.TipoEstado == "TAREA")
                .OrderBy(estado => estado.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", filtro.IdEstado);
    }
}
