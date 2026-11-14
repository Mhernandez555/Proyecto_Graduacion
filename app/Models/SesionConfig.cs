using System.Collections.Concurrent;
using Controlex.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Services;

public static class SesionConfig
{
    /// <summary>Minutos sin actividad después de los cuales se cierra la sesión (RF-006).</summary>
    public const int MinutosInactividad = 15;
    public const string MotivoCierreManual = "Cierre manual";
    public const string MotivoInactividad = "Inactividad";
}

/// <summary>
/// Anota la última actividad de cada sesión (como máximo una vez por minuto) y cierra las vencidas.
/// La hora de salida de una sesión que venció por inactividad es la de su última actividad más los minutos de inactividad.
/// Usa la columna bitacora_ingresos.fecha_hora_ultima_actividad (database/sql/003_ultima_actividad_sesion.sql).
/// </summary>
public class ActividadSesionService(IServiceScopeFactory fabricaAlcances, ILogger<ActividadSesionService> registrador)
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
            using var alcance = fabricaAlcances.CreateScope();
            var contexto = alcance.ServiceProvider.GetRequiredService<ControlexDbContext>();
            await contexto.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.bitacora_ingresos SET fecha_hora_ultima_actividad = {ahora} WHERE id_bitacora_ingreso = {idBitacoraIngreso} AND fecha_hora_salida IS NULL");
        }
        catch (Exception excepcion) { Avisar(excepcion); }
    }

    public async Task CerrarVencidasAsync()
    {
        try
        {
            var ahora = DateTime.Now;
            var minutos = SesionConfig.MinutosInactividad;
            using var alcance = fabricaAlcances.CreateScope();
            var contexto = alcance.ServiceProvider.GetRequiredService<ControlexDbContext>();
            var sesionesCerradas = await contexto.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.bitacora_ingresos
                SET fecha_hora_salida = DATEADD(MINUTE, {minutos}, COALESCE(fecha_hora_ultima_actividad, fecha_hora_entrada))
                    , motivo_salida = {SesionConfig.MotivoInactividad}
                WHERE fecha_hora_salida IS NULL
                  AND DATEADD(MINUTE, {minutos}, COALESCE(fecha_hora_ultima_actividad, fecha_hora_entrada)) < {ahora}");
            if (sesionesCerradas > 0)
                registrador.LogInformation("Se cerraron {Cantidad} sesión(es) por inactividad.", sesionesCerradas);

            foreach (var idIngresoAntiguo in _ultimaEscritura
                         .Where(registro => (ahora - registro.Value).TotalMinutes > 60)
                         .Select(registro => registro.Key).ToList())
                _ultimaEscritura.TryRemove(idIngresoAntiguo, out _);
        }
        catch (Exception excepcion) { Avisar(excepcion); }
    }

    private void Avisar(Exception excepcion)
    {
        if (_avisado) return;
        _avisado = true;
        registrador.LogWarning(excepcion,
            "No se pudo actualizar o cerrar la sesión. ¿Se ejecutaron los scripts database/sql/003_ultima_actividad_sesion.sql y database/sql/004_motivo_salida_sesion.sql?");
    }
}

/// <summary>En cada petición de un usuario autenticado, anota que su sesión sigue activa.</summary>
public class ActividadSesionMiddleware(RequestDelegate siguiente)
{
    public async Task InvokeAsync(HttpContext contexto, ActividadSesionService actividad)
    {
        if (contexto.User.Identity?.IsAuthenticated == true &&
            int.TryParse(contexto.User.FindFirst("bitacora_ingreso")?.Value, out var idBitacoraIngreso))
            await actividad.RegistrarAsync(idBitacoraIngreso);
        await siguiente(contexto);
    }
}

/// <summary>Cada minuto cierra las sesiones que llevan más del tiempo permitido sin actividad.</summary>
public class CierreSesionesService(ActividadSesionService actividad) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken tokenCancelacion)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), tokenCancelacion);   // deja arrancar la aplicación
        using var temporizador = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do { await actividad.CerrarVencidasAsync(); }
        while (await temporizador.WaitForNextTickAsync(tokenCancelacion));
    }
}
