using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Data;

public class ControlexDbContext(DbContextOptions<ControlexDbContext> options) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<BitacoraIngreso> BitacoraIngresos => Set<BitacoraIngreso>();
    public DbSet<BitacoraMovimiento> BitacoraMovimientos => Set<BitacoraMovimiento>();
}
