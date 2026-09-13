using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AsistenciaDesktop_v2.Data;
using AsistenciaDesktop_v2.Views;

namespace AsistenciaDesktop_v2.Services
{
    public static class ReportesAutomaticosService
    {
        public static string GenerarReporteComedor(string fechaStr)
        {
            var alertasComedor = new List<RegistroFila>();

            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    
                    cmd.CommandText = @"
                        SELECT a.NumeroUnico, a.Nombre, a.Curso,
                               ac.HoraIngreso, acom.HoraIngreso, ac.HoraSalida
                        FROM Alumnos a
                        LEFT JOIN AsistenciaColegio ac ON a.NumeroUnico = ac.AlumnoId AND ac.Fecha = @fecha
                        LEFT JOIN AsistenciaComedor acom ON a.NumeroUnico = acom.AlumnoId AND acom.Fecha = @fecha
                        WHERE ac.HoraIngreso IS NOT NULL AND ac.HoraIngreso != ''
                          AND (acom.HoraIngreso IS NULL OR acom.HoraIngreso = '')
                        ORDER BY a.Curso ASC, a.Nombre ASC";
                    
                    cmd.Parameters.AddWithValue("@fecha", fechaStr);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            alertasComedor.Add(new RegistroFila
                            {
                                Rut = reader.GetString(0),
                                Nombre = reader.GetString(1),
                                Curso = reader.GetString(2),
                                HoraIngreso = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                                HoraComedor = "-",
                                HoraSalida = reader.IsDBNull(5) ? "-" : reader.GetString(5)
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { Logger.Log("GenerarReporteComedor DB error: " + ex.ToString()); return null; }

            Logger.Log("Alertas encontradas: " + alertasComedor.Count); if (alertasComedor.Count == 0) return null;

            string filename = $"Reporte_AlertaComedor_{fechaStr.Replace("-","")}.pdf";
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Asistencia_Reportes", fechaStr);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            
            string fullPath = Path.Combine(path, filename);

            QuestPDF.Settings.License = LicenseType.Community;
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, fechaStr));
                    page.Content().Element(c => ComposeContent(c, alertasComedor));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(fullPath);

            return fullPath;
        }

        static void ComposeHeader(IContainer container, string fechaStr)
        {
            container.Row(row =>
            {
                try {
                    var uri = new Uri("pack://application:,,,/Assets/logo.png");
                    var resInfo = System.Windows.Application.GetResourceStream(uri);
                    if (resInfo != null) {
                        using (var ms = new System.IO.MemoryStream()) {
                            resInfo.Stream.CopyTo(ms);
                            row.ConstantItem(60).Height(60).Image(ms.ToArray());
                        }
                    }
                } catch (Exception ex) { Logger.Log("ComposeHeader error: " + ex.ToString()); }

                row.RelativeItem().PaddingLeft(10).Column(column =>
                {
                    column.Item().Text("Liceo TP Gonzalo Guglielmi Montiel").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text("Reporte Automático de Alerta Comedor").FontSize(14).SemiBold();
                    column.Item().Text($"Fecha: {fechaStr}").FontSize(12);
                });
            });
        }

        static void ComposeContent(IContainer container, List<RegistroFila> alertasComedor)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Item().Background(Colors.Red.Lighten5).Padding(10).Column(alertCol =>
                {
                    alertCol.Spacing(10);
                    alertCol.Item().Text("ALERTA COMEDOR").FontSize(16).Bold().FontColor(Colors.Red.Darken2);
                    alertCol.Item().Text("Los siguientes alumnos registraron ingreso al establecimiento, pero NO pasaron por el comedor:").FontSize(12).FontColor(Colors.Red.Darken2);
                    
                    alertCol.Item().Table(table => BuildTable(table, alertasComedor));
                });
            });
        }

        static void BuildTable(QuestPDF.Fluent.TableDescriptor table, List<RegistroFila> data)
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(70);
                columns.RelativeColumn();
                columns.ConstantColumn(40);
                columns.ConstantColumn(60);
                columns.ConstantColumn(60);
                columns.ConstantColumn(60);
            });

            table.Header(header =>
            {
                header.Cell().PaddingBottom(5).Text("RUT").SemiBold();
                header.Cell().PaddingBottom(5).Text("Nombre").SemiBold();
                header.Cell().PaddingBottom(5).Text("Curso").SemiBold();
                header.Cell().PaddingBottom(5).Text("Ingreso").SemiBold();
                header.Cell().PaddingBottom(5).Text("Comedor").SemiBold();
                header.Cell().PaddingBottom(5).Text("Salida").SemiBold();
                header.Cell().ColumnSpan(6).BorderBottom(1).BorderColor(Colors.Black);
            });

            foreach (var item in data)
            {
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.Rut).FontColor(Colors.Red.Darken2);
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.Nombre).FontColor(Colors.Red.Darken2);
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.Curso).FontColor(Colors.Red.Darken2);
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.HoraIngreso).FontColor(Colors.Red.Darken2);
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.HoraComedor).FontColor(Colors.Red.Darken2);
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.HoraSalida).FontColor(Colors.Red.Darken2);
            }
        }
    }
}





