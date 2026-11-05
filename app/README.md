# CONTROLEX – Proyecto Profesional Informático (WallRed)

Aplicación web ASP.NET Core MVC (C# 14, .NET 10) con SQL Server 2025 Express.

## Estructura
| Carpeta | Contenido |
|---|---|
| `app/Controllers` | Controladores MVC |
| `app/Models` | Entidades de `CONTROLEX_DB`, DbContext y modelos de formulario |
| `app/Services` | Lógica reutilizable: hash de contraseñas, permisos, bitácoras |
| `app/Views/<módulo>` | Vistas Razor: `auth`, `dashboard`, `tareas`, `usuarios`, `reportes`, `shared` |
| `public/` | Archivos estáticos (css, js, imágenes) |
| `config/` | Configuración. `appsettings.Development.json` NO se sube a Git |
| `database/sql` | Script `CONTROLEX_DB.sql` |
| `storage/` | Logs y archivos generados |
| `test/` | Pruebas |

## Primera ejecución
1. Copiar `config/appsettings.Development.example.json` a `config/appsettings.Development.json` y poner los datos reales de conexión.
2. `dotnet dev-certs https --trust` (una sola vez).
3. `dotnet run -- hash "TuClave1!"` imprime un hash; pegarlo en `usuarios.contrasena_hash` del usuario `mhernandez`.
4. `dotnet run` (o F5 en Visual Studio) y abrir https://localhost:7080
