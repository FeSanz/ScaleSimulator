using System.Windows;

namespace ScaleSim.App;

public partial class App : Application
{
    private Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Two copies would fight over the same TCP ports, so only one window per user session.
        _singleInstance = new Mutex(true, @"Local\ScaleSimulator.SingleInstance", out var isFirst);
        if (!isFirst)
        {
            MessageBox.Show("El Simulador de Básculas ya está abierto.", "Simulador de Básculas",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
