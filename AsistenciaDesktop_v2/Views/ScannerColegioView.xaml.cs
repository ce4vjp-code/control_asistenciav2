using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Media;
using AsistenciaDesktop_v2.Data;
using AsistenciaDesktop_v2.Services;

namespace AsistenciaDesktop_v2.Views
{
    public partial class ScannerColegioView : UserControl
    {
        public ScannerColegioView()
        {
            InitializeComponent();
            this.Loaded += (s, e) => { 
                txtScanColegio.Focus(); 
                ActualizarEstadisticas(); 
                CargarListaAlumnos();
            };
        }

        private void txtScanColegio_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string rut = txtScanColegio.Text.Trim();
                txtScanColegio.Text = "";
                if (string.IsNullOrEmpty(rut)) return;

                string cleanRut = LocalDatabaseManager.CleanRut(rut);
                ProcesarIngreso(cleanRut);
            }
        }

        private void tglModo_Click(object sender, RoutedEventArgs e)
        {
            bool isSalida = tglModo.IsChecked == true;
            txtTitulo.Text = isSalida ? "Control de Asistencia - Salida" : "Control de Asistencia - Ingreso";
            txtLabelTotal.Text = isSalida ? "Total Salidas" : "Total Ingresos";
            ActualizarEstadisticas();
            CargarListaAlumnos();
            txtScanColegio.Focus();
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            CargarListaAlumnos();
        }

        private void btnMarcarManual_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string rut)
            {
                ProcesarIngreso(rut);
            }
        }

        public void CargarListaAlumnos()
        {
            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    string filter = txtBuscar.Text.Trim();
                    string fechaHoy = DateTime.Now.ToString("yyyy-MM-dd");
                    bool isSalida = tglModo.IsChecked == true;

                    string sql = "";
                    if (isSalida)
                    {
                        sql = @"SELECT a.NumeroUnico, a.Nombre, a.Curso 
                                FROM Alumnos a 
                                INNER JOIN AsistenciaColegio ac ON a.NumeroUnico = ac.AlumnoId 
                                WHERE ac.Fecha = @fecha AND ac.HoraSalida IS NULL ";
                    }
                    else
                    {
                        sql = "SELECT NumeroUnico, Nombre, Curso FROM Alumnos WHERE 1=1 ";
                    }

                    if (!string.IsNullOrEmpty(filter))
                    {
                        sql += " AND (a.NumeroUnico LIKE @f OR a.Nombre LIKE @f OR a.Curso LIKE @f) ";
                    }

                    sql += " ORDER BY a.Nombre";
                    if (!isSalida) sql = sql.Replace("a.NumeroUnico", "NumeroUnico").Replace("a.Nombre", "Nombre").Replace("a.Curso", "Curso");

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    if (isSalida) cmd.Parameters.AddWithValue("@fecha", fechaHoy);
                    if (!string.IsNullOrEmpty(filter)) cmd.Parameters.AddWithValue("@f", "%" + filter + "%");

                    var list = new System.Collections.Generic.List<Alumno>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Alumno
                            {
                                NumeroUnico = reader.GetString(0),
                                Nombre = reader.GetString(1),
                                Curso = reader.GetString(2)
                            });
                        }
                    }
                    dgAlumnos.ItemsSource = list;
                }
            }
            catch { }
        }

        private void ProcesarIngreso(string rut)
        {
            bool isSalida = tglModo.IsChecked == true;
            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    
                    // Buscar alumno
                    var cmdBusqueda = conn.CreateCommand();
                    cmdBusqueda.CommandText = "SELECT Nombre, Curso, EmailApoderado FROM Alumnos WHERE NumeroUnico = @rut";
                    cmdBusqueda.Parameters.AddWithValue("@rut", rut);
                    
                    string nombre = null, curso = null, email = null;
                    using (var reader = cmdBusqueda.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            nombre = reader.GetString(0);
                            curso = reader.GetString(1);
                            email = reader.IsDBNull(2) ? null : reader.GetString(2);
                        }
                    }

                    if (nombre == null)
                    {
                        MostrarMensaje("No Encontrado", "-", "El alumno no está registrado.", false);
                        SystemSounds.Hand.Play();
                        return;
                    }

                    string fechaHoy = DateTime.Now.ToString("yyyy-MM-dd");
                    string hora = DateTime.Now.ToString("HH:mm:ss");

                    if (isSalida)
                    {
                        // Chequear si ya ingresó y si ya salió
                        var cmdCheck = conn.CreateCommand();
                        cmdCheck.CommandText = "SELECT HoraSalida FROM AsistenciaColegio WHERE AlumnoId = @rut AND Fecha = @fecha";
                        cmdCheck.Parameters.AddWithValue("@rut", rut);
                        cmdCheck.Parameters.AddWithValue("@fecha", fechaHoy);

                        using (var reader = cmdCheck.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    MostrarMensaje(nombre, curso, "Ya registró su salida hoy.", true);
                                    SystemSounds.Exclamation.Play();
                                    return;
                                }
                            }
                            else
                            {
                                MostrarMensaje(nombre, curso, "No ha registrado ingreso hoy.", false);
                                SystemSounds.Hand.Play();
                                return;
                            }
                        }

                        // Registrar Salida
                        var cmdUpdate = conn.CreateCommand();
                        cmdUpdate.CommandText = "UPDATE AsistenciaColegio SET HoraSalida = @hora, IsSynced = 0 WHERE AlumnoId = @rut AND Fecha = @fecha";
                        cmdUpdate.Parameters.AddWithValue("@rut", rut);
                        cmdUpdate.Parameters.AddWithValue("@fecha", fechaHoy);
                        cmdUpdate.Parameters.AddWithValue("@hora", hora);
                        cmdUpdate.ExecuteNonQuery();

                        MostrarMensaje(nombre, curso, $"Salida registrada a las {hora}", true);
                        SystemSounds.Beep.Play();
                        ActualizarEstadisticas();
                        CargarListaAlumnos();
                    }
                    else
                    {
                        // Chequear si ya ingresó
                        var cmdCheck = conn.CreateCommand();
                        cmdCheck.CommandText = "SELECT COUNT(*) FROM AsistenciaColegio WHERE AlumnoId = @rut AND Fecha = @fecha";
                        cmdCheck.Parameters.AddWithValue("@rut", rut);
                        cmdCheck.Parameters.AddWithValue("@fecha", fechaHoy);

                        long count = (long)cmdCheck.ExecuteScalar();
                        if (count > 0)
                        {
                            MostrarMensaje(nombre, curso, "Ya registró su ingreso hoy.", true);
                            SystemSounds.Exclamation.Play();
                            return;
                        }

                        // Registrar Ingreso (IsSynced = 0)
                        var cmdInsert = conn.CreateCommand();
                        cmdInsert.CommandText = "INSERT INTO AsistenciaColegio (AlumnoId, Fecha, HoraIngreso, IsSynced) VALUES (@rut, @fecha, @hora, 0)";
                        cmdInsert.Parameters.AddWithValue("@rut", rut);
                        cmdInsert.Parameters.AddWithValue("@fecha", fechaHoy);
                        cmdInsert.Parameters.AddWithValue("@hora", hora);
                        cmdInsert.ExecuteNonQuery();

                        MostrarMensaje(nombre, curso, $"Ingreso registrado a las {hora}", true);
                        SystemSounds.Beep.Play();
                        ActualizarEstadisticas();
                        CargarListaAlumnos();

                        // Disparar Email de forma asíncrona (fuego y olvido)
                        if (!string.IsNullOrEmpty(email))
                        {
                            _ = EmailService.EnviarNotificacionIngresoAsync(email, nombre, hora);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error", "", ex.Message, false);
            }
        }

        private void MostrarMensaje(string titulo, string subtitulo, string mensaje, bool exito)
        {
            txtNombreUltimo.Text = titulo;
            txtCursoUltimo.Text = subtitulo;
            txtMensajeUltimo.Text = mensaje;
            
            if (exito)
            {
                txtMensajeUltimo.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // Verde
                IconoEstado.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else
            {
                txtMensajeUltimo.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Rojo
                IconoEstado.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
        }

        public void ActualizarEstadisticas()
        {
            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    string fechaHoy = DateTime.Now.ToString("yyyy-MM-dd");
                    bool isSalida = tglModo.IsChecked == true;

                    var cmdTotal = conn.CreateCommand();
                    cmdTotal.CommandText = "SELECT COUNT(*) FROM Alumnos";
                    long totalAlumnos = (long)cmdTotal.ExecuteScalar();

                    var cmdAsist = conn.CreateCommand();
                    if (isSalida)
                    {
                        cmdAsist.CommandText = "SELECT COUNT(*) FROM AsistenciaColegio WHERE Fecha = @fecha AND HoraSalida IS NOT NULL";
                    }
                    else
                    {
                        cmdAsist.CommandText = "SELECT COUNT(*) FROM AsistenciaColegio WHERE Fecha = @fecha AND HoraIngreso IS NOT NULL";
                    }
                    cmdAsist.Parameters.AddWithValue("@fecha", fechaHoy);
                    long totalAsistencias = (long)cmdAsist.ExecuteScalar();

                    txtTotalIngresos.Text = totalAsistencias.ToString();

                    if (totalAlumnos > 0)
                    {
                        double pct = (double)totalAsistencias / totalAlumnos * 100.0;
                        txtPorcentaje.Text = $"{pct:F1}%";
                    }
                }
            }
            catch { }
        }
    }
}
