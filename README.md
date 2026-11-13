# CONTROLEX – Proyecto Profesional Informático (WallRed)

Aplicación web ASP.NET Core MVC (C# 14, .NET 10) con SQL Server 2025 Express.

## Estructura
| Carpeta | Contenido |
|---|---|
| `app/Controllers`, `app/Models`, `app/Services` | Código de la aplicación |
| `app/Views/<módulo>` | Vistas Razor: `auth`, `dashboard`, `proyectos`, `tareas`, `usuarios`, `roles`, `clientes`, `reportes`, `bitacoras`, `informacion`, `seguridad`, `shared` |
| `public/` | Archivos estáticos (css, js, imágenes) |
| `config/` | Configuración. `appsettings.Development.json` y `appsettings.Production.json` NO se suben a Git |
| `database/sql` | Scripts de la base de datos (ver orden abajo) |
| `docs/` | Guía de instalación, plan de pruebas y ajustes pendientes a los entregables |
| `storage/`, `test/` | Logs y pruebas |

## Base de datos – orden de los scripts
1. `CONTROLEX_DB.sql` (Entregable 4): tablas, vista de rentabilidad y datos iniciales.
2. `003_ultima_actividad_sesion.sql`: columna para registrar la salida por inactividad.
3. `002_datos_prueba_reportes.sql`: datos ficticios, **solo para pruebas**. No ejecutarlo en la instalación real.

## Primera ejecución (desarrollo)
1. Copiar `config/appsettings.Development.example.json` a `config/appsettings.Development.json` y completar la conexión.
2. `dotnet dev-certs https --trust` (una sola vez).
3. `dotnet run -- hash "TuClave1!"` imprime un hash para `usuarios.contrasena_hash` del administrador.
4. `dotnet run --launch-profile https` y abrir https://localhost:7080

Instalación en el equipo del cliente: ver `docs/Guia_de_instalacion.md`.
