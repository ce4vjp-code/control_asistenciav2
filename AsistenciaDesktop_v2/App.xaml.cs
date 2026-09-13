using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AsistenciaDesktop_v2.Data;

namespace AsistenciaDesktop_v2
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            MessageBox.Show("Error de Tarea en Segundo Plano:\n" + e.Exception.Message, "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Error Crítico del Sistema:\n" + ((Exception)e.ExceptionObject).Message, "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
        }

                private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Console.WriteLine(e.Exception.ToString());
            MessageBox.Show("Error Inesperado en la Interfaz:\n" + e.Exception.ToString(), "Error de UI", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Fix encoding para CSV si se corre en Windows viejo
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // 1. Inicializar DB Local (Obligatoria siempre)
            LocalDatabaseManager.InitializeDatabase();

            // 2. Intentar Inicializar DB Remota (Asíncrono, no bloquea inicio)
            _ = RemoteDatabaseManager.InitializeDatabaseAsync();

            // 3. Iniciar el motor de sincronización de datos local -> remoto (cada 30s)
            SyncEngine.StartSyncTimer();

            // 4. Iniciar Login
            var login = new Views.LoginWindow();
            login.Show();
        }
    }
}

