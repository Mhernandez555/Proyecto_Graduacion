using System.Globalization;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// Utilidad: dotnet run -- hash "MiClave123!"  -> imprime el hash para usuarios.contrasena_hash
if (args.Length == 2 && args[0] == "hash") { Console.WriteLine(PasswordHasher.Hash(args[1])); return; }

// Cultura fija: los decimales se escriben con punto (1500.50) sin depender de la configuración regional del equipo
var cultura = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

// Licencia de QuestPDF (exportación a PDF): Community, gratuita para organizaciones con ingresos menores a 1 millón de dólares
QuestPDF.Settings.License = LicenseType.Community;

// Los archivos estáticos viven en public/ (no en wwwroot/)
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, WebRootPath = "public" });

// Permite instalar la aplicación como servicio de Windows (si se ejecuta normal, no hace nada)
builder.Host.UseWindowsService();

// La configuración vive en config/. Aquí se leen la cadena de conexión, el correo, Kestrel (HTTPS) y demás ajustes.
builder.Configuration
    .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"config/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

var cadena = builder.Configuration.GetConnectionString("Controlex");
if (string.IsNullOrWhiteSpace(cadena))
    throw new InvalidOperationException(
        "Falta la cadena de conexión. Copie config/appsettings.Development.example.json a " +
        "config/appsettings.Development.json y complete los datos de su servidor.");

// Filtro global: si el usuario debe cambiar su contraseña, solo puede ir a esa pantalla (RF-022/RF-023)
builder.Services.AddControllersWithViews(o => o.Filters.Add<CambioContrasenaObligatorioFilter>());
// Las vistas se buscan en app/Views/<controlador>/ y app/Views/shared/
builder.Services.Configure<RazorViewEngineOptions>(o => o.ViewLocationExpanders.Add(new AppViewLocationExpander()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ControlexDbContext>(o => o.UseSqlServer(cadena));
builder.Services.AddScoped<PermisoService>();
builder.Services.AddScoped<BitacoraService>();
builder.Services.AddScoped<ReporteService>();
builder.Services.AddSingleton<CorreoService>();
// Hora de salida de las sesiones cerradas por inactividad
builder.Services.AddSingleton<ActividadSesionService>();
builder.Services.AddHostedService<CierreSesionesService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/auth/login";
        o.AccessDeniedPath = "/auth/accesodenegado";
        o.ExpireTimeSpan = TimeSpan.FromMinutes(SesionConfig.MinutosInactividad);   // cierre por inactividad (RF-006)
        o.SlidingExpiration = true;
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/dashboard/error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<ActividadSesionMiddleware>();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");
app.Run();
