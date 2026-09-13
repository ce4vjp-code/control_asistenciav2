using System;
using System.Windows;
using System.Windows.Controls;
using AsistenciaDesktop_v2.Data;

namespace AsistenciaDesktop_v2.Dialogs
{
    public partial class EditarUsuarioWindow : Window
    {
        public EditarUsuarioWindow(Usuario usuario)
        {
            InitializeComponent();
            txtUsername.Text = usuario.Username;
            
            if (usuario.Role == "admin") cmbRole.SelectedIndex = 0;
            else if (usuario.Role == "operador") cmbRole.SelectedIndex = 1;

            txtEmail.Text = usuario.Email;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;
            string role = (cmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrEmpty(role))
            {
                MessageBox.Show("El rol es obligatorio.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();

                    if (!string.IsNullOrEmpty(password))
                    {
                        var cmdUpdate = conn.CreateCommand();
                        cmdUpdate.CommandText = "UPDATE Usuarios SET PasswordHash = @pass, Role = @role, Email = @email, IsSynced = 0 WHERE Username = @user";
                        cmdUpdate.Parameters.AddWithValue("@user", username);
                        cmdUpdate.Parameters.AddWithValue("@pass", LocalDatabaseManager.HashPassword(password));
                        cmdUpdate.Parameters.AddWithValue("@role", role);
                        cmdUpdate.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                        cmdUpdate.ExecuteNonQuery();
                    }
                    else
                    {
                        var cmdUpdate = conn.CreateCommand();
                        cmdUpdate.CommandText = "UPDATE Usuarios SET Role = @role, Email = @email, IsSynced = 0 WHERE Username = @user";
                        cmdUpdate.Parameters.AddWithValue("@user", username);
                        cmdUpdate.Parameters.AddWithValue("@role", role);
                        cmdUpdate.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                        cmdUpdate.ExecuteNonQuery();
                    }

                    MessageBox.Show("Usuario actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
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
