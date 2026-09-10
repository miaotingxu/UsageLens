using UsageLens.Models;

namespace UsageLens.Services;

public static class TokenUsagePresentation
{
    public static string FormatCompact(long value) => value switch
    {
        >= 100_000_000 => $"{value / 100_000_000d:0.##}亿",
        >= 10_000 => $"{value / 10_000d:0.##}万",
        _ => value.ToString()
    };

    public static string FormatEstimatedPrice(decimal estimatedPriceUsd)
    {
        return estimatedPriceUsd < 0.01m && estimatedPriceUsd > 0
            ? "<$0.01"
            : $"${estimatedPriceUsd:0.##}";
    }

    public static string FormatTooltip(TokenUsagePeriod period) =>
        $"总 Token {(period.InputTokens + period.OutputTokens):N0}\n" +
        $"IN {period.InputTokens:N0} (includes cached input)\n" +
        $"OUT {period.OutputTokens:N0}\n" +
        $"预估价格 {FormatEstimatedPrice(period.EstimatedPriceUsd)}（非实际账单）" +
        (period.UnpricedTokens > 0
            ? $"\n未计价 Token {period.UnpricedTokens:N0}（模型未在价格表中）"
            : string.Empty);
}
