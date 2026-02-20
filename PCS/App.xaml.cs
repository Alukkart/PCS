using System.Windows;
using PCS.Data;

namespace PCS;

public partial class App : Application
{
    private void Application_OnStartup(object sender, StartupEventArgs e)
    {
        try
        {
            LibraryDbContextFactory.InitializeDatabase();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "Failed to connect to SQLite database.\n" +
                $"Check env var {DatabaseSettings.ConnectionStringEnvName} or default connection settings.\n\n" +
                $"Error: {exception.Message}",
                "Database connection error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}
