using Microsoft.Win32;
using System.IO;

namespace UsageLens.Services;

public interface IRunKeyStore
{
    string? GetValue(string name);
    void SetValue(string name, string command);
    void DeleteValue(string name);
}

public sealed class StartupRegistrationService
{
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string ValueName = "UsageLens";
    private readonly IRunKeyStore _store;

    public StartupRegistrationService(IRunKeyStore? store = null)
    {
        _store = store ?? new RegistryRunKeyStore(RunKeyPath);
    }

    public bool Apply(bool enabled)
    {
        if (!enabled)
        {
            try
            {
                _store.DeleteValue(ValueName);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            return false;
        }

        try
        {
            _store.SetValue(ValueName, $"\"{processPath}\"");
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private sealed class RegistryRunKeyStore : IRunKeyStore
    {
        private readonly string _path;

        public RegistryRunKeyStore(string path) => _path = path;

        public string? GetValue(string name)
        {
            using var key = Registry.CurrentUser.OpenSubKey(_path, writable: false);
            return key?.GetValue(name) as string;
        }

        public void SetValue(string name, string command)
        {
            using var key = Registry.CurrentUser.CreateSubKey(_path);
            if (key is null)
            {
                throw new IOException("Unable to open the current-user startup key.");
            }

            key.SetValue(name, command, RegistryValueKind.String);
        }

        public void DeleteValue(string name)
        {
            using var key = Registry.CurrentUser.OpenSubKey(_path, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
    }
}
