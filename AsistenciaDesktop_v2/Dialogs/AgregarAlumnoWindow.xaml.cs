using System;
using System.Windows;
using AsistenciaDesktop_v2.Data;

namespace AsistenciaDesktop_v2.Dialogs
{
    public partial class AgregarAlumnoWindow : Window
    {
        public AgregarAlumnoWindow()
        {
            InitializeComponent();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string rutOriginal = txtRut.Text.Trim();
            string nombre = txtNombre.Text.Trim();
            string curso = txtCurso.Text.Trim();
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrEmpty(rutOriginal) || string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(curso))
            {
                MessageBox.Show("RUT, Nombre y Curso son obligatorios.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!LocalDatabaseManager.ValidaRut(rutOriginal))
            {
                MessageBox.Show("RUT Inválido. Por favor, verifique.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string cleanRut = LocalDatabaseManager.CleanRut(rutOriginal);

            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();

                    // Check if exists
                    var cmdCheck = conn.CreateCommand();
                    cmdCheck.CommandText = "SELECT COUNT(*) FROM Alumnos WHERE NumeroUnico = @rut";
                    cmdCheck.Parameters.AddWithValue("@rut", cleanRut);
                    
                    if ((long)cmdCheck.ExecuteScalar() > 0)
                    {
                        MessageBox.Show("Ya existe un alumno con ese RUT.", "Duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Insert (IsSynced = 0 para que el motor lo suba a MySQL)
                    var cmdInsert = conn.CreateCommand();
                    cmdInsert.CommandText = "INSERT INTO Alumnos (NumeroUnico, Nombre, Curso, EmailApoderado, IsSynced) VALUES (@rut, @nombre, @curso, @email, 0)";
                    cmdInsert.Parameters.AddWithValue("@rut", cleanRut);
                    cmdInsert.Parameters.AddWithValue("@nombre", nombre);
                    cmdInsert.Parameters.AddWithValue("@curso", curso);
                    cmdInsert.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                    cmdInsert.ExecuteNonQuery();

                    MessageBox.Show("Alumno registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
