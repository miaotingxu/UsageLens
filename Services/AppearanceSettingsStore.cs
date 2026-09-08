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

    public FloatingStyleKind Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return FloatingStyleKind.Glass;
            }

            var settings = JsonSerializer.Deserialize<AppearanceSettings>(File.ReadAllText(SettingsPath));
            return settings is not null && Enum.IsDefined(settings.Style)
                ? settings.Style
                : FloatingStyleKind.Glass;
        }
        catch
        {
            return FloatingStyleKind.Glass;
        }
    }

    public void Save(FloatingStyleKind style)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(new AppearanceSettings(style));
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // 外观偏好写入失败不影响悬浮窗的主要功能。
        }
    }

    private sealed record AppearanceSettings(FloatingStyleKind Style);
}
