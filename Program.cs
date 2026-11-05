using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

// Utilidad: dotnet run -- hash "MiClave123!"  -> imprime el hash para usuarios.contrasena_hash
if (args.Length == 2 && args[0] == "hash") { Console.WriteLine(PasswordHasher.Hash(args[1])); return; }

// Los archivos estáticos viven en public/ (no en wwwroot/)
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, WebRootPath = "public" });

// La configuración vive en config/. Aquí se leen la cadena de conexión y demás ajustes.
builder.Configuration
    .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"config/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

var cadena = builder.Configuration.GetConnectionString("Controlex");
if (string.IsNullOrWhiteSpace(cadena))
    throw new InvalidOperationException(
        "Falta la cadena de conexión. Copie config/appsettings.Development.example.json a " +
        "config/appsettings.Development.json y complete los datos de su servidor.");

builder.Services.AddControllersWithViews();
// Las vistas se buscan en app/Views/<controlador>/ y app/Views/shared/
builder.Services.Configure<RazorViewEngineOptions>(o => o.ViewLocationExpanders.Add(new AppViewLocationExpander()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ControlexDbContext>(o => o.UseSqlServer(cadena));
builder.Services.AddScoped<PermisoService>();
builder.Services.AddScoped<BitacoraService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/auth/login";
        o.AccessDeniedPath = "/auth/accesodenegado";
        o.ExpireTimeSpan = TimeSpan.FromMinutes(15);   // cierre por inactividad (RF-006)
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
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");
app.Run();
