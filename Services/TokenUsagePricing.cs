namespace CodexQuotaFloat.Services;

public static class TokenUsagePricing
{
    private static readonly IReadOnlyDictionary<string, ModelPrice> Prices =
        new Dictionary<string, ModelPrice>(StringComparer.OrdinalIgnoreCase)
        {
            ["gpt-5.6-sol"] = new(4m, 20m),
            ["gpt-5.6-terra"] = new(2m, 12m),
            ["gpt-5.6-luna"] = new(0.20m, 1.20m),
            ["gpt-6-astra"] = new(8m, 40m),
            ["codex-auto-review"] = new(2m, 12m)
        };

    public static bool TryEstimateUsd(
        string? model,
        long inputTokens,
        long outputTokens,
        out decimal estimatedPriceUsd)
    {
        estimatedPriceUsd = 0m;
        if (model is null || !Prices.TryGetValue(model, out var price))
        {
            return false;
        }

        // 给定价格表只区分输入和输出；缓存读/写已包含在本地 input_tokens 中，按输入价计算。
        estimatedPriceUsd = Math.Max(0, inputTokens) / 1_000_000m * price.InputPerMillion
                          + Math.Max(0, outputTokens) / 1_000_000m * price.OutputPerMillion;
        return true;
    }

    private readonly record struct ModelPrice(decimal InputPerMillion, decimal OutputPerMillion);
}
