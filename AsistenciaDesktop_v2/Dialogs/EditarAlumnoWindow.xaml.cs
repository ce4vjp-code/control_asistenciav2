using System;
using System.Windows;
using AsistenciaDesktop_v2.Data;

namespace AsistenciaDesktop_v2.Dialogs
{
    public partial class EditarAlumnoWindow : Window
    {
        public EditarAlumnoWindow(Alumno alumno)
        {
            InitializeComponent();
            txtRut.Text = alumno.NumeroUnico;
            txtNombre.Text = alumno.Nombre;
            txtCurso.Text = alumno.Curso;
            txtEmail.Text = alumno.EmailApoderado;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string rut = txtRut.Text.Trim();
            string nombre = txtNombre.Text.Trim();
            string curso = txtCurso.Text.Trim();
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(curso))
            {
                MessageBox.Show("Nombre y Curso son obligatorios.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();

                    // Actualizar (IsSynced = 0 para que suba a MySQL el cambio)
                    var cmdUpdate = conn.CreateCommand();
                    cmdUpdate.CommandText = "UPDATE Alumnos SET Nombre = @nombre, Curso = @curso, EmailApoderado = @email, IsSynced = 0 WHERE NumeroUnico = @rut";
                    cmdUpdate.Parameters.AddWithValue("@rut", rut);
                    cmdUpdate.Parameters.AddWithValue("@nombre", nombre);
                    cmdUpdate.Parameters.AddWithValue("@curso", curso);
                    cmdUpdate.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                    cmdUpdate.ExecuteNonQuery();

                    MessageBox.Show("Alumno actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
