using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AsistenciaDesktop_v2.Data;
using AsistenciaDesktop_v2.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;

namespace AsistenciaDesktop_v2.Views
{
    public class RegistroFila
    {
        public string Rut { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Curso { get; set; } = "";
        public string HoraIngreso { get; set; } = "";
        public string HoraComedor { get; set; } = "";
        public string HoraSalida { get; set; } = "";
        public Visibility BotonBorrarVisibilidad { get; set; } = Visibility.Collapsed;
    }

    public partial class RegistrosView : UserControl
    {
        private List<RegistroFila> listaActual = new List<RegistroFila>();

        public RegistrosView()
        {
            InitializeComponent();
            QuestPDF.Settings.License = LicenseType.Community;

            this.Loaded += (s, e) => 
            { 
                CargarCursos();
                if (MainShell.CurrentUser == "cirdam")
                {
                    btnBorrarTodo.Visibility = Visibility.Visible;
                }
                dpFecha.SelectedDate = DateTime.Now; 
            };
        }

        private void DpFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            CargarRegistros();
        }

                private void CargarCursos()
        {
            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT DISTINCT Curso FROM Alumnos ORDER BY Curso";
                    var cursos = new System.Collections.Generic.List<string> { "Todos los Cursos" };
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cursos.Add(reader.GetString(0));
                        }
                    }
                    cmbCurso.ItemsSource = cursos;
                    cmbCurso.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void CmbCurso_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CargarRegistros();
        }

        private void CargarRegistros()
        {
            if (!dpFecha.SelectedDate.HasValue) return;

            string fechaStr = dpFecha.SelectedDate.Value.ToString("yyyy-MM-dd");
            string cursoFilter = cmbCurso.SelectedItem as string;
            
            listaActual.Clear();

            Visibility borrarVisibilidad = Visibility.Collapsed;
            if (MainShell.CurrentRole == "admin")
            {
                borrarVisibilidad = Visibility.Visible;
            }

            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    
                    string sql = @"
                        SELECT a.NumeroUnico, a.Nombre, a.Curso, 
                               co.HoraIngreso as HoraCol, 
                               cm.HoraIngreso as HoraCom,
                               co.HoraSalida as HoraSal
                        FROM Alumnos a
                        LEFT JOIN AsistenciaColegio co ON a.NumeroUnico = co.AlumnoId AND co.Fecha = @fecha
                        LEFT JOIN AsistenciaComedor cm ON a.NumeroUnico = cm.AlumnoId AND cm.Fecha = @fecha
                        WHERE (co.HoraIngreso IS NOT NULL OR cm.HoraIngreso IS NOT NULL)";
                        
                    if (!string.IsNullOrEmpty(cursoFilter) && cursoFilter != "Todos los Cursos") {
                        sql += " AND a.Curso = @curso";
                        cmd.Parameters.AddWithValue("@curso", cursoFilter);
                    }
                    
                    sql += " ORDER BY a.Curso ASC, a.Nombre ASC";
                    
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@fecha", fechaStr);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaActual.Add(new RegistroFila
                            {
                                Rut = reader.GetString(0),
                                Nombre = reader.GetString(1),
                                Curso = reader.GetString(2),
                                HoraIngreso = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                                HoraComedor = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                                HoraSalida = reader.IsDBNull(5) ? "-" : reader.GetString(5),
                                BotonBorrarVisibilidad = borrarVisibilidad
                            });
                        }
                    }
                }
                GridRegistros.ItemsSource = null;
                GridRegistros.ItemsSource = listaActual;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando registros: " + ex.Message);
            }
        }

                        private string GenerarReportePdfFisico(bool especialSinComedor = false)
        {
            var dataToPrint = listaActual;

            if (dataToPrint.Count == 0) return null;

            string fecha = dpFecha.SelectedDate.Value.ToString("yyyy-MM-dd");
            string cursoFilter = cmbCurso.SelectedItem as string;
            string cursoSufijo = (cursoFilter == "Todos los Cursos" || string.IsNullOrEmpty(cursoFilter)) ? "General" : cursoFilter;
            string filename = $"Reporte_Asistencia_{cursoSufijo}_{fecha}.pdf";
            string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Asistencia_Reportes", fecha);
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            string path = Path.Combine(basePath, filename);

            var alertaComedor = listaActual.Where(r => 
                !string.IsNullOrWhiteSpace(r.HoraIngreso) && r.HoraIngreso != "-" && 
                !string.IsNullOrWhiteSpace(r.HoraSalida) && r.HoraSalida != "-" && 
                (string.IsNullOrWhiteSpace(r.HoraComedor) || r.HoraComedor == "-")
            ).ToList();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, "Reporte Diario de Asistencia y Comedor", cursoFilter));
                    page.Content().Element(c => ComposeContent(c, dataToPrint, alertaComedor));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(path);

            return path;
        }

        private string GenerarReporteInasistenciaPdfFisico()
        {
            string fechaStr = dpFecha.SelectedDate.Value.ToString("yyyy-MM-dd");
            string cursoFilter = cmbCurso.SelectedItem as string;
            string cursoSufijo = (cursoFilter == "Todos los Cursos" || string.IsNullOrEmpty(cursoFilter)) ? "General" : cursoFilter;
            string filename = $"Reporte_Inasistencia_{cursoSufijo}_{fechaStr}.pdf";
            string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Asistencia_Reportes", fechaStr);
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            string path = Path.Combine(basePath, filename);

            var inasistentes = new List<RegistroFila>();

            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    
                    string sql = @"
                        SELECT a.NumeroUnico, a.Nombre, a.Curso
                        FROM Alumnos a
                        WHERE a.NumeroUnico NOT IN (
                            SELECT AlumnoId FROM AsistenciaColegio WHERE Fecha = @fecha AND HoraIngreso IS NOT NULL AND HoraIngreso != ''
                        )";
                        
                    if (!string.IsNullOrEmpty(cursoFilter) && cursoFilter != "Todos los Cursos") {
                        sql += " AND a.Curso = @curso";
                        cmd.Parameters.AddWithValue("@curso", cursoFilter);
                    }
                    
                    sql += " ORDER BY a.Curso ASC, a.Nombre ASC";
                    
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@fecha", fechaStr);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            inasistentes.Add(new RegistroFila
                            {
                                Rut = reader.GetString(0),
                                Nombre = reader.GetString(1),
                                Curso = reader.GetString(2),
                                HoraIngreso = "-",
                                HoraComedor = "-",
                                HoraSalida = "-"
                            });
                        }
                    }
                }
            }
            catch { return null; }

            if (inasistentes.Count == 0) return null;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, "Registro de Inasistencia", cursoFilter));
                    page.Content().Element(c => ComposeContentInasistencia(c, inasistentes));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(path);

            return path;
        }

        void ComposeHeader(IContainer container, string title, string curso)
        {
            string fechaStr = dpFecha.SelectedDate.Value.ToString("dd/MM/yyyy");
            string cursoStr = (curso == "Todos los Cursos" || string.IsNullOrEmpty(curso)) ? "Todos los Cursos" : curso;
            
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
                } catch { }

                row.RelativeItem().PaddingLeft(10).Column(column =>
                {
                    column.Item().Text("Liceo TP Gonzalo Guglielmi Montiel").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text(title).FontSize(14).SemiBold();
                    column.Item().Text($"Fecha: {fechaStr} | Curso: {cursoStr}").FontSize(12);
                });
            });
        }

                void ComposeContent(IContainer container, List<RegistroFila> data, List<RegistroFila> alertasComedor)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(20);
                
                column.Item().Text("Registro General de Asistencia").FontSize(15).SemiBold().FontColor(Colors.Grey.Darken3);
                column.Item().Table(table => BuildTable(table, data));

                if (alertasComedor.Count > 0)
                {
                    column.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    
                    column.Item().Background(Colors.Red.Lighten5).Padding(10).Column(alertCol =>
                    {
                        alertCol.Spacing(10);
                        alertCol.Item().Text("ALERTA COMEDOR").FontSize(16).Bold().FontColor(Colors.Red.Darken2);
                        alertCol.Item().Text("Los siguientes alumnos registraron ingreso y salida del establecimiento, pero NO pasaron por el comedor:").FontSize(12).FontColor(Colors.Red.Darken2);
                        
                        alertCol.Item().Table(table => BuildTable(table, alertasComedor, true));
                    });
                }
            });
        }

        void ComposeContentInasistencia(IContainer container, List<RegistroFila> data)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(15);
                column.Item().Text("Alumnos Ausentes (Sin ingreso registrado)").FontSize(15).SemiBold().FontColor(Colors.Grey.Darken3);
                column.Item().Table(table => BuildTable(table, data));
            });
        }

        void BuildTable(QuestPDF.Fluent.TableDescriptor table, List<RegistroFila> data, bool isAlert = false)
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(70); // RUT
                columns.RelativeColumn();   // Nombre
                columns.ConstantColumn(40); // Curso
                columns.ConstantColumn(60); // Ingreso
                columns.ConstantColumn(60); // Comedor
                columns.ConstantColumn(60); // Salida
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
                var fontColor = isAlert ? Colors.Red.Darken2 : Colors.Black;
                
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.Rut).FontColor(fontColor);
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.Nombre).FontColor(fontColor);
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.Curso).FontColor(fontColor);
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.HoraIngreso).FontColor(fontColor);
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.HoraComedor).FontColor(fontColor);
                table.Cell().PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Text(item.HoraSalida).FontColor(fontColor);
            }
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            if (listaActual.Count == 0)
            {
                MessageBox.Show("No hay registros para exportar en esta fecha.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string pdfPath = GenerarReportePdfFisico(false);
                if (pdfPath != null)
                {
                    try {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                            FileName = pdfPath,
                            UseShellExecute = true
                        });
                    } catch {
                        MessageBox.Show($"Reporte guardado exitosamente en:\n{pdfPath}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generando PDF: " + ex.Message);
            }
        }

        private void BtnReporteInasistencia_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pdfPath = GenerarReporteInasistenciaPdfFisico();
                if (pdfPath != null)
                {
                    try {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                            FileName = pdfPath,
                            UseShellExecute = true
                        });
                    } catch {
                        MessageBox.Show($"Reporte guardado exitosamente en:\n{pdfPath}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("No se encontraron alumnos ausentes para los criterios seleccionados.", "Sin resultados", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generando PDF: " + ex.Message);
            }
        }

        private void BtnReporteComedorPendiente_Click(object sender, RoutedEventArgs e)
        {
            if (!dpFecha.SelectedDate.HasValue) return;
            string fechaStr = dpFecha.SelectedDate.Value.ToString("yyyy-MM-dd");

            string pdfPath = Services.ReportesAutomaticosService.GenerarReporteComedor(fechaStr);
            if (pdfPath != null && System.IO.File.Exists(pdfPath))
            {
                try {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                        FileName = pdfPath,
                        UseShellExecute = true
                    });
                } catch {
                    MessageBox.Show($"Reporte generado exitosamente en:\n{pdfPath}", "Reporte Generado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("No hay alumnos que cumplan la condición (con ingreso pero sin comedor) para la fecha seleccionada.", "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnEnviar_Click(object sender, RoutedEventArgs e)
        {
            if (listaActual.Count == 0)
            {
                MessageBox.Show("No hay registros para enviar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            btnEnviar.IsEnabled = false;
            btnEnviar.Content = "ENVIANDO...";

            try
            {
                // Buscar correos de admins
                List<string> correosAdmins = new List<string>();
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Email FROM Usuarios WHERE Role = 'admin' AND Email IS NOT NULL AND Email != ''";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            correosAdmins.Add(reader.GetString(0));
                        }
                    }
                }

                if (correosAdmins.Count == 0)
                {
                    MessageBox.Show("No hay administradores con correo configurado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string pdfPath = GenerarReportePdfFisico(false);
                string fecha = dpFecha.SelectedDate.Value.ToString("dd/MM/yyyy");

                if (pdfPath != null)
                {
                    await EmailService.EnviarReporteAdminsAsync(correosAdmins, pdfPath, fecha);
                    MessageBox.Show("Reporte encolado exitosamente para envío a los administradores.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error enviando correos: " + ex.Message);
            }
            finally
            {
                btnEnviar.IsEnabled = true;
                btnEnviar.Content = "ENVIAR A ADMINS";
            }
        }
            private void BtnEliminarIngreso_Click(object sender, RoutedEventArgs e)
        {
            if (MainShell.CurrentRole != "admin") return;
            string rut = (sender as Button)?.CommandParameter?.ToString();
            if (!string.IsNullOrEmpty(rut)) {
                string fecha = dpFecha.SelectedDate.Value.ToString("yyyy-MM-dd");
                if (MessageBox.Show($"¿Eliminar registro de INGRESO de {rut} del día {fecha}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) {
                    EjecutarSql("UPDATE AsistenciaColegio SET HoraIngreso = '', IsSynced = 0 WHERE AlumnoId = @rut AND Fecha = @fecha", rut, fecha);
                    if (LimpiarColegioVacio(rut, fecha)) {
                        DeleteFromApi("delete_asistencia_colegio", new { rut = rut, fecha = fecha });
                    }
                    CargarRegistros();
                }
            }
        }
        
        private void BtnEliminarSalida_Click(object sender, RoutedEventArgs e)
        {
            if (MainShell.CurrentRole != "admin") return;
            string rut = (sender as Button)?.CommandParameter?.ToString();
            if (!string.IsNullOrEmpty(rut)) {
                string fecha = dpFecha.SelectedDate.Value.ToString("yyyy-MM-dd");
                if (MessageBox.Show($"¿Eliminar registro de SALIDA de {rut} del día {fecha}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) {
                    EjecutarSql("UPDATE AsistenciaColegio SET HoraSalida = NULL, IsSynced = 0 WHERE AlumnoId = @rut AND Fecha = @fecha", rut, fecha);
                    if (LimpiarColegioVacio(rut, fecha)) {
                        DeleteFromApi("delete_asistencia_colegio", new { rut = rut, fecha = fecha });
                    }
                    CargarRegistros();
                }
            }
        }

        private bool LimpiarColegioVacio(string rut, string fecha)
        {
            try {
                using (var conn = LocalDatabaseManager.GetConnection()) {
                    conn.Open();
                    var cmdCheck = conn.CreateCommand();
                    cmdCheck.CommandText = "SELECT COUNT(*) FROM AsistenciaColegio WHERE AlumnoId = @rut AND Fecha = @fecha AND (HoraIngreso = '' OR HoraIngreso IS NULL) AND HoraSalida IS NULL";
                    cmdCheck.Parameters.AddWithValue("@rut", rut);
                    cmdCheck.Parameters.AddWithValue("@fecha", fecha);
                    if ((long)cmdCheck.ExecuteScalar() > 0) {
                        var cmdDel = conn.CreateCommand();
                        cmdDel.CommandText = "DELETE FROM AsistenciaColegio WHERE AlumnoId = @rut AND Fecha = @fecha";
                        cmdDel.Parameters.AddWithValue("@rut", rut);
                        cmdDel.Parameters.AddWithValue("@fecha", fecha);
                        cmdDel.ExecuteNonQuery();
                        return true;
                    }
                }
            } catch { }
            return false;
        }

        private void BtnEliminarComedor_Click(object sender, RoutedEventArgs e)
        {
            if (MainShell.CurrentRole != "admin") return;
            string rut = (sender as Button)?.CommandParameter?.ToString();
            if (!string.IsNullOrEmpty(rut)) {
                string fecha = dpFecha.SelectedDate.Value.ToString("yyyy-MM-dd");
                if (MessageBox.Show($"¿Eliminar registro de COMEDOR de {rut} del día {fecha}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) {
                    EjecutarSql("DELETE FROM AsistenciaComedor WHERE AlumnoId = @rut AND Fecha = @fecha", rut, fecha);
                    DeleteFromApi("delete_asistencia_comedor", new { rut = rut, fecha = fecha });
                    CargarRegistros();
                }
            }
        }
        
        private void EjecutarSql(string sql, string rut, string fecha)
        {
            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@rut", rut);
                    cmd.Parameters.AddWithValue("@fecha", fecha);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al borrar registro: " + ex.Message);
            }
        }

        private void BtnBorrarTodo_Click(object sender, RoutedEventArgs e)
        {
            if (MainShell.CurrentUser != "cirdam") return;

            string fecha = dpFecha.SelectedDate.Value.ToString("yyyy-MM-dd");
            var result = MessageBox.Show($"¡ATENCIÓN! ¿Está absolutamente seguro de querer ELIMINAR TODOS los registros de asistencia y comedor del día {fecha}?\n\nEsta acción no se puede deshacer.", 
                                         "ALERTA CRÍTICA", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = LocalDatabaseManager.GetConnection())
                    {
                        conn.Open();

                        var cmdCol = conn.CreateCommand();
                        cmdCol.CommandText = "DELETE FROM AsistenciaColegio WHERE Fecha = @fecha";
                        cmdCol.Parameters.AddWithValue("@fecha", fecha);
                        cmdCol.ExecuteNonQuery();

                        var cmdCom = conn.CreateCommand();
                        cmdCom.CommandText = "DELETE FROM AsistenciaComedor WHERE Fecha = @fecha";
                        cmdCom.Parameters.AddWithValue("@fecha", fecha);
                        cmdCom.ExecuteNonQuery();
                    }
                    DeleteFromApi("delete_todo_fecha", new { fecha = fecha });
                    MessageBox.Show("Todos los registros de la fecha seleccionada han sido eliminados localmente y en la nube.", "Operación Completada", MessageBoxButton.OK, MessageBoxImage.Information);
                    CargarRegistros();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al borrar los registros: " + ex.Message);
                }
            }
        }

        private async void DeleteFromApi(string action, object data)
        {
            try
            {
                var payload = new { action = action, data = data };
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, AsistenciaDesktop_v2.Config.AppConfig.ApiUrl);
                request.Headers.Add("Authorization", "Bearer " + AsistenciaDesktop_v2.Config.AppConfig.ApiSecret);
                request.Headers.Add("User-Agent", "Mozilla/5.0");
                request.Headers.Add("Accept", "application/json");
                request.Content = content;

                var client = new System.Net.Http.HttpClient();
                await client.SendAsync(request);
            }
            catch { }
        }
    }
}
















