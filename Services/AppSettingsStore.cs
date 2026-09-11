using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UsageLens.Models;

namespace UsageLens.Services;

/// <summary>
/// 保存纯界面偏好；不保存账户、额度、Token、会话或凭据。
/// </summary>
public sealed class AppSettingsStore
{
    private const int CurrentSchemaVersion = 1;
    private readonly string _settingsPath;
    private readonly string _currentAppearancePath;
    private readonly string _legacyAppearancePath;

    public AppSettingsStore(string? localAppDataDirectory = null)
    {
        var baseDirectory = localAppDataDirectory ??
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsPath = Path.Combine(baseDirectory, "UsageLens", "settings.json");
        _currentAppearancePath = Path.Combine(baseDirectory, "UsageLens", "appearance.json");
        _legacyAppearancePath = Path.Combine(baseDirectory, "CodexQuotaFloat", "appearance.json");
    }

    public AppSettings Load()
    {
        if (TryReadSettings(_settingsPath, out var settings))
        {
            return Sanitize(settings);
        }

        if (TryReadAppearance(_currentAppearancePath, out var currentAppearance) ||
            TryReadAppearance(_legacyAppearancePath, out currentAppearance))
        {
            var migrated = AppSettings.Default with
            {
                FloatingStyle = currentAppearance.Style,
                FloatingLeft = currentAppearance.Left
            };
            Save(migrated);
            return migrated;
        }

        return AppSettings.Default;
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = _settingsPath + ".tmp";
            var json = JsonSerializer.Serialize(StoredSettings.From(Sanitize(settings)), JsonOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
        catch (IOException)
        {
            // 偏好写入失败不影响主程序运行。
        }
        catch (UnauthorizedAccessException)
        {
            // 偏好写入失败不影响主程序运行。
        }
        finally
        {
            TryDeleteTemporaryFile();
        }
    }

    public void Reset()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                File.Delete(_settingsPath);
            }
        }
        catch (IOException)
        {
            // 重置失败不影响当前运行中的偏好。
        }
        catch (UnauthorizedAccessException)
        {
            // 重置失败不影响当前运行中的偏好。
        }
    }

    private bool TryReadSettings(string path, out AppSettings settings)
    {
        settings = AppSettings.Default;
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var stored = JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(path), JsonOptions);
            if (stored is null || stored.SchemaVersion != CurrentSchemaVersion)
            {
                return false;
            }

            settings = stored.ToSettings();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryReadAppearance(string path, out LegacyAppearance appearance)
    {
        appearance = LegacyAppearance.Default;
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var stored = JsonSerializer.Deserialize<StoredAppearance>(File.ReadAllText(path), JsonOptions);
            if (stored is null || !Enum.IsDefined(stored.Style))
            {
                return false;
            }

            appearance = new LegacyAppearance(
                stored.Style,
                IsFinite(stored.Left) ? stored.Left : null);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static AppSettings Sanitize(AppSettings settings)
    {
        var style = Enum.IsDefined(settings.FloatingStyle)
            ? settings.FloatingStyle
            : AppSettings.Default.FloatingStyle;
        var left = IsFinite(settings.FloatingLeft) ? settings.FloatingLeft : null;
        var opacity = settings.FloatingOpacity is >= 0.35 and <= 1.0 && double.IsFinite(settings.FloatingOpacity)
            ? settings.FloatingOpacity
            : AppSettings.Default.FloatingOpacity;
        var collapseDelay = settings.CollapseDelay.TotalSeconds is 1 or 2 or 3
            ? settings.CollapseDelay
            : AppSettings.Default.CollapseDelay;
        var quotaInterval = settings.QuotaRefreshInterval.TotalMinutes is 1 or 3 or 5
            ? settings.QuotaRefreshInterval
            : AppSettings.Default.QuotaRefreshInterval;
        var tokenInterval = settings.TokenRefreshInterval.TotalMinutes is 5 or 10 or 15
            ? settings.TokenRefreshInterval
            : AppSettings.Default.TokenRefreshInterval;
        var trayAction = Enum.IsDefined(settings.TrayPrimaryAction)
            ? settings.TrayPrimaryAction
            : AppSettings.Default.TrayPrimaryAction;

        return settings with
        {
            FloatingStyle = style,
            FloatingLeft = left,
            FloatingOpacity = opacity,
            CollapseDelay = collapseDelay,
            QuotaRefreshInterval = quotaInterval,
            TokenRefreshInterval = tokenInterval,
            TrayPrimaryAction = trayAction
        };
    }

    private void TryDeleteTemporaryFile()
    {
        try
        {
            var temporaryPath = _settingsPath + ".tmp";
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static bool IsFinite(double? value) => value is double number && double.IsFinite(number);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static AppSettingsStore()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private sealed record StoredSettings(
        int SchemaVersion,
        FloatingStyleKind FloatingStyle,
        double? FloatingLeft,
        double FloatingOpacity,
        bool AutoCollapseEnabled,
        int CollapseDelaySeconds,
        bool ExpandOnHandleHover,
        int QuotaRefreshMinutes,
        int TokenRefreshMinutes,
        bool StartWithWindows,
        bool HideControlCenterOnClose,
        TrayPrimaryAction TrayPrimaryAction)
    {
        public static StoredSettings From(AppSettings settings) => new(
            CurrentSchemaVersion,
            settings.FloatingStyle,
            settings.FloatingLeft,
            settings.FloatingOpacity,
            settings.AutoCollapseEnabled,
            (int)settings.CollapseDelay.TotalSeconds,
            settings.ExpandOnHandleHover,
            (int)settings.QuotaRefreshInterval.TotalMinutes,
            (int)settings.TokenRefreshInterval.TotalMinutes,
            settings.StartWithWindows,
            settings.HideControlCenterOnClose,
            settings.TrayPrimaryAction);

        public AppSettings ToSettings() => new(
            FloatingStyle,
            FloatingLeft,
            FloatingOpacity,
            AutoCollapseEnabled,
            TimeSpan.FromSeconds(CollapseDelaySeconds),
            ExpandOnHandleHover,
            TimeSpan.FromMinutes(QuotaRefreshMinutes),
            TimeSpan.FromMinutes(TokenRefreshMinutes),
            StartWithWindows,
            HideControlCenterOnClose,
            TrayPrimaryAction);
    }

    private sealed record StoredAppearance(FloatingStyleKind Style, double? Left);

    private sealed record LegacyAppearance(FloatingStyleKind Style, double? Left)
    {
        public static LegacyAppearance Default { get; } = new(FloatingStyleKind.Glass, null);
    }
}
