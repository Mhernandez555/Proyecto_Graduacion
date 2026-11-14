using Controlex.Web.Models;
namespace Controlex.Web.Services;

public class BitacoraService(ControlexDbContext contexto, IHttpContextAccessor accesorHttp)
{
    private string Ip
    {
        get
        {
            var direccion = accesorHttp.HttpContext?.Connection.RemoteIpAddress;
            if (direccion is null) return "desconocida";
            if (direccion.IsIPv4MappedToIPv6) direccion = direccion.MapToIPv4();
            return direccion.ToString();
        }
    }

    public async Task<int> IngresoAsync(int idUsuario)
    {
        var ingreso = new BitacoraIngreso { IdUsuario = idUsuario, DireccionIp = Ip, FechaHoraEntrada = DateTime.Now };
        contexto.BitacoraIngresos.Add(ingreso);
        await contexto.SaveChangesAsync();
        return ingreso.IdBitacoraIngreso;
    }

    public async Task SalidaAsync(int idBitacoraIngreso, string motivo)
    {
        var ingreso = await contexto.BitacoraIngresos.FindAsync(idBitacoraIngreso);
        if (ingreso is null || ingreso.FechaHoraSalida is not null) return;
        ingreso.FechaHoraSalida = DateTime.Now;
        ingreso.MotivoSalida = motivo;
        await contexto.SaveChangesAsync();
    }

    public async Task MovimientoAsync(int idUsuario, string modulo, string tipo, string? detalle)
    {
        contexto.BitacoraMovimientos.Add(new BitacoraMovimiento
        {
            IdUsuario = idUsuario, DireccionIp = Ip, FechaHora = DateTime.Now,
            Modulo = modulo, TipoMovimiento = tipo, Detalle = detalle
        });
        await contexto.SaveChangesAsync();
    }
}
