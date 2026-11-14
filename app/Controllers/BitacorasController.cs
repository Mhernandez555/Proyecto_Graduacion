using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

/// <summary>Consulta de bitácoras (RF-007, RF-011, RF-012). Al abrir se muestran los últimos registros.</summary>
[RequiereModulo("BITACORAS", 1)]
public class BitacorasController(ControlexDbContext db, BitacoraService bitacora) : Controller
{
    private const int Tam = 50;
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ---------- RF-011: ingresos y salidas ----------
    public async Task<IActionResult> Ingresos(IngresosFiltro f, string? consultar)
    {
        var q = from b in db.BitacoraIngresos.AsNoTracking()
                join u in db.Usuarios.AsNoTracking() on b.IdUsuario equals u.IdUsuario
                select new { b, u };
        if (f.IdUsuario is int id) q = q.Where(x => x.b.IdUsuario == id);
        if (f.Desde is DateTime d) { var desde = d.Date; q = q.Where(x => x.b.FechaHoraEntrada >= desde); }
        if (f.Hasta is DateTime h) { var hasta = h.Date.AddDays(1); q = q.Where(x => x.b.FechaHoraEntrada < hasta); }

        var pag = new Paginacion { Accion = nameof(Ingresos), Tam = Tam, Total = await q.CountAsync(), Filtros = Ruta(f.IdUsuario, f.Desde, f.Hasta) };
        pag.Pagina = Math.Clamp(f.Pagina, 1, pag.TotalPaginas);

        var filas = await q.OrderByDescending(x => x.b.FechaHoraEntrada).ThenByDescending(x => x.b.IdBitacoraIngreso)
            .Skip((pag.Pagina - 1) * Tam).Take(Tam)
            .Select(x => new FilaIngreso(x.b.IdBitacoraIngreso, x.b.DireccionIp, x.u.NombreUsuario,
                x.u.Nombre + " " + x.u.Apellido1, x.b.FechaHoraEntrada, x.b.FechaHoraSalida))
            .ToListAsync();

        if (consultar is not null)
            await bitacora.MovimientoAsync(IdActual, "BITACORAS", "CONSULTA", "Bitácora de ingresos");
        await CargarUsuarios(f.IdUsuario);
        return View(new IngresosViewModel { Filtro = f, Filas = filas, Paginacion = pag });
    }

    // ---------- RF-012: movimientos ----------
    public async Task<IActionResult> Movimientos(MovimientosFiltro f, string? consultar)
    {
        var q = from m in db.BitacoraMovimientos.AsNoTracking()
                join u in db.Usuarios.AsNoTracking() on m.IdUsuario equals u.IdUsuario
                select new { m, u };
        if (f.IdUsuario is int id) q = q.Where(x => x.m.IdUsuario == id);
        if (!string.IsNullOrEmpty(f.TipoMovimiento)) q = q.Where(x => x.m.TipoMovimiento == f.TipoMovimiento);
        if (!string.IsNullOrEmpty(f.Modulo)) q = q.Where(x => x.m.Modulo == f.Modulo);
        if (f.Desde is DateTime d) { var desde = d.Date; q = q.Where(x => x.m.FechaHora >= desde); }
        if (f.Hasta is DateTime h) { var hasta = h.Date.AddDays(1); q = q.Where(x => x.m.FechaHora < hasta); }

        var pag = new Paginacion
        {
            Accion = nameof(Movimientos), Tam = Tam, Total = await q.CountAsync(),
            Filtros = Ruta(f.IdUsuario, f.Desde, f.Hasta, f.TipoMovimiento, f.Modulo)
        };
        pag.Pagina = Math.Clamp(f.Pagina, 1, pag.TotalPaginas);

        var filas = await q.OrderByDescending(x => x.m.FechaHora).ThenByDescending(x => x.m.IdMovimiento)
            .Skip((pag.Pagina - 1) * Tam).Take(Tam)
            .Select(x => new FilaMovimiento(x.m.IdMovimiento, x.m.DireccionIp, x.u.NombreUsuario,
                x.u.Nombre + " " + x.u.Apellido1, x.m.FechaHora, x.m.Modulo, x.m.TipoMovimiento, x.m.Detalle))
            .ToListAsync();

        if (consultar is not null)
            await bitacora.MovimientoAsync(IdActual, "BITACORAS", "CONSULTA", "Bitácora de movimientos");
        await CargarUsuarios(f.IdUsuario);
        ViewBag.Tipos = new SelectList(await db.BitacoraMovimientos.AsNoTracking()
            .Select(m => m.TipoMovimiento).Distinct().OrderBy(t => t).ToListAsync(), f.TipoMovimiento);
        ViewBag.Modulos = new SelectList(await db.BitacoraMovimientos.AsNoTracking()
            .Select(m => m.Modulo).Distinct().OrderBy(m => m).ToListAsync(), f.Modulo);
        return View(new MovimientosViewModel { Filtro = f, Filas = filas, Paginacion = pag });
    }

    // ---------- apoyo ----------
    private static RouteValueDictionary Ruta(int? idUsuario, DateTime? desde, DateTime? hasta, string? tipo = null, string? modulo = null)
    {
        var r = new RouteValueDictionary();
        if (idUsuario is int u) r["IdUsuario"] = u;
        if (!string.IsNullOrEmpty(tipo)) r["TipoMovimiento"] = tipo;
        if (!string.IsNullOrEmpty(modulo)) r["Modulo"] = modulo;
        if (desde is DateTime d) r["Desde"] = d.ToString("yyyy-MM-dd");
        if (hasta is DateTime h) r["Hasta"] = h.ToString("yyyy-MM-dd");
        return r;
    }

    private async Task CargarUsuarios(int? seleccionado) =>
        ViewBag.Usuarios = new SelectList(
            await db.Usuarios.AsNoTracking().OrderBy(u => u.Nombre)
                .Select(u => new { u.IdUsuario, Texto = u.Nombre + " " + u.Apellido1 + " (" + u.NombreUsuario + ")" }).ToListAsync(),
            "IdUsuario", "Texto", seleccionado);
}
