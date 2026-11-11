using Controlex.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace Controlex.Web.Services;

/// <summary>Genera los reportes en PDF (botón Exportar PDF).</summary>
public static class ReportePdf
{
    private static IContainer Celda(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingVertical(3).PaddingHorizontal(3);

    private static IContainer Encabezado(IContainer c) =>
        c.Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(4).PaddingHorizontal(3);

    private static void Estructura(PageDescriptor p, string titulo, string filtros, string generadoPor, Action<IContainer> contenido)
    {
        p.Size(PageSizes.A4.Landscape());
        p.Margin(28);
        p.DefaultTextStyle(t => t.FontSize(8.5f));
        p.Header().Column(col =>
        {
            col.Item().Text("CONTROLEX · WallRed").FontSize(9).FontColor(Colors.Blue.Darken2).SemiBold();
            col.Item().Text(titulo).FontSize(16).Bold();
            col.Item().PaddingBottom(6).Text(filtros).FontSize(8).FontColor(Colors.Grey.Darken2);
        });
        p.Content().PaddingVertical(6).Element(contenido);
        p.Footer().Row(r =>
        {
            r.RelativeItem().Text($"Generado por {generadoPor} el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
            r.AutoItem().Text(x =>
            {
                x.Span("Página ").FontSize(8);
                x.CurrentPageNumber().FontSize(8);
                x.Span(" de ").FontSize(8);
                x.TotalPages().FontSize(8);
            });
        });
    }

    public static byte[] Rentabilidad(List<FilaRentabilidad> filas, string filtros, string generadoPor) =>
        Document.Create(doc => doc.Page(p => Estructura(p, "Reporte de proyectos y rentabilidad", filtros, generadoPor, c =>
        {
            if (filas.Count == 0) { c.Text("No se encontraron proyectos con esos criterios."); return; }
            c.Table(t =>
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(52); cd.RelativeColumn(3); cd.RelativeColumn(2.2f); cd.ConstantColumn(62);
                    cd.ConstantColumn(56); cd.ConstantColumn(56); cd.ConstantColumn(82); cd.ConstantColumn(82); cd.ConstantColumn(82); cd.ConstantColumn(48);
                });
                t.Header(h =>
                {
                    foreach (var n in new[] { "Código", "Proyecto", "Cliente", "Estado", "Inicio", "Fin" })
                        h.Cell().Element(Encabezado).Text(n).Bold();
                    foreach (var n in new[] { "Precio de venta", "Costo", "Rentabilidad", "Margen" })
                        h.Cell().Element(Encabezado).AlignRight().Text(n).Bold();
                });
                foreach (var f in filas)
                {
                    t.Cell().Element(Celda).Text(f.Codigo);
                    t.Cell().Element(Celda).Text(f.Proyecto);
                    t.Cell().Element(Celda).Text(f.Cliente);
                    t.Cell().Element(Celda).Text(f.Estado);
                    t.Cell().Element(Celda).Text(Formato.Fecha(f.FechaInicio));
                    t.Cell().Element(Celda).Text(Formato.Fecha(f.FechaFin));
                    t.Cell().Element(Celda).AlignRight().Text(Formato.Dinero(f.PrecioVenta));
                    t.Cell().Element(Celda).AlignRight().Text(Formato.Dinero(f.Costo));
                    var neg = f.Rentabilidad < 0;
                    var cr = t.Cell().Element(Celda).AlignRight().Text(Formato.Dinero(f.Rentabilidad)).SemiBold();
                    if (neg) cr.FontColor(Colors.Red.Darken2);
                    t.Cell().Element(Celda).AlignRight().Text(Formato.Porcentaje(f.Margen));
                }
                var venta = filas.Sum(x => x.PrecioVenta);
                var costo = filas.Sum(x => x.Costo);
                var rent = filas.Sum(x => x.Rentabilidad);
                t.Cell().ColumnSpan(6).Element(Encabezado).Text($"Totales ({filas.Count} proyectos)").Bold();
                t.Cell().Element(Encabezado).AlignRight().Text(Formato.Dinero(venta)).Bold();
                t.Cell().Element(Encabezado).AlignRight().Text(Formato.Dinero(costo)).Bold();
                t.Cell().Element(Encabezado).AlignRight().Text(Formato.Dinero(rent)).Bold();
                t.Cell().Element(Encabezado).AlignRight().Text(venta > 0 ? Formato.Porcentaje(rent / venta) : "—").Bold();
            });
        }))).GeneratePdf();

    public static byte[] Tareas(List<FilaTarea> filas, bool eficiencia, string filtros, string generadoPor) =>
        Document.Create(doc => doc.Page(p => Estructura(p,
            eficiencia ? "Reporte de eficiencia de tareas" : "Reporte de tareas pendientes", filtros, generadoPor, c =>
        {
            if (filas.Count == 0) { c.Text("No se encontraron tareas con esos criterios."); return; }
            c.Table(t =>
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(50); cd.RelativeColumn(2.2f); cd.RelativeColumn(1.6f); cd.ConstantColumn(70);
                    cd.RelativeColumn(1.6f); cd.ConstantColumn(58); cd.ConstantColumn(54); cd.ConstantColumn(54);
                    cd.ConstantColumn(46); cd.ConstantColumn(50);
                    if (eficiencia) { cd.ConstantColumn(50); cd.ConstantColumn(52); } else cd.ConstantColumn(54);
                });
                t.Header(h =>
                {
                    foreach (var n in new[] { "Código", "Tarea", "Proyecto", "Categoría", "Asignados", "Estado", "Inicio", "Fin" })
                        h.Cell().Element(Encabezado).Text(n).Bold();
                    h.Cell().Element(Encabezado).AlignRight().Text("Estimado (h)").Bold();
                    h.Cell().Element(Encabezado).AlignRight().Text("Registrado (h)").Bold();
                    if (eficiencia)
                    {
                        h.Cell().Element(Encabezado).AlignRight().Text("Diferencia (h)").Bold();
                        h.Cell().Element(Encabezado).AlignRight().Text("Eficiencia").Bold();
                    }
                    else h.Cell().Element(Encabezado).Text("Situación").Bold();
                });
                foreach (var f in filas)
                {
                    t.Cell().Element(Celda).Text(f.Codigo);
                    t.Cell().Element(Celda).Text(f.Nombre);
                    t.Cell().Element(Celda).Text(f.Proyecto);
                    t.Cell().Element(Celda).Text(f.Categoria);
                    t.Cell().Element(Celda).Text(f.Usuarios);
                    t.Cell().Element(Celda).Text(f.Estado);
                    t.Cell().Element(Celda).Text(Formato.Fecha(f.FechaInicio));
                    t.Cell().Element(Celda).Text(Formato.Fecha(f.FechaFin));
                    t.Cell().Element(Celda).AlignRight().Text(Formato.Horas(f.Estimado));
                    t.Cell().Element(Celda).AlignRight().Text(Formato.Horas(f.Registrado));
                    if (eficiencia)
                    {
                        var dif = t.Cell().Element(Celda).AlignRight().Text(Formato.Horas(f.Diferencia));
                        if (f.Diferencia < 0) dif.FontColor(Colors.Red.Darken2);
                        t.Cell().Element(Celda).AlignRight().Text(Formato.Porcentaje(f.Eficiencia));
                    }
                    else
                    {
                        var s = t.Cell().Element(Celda).Text(f.Vencida ? "Vencida" : "En plazo");
                        if (f.Vencida) s.FontColor(Colors.Red.Darken2).SemiBold();
                    }
                }
                var est = filas.Sum(x => x.Estimado);
                var reg = filas.Sum(x => x.Registrado);
                t.Cell().ColumnSpan(8).Element(Encabezado).Text($"Totales ({filas.Count} tareas)").Bold();
                t.Cell().Element(Encabezado).AlignRight().Text(Formato.Horas(est)).Bold();
                t.Cell().Element(Encabezado).AlignRight().Text(Formato.Horas(reg)).Bold();
                if (eficiencia)
                {
                    var conTiempo = filas.Where(x => x.Registrado > 0).ToList();
                    var regCon = conTiempo.Sum(x => x.Registrado);
                    t.Cell().Element(Encabezado).AlignRight().Text(Formato.Horas(est - reg)).Bold();
                    t.Cell().Element(Encabezado).AlignRight().Text(regCon > 0 ? Formato.Porcentaje(conTiempo.Sum(x => x.Estimado) / regCon) : "—").Bold();
                }
                else t.Cell().Element(Encabezado).Text("");
            });
        }))).GeneratePdf();
}
