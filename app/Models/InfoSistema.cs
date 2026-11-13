namespace Controlex.Web.Models;

/// <summary>Datos de la pantalla "Acerca de" (RF-013). Actualizar la versión y la fecha al entregar cada versión.</summary>
public static class InfoSistema
{
    public const string Acronimo = "CONTROLEX";
    public const string Nombre = "Sistema de gestión de proyectos y tareas de trabajo";
    public const string Version = "1.0.0";
    public static readonly DateOnly FechaVersion = new(2026, 11, 10);
    public const string Propietario = "WallRed, Sociedad de Responsabilidad Limitada (cédula jurídica 3-102-946775)";
    public const string Desarrollador = "Dixon Jareth Martínez Jarquín y Marcos Hernández López";
    public const string Lenguaje = "C# 14 sobre .NET 10, ASP.NET Core MVC";
}
