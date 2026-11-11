using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Models;

public class ControlexDbContext(DbContextOptions<ControlexDbContext> options) : DbContext(options)
{
    // Seguridad y bitácoras
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<BitacoraIngreso> BitacoraIngresos => Set<BitacoraIngreso>();
    public DbSet<BitacoraMovimiento> BitacoraMovimientos => Set<BitacoraMovimiento>();

    // Clientes
    public DbSet<TipoCliente> TiposCliente => Set<TipoCliente>();
    public DbSet<Cliente> Clientes => Set<Cliente>();

    // Proyectos y tareas
    public DbSet<EstadoCatalogo> Estados => Set<EstadoCatalogo>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<Tarea> Tareas => Set<Tarea>();
    public DbSet<TareaUsuario> TareaUsuarios => Set<TareaUsuario>();
    public DbSet<RegistroTiempo> RegistrosTiempo => Set<RegistroTiempo>();

    // Vista de rentabilidad
    public DbSet<VistaRentabilidad> RentabilidadProyectos => Set<VistaRentabilidad>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Usuario>().Property(u => u.CostoHora).HasPrecision(12, 2);
        b.Entity<Proyecto>().Property(p => p.PrecioVenta).HasPrecision(14, 2);
        b.Entity<Tarea>().Property(t => t.TiempoEstimado).HasPrecision(8, 2);
        b.Entity<RegistroTiempo>().Property(r => r.Horas).HasPrecision(6, 2);
        b.Entity<RegistroTiempo>().Property(r => r.CostoHora).HasPrecision(12, 2);

        b.Entity<TareaUsuario>().HasKey(x => new { x.IdTarea, x.IdUsuario });

        b.Entity<VistaRentabilidad>().ToView("vw_rentabilidad_proyectos");
        b.Entity<VistaRentabilidad>().Property(v => v.PrecioVenta).HasPrecision(14, 2);
        b.Entity<VistaRentabilidad>().Property(v => v.CostoAcumulado).HasPrecision(18, 2);
        b.Entity<VistaRentabilidad>().Property(v => v.Rentabilidad).HasPrecision(18, 2);
    }
}
