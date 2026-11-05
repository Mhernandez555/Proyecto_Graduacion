using Controlex.Web.Data;
namespace Controlex.Web.Services;

public class BitacoraService(ControlexDbContext db, IHttpContextAccessor http)
{
    private string Ip => http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

    public async Task<int> IngresoAsync(int idUsuario)
    {
        var b = new BitacoraIngreso { IdUsuario = idUsuario, DireccionIp = Ip, FechaHoraEntrada = DateTime.Now };
        db.BitacoraIngresos.Add(b);
        await db.SaveChangesAsync();
        return b.IdBitacoraIngreso;
    }

    public async Task SalidaAsync(int idBitacoraIngreso)
    {
        var b = await db.BitacoraIngresos.FindAsync(idBitacoraIngreso);
        if (b is null) return;
        b.FechaHoraSalida = DateTime.Now;
        await db.SaveChangesAsync();
    }

    public async Task MovimientoAsync(int idUsuario, string modulo, string tipo, string? detalle)
    {
        db.BitacoraMovimientos.Add(new BitacoraMovimiento
        {
            IdUsuario = idUsuario, DireccionIp = Ip, FechaHora = DateTime.Now,
            Modulo = modulo, TipoMovimiento = tipo, Detalle = detalle
        });
        await db.SaveChangesAsync();
    }
}
