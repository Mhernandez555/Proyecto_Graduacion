using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Models;

public class ControlexDbContext(DbContextOptions<ControlexDbContext> opciones) : DbContext(opciones)
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

    protected override void OnModelCreating(ModelBuilder constructorModelo)
    {
        constructorModelo.Entity<Usuario>().Property(usuario => usuario.CostoHora).HasPrecision(12, 2);
        constructorModelo.Entity<Proyecto>().Property(proyecto => proyecto.PrecioVenta).HasPrecision(14, 2);
        constructorModelo.Entity<Tarea>().Property(tarea => tarea.TiempoEstimado).HasPrecision(8, 2);
        constructorModelo.Entity<RegistroTiempo>().Property(registro => registro.Horas).HasPrecision(6, 2);
        constructorModelo.Entity<RegistroTiempo>().Property(registro => registro.CostoHora).HasPrecision(12, 2);

        constructorModelo.Entity<TareaUsuario>().HasKey(asignacion => new { asignacion.IdTarea, asignacion.IdUsuario });

        constructorModelo.Entity<VistaRentabilidad>().ToView("vw_rentabilidad_proyectos");
        constructorModelo.Entity<VistaRentabilidad>().Property(resumen => resumen.PrecioVenta).HasPrecision(14, 2);
        constructorModelo.Entity<VistaRentabilidad>().Property(resumen => resumen.CostoAcumulado).HasPrecision(18, 2);
        constructorModelo.Entity<VistaRentabilidad>().Property(resumen => resumen.Rentabilidad).HasPrecision(18, 2);
    }
}
