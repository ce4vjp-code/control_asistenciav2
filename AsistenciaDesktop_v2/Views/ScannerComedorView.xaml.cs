using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Media;
using AsistenciaDesktop_v2.Data;

namespace AsistenciaDesktop_v2.Views
{
    public partial class ScannerComedorView : UserControl
    {
        public ScannerComedorView()
        {
            InitializeComponent();
            this.Loaded += (s, e) => { 
                txtScanComedor.Focus(); 
                ActualizarEstadisticas(); 
                CargarListaAlumnos();
            };
        }

        private void txtScanComedor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string rut = txtScanComedor.Text.Trim();
                txtScanComedor.Text = "";
                if (string.IsNullOrEmpty(rut)) return;

                string cleanRut = LocalDatabaseManager.CleanRut(rut);
                ProcesarComedor(cleanRut);
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            CargarListaAlumnos();
        }

        private void btnMarcarManual_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string rut)
            {
                ProcesarComedor(rut);
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

                    string sql = @"SELECT a.NumeroUnico, a.Nombre, a.Curso 
                                 FROM Alumnos a
                                 INNER JOIN AsistenciaColegio ac ON a.NumeroUnico = ac.AlumnoId
                                 LEFT JOIN AsistenciaComedor am ON a.NumeroUnico = am.AlumnoId AND am.Fecha = ac.Fecha
                                 WHERE ac.Fecha = @fecha 
                                   AND ac.HoraSalida IS NULL 
                                   AND am.HoraIngreso IS NULL ";

                    if (!string.IsNullOrEmpty(filter))
                    {
                        sql += " AND (a.NumeroUnico LIKE @f OR a.Nombre LIKE @f OR a.Curso LIKE @f) ";
                    }
                    sql += " ORDER BY a.Nombre";

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@fecha", fechaHoy);
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

        private void ProcesarComedor(string rut)
        {
            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    string fechaHoy = DateTime.Now.ToString("yyyy-MM-dd");
                    string hora = DateTime.Now.ToString("HH:mm:ss");
                    
                    // Buscar alumno
                    var cmdBusqueda = conn.CreateCommand();
                    cmdBusqueda.CommandText = "SELECT Nombre, Curso FROM Alumnos WHERE NumeroUnico = @rut";
                    cmdBusqueda.Parameters.AddWithValue("@rut", rut);
                    
                    string nombre = null, curso = null;
                    using (var reader = cmdBusqueda.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            nombre = reader.GetString(0);
                            curso = reader.GetString(1);
                        }
                    }

                    if (nombre == null)
                    {
                        MostrarMensaje("No Encontrado", "-", "El alumno no está registrado.", false);
                        SystemSounds.Hand.Play();
                        return;
                    }

                    // Chequear si ya marcó salida (no puede almorzar)
                    var cmdCheckSalida = conn.CreateCommand();
                    cmdCheckSalida.CommandText = "SELECT HoraSalida FROM AsistenciaColegio WHERE AlumnoId = @rut AND Fecha = @fecha";
                    cmdCheckSalida.Parameters.AddWithValue("@rut", rut);
                    cmdCheckSalida.Parameters.AddWithValue("@fecha", fechaHoy);

                    bool hasSalida = false;
                    bool hasIngreso = false;
                    using (var reader = cmdCheckSalida.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            hasIngreso = true;
                            if (!reader.IsDBNull(0))
                            {
                                hasSalida = true;
                            }
                        }
                    }

                    if (!hasIngreso)
                    {
                        MostrarMensaje(nombre, curso, "No ha registrado ingreso al colegio.", false);
                        SystemSounds.Hand.Play();
                        return;
                    }

                    if (hasSalida)
                    {
                        MostrarMensaje(nombre, curso, "El alumno ya registró salida.", false);
                        SystemSounds.Hand.Play();
                        return;
                    }

                    // Chequear si ya recibió colación
                    var cmdCheck = conn.CreateCommand();
                    cmdCheck.CommandText = "SELECT COUNT(*) FROM AsistenciaComedor WHERE AlumnoId = @rut AND Fecha = @fecha";
                    cmdCheck.Parameters.AddWithValue("@rut", rut);
                    cmdCheck.Parameters.AddWithValue("@fecha", fechaHoy);

                    long count = (long)cmdCheck.ExecuteScalar();
                    if (count > 0)
                    {
                        MostrarMensaje(nombre, curso, "Ya recibió su colación hoy.", true);
                        SystemSounds.Exclamation.Play();
                        return;
                    }

                    // Registrar (IsSynced = 0)
                    var cmdInsert = conn.CreateCommand();
                    cmdInsert.CommandText = "INSERT INTO AsistenciaComedor (AlumnoId, Fecha, HoraIngreso, IsSynced) VALUES (@rut, @fecha, @hora, 0)";
                    cmdInsert.Parameters.AddWithValue("@rut", rut);
                    cmdInsert.Parameters.AddWithValue("@fecha", fechaHoy);
                    cmdInsert.Parameters.AddWithValue("@hora", hora);
                    cmdInsert.ExecuteNonQuery();

                    MostrarMensaje(nombre, curso, $"Colación entregada a las {hora}", true);
                    SystemSounds.Beep.Play();
                    ActualizarEstadisticas();
                    CargarListaAlumnos();
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
                txtMensajeUltimo.Foreground = new SolidColorBrush(Color.FromRgb(0, 150, 136)); // Teal (Exito)
                IconoEstado.Foreground = new SolidColorBrush(Color.FromRgb(0, 150, 136));
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

                    var cmdAsist = conn.CreateCommand();
                    cmdAsist.CommandText = "SELECT COUNT(*) FROM AsistenciaComedor WHERE Fecha = @fecha";
                    cmdAsist.Parameters.AddWithValue("@fecha", fechaHoy);
                    long total = (long)cmdAsist.ExecuteScalar();

                    txtTotalComedor.Text = total.ToString();
                }
            }
            catch { }
        }
    }
}
