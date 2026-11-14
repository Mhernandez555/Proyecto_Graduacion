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
public class BitacorasController(ControlexDbContext contexto, BitacoraService bitacora) : Controller
{
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ---------- RF-011: ingresos y salidas ----------
    public async Task<IActionResult> Ingresos(IngresosFiltro filtro, string? consultar)
    {
        var consulta = from ingreso in contexto.BitacoraIngresos.AsNoTracking()
                       join usuario in contexto.Usuarios.AsNoTracking() on ingreso.IdUsuario equals usuario.IdUsuario
                       select new { ingreso, usuario };
        if (filtro.IdUsuario is int idUsuario) consulta = consulta.Where(registro => registro.ingreso.IdUsuario == idUsuario);
        if (filtro.Desde is DateTime fechaInicial)
        {
            var fechaDesde = fechaInicial.Date;
            consulta = consulta.Where(registro => registro.ingreso.FechaHoraEntrada >= fechaDesde);
        }
        if (filtro.Hasta is DateTime fechaFinal)
        {
            var fechaHasta = fechaFinal.Date.AddDays(1);
            consulta = consulta.Where(registro => registro.ingreso.FechaHoraEntrada < fechaHasta);
        }

        var paginacion = Paginacion.Crear("Bitacoras", nameof(Ingresos), filtro.Pagina,
            await consulta.CountAsync(), Ruta(filtro.IdUsuario, filtro.Desde, filtro.Hasta));

        var filas = await consulta.OrderByDescending(registro => registro.ingreso.FechaHoraEntrada)
            .ThenByDescending(registro => registro.ingreso.IdBitacoraIngreso)
            .Skip((paginacion.Pagina - 1) * paginacion.Tam).Take(paginacion.Tam)
            .Select(registro => new FilaIngreso(registro.ingreso.IdBitacoraIngreso, registro.ingreso.DireccionIp,
                registro.usuario.NombreUsuario, registro.usuario.Nombre + " " + registro.usuario.Apellido1,
                registro.ingreso.FechaHoraEntrada, registro.ingreso.FechaHoraSalida, registro.ingreso.MotivoSalida))
            .ToListAsync();

        if (consultar is not null)
            await bitacora.MovimientoAsync(IdActual, "BITACORAS", "CONSULTA", "Bitácora de ingresos");
        await CargarUsuarios(filtro.IdUsuario);
        return View(new IngresosViewModel { Filtro = filtro, Filas = filas, Paginacion = paginacion });
    }

    // ---------- RF-012: movimientos ----------
    public async Task<IActionResult> Movimientos(MovimientosFiltro filtro, string? consultar)
    {
        var consulta = from movimiento in contexto.BitacoraMovimientos.AsNoTracking()
                       join usuario in contexto.Usuarios.AsNoTracking() on movimiento.IdUsuario equals usuario.IdUsuario
                       select new { movimiento, usuario };
        if (filtro.IdUsuario is int idUsuario) consulta = consulta.Where(registro => registro.movimiento.IdUsuario == idUsuario);
        if (!string.IsNullOrEmpty(filtro.TipoMovimiento))
            consulta = consulta.Where(registro => registro.movimiento.TipoMovimiento == filtro.TipoMovimiento);
        if (!string.IsNullOrEmpty(filtro.Modulo))
            consulta = consulta.Where(registro => registro.movimiento.Modulo == filtro.Modulo);
        if (filtro.Desde is DateTime fechaInicial)
        {
            var fechaDesde = fechaInicial.Date;
            consulta = consulta.Where(registro => registro.movimiento.FechaHora >= fechaDesde);
        }
        if (filtro.Hasta is DateTime fechaFinal)
        {
            var fechaHasta = fechaFinal.Date.AddDays(1);
            consulta = consulta.Where(registro => registro.movimiento.FechaHora < fechaHasta);
        }

        var paginacion = Paginacion.Crear("Bitacoras", nameof(Movimientos), filtro.Pagina,
            await consulta.CountAsync(), Ruta(filtro.IdUsuario, filtro.Desde, filtro.Hasta, filtro.TipoMovimiento, filtro.Modulo));

        var filas = await consulta.OrderByDescending(registro => registro.movimiento.FechaHora)
            .ThenByDescending(registro => registro.movimiento.IdMovimiento)
            .Skip((paginacion.Pagina - 1) * paginacion.Tam).Take(paginacion.Tam)
            .Select(registro => new FilaMovimiento(registro.movimiento.IdMovimiento, registro.movimiento.DireccionIp,
                registro.usuario.NombreUsuario, registro.usuario.Nombre + " " + registro.usuario.Apellido1,
                registro.movimiento.FechaHora, registro.movimiento.Modulo,
                registro.movimiento.TipoMovimiento, registro.movimiento.Detalle))
            .ToListAsync();

        if (consultar is not null)
            await bitacora.MovimientoAsync(IdActual, "BITACORAS", "CONSULTA", "Bitácora de movimientos");
        await CargarUsuarios(filtro.IdUsuario);
        ViewBag.Tipos = new SelectList(await contexto.BitacoraMovimientos.AsNoTracking()
            .Select(movimiento => movimiento.TipoMovimiento).Distinct().OrderBy(tipo => tipo).ToListAsync(), filtro.TipoMovimiento);
        ViewBag.Modulos = new SelectList(await contexto.BitacoraMovimientos.AsNoTracking()
            .Select(movimiento => movimiento.Modulo).Distinct().OrderBy(modulo => modulo).ToListAsync(), filtro.Modulo);
        return View(new MovimientosViewModel { Filtro = filtro, Filas = filas, Paginacion = paginacion });
    }

    // ---------- apoyo ----------
    private static RouteValueDictionary Ruta(int? idUsuario, DateTime? desde, DateTime? hasta, string? tipo = null, string? modulo = null)
    {
        var valoresRuta = new RouteValueDictionary();
        if (idUsuario is int usuarioSeleccionado) valoresRuta["IdUsuario"] = usuarioSeleccionado;
        if (!string.IsNullOrEmpty(tipo)) valoresRuta["TipoMovimiento"] = tipo;
        if (!string.IsNullOrEmpty(modulo)) valoresRuta["Modulo"] = modulo;
        if (desde is DateTime fechaDesde) valoresRuta["Desde"] = fechaDesde.ToString("yyyy-MM-dd");
        if (hasta is DateTime fechaHasta) valoresRuta["Hasta"] = fechaHasta.ToString("yyyy-MM-dd");
        return valoresRuta;
    }

    private async Task CargarUsuarios(int? idUsuarioSeleccionado) =>
        ViewBag.Usuarios = new SelectList(
            await contexto.Usuarios.AsNoTracking().OrderBy(usuario => usuario.Nombre)
                .Select(usuario => new { usuario.IdUsuario, Texto = usuario.Nombre + " " + usuario.Apellido1 + " (" + usuario.NombreUsuario + ")" }).ToListAsync(),
            "IdUsuario", "Texto", idUsuarioSeleccionado);
}
