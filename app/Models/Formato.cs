using System.Globalization;
namespace Controlex.Web.Models;

/// <summary>Formato de presentación: 1.234.567,89 y dd/MM/yyyy, como en los entregables.</summary>
public static class Formato
{
    private static readonly NumberFormatInfo Nfi = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = new[] { 3 }
    };

    public static string Dinero(decimal monto) => (monto < 0 ? "-" : "") + "₡" + Math.Abs(monto).ToString("N2", Nfi);
    public static string Horas(decimal horas) => horas.ToString("N2", Nfi);
    public static string Porcentaje(decimal? porcentaje) =>
        porcentaje is null ? "—" : (porcentaje.Value * 100).ToString("N1", Nfi) + " %";
    public static string Fecha(DateOnly? fecha) => fecha is null ? "—" : fecha.Value.ToString("dd/MM/yyyy");
    public static string ClaseEstado(string estado) => estado switch
    {
        "Completada" or "Finalizado" => "ok",
        "En proceso" or "En ejecución" => "info",
        "Cancelado" => "off",
        _ => "warn"
    };
}
