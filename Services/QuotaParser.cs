using System.Text.Json;
using UsageLens.Models;

namespace UsageLens.Services;

public sealed class QuotaParser
{
    private const long FiveHourWindowMinutes = 300;
    private const long WeeklyWindowMinutes = 10080;

    public QuotaState Parse(JsonElement result, DateTimeOffset updatedAt)
    {
        if (!TrySelectSnapshot(result, out var snapshot))
        {
            throw new FormatException("Codex App Server did not return a rate-limit snapshot.");
        }

        QuotaWindow? fiveHour = null;
        QuotaWindow? weekly = null;

        foreach (var propertyName in new[] { "primary", "secondary" })
        {
            if (!snapshot.TryGetProperty(propertyName, out var window))
            {
                continue;
            }

            if (!TryReadWindow(window, out var parsedWindow))
            {
                continue;
            }

            switch (parsedWindow.DurationMinutes)
            {
                case FiveHourWindowMinutes:
                    fiveHour ??= parsedWindow;
                    break;
                case WeeklyWindowMinutes:
                    weekly ??= parsedWindow;
                    break;
            }
        }

        return new QuotaState(
            FiveHourRemaining: fiveHour?.RemainingPercent,
            WeeklyRemaining: weekly?.RemainingPercent,
            FiveHourResetAt: fiveHour?.ResetAt,
            WeeklyResetAt: weekly?.ResetAt,
            LastUpdatedAt: updatedAt,
            Status: QuotaStatus.Ready);
    }

    private static bool TrySelectSnapshot(JsonElement result, out JsonElement snapshot)
    {
        if (result.TryGetProperty("rateLimitsByLimitId", out var byLimitId) &&
            byLimitId.ValueKind == JsonValueKind.Object &&
            byLimitId.TryGetProperty("codex", out var codexSnapshot) &&
            codexSnapshot.ValueKind == JsonValueKind.Object)
        {
            snapshot = codexSnapshot;
            return true;
        }

        if (result.TryGetProperty("rateLimits", out var legacySnapshot) &&
            legacySnapshot.ValueKind == JsonValueKind.Object)
        {
            snapshot = legacySnapshot;
            return true;
        }

        snapshot = default;
        return false;
    }

    private static bool TryReadWindow(JsonElement window, out QuotaWindow parsedWindow)
    {
        parsedWindow = default;

        if (window.ValueKind != JsonValueKind.Object ||
            !TryReadInt64(window, "windowDurationMins", out var durationMinutes) ||
            !TryReadInt64(window, "usedPercent", out var usedPercent))
        {
            return false;
        }

        var remainingPercent = 100 - Math.Clamp(usedPercent, 0, 100);
        parsedWindow = new QuotaWindow(
            DurationMinutes: durationMinutes,
            RemainingPercent: (int)remainingPercent,
            ResetAt: TryReadResetAt(window));
        return true;
    }

    private static bool TryReadInt64(JsonElement element, string propertyName, out long value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt64(out value);
    }

    private static DateTimeOffset? TryReadResetAt(JsonElement window)
    {
        if (!TryReadInt64(window, "resetsAt", out var unixSeconds))
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private readonly record struct QuotaWindow(
        long DurationMinutes,
        int RemainingPercent,
        DateTimeOffset? ResetAt);
}
