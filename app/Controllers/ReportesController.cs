using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

public class ReportesController(ControlexDbContext db, ReporteService reportes, BitacoraService bitacora) : Controller
{
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string NombreActual => User.FindFirst("nombre_completo")?.Value ?? "";

    /// <summary>Con acceso parcial a REP_TAREAS (Empleado) solo se consultan las tareas propias.</summary>
    private bool SoloPropias => !(int.TryParse(User.FindFirst("perm:REP_TAREAS")?.Value, out var n) && n >= 2);

    // ---------- RF-005: proyectos y rentabilidad ----------
    [RequiereModulo("REP_RENTABILIDAD", 1)]
    public async Task<IActionResult> Rentabilidad(RentabilidadFiltro f, string? consultar)
    {
        var vm = new RentabilidadViewModel { Filtro = f };
        if (consultar is not null)
        {
            vm.Filas = await reportes.RentabilidadAsync(f);
            vm.Consultado = true;
            await bitacora.MovimientoAsync(IdActual, "REP_RENTABILIDAD", "CONSULTA", Recortar(await reportes.DescribirAsync(f)));
        }
        await CargarProyectos(f.IdProyecto);
        return View(vm);
    }

    [RequiereModulo("REP_RENTABILIDAD", 1)]
    public async Task<IActionResult> RentabilidadPdf(RentabilidadFiltro f)
    {
        var filas = await reportes.RentabilidadAsync(f);
        var descripcion = await reportes.DescribirAsync(f);
        await bitacora.MovimientoAsync(IdActual, "REP_RENTABILIDAD", "EXPORTAR PDF", Recortar(descripcion));
        return Pdf(ReportePdf.Rentabilidad(filas, descripcion, NombreActual), "rentabilidad");
    }

    // ---------- RF-024: eficiencia de tareas y pendientes ----------
    [RequiereModulo("REP_TAREAS", 1)]
    public async Task<IActionResult> Tareas(TareasFiltro f, string? consultar)
    {
        Normalizar(f);
        var vm = new TareasReporteViewModel { Filtro = f, SoloPropias = SoloPropias };
        if (consultar is not null)
        {
            vm.Filas = await reportes.TareasAsync(f);
            vm.Consultado = true;
            await bitacora.MovimientoAsync(IdActual, "REP_TAREAS", "CONSULTA", Recortar(await reportes.DescribirAsync(f)));
        }
        await CargarFiltrosTareas(f);
        return View(vm);
    }

    [RequiereModulo("REP_TAREAS", 1)]
    public async Task<IActionResult> TareasPdf(TareasFiltro f)
    {
        Normalizar(f);
        var filas = await reportes.TareasAsync(f);
        var descripcion = await reportes.DescribirAsync(f);
        await bitacora.MovimientoAsync(IdActual, "REP_TAREAS", "EXPORTAR PDF", Recortar(descripcion));
        return Pdf(ReportePdf.Tareas(filas, f.Tipo != "pendientes", descripcion, NombreActual),
                   f.Tipo == "pendientes" ? "tareas_pendientes" : "eficiencia_tareas");
    }

    // ---------- apoyo ----------
    private void Normalizar(TareasFiltro f)
    {
        f.Tipo = f.Tipo == "pendientes" ? "pendientes" : "eficiencia";
        if (SoloPropias) f.IdUsuario = IdActual;
    }

    private FileContentResult Pdf(byte[] bytes, string prefijo)
    {
        Response.Headers.ContentDisposition = $"inline; filename=\"{prefijo}_{DateTime.Now:yyyyMMdd_HHmm}.pdf\"";
        return File(bytes, "application/pdf");
    }

    private static string Recortar(string texto) => texto.Length <= 450 ? texto : texto[..450];

    private async Task CargarProyectos(int? seleccionado) =>
        ViewBag.Proyectos = new SelectList(
            await db.Proyectos.AsNoTracking().OrderBy(p => p.Codigo)
                .Select(p => new { p.IdProyecto, Texto = p.Codigo + " · " + p.Nombre }).ToListAsync(),
            "IdProyecto", "Texto", seleccionado);

    private async Task CargarFiltrosTareas(TareasFiltro f)
    {
        await CargarProyectos(f.IdProyecto);
        ViewBag.Usuarios = new SelectList(
            await db.Usuarios.AsNoTracking().Where(u => u.Estado).OrderBy(u => u.Nombre)
                .Select(u => new { u.IdUsuario, Texto = u.Nombre + " " + u.Apellido1 }).ToListAsync(),
            "IdUsuario", "Texto", f.IdUsuario);
        ViewBag.Estados = new SelectList(
            await db.Estados.AsNoTracking().Where(e => e.TipoEstado == "TAREA").OrderBy(e => e.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", f.IdEstado);
    }
}
