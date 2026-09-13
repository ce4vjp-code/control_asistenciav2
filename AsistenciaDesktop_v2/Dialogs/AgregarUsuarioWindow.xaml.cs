using System;
using System.Windows;
using System.Windows.Controls;
using AsistenciaDesktop_v2.Data;

namespace AsistenciaDesktop_v2.Dialogs
{
    public partial class AgregarUsuarioWindow : Window
    {
        public AgregarUsuarioWindow()
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
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Password;
            string role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "operador";
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Usuario y Contraseña son obligatorios.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();

                    var cmdCheck = conn.CreateCommand();
                    cmdCheck.CommandText = "SELECT COUNT(*) FROM Usuarios WHERE Username = @user";
                    cmdCheck.Parameters.AddWithValue("@user", user);
                    
                    if ((long)cmdCheck.ExecuteScalar() > 0)
                    {
                        MessageBox.Show("Ya existe ese nombre de usuario.", "Duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var cmdInsert = conn.CreateCommand();
                    cmdInsert.CommandText = "INSERT INTO Usuarios (Username, PasswordHash, Role, Email, IsSynced) VALUES (@u, @p, @r, @e, 0)";
                    cmdInsert.Parameters.AddWithValue("@u", user);
                    cmdInsert.Parameters.AddWithValue("@p", LocalDatabaseManager.HashPassword(pass));
                    cmdInsert.Parameters.AddWithValue("@r", role);
                    cmdInsert.Parameters.AddWithValue("@e", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                    
                    cmdInsert.ExecuteNonQuery();

                    MessageBox.Show("Usuario creado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
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
