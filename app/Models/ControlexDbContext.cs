using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Models;

public class ControlexDbContext(DbContextOptions<ControlexDbContext> options) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TipoCliente> TiposCliente => Set<TipoCliente>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<BitacoraIngreso> BitacoraIngresos => Set<BitacoraIngreso>();
    public DbSet<BitacoraMovimiento> BitacoraMovimientos => Set<BitacoraMovimiento>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Usuario>().Property(u => u.CostoHora).HasPrecision(12, 2);
    }
}
