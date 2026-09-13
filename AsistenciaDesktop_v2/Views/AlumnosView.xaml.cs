using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using AsistenciaDesktop_v2.Data;
using AsistenciaDesktop_v2.Dialogs;

namespace AsistenciaDesktop_v2.Views
{
    public partial class AlumnosView : UserControl
    {
        private List<Alumno> listaCompleta = new List<Alumno>();

        public AlumnosView()
        {
            InitializeComponent();
            this.Loaded += (s, e) => CargarAlumnos();
        }

        private void CargarAlumnos()
        {
            listaCompleta.Clear();
            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT NumeroUnico, Nombre, Curso, EmailApoderado FROM Alumnos ORDER BY Nombre ASC";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaCompleta.Add(new Alumno
                            {
                                NumeroUnico = reader.GetString(0),
                                Nombre = reader.GetString(1),
                                Curso = reader.GetString(2),
                                EmailApoderado = reader.IsDBNull(3) ? "" : reader.GetString(3)
                            });
                        }
                    }
                }
                Filtrar();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando alumnos: " + ex.Message);
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            Filtrar();
        }

        private void Filtrar()
        {
            string q = txtBuscar.Text.ToLower();
            
            GridAlumnos.ItemsSource = null; // Fuerza el refresco visual

            if (string.IsNullOrWhiteSpace(q))
            {
                GridAlumnos.ItemsSource = listaCompleta.ToList();
            }
            else
            {
                var filtrados = listaCompleta.FindAll(a => 
                    a.Nombre.ToLower().Contains(q) || 
                    a.NumeroUnico.ToLower().Contains(q)
                );
                GridAlumnos.ItemsSource = filtrados;
            }
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AgregarAlumnoWindow();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                CargarAlumnos();
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var alumno = button?.CommandParameter as Alumno;

            if (alumno != null)
            {
                var dialog = new EditarAlumnoWindow(alumno);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    CargarAlumnos();
                }
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string rut = button?.CommandParameter?.ToString();

            if (!string.IsNullOrEmpty(rut))
            {
                var result = MessageBox.Show($"Â¿Eliminar al alumno con RUT {rut} y toda su asistencia local?", 
                                             "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var conn = LocalDatabaseManager.GetConnection())
                        {
                            conn.Open();
                            
                            var cmdDeleteAsistCol = conn.CreateCommand();
                            cmdDeleteAsistCol.CommandText = "DELETE FROM AsistenciaColegio WHERE AlumnoId = @rut";
                            cmdDeleteAsistCol.Parameters.AddWithValue("@rut", rut);
                            cmdDeleteAsistCol.ExecuteNonQuery();

                            var cmdDeleteAsistCom = conn.CreateCommand();
                            cmdDeleteAsistCom.CommandText = "DELETE FROM AsistenciaComedor WHERE AlumnoId = @rut";
                            cmdDeleteAsistCom.Parameters.AddWithValue("@rut", rut);
                            cmdDeleteAsistCom.ExecuteNonQuery();

                            var cmdDelete = conn.CreateCommand();
                            cmdDelete.CommandText = "DELETE FROM Alumnos WHERE NumeroUnico = @rut";
                                                        cmdDelete.Parameters.AddWithValue("@rut", rut);
                            cmdDelete.ExecuteNonQuery();
                        }
                        
                        // Sincronizar eliminación con el backend
                        _ = AsistenciaDesktop_v2.Data.SyncEngine.SendApiRequest("delete_alumno", new { rut = rut });
                        
                        CargarAlumnos();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al eliminar alumno: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnImportarCSV_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "Archivos CSV (*.csv)|*.csv" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    int agregados = 0;
                    int ignorados = 0;

                                        // Detectar codificación inteligentemente (UTF-8 o ANSI/1252)
                    byte[] bytes = System.IO.File.ReadAllBytes(ofd.FileName);
                    string fileContent = System.Text.Encoding.UTF8.GetString(bytes);
                    if (fileContent.Contains('\uFFFD')) {
                        // Fallback a Windows-1252 (Excel por defecto en español)
                        fileContent = System.Text.Encoding.GetEncoding(1252).GetString(bytes);
                    }
                    var lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length == 0) return;

                    using (var conn = LocalDatabaseManager.GetConnection())
                    {
                        conn.Open();
                        using (var tx = conn.BeginTransaction())
                        {
                            var cmdCheck = conn.CreateCommand();
                            cmdCheck.CommandText = "SELECT COUNT(*) FROM Alumnos WHERE NumeroUnico = @rut";

                            var cmdInsert = conn.CreateCommand();
                            cmdInsert.CommandText = "INSERT INTO Alumnos (NumeroUnico, Nombre, Curso, EmailApoderado, IsSynced) VALUES (@rut, @nombre, @curso, @email, 0)";

                            // Detectar ÃƒÂ­ndices de columnas
                            int idxNombre = -1, idxRut = -1, idxCurso = -1, idxEmail = -1;
                            var header = lines[0].Split(';');
                            if (header.Length < 3) header = lines[0].Split(',');

                            for (int h = 0; h < header.Length; h++)
                            {
                                string hLower = header[h].ToLower().Trim();
                                if (hLower.Contains("nombre")) idxNombre = h;
                                else if (hLower.Contains("rut")) idxRut = h;
                                else if (hLower.Contains("curso")) idxCurso = h;
                                else if (hLower.Contains("email") || hLower.Contains("correo")) idxEmail = h;
                            }

                            // Fallback si no hay cabecera explÃƒÂ­cita
                            if (idxRut == -1) idxRut = 0;
                            if (idxNombre == -1) idxNombre = 1;
                            if (idxCurso == -1) idxCurso = 2;

                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (i == 0 && lines[i].ToLower().Contains("rut")) continue; // Skip header
                                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                                var partes = lines[i].Split(';');
                                if (partes.Length < 3)
                                {
                                    partes = lines[i].Split(','); // Fallback comma
                                }

                                if (partes.Length > Math.Max(idxRut, Math.Max(idxNombre, idxCurso)))
                                {
                                    string rutOriginal = partes[idxRut].Trim();
                                    string nombre = partes[idxNombre].Trim();
                                    string curso = partes[idxCurso].Trim();
                                    string email = (idxEmail != -1 && partes.Length > idxEmail) ? partes[idxEmail].Trim() : "";

                                    string cleanRut = LocalDatabaseManager.CleanRut(rutOriginal);
                                    if (string.IsNullOrEmpty(cleanRut)) continue;

                                    if (!LocalDatabaseManager.ValidaRut(cleanRut))
                                    {
                                        ignorados++; // Opcionalmente mostrar mensaje si es invalido
                                        continue;
                                    }

                                    cmdCheck.Parameters.Clear();
                                    cmdCheck.Parameters.AddWithValue("@rut", cleanRut);
                                    if ((long)cmdCheck.ExecuteScalar() == 0)
                                    {
                                        cmdInsert.Parameters.Clear();
                                        cmdInsert.Parameters.AddWithValue("@rut", cleanRut);
                                        cmdInsert.Parameters.AddWithValue("@nombre", nombre);
                                        cmdInsert.Parameters.AddWithValue("@curso", curso);
                                        cmdInsert.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                                        cmdInsert.ExecuteNonQuery();
                                        agregados++;
                                    }
                                    else
                                    {
                                        ignorados++;
                                    }
                                }
                            }
                            tx.Commit();
                        }
                    }

                    MessageBox.Show($"Carga completa.\nAgregados: {agregados}\nIgnorados (Ya existÃ­an): {ignorados}", "Resultado", MessageBoxButton.OK, MessageBoxImage.Information);
                    CargarAlumnos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al leer archivo: " + ex.Message);
                }
            }
        }
    }
}




