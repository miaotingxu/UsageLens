using UsageLens.Models;
using UsageLens.Services;

var root = Path.Combine(Path.GetTempPath(), $"UsageLens-settings-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);

try
{
    var store = new AppSettingsStore(root);
    AssertEqual(AppSettings.Default, store.Load(), "default settings");

    var oldAppearance = Path.Combine(root, "CodexQuotaFloat", "appearance.json");
    Directory.CreateDirectory(Path.GetDirectoryName(oldAppearance)!);
    await File.WriteAllTextAsync(oldAppearance, "{\"Style\":\"Terminal\",\"Left\":120.5}");
    var migrated = new AppSettingsStore(root).Load();
    AssertEqual(FloatingStyleKind.Terminal, migrated.FloatingStyle, "migrated style");
    AssertEqual(120.5, migrated.FloatingLeft ?? double.NaN, "migrated left");
    AssertTrue(File.Exists(Path.Combine(root, "UsageLens", "settings.json")), "settings file created");

    var settingsPath = Path.Combine(root, "UsageLens", "settings.json");
    await File.WriteAllTextAsync(settingsPath, "{\"schemaVersion\":1,\"floatingOpacity\":3,\"quotaRefreshMinutes\":0,\"tokenRefreshMinutes\":1,\"collapseDelaySeconds\":4}");
    var repaired = new AppSettingsStore(root).Load();
    AssertEqual(0.70, repaired.FloatingOpacity, "opacity fallback");
    AssertEqual(TimeSpan.FromMinutes(1), repaired.QuotaRefreshInterval, "quota fallback");
    AssertEqual(TimeSpan.FromMinutes(5), repaired.TokenRefreshInterval, "token fallback");
    AssertEqual(TimeSpan.FromSeconds(1), repaired.CollapseDelay, "collapse fallback");

    var fake = new FakeRunKeyStore();
    var startup = new StartupRegistrationService(fake);
    AssertTrue(startup.Apply(true), "startup enable");
    AssertTrue(fake.Values.TryGetValue("UsageLens", out var command) && command.StartsWith('"'), "quoted startup path");
    fake.Values["OtherApp"] = "other.exe";
    AssertTrue(startup.Apply(false), "startup disable");
    AssertTrue(!fake.Values.ContainsKey("UsageLens"), "only UsageLens removed");
    AssertTrue(fake.Values.ContainsKey("OtherApp"), "other startup preserved");

    Console.WriteLine("Settings tests passed.");
}
finally
{
    if (Directory.Exists(root))
    {
        Directory.Delete(root, recursive: true);
    }
}

static void AssertEqual<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{label}: expected {expected}, actual {actual}");
    }
}

static void AssertTrue(bool condition, string label)
{
    if (!condition)
    {
        throw new InvalidOperationException(label);
    }
}

file sealed class FakeRunKeyStore : IRunKeyStore
{
    public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? GetValue(string name) => Values.GetValueOrDefault(name);

    public void SetValue(string name, string command) => Values[name] = command;

    public void DeleteValue(string name) => Values.Remove(name);
}
