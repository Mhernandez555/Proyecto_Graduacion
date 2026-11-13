using System.Collections.Concurrent;
using Controlex.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Services;

public static class SesionConfig
{
    /// <summary>Minutos sin actividad después de los cuales se cierra la sesión (RF-006).</summary>
    public const int MinutosInactividad = 15;
}

/// <summary>
/// Anota la última actividad de cada sesión (como máximo una vez por minuto) y cierra las vencidas.
/// La hora de salida de una sesión que venció por inactividad es la de su última actividad más los minutos de inactividad.
/// Usa la columna bitacora_ingresos.fecha_hora_ultima_actividad (database/sql/003_ultima_actividad_sesion.sql).
/// </summary>
public class ActividadSesionService(IServiceScopeFactory scopes, ILogger<ActividadSesionService> log)
{
    private readonly ConcurrentDictionary<int, DateTime> _ultimaEscritura = new();
    private bool _avisado;

    public async Task RegistrarAsync(int idBitacoraIngreso)
    {
        var ahora = DateTime.Now;
        if (_ultimaEscritura.TryGetValue(idBitacoraIngreso, out var previo) && (ahora - previo).TotalSeconds < 60) return;
        _ultimaEscritura[idBitacoraIngreso] = ahora;
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ControlexDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.bitacora_ingresos SET fecha_hora_ultima_actividad = {ahora} WHERE id_bitacora_ingreso = {idBitacoraIngreso} AND fecha_hora_salida IS NULL");
        }
        catch (Exception ex) { Avisar(ex); }
    }

    public async Task CerrarVencidasAsync()
    {
        try
        {
            var ahora = DateTime.Now;
            var minutos = SesionConfig.MinutosInactividad;
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ControlexDbContext>();
            var cerradas = await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.bitacora_ingresos
                SET fecha_hora_salida = DATEADD(MINUTE, {minutos}, COALESCE(fecha_hora_ultima_actividad, fecha_hora_entrada))
                WHERE fecha_hora_salida IS NULL
                  AND DATEADD(MINUTE, {minutos}, COALESCE(fecha_hora_ultima_actividad, fecha_hora_entrada)) < {ahora}");
            if (cerradas > 0) log.LogInformation("Se cerraron {Cantidad} sesión(es) por inactividad.", cerradas);

            foreach (var k in _ultimaEscritura.Where(x => (ahora - x.Value).TotalMinutes > 60).Select(x => x.Key).ToList())
                _ultimaEscritura.TryRemove(k, out _);
        }
        catch (Exception ex) { Avisar(ex); }
    }

    private void Avisar(Exception ex)
    {
        if (_avisado) return;
        _avisado = true;
        log.LogWarning(ex, "No se pudo actualizar la actividad de las sesiones. ¿Se ejecutó database/sql/003_ultima_actividad_sesion.sql?");
    }
}

/// <summary>En cada petición de un usuario autenticado, anota que su sesión sigue activa.</summary>
public class ActividadSesionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ActividadSesionService actividad)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            int.TryParse(context.User.FindFirst("bitacora_ingreso")?.Value, out var idIngreso))
            await actividad.RegistrarAsync(idIngreso);
        await next(context);
    }
}

/// <summary>Cada minuto cierra las sesiones que llevan más del tiempo permitido sin actividad.</summary>
public class CierreSesionesService(ActividadSesionService actividad) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);   // deja arrancar la aplicación
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do { await actividad.CerrarVencidasAsync(); }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
