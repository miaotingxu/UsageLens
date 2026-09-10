using System.IO;
using System.Text.Json;
using CodexQuotaFloat.Models;

namespace CodexQuotaFloat.Services;

/// <summary>
/// 保存纯界面偏好；不保存账户、额度或会话内容。
/// </summary>
public sealed class AppearanceSettingsStore
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CodexQuotaFloat",
        "appearance.json");

    public FloatingWindowAppearance Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return FloatingWindowAppearance.Default;
            }

            var settings = JsonSerializer.Deserialize<StoredAppearanceSettings>(File.ReadAllText(SettingsPath));
            return settings is not null && Enum.IsDefined(settings.Style)
                ? new FloatingWindowAppearance(
                    settings.Style,
                    IsFinite(settings.Left) ? settings.Left : null)
                : FloatingWindowAppearance.Default;
        }
        catch
        {
            return FloatingWindowAppearance.Default;
        }
    }

    public void Save(FloatingStyleKind style, double left)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(new WritableAppearanceSettings(
                style,
                IsFinite(left) ? left : null));
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // 外观偏好写入失败不影响悬浮窗的主要功能。
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
