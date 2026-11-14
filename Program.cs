using System.Globalization;
using System.Net;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var argumentos = args;

// Utilidad: dotnet run -- hash "MiClave123!"  -> imprime el hash para usuarios.contrasena_hash
if (argumentos.Length == 2 && argumentos[0] == "hash")
{
    Console.WriteLine(PasswordHasher.Hash(argumentos[1]));
    return;
}

// Cultura fija: los decimales se escriben con punto (1500.50) sin depender de la configuración regional del equipo
var cultura = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

// Licencia de QuestPDF (exportación a PDF): Community, gratuita para organizaciones con ingresos menores a 1 millón de dólares
QuestPDF.Settings.License = LicenseType.Community;

// Los archivos estáticos viven en public/ (no en wwwroot/)
var constructorAplicacion = WebApplication.CreateBuilder(new WebApplicationOptions
    { Args = argumentos, WebRootPath = "public" });

// Permite instalar la aplicación como servicio de Windows (si se ejecuta normal, no hace nada)
constructorAplicacion.Host.UseWindowsService();

// La configuración vive en config/. Aquí se leen la cadena de conexión, el correo, Kestrel (HTTPS) y demás ajustes.
constructorAplicacion.Configuration
    .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"config/appsettings.{constructorAplicacion.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

var cadena = constructorAplicacion.Configuration.GetConnectionString("Controlex");
if (string.IsNullOrWhiteSpace(cadena))
    throw new InvalidOperationException(
        "Falta la cadena de conexión. Copie config/appsettings.Development.example.json a " +
        "config/appsettings.Development.json y complete los datos de su servidor.");

// Filtro global: si el usuario debe cambiar su contraseña, solo puede ir a esa pantalla (RF-022/RF-023)
constructorAplicacion.Services.AddControllersWithViews(opciones => opciones.Filters.Add<CambioContrasenaObligatorioFilter>());
// Las vistas se buscan en app/Views/<controlador>/ y app/Views/shared/
constructorAplicacion.Services.Configure<RazorViewEngineOptions>(opciones => opciones.ViewLocationExpanders.Add(new AppViewLocationExpander()));
constructorAplicacion.Services.AddHttpContextAccessor();
var proxiesConfiables = constructorAplicacion.Configuration.GetSection("Red:ProxiesConfiables").Get<string[]>() ?? [];
constructorAplicacion.Services.Configure<ForwardedHeadersOptions>(opciones =>
{
    opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opciones.ForwardLimit = 1;
    foreach (var proxy in proxiesConfiables)
    {
        if (!IPAddress.TryParse(proxy, out var direccionProxy))
            throw new InvalidOperationException($"La dirección de proxy confiable '{proxy}' no es válida.");
        opciones.KnownProxies.Add(direccionProxy);
    }
});
constructorAplicacion.Services.AddDbContext<ControlexDbContext>(opciones => opciones.UseSqlServer(cadena));
constructorAplicacion.Services.AddScoped<PermisoService>();
constructorAplicacion.Services.AddScoped<BitacoraService>();
constructorAplicacion.Services.AddScoped<ReporteService>();
constructorAplicacion.Services.AddSingleton<CorreoService>();
// Hora de salida de las sesiones cerradas por inactividad
constructorAplicacion.Services.AddSingleton<ActividadSesionService>();
constructorAplicacion.Services.AddHostedService<CierreSesionesService>();
constructorAplicacion.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/auth/login";
        opciones.AccessDeniedPath = "/auth/accesodenegado";
        opciones.ExpireTimeSpan = TimeSpan.FromMinutes(SesionConfig.MinutosInactividad);   // cierre por inactividad (RF-006)
        opciones.SlidingExpiration = true;
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
constructorAplicacion.Services.AddAuthorization(opciones => opciones.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var aplicacion = constructorAplicacion.Build();
aplicacion.UseForwardedHeaders();
if (!aplicacion.Environment.IsDevelopment()) { aplicacion.UseExceptionHandler("/dashboard/error"); aplicacion.UseHsts(); }
aplicacion.UseHttpsRedirection();
aplicacion.UseStaticFiles();
aplicacion.UseRouting();
aplicacion.UseAuthentication();
aplicacion.UseMiddleware<ActividadSesionMiddleware>();
aplicacion.UseAuthorization();
aplicacion.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");
aplicacion.Run();
