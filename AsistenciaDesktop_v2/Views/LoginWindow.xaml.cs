using System;
using System.Windows;
using System.Windows.Input;
using AsistenciaDesktop_v2.Data;

namespace AsistenciaDesktop_v2.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtUsername.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                txtError.Text = "Ingrese usuario y contraseña.";
                return;
            }

            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT PasswordHash, Role, Email FROM Usuarios WHERE Username = @user";
                    cmd.Parameters.AddWithValue("@user", user);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string hash = reader.GetString(0);
                            string role = reader.GetString(1);

                            if (LocalDatabaseManager.VerifyPassword(pass, hash))
                            {
                                var main = new MainShell(user, role);
                                main.Show();
                                this.Close();
                                return;
                            }
                        }
                    }
                }
                txtError.Text = "Credenciales incorrectas.";
            }
            catch (Exception ex)
            {
                txtError.Text = "Error BD: " + ex.Message;
            }
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }
    }
}
