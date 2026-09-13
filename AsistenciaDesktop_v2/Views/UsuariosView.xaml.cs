using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AsistenciaDesktop_v2.Data;
using AsistenciaDesktop_v2.Dialogs;

namespace AsistenciaDesktop_v2.Views
{
    public partial class UsuariosView : UserControl
    {
                public UsuariosView()
        {
            InitializeComponent();
            this.Loaded += (s, e) => {
                CargarUsuarios();
                if (MainShell.CurrentUser == "cirdam") {
                    CardConfiguracion.Visibility = Visibility.Visible;
                    CargarConfiguracion();
                }
            };
        }

        private void CargarConfiguracion()
        {
            try {
                using (var conn = LocalDatabaseManager.GetConnection()) {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Valor FROM Configuracion WHERE Clave = 'HoraReporteComedor'";
                    var result = cmd.ExecuteScalar()?.ToString();
                    if (!string.IsNullOrEmpty(result)) {
                        if (DateTime.TryParse(result, out DateTime dt)) {
                            tpReporteComedor.SelectedTime = dt;
                        }
                    }
                }
            } catch { }
        }

        private void BtnGuardarHora_Click(object sender, RoutedEventArgs e)
        {
            if (tpReporteComedor.SelectedTime.HasValue) {
                string hora = tpReporteComedor.SelectedTime.Value.ToString("HH:mm");
                try {
                    using (var conn = LocalDatabaseManager.GetConnection()) {
                        conn.Open();
                        var cmd = conn.CreateCommand();
                                                cmd.CommandText = "UPDATE Configuracion SET Valor = @val WHERE Clave = 'HoraReporteComedor'";
                        cmd.Parameters.AddWithValue("@val", hora);
                        if (cmd.ExecuteNonQuery() == 0) {
                            cmd.CommandText = "INSERT INTO Configuracion (Clave, Valor) VALUES ('HoraReporteComedor', @val)";
                            cmd.ExecuteNonQuery();
                        }
                        
                        cmd.CommandText = "UPDATE Configuracion SET Valor = '' WHERE Clave = 'UltimoReporteComedorFecha'";
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Hora de reporte actualizada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                } catch (Exception ex) {
                    MessageBox.Show("Error guardando configuración: " + ex.Message);
                }
            } else {
                MessageBox.Show("Por favor seleccione una hora.");
            }
        }

        private void CargarUsuarios()
        {
            var lista = new List<Usuario>();
            try
            {
                using (var conn = LocalDatabaseManager.GetConnection())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Username, Role, Email FROM Usuarios ORDER BY Username ASC";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Usuario
                            {
                                Username = reader.GetString(0),
                                Role = reader.GetString(1),
                                Email = reader.IsDBNull(2) ? "" : reader.GetString(2)
                            });
                        }
                    }
                }
                GridUsuarios.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando usuarios: " + ex.Message);
            }
        }

        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AgregarUsuarioWindow();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                CargarUsuarios();
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var usuario = button?.CommandParameter as Usuario;

            if (usuario != null)
            {
                var dialog = new EditarUsuarioWindow(usuario);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    CargarUsuarios();
                }
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string user = button?.CommandParameter?.ToString();

            if (user == "cirdam")
            {
                MessageBox.Show("El usuario 'cirdam' es el administrador principal y no puede ser eliminado.", "Acción denegada", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            if (!string.IsNullOrEmpty(user))
            {
                var result = MessageBox.Show($"Â¿Eliminar al usuario {user}?", 
                                             "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var conn = LocalDatabaseManager.GetConnection())
                    {
                        conn.Open();
                        var cmdDelete = conn.CreateCommand();
                        cmdDelete.CommandText = "DELETE FROM Usuarios WHERE Username = @u";
                                                cmdDelete.Parameters.AddWithValue("@u", user);
                        cmdDelete.ExecuteNonQuery();
                    }
                    
                    // Sincronizar eliminación con el backend
                    _ = AsistenciaDesktop_v2.Data.SyncEngine.SendApiRequest("delete_usuario", new { username = user });
                    
                    CargarUsuarios();
                }
            }
        }
    }
}




