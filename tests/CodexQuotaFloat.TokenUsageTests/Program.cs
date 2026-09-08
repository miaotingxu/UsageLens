using System.Text.Json;
using CodexQuotaFloat.Services;

if (args.Contains("--real", StringComparer.OrdinalIgnoreCase))
{
    var realState = await new LocalTokenUsageService().ReadAsync(DateTimeOffset.Now);
    Console.WriteLine(JsonSerializer.Serialize(realState));
    return;
}

var root = Path.Combine(Path.GetTempPath(), $"CodexQuotaFloat-tests-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);

try
{
    var now = DateTimeOffset.Now;
    var firstFile = Path.Combine(root, "rollout-first.jsonl");
    await File.WriteAllLinesAsync(firstFile,
    [
        TurnContext("gpt-5.6-sol"),
        Event(now.AddDays(-8), 100, 10),
        TurnContext("gpt-5.6-terra"),
        Event(now.AddDays(-6), 150, 20),
        Event(now.AddDays(-6), 150, 20), // 重复通知不累计
        "{broken-json}",
        TurnContext("gpt-5.6-luna"),
        Event(now, 230, 35),
        Event(now, 25, 5) // 累计计数器重置
    ]);

    var secondFile = Path.Combine(root, "rollout-second.jsonl");
    await File.WriteAllLinesAsync(secondFile,
    [
        TurnContext("gpt-5.6-terra"),
        Event(now.AddDays(-29), 40, 4),
        TurnContext("gpt-5.6-sol"),
        Event(now.AddDays(-20), 60, 8)
    ]);

    var service = new LocalTokenUsageService(root);
    var state = await service.ReadAsync(now);
    AssertEqual(105L, state.Today.InputTokens, "today input");
    AssertEqual(20L, state.Today.OutputTokens, "today output");
    AssertEqual(155L, state.Last7Days.InputTokens, "7 day input");
    AssertEqual(30L, state.Last7Days.OutputTokens, "7 day output");
    AssertEqual(315L, state.Last30Days.InputTokens, "30 day input");
    AssertEqual(48L, state.Last30Days.OutputTokens, "30 day output");
    AssertEqual(0.000045m, state.Today.EstimatedPriceUsd, "today model-aware price");
    AssertEqual(0.001153m, state.Last30Days.EstimatedPriceUsd, "30 day model-aware price");
    AssertEqual(0L, state.Last30Days.UnpricedTokens, "all test models priced");

    await File.AppendAllTextAsync(firstFile, Event(now, 55, 9)[..^1]);
    var beforeComplete = await service.ReadAsync(now);
    AssertEqual(105L, beforeComplete.Today.InputTokens, "incomplete line ignored");
    await File.AppendAllTextAsync(firstFile, "}\n");
    var afterComplete = await service.ReadAsync(now);
    AssertEqual(135L, afterComplete.Today.InputTokens, "appended input delta");
    AssertEqual(24L, afterComplete.Today.OutputTokens, "appended output delta");

    AssertEqual("999", TokenUsagePresentation.FormatCompact(999), "plain format");
    AssertEqual("1.25万", TokenUsagePresentation.FormatCompact(12_500), "ten-thousand format");
    AssertEqual("9999.99万", TokenUsagePresentation.FormatCompact(99_999_900), "under one hundred million format");
    AssertEqual("1亿", TokenUsagePresentation.FormatCompact(100_000_000), "hundred-million format");
    AssertEqual("1.25亿", TokenUsagePresentation.FormatCompact(125_000_000), "fractional hundred-million format");
    AssertTrue(TokenUsagePricing.TryEstimateUsd("gpt-5.6-sol", 1_000_000, 1_000_000, out var solPrice), "Sol price available");
    AssertEqual(24m, solPrice, "Sol input and output price");
    AssertTrue(TokenUsagePricing.TryEstimateUsd("gpt-5.6-terra", 1_000_000, 1_000_000, out var terraPrice), "Terra price available");
    AssertEqual(14m, terraPrice, "Terra input and output price");
    AssertTrue(TokenUsagePricing.TryEstimateUsd("gpt-5.6-luna", 1_000_000, 1_000_000, out var lunaPrice), "Luna price available");
    AssertEqual(1.4m, lunaPrice, "Luna input and output price");
    AssertTrue(TokenUsagePricing.TryEstimateUsd("gpt-6-astra", 1_000_000, 1_000_000, out var astraPrice), "Astra price available");
    AssertEqual(48m, astraPrice, "Astra is twice Sol price");
    AssertTrue(TokenUsagePricing.TryEstimateUsd("codex-auto-review", 1_000_000, 1_000_000, out var reviewPrice), "review price available");
    AssertEqual(14m, reviewPrice, "review equals Terra price");
    AssertTrue(!TokenUsagePricing.TryEstimateUsd("unlisted-model", 1_000_000, 1_000_000, out _), "unlisted model is not priced");
    AssertEqual("$25", TokenUsagePresentation.FormatEstimatedPrice(25m), "estimated dollar display");
    AssertEqual("<$0.01", TokenUsagePresentation.FormatEstimatedPrice(0.001m), "estimated tiny price");

    Console.WriteLine("Token usage tests passed.");
}
finally
{
    Directory.Delete(root, recursive: true);
}

static string Event(DateTimeOffset timestamp, long input, long output) => JsonSerializer.Serialize(new
{
    timestamp = timestamp.ToString("O"),
    type = "event_msg",
    payload = new
    {
        type = "token_count",
        info = new
        {
            total_token_usage = new
            {
                input_tokens = input,
                cached_input_tokens = input / 2,
                output_tokens = output,
                reasoning_output_tokens = output / 2,
                total_tokens = input + output
            }
        }
    }
});

static string TurnContext(string model) => JsonSerializer.Serialize(new
{
    type = "turn_context",
    payload = new { model }
});

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
