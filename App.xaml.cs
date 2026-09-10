using System.Threading;
using System.Windows;

namespace UsageLens;

public partial class App : Application
{
    private const string MutexName = "Local\\UsageLens";

    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        _ownsMutex = createdNew;

        if (!createdNew)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        if (MainWindow is MainWindow window)
        {
            window.Dispose();
        }

        if (_ownsMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        _singleInstanceMutex = null;
    }
}
