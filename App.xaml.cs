using System.Threading;
using System.Windows;

namespace UsageLens;

public partial class App : Application
{
    private const string MutexName = "Local\\UsageLens";

    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;
    private AppHost? _host;

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

        _host = new AppHost();
        _host.Start();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _host?.Dispose();
        _host = null;

        if (_ownsMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        _singleInstanceMutex = null;
    }
}
