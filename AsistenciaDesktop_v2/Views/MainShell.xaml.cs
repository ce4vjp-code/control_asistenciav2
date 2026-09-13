using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AsistenciaDesktop_v2.Data;
using AsistenciaDesktop_v2.Services;

namespace AsistenciaDesktop_v2.Views
{
    public partial class MainShell : Window
    {
        public static string CurrentUser { get; private set; }
        public static string CurrentRole { get; private set; }

                private DispatcherTimer _timerNotificaciones;

        public MainShell(string user, string role)
        {
            InitializeComponent();
            CurrentUser = user;
            CurrentRole = role;

            if (CurrentRole == "admin")
            {
                BtnAlumnos.Visibility = Visibility.Visible;
                BtnReportes.Visibility = Visibility.Visible;
                BtnUsuarios.Visibility = Visibility.Visible;
            }
            else
            {
                BtnReportes.Visibility = Visibility.Visible;
            }

            MainContent.Content = new ScannerColegioView();
            ActualizarEstilosBotones(BtnEscanner);

            _timerNotificaciones = new DispatcherTimer();
            _timerNotificaciones.Interval = TimeSpan.FromSeconds(30);
            _timerNotificaciones.Tick += (s, e) => CheckNotificacionesYReporteAutomatico();
            _timerNotificaciones.Start();
            CheckNotificacionesYReporteAutomatico();
        }

        private void CheckNotificacionesYReporteAutomatico()
        {
            try {
                using (var conn = LocalDatabaseManager.GetConnection()) {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    
                    cmd.CommandText = "SELECT COUNT(*) FROM ReportesGenerados WHERE Visto = 0";
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count > 0) {
                        BadgeNotificaciones.Badge = count.ToString();
                        BadgeNotificaciones.Visibility = Visibility.Visible;
                    } else {
                        BadgeNotificaciones.Visibility = Visibility.Collapsed;
                    }

                    cmd.CommandText = "SELECT Valor FROM Configuracion WHERE Clave = 'HoraReporteComedor'";
                    string horaConfigStr = cmd.ExecuteScalar()?.ToString();
                    
                    cmd.CommandText = "SELECT Valor FROM Configuracion WHERE Clave = 'UltimoReporteComedorFecha'";
                    string ultimaFecha = cmd.ExecuteScalar()?.ToString();

                    string hoy = DateTime.Now.ToString("yyyy-MM-dd");

                    if (!string.IsNullOrEmpty(horaConfigStr) && DateTime.TryParse(horaConfigStr, out DateTime horaConfig)) {
                        Logger.Log("Checking Time: " + DateTime.Now.TimeOfDay + " >= " + horaConfig.TimeOfDay + " && " + ultimaFecha + " != " + hoy);
                        if (DateTime.Now.TimeOfDay >= horaConfig.TimeOfDay && ultimaFecha != hoy) {
                            string pdfPath = ReportesAutomaticosService.GenerarReporteComedor(hoy);
                            
                            var updCmd = conn.CreateCommand();
                            updCmd.CommandText = "UPDATE Configuracion SET Valor = @v WHERE Clave = 'UltimoReporteComedorFecha'";
                            updCmd.Parameters.AddWithValue("@v", hoy);
                            if (updCmd.ExecuteNonQuery() == 0) {
                                updCmd.CommandText = "INSERT INTO Configuracion (Clave, Valor) VALUES ('UltimoReporteComedorFecha', @v)";
                                updCmd.ExecuteNonQuery();
                            }

                            if (!string.IsNullOrEmpty(pdfPath)) {
                                var insertCmd = conn.CreateCommand();
                                insertCmd.CommandText = "INSERT INTO ReportesGenerados (Fecha, Tipo, RutaArchivo, Visto) VALUES (@f, @t, @r, 0)";
                                insertCmd.Parameters.AddWithValue("@f", hoy);
                                insertCmd.Parameters.AddWithValue("@t", "Alerta Comedor Automático");
                                insertCmd.Parameters.AddWithValue("@r", pdfPath);
                                insertCmd.ExecuteNonQuery();
                                
                                CheckNotificacionesYReporteAutomatico(); // Refrescar badge
                            }
                        }
                    }
                }
            } catch (Exception ex) { Logger.Log("MainShell CheckNoti error: " + ex.ToString()); }
        }

        private void BtnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            try {
                using (var conn = LocalDatabaseManager.GetConnection()) {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT RutaArchivo FROM ReportesGenerados WHERE Visto = 0 ORDER BY Id DESC LIMIT 1";
                    string ruta = cmd.ExecuteScalar()?.ToString();
                    
                    if (!string.IsNullOrEmpty(ruta) && System.IO.File.Exists(ruta)) {
                        var sfd = new Microsoft.Win32.SaveFileDialog {
                            Filter = "PDF Document (*.pdf)|*.pdf",
                            FileName = System.IO.Path.GetFileName(ruta)
                        };
                        if (sfd.ShowDialog() == true) {
                            System.IO.File.Copy(ruta, sfd.FileName, true);
                            MessageBox.Show("Reporte descargado con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            cmd.CommandText = "UPDATE ReportesGenerados SET Visto = 1 WHERE RutaArchivo = @r";
                            cmd.Parameters.AddWithValue("@r", ruta);
                            cmd.ExecuteNonQuery();
                            
                            CheckNotificacionesYReporteAutomatico();
                        }
                    } else {
                        MessageBox.Show("No hay reportes automáticos pendientes, o el archivo ya no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                        cmd.CommandText = "UPDATE ReportesGenerados SET Visto = 1 WHERE Visto = 0";
                        cmd.ExecuteNonQuery();
                        CheckNotificacionesYReporteAutomatico();
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show("Error al descargar: " + ex.Message);
            }
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == BtnEscanner) MainContent.Content = new ScannerColegioView();
            else if (btn == BtnComedor) MainContent.Content = new ScannerComedorView();
            else if (btn == BtnAlumnos) MainContent.Content = new AlumnosView();
            else if (btn == BtnReportes) MainContent.Content = new RegistrosView();
            else if (btn == BtnUsuarios) MainContent.Content = new UsuariosView();

            ActualizarEstilosBotones(btn);
        }

        private void ActualizarEstilosBotones(Button activo)
        {
            // Limpiar fondo de todos
            BtnEscanner.Background = System.Windows.Media.Brushes.Transparent;
            BtnComedor.Background = System.Windows.Media.Brushes.Transparent;
            BtnAlumnos.Background = System.Windows.Media.Brushes.Transparent;
            BtnReportes.Background = System.Windows.Media.Brushes.Transparent;
            BtnUsuarios.Background = System.Windows.Media.Brushes.Transparent;

            // Resaltar el activo
            if (activo != null)
            {
                activo.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255));
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}



