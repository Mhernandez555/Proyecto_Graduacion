using Controlex.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace Controlex.Web.Services;

/// <summary>Genera los reportes en PDF (botón Exportar PDF).</summary>
public static class ReportePdf
{
    private static IContainer Celda(IContainer contenedor) =>
        contenedor.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingVertical(3).PaddingHorizontal(3);

    private static IContainer Encabezado(IContainer contenedor) =>
        contenedor.Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(4).PaddingHorizontal(3);

    private static void Estructura(PageDescriptor pagina, string titulo, string filtros, string generadoPor, Action<IContainer> contenido)
    {
        pagina.Size(PageSizes.A4.Landscape());
        pagina.Margin(28);
        pagina.DefaultTextStyle(estilo => estilo.FontSize(8.5f));
        pagina.Header().Column(columna =>
        {
            columna.Item().Text("CONTROLEX · WallRed").FontSize(9).FontColor(Colors.Blue.Darken2).SemiBold();
            columna.Item().Text(titulo).FontSize(16).Bold();
            columna.Item().PaddingBottom(6).Text(filtros).FontSize(8).FontColor(Colors.Grey.Darken2);
        });
        pagina.Content().PaddingVertical(6).Element(contenido);
        pagina.Footer().Row(fila =>
        {
            fila.RelativeItem().Text($"Generado por {generadoPor} el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
            fila.AutoItem().Text(texto =>
            {
                texto.Span("Página ").FontSize(8);
                texto.CurrentPageNumber().FontSize(8);
                texto.Span(" de ").FontSize(8);
                texto.TotalPages().FontSize(8);
            });
        });
    }

    public static byte[] Rentabilidad(List<FilaRentabilidad> filas, string filtros, string generadoPor) =>
        Document.Create(documento => documento.Page(pagina =>
            Estructura(pagina, "Reporte de proyectos y rentabilidad", filtros, generadoPor, contenedor =>
        {
            if (filas.Count == 0) { contenedor.Text("No se encontraron proyectos con esos criterios."); return; }
            contenedor.Table(tabla =>
            {
                tabla.ColumnsDefinition(definicionColumnas =>
                {
                    definicionColumnas.ConstantColumn(52); definicionColumnas.RelativeColumn(3);
                    definicionColumnas.RelativeColumn(2.2f); definicionColumnas.ConstantColumn(62);
                    definicionColumnas.ConstantColumn(56); definicionColumnas.ConstantColumn(56);
                    definicionColumnas.ConstantColumn(82); definicionColumnas.ConstantColumn(82);
                    definicionColumnas.ConstantColumn(82); definicionColumnas.ConstantColumn(48);
                });
                tabla.Header(encabezadoTabla =>
                {
                    foreach (var tituloColumna in new[] { "Código", "Proyecto", "Cliente", "Estado", "Inicio", "Fin" })
                        encabezadoTabla.Cell().Element(Encabezado).Text(tituloColumna).Bold();
                    foreach (var tituloColumna in new[] { "Precio de venta", "Costo", "Rentabilidad", "Margen" })
                        encabezadoTabla.Cell().Element(Encabezado).AlignRight().Text(tituloColumna).Bold();
                });
                foreach (var fila in filas)
                {
                    tabla.Cell().Element(Celda).Text(fila.Codigo);
                    tabla.Cell().Element(Celda).Text(fila.Proyecto);
                    tabla.Cell().Element(Celda).Text(fila.Cliente);
                    tabla.Cell().Element(Celda).Text(fila.Estado);
                    tabla.Cell().Element(Celda).Text(Formato.Fecha(fila.FechaInicio));
                    tabla.Cell().Element(Celda).Text(Formato.Fecha(fila.FechaFin));
                    tabla.Cell().Element(Celda).AlignRight().Text(Formato.Dinero(fila.PrecioVenta));
                    tabla.Cell().Element(Celda).AlignRight().Text(Formato.Dinero(fila.Costo));
                    var rentabilidadNegativa = fila.Rentabilidad < 0;
                    var celdaRentabilidad = tabla.Cell().Element(Celda).AlignRight()
                        .Text(Formato.Dinero(fila.Rentabilidad)).SemiBold();
                    if (rentabilidadNegativa) celdaRentabilidad.FontColor(Colors.Red.Darken2);
                    tabla.Cell().Element(Celda).AlignRight().Text(Formato.Porcentaje(fila.Margen));
                }
                var totalVentas = filas.Sum(fila => fila.PrecioVenta);
                var totalCostos = filas.Sum(fila => fila.Costo);
                var rentabilidadTotal = filas.Sum(fila => fila.Rentabilidad);
                tabla.Cell().ColumnSpan(6).Element(Encabezado).Text($"Totales ({filas.Count} proyectos)").Bold();
                tabla.Cell().Element(Encabezado).AlignRight().Text(Formato.Dinero(totalVentas)).Bold();
                tabla.Cell().Element(Encabezado).AlignRight().Text(Formato.Dinero(totalCostos)).Bold();
                tabla.Cell().Element(Encabezado).AlignRight().Text(Formato.Dinero(rentabilidadTotal)).Bold();
                tabla.Cell().Element(Encabezado).AlignRight()
                    .Text(totalVentas > 0 ? Formato.Porcentaje(rentabilidadTotal / totalVentas) : "—").Bold();
            });
        }))).GeneratePdf();

    public static byte[] Tareas(List<FilaTarea> filas, bool eficiencia, string filtros, string generadoPor) =>
        Document.Create(documento => documento.Page(pagina => Estructura(pagina,
            eficiencia ? "Reporte de eficiencia de tareas" : "Reporte de tareas pendientes", filtros, generadoPor, contenedor =>
        {
            if (filas.Count == 0) { contenedor.Text("No se encontraron tareas con esos criterios."); return; }
            contenedor.Table(tabla =>
            {
                tabla.ColumnsDefinition(definicionColumnas =>
                {
                    definicionColumnas.ConstantColumn(50); definicionColumnas.RelativeColumn(2.2f);
                    definicionColumnas.RelativeColumn(1.6f); definicionColumnas.ConstantColumn(70);
                    definicionColumnas.RelativeColumn(1.6f); definicionColumnas.ConstantColumn(58);
                    definicionColumnas.ConstantColumn(54); definicionColumnas.ConstantColumn(54);
                    definicionColumnas.ConstantColumn(46); definicionColumnas.ConstantColumn(50);
                    if (eficiencia)
                    {
                        definicionColumnas.ConstantColumn(50);
                        definicionColumnas.ConstantColumn(52);
                    }
                    else definicionColumnas.ConstantColumn(54);
                });
                tabla.Header(encabezadoTabla =>
                {
                    foreach (var tituloColumna in new[] { "Código", "Tarea", "Proyecto", "Categoría", "Asignados", "Estado", "Inicio", "Fin" })
                        encabezadoTabla.Cell().Element(Encabezado).Text(tituloColumna).Bold();
                    encabezadoTabla.Cell().Element(Encabezado).AlignRight().Text("Estimado (h)").Bold();
                    encabezadoTabla.Cell().Element(Encabezado).AlignRight().Text("Registrado (h)").Bold();
                    if (eficiencia)
                    {
                        encabezadoTabla.Cell().Element(Encabezado).AlignRight().Text("Diferencia (h)").Bold();
                        encabezadoTabla.Cell().Element(Encabezado).AlignRight().Text("Eficiencia").Bold();
                    }
                    else encabezadoTabla.Cell().Element(Encabezado).Text("Situación").Bold();
                });
                foreach (var fila in filas)
                {
                    tabla.Cell().Element(Celda).Text(fila.Codigo);
                    tabla.Cell().Element(Celda).Text(fila.Nombre);
                    tabla.Cell().Element(Celda).Text(fila.Proyecto);
                    tabla.Cell().Element(Celda).Text(fila.Categoria);
                    tabla.Cell().Element(Celda).Text(fila.Usuarios);
                    tabla.Cell().Element(Celda).Text(fila.Estado);
                    tabla.Cell().Element(Celda).Text(Formato.Fecha(fila.FechaInicio));
                    tabla.Cell().Element(Celda).Text(Formato.Fecha(fila.FechaFin));
                    tabla.Cell().Element(Celda).AlignRight().Text(Formato.Horas(fila.Estimado));
                    tabla.Cell().Element(Celda).AlignRight().Text(Formato.Horas(fila.Registrado));
                    if (eficiencia)
                    {
                        var diferencia = tabla.Cell().Element(Celda).AlignRight().Text(Formato.Horas(fila.Diferencia));
                        if (fila.Diferencia < 0) diferencia.FontColor(Colors.Red.Darken2);
                        tabla.Cell().Element(Celda).AlignRight().Text(Formato.Porcentaje(fila.Eficiencia));
                    }
                    else
                    {
                        var textoSituacion = tabla.Cell().Element(Celda).Text(fila.Vencida ? "Vencida" : "En plazo");
                        if (fila.Vencida) textoSituacion.FontColor(Colors.Red.Darken2).SemiBold();
                    }
                }
                var totalEstimado = filas.Sum(fila => fila.Estimado);
                var totalRegistrado = filas.Sum(fila => fila.Registrado);
                tabla.Cell().ColumnSpan(8).Element(Encabezado).Text($"Totales ({filas.Count} tareas)").Bold();
                tabla.Cell().Element(Encabezado).AlignRight().Text(Formato.Horas(totalEstimado)).Bold();
                tabla.Cell().Element(Encabezado).AlignRight().Text(Formato.Horas(totalRegistrado)).Bold();
                if (eficiencia)
                {
                    var filasConTiempo = filas.Where(fila => fila.Registrado > 0).ToList();
                    var horasRegistradasConTiempo = filasConTiempo.Sum(fila => fila.Registrado);
                    tabla.Cell().Element(Encabezado).AlignRight().Text(Formato.Horas(totalEstimado - totalRegistrado)).Bold();
                    tabla.Cell().Element(Encabezado).AlignRight().Text(horasRegistradasConTiempo > 0
                        ? Formato.Porcentaje(filasConTiempo.Sum(fila => fila.Estimado) / horasRegistradasConTiempo) : "—").Bold();
                }
                else tabla.Cell().Element(Encabezado).Text("");
            });
        }))).GeneratePdf();
}
