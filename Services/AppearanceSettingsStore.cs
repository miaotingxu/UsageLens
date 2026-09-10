using System.IO;
using System.Text.Json;
using UsageLens.Models;

namespace UsageLens.Services;

/// <summary>
/// 保存纯界面偏好；不保存账户、额度或会话内容。
/// </summary>
public sealed class AppearanceSettingsStore
{
    private readonly string _settingsPath;
    private readonly string _legacySettingsPath;

    public AppearanceSettingsStore(string? localAppDataDirectory = null)
    {
        var baseDirectory = localAppDataDirectory ??
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsPath = Path.Combine(baseDirectory, "UsageLens", "appearance.json");
        _legacySettingsPath = Path.Combine(baseDirectory, "CodexQuotaFloat", "appearance.json");
    }

    public FloatingWindowAppearance Load()
    {
        if (TryRead(_settingsPath, out var currentSettings))
        {
            return currentSettings;
        }

        if (TryRead(_legacySettingsPath, out var legacySettings))
        {
            // 只迁移外观偏好；旧目录保留，便于用户回滚旧版本。
            Save(legacySettings.Style, legacySettings.Left ?? double.NaN);
            return legacySettings;
        }

        return FloatingWindowAppearance.Default;
    }

    public void Save(FloatingStyleKind style, double left)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(new WritableAppearanceSettings(
                style,
                IsFinite(left) ? left : null));
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // 外观偏好写入失败不影响悬浮窗的主要功能。
        }
    }

    private static bool TryRead(string path, out FloatingWindowAppearance appearance)
    {
        appearance = FloatingWindowAppearance.Default;
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var settings = JsonSerializer.Deserialize<StoredAppearanceSettings>(File.ReadAllText(path));
            if (settings is null || !Enum.IsDefined(settings.Style))
            {
                return false;
            }

            appearance = new FloatingWindowAppearance(
                settings.Style,
                IsFinite(settings.Left) ? settings.Left : null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsFinite(double? value) => value is double number && double.IsFinite(number);

    private sealed record StoredAppearanceSettings(FloatingStyleKind Style, double? Left, double? Top);

    private sealed record WritableAppearanceSettings(FloatingStyleKind Style, double? Left);
}

public sealed record FloatingWindowAppearance(FloatingStyleKind Style, double? Left)
{
    public static FloatingWindowAppearance Default { get; } = new(FloatingStyleKind.Glass, null);
}
