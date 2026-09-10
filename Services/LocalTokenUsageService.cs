using System.IO;
using System.Text;
using System.Text.Json;
using UsageLens.Models;

namespace UsageLens.Services;

public sealed class LocalTokenUsageService
{
    private readonly string _sessionsRoot;
    private readonly Dictionary<string, FileUsageState> _files = new(StringComparer.OrdinalIgnoreCase);

    public LocalTokenUsageService(string? sessionsRoot = null)
    {
        _sessionsRoot = sessionsRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");
    }

    public async Task<TokenUsageState> ReadAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var today = now.LocalDateTime.Date;
        var cutoff = today.AddDays(-29);

        if (!Directory.Exists(_sessionsRoot))
        {
            return new TokenUsageState(default, default, default, now, TokenUsageStatus.Ready);
        }

        var candidates = Directory.EnumerateFiles(_sessionsRoot, "rollout-*.jsonl", SearchOption.AllDirectories)
            .Where(path => File.GetLastWriteTime(path) >= cutoff)
            .ToArray();
        var candidateSet = candidates.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var obsolete in _files.Keys.Where(path => !candidateSet.Contains(path)).ToArray())
        {
            _files.Remove(obsolete);
        }

        foreach (var path in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_files.TryGetValue(path, out var state))
            {
                state = new FileUsageState();
                _files.Add(path, state);
            }

            var length = new FileInfo(path).Length;
            if (length < state.Offset)
            {
                state.Reset();
            }

            if (length > state.Offset)
            {
                await ReadAppendedLinesAsync(path, state, cancellationToken);
            }
        }

        var todayUsage = SumRange(today, today);
        var sevenDayUsage = SumRange(today.AddDays(-6), today);
        var thirtyDayUsage = SumRange(cutoff, today);
        return new TokenUsageState(todayUsage, sevenDayUsage, thirtyDayUsage, now, TokenUsageStatus.Ready);
    }

    private TokenUsagePeriod SumRange(DateTime start, DateTime end)
    {
        long input = 0;
        long output = 0;
        decimal price = 0;
        long unpriced = 0;
        foreach (var state in _files.Values)
        {
            foreach (var (date, usage) in state.ByDate)
            {
                if (date >= start && date <= end)
                {
                    input += usage.InputTokens;
                    output += usage.OutputTokens;
                    price += usage.EstimatedPriceUsd;
                    unpriced += usage.UnpricedTokens;
                }
            }
        }

        return new TokenUsagePeriod(input, output, price, unpriced);
    }

    private static async Task ReadAppendedLinesAsync(
        string path,
        FileUsageState state,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        stream.Seek(state.Offset, SeekOrigin.Begin);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var lineStart = 0;

        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] != (byte)'\n')
            {
                continue;
            }

            var length = index - lineStart;
            if (length > 0 && bytes[index - 1] == (byte)'\r')
            {
                length--;
            }

            if (length > 0)
            {
                ParseLine(bytes.AsSpan(lineStart, length), state);
            }

            lineStart = index + 1;
        }

        // 未写完的最后一行下次重新读取，避免把半条 JSON 当作永久坏数据。
        state.Offset += lineStart;
    }

    private static void ParseLine(ReadOnlySpan<byte> utf8Line, FileUsageState state)
    {
        try
        {
            using var document = JsonDocument.Parse(utf8Line.ToArray());
            var root = document.RootElement;
            if (root.TryGetProperty("type", out var recordType) && recordType.GetString() == "turn_context" &&
                root.TryGetProperty("payload", out var turnContext) &&
                turnContext.TryGetProperty("model", out var modelElement))
            {
                state.CurrentModel = modelElement.GetString();
                return;
            }

            if (!root.TryGetProperty("type", out var type) || type.GetString() != "event_msg" ||
                !root.TryGetProperty("payload", out var payload) ||
                !payload.TryGetProperty("type", out var payloadType) || payloadType.GetString() != "token_count" ||
                !payload.TryGetProperty("info", out var info) || info.ValueKind != JsonValueKind.Object ||
                !info.TryGetProperty("total_token_usage", out var usage) ||
                !TryGetInt64(usage, "input_tokens", out var currentInput) ||
                !TryGetInt64(usage, "output_tokens", out var currentOutput) ||
                !root.TryGetProperty("timestamp", out var timestampElement) ||
                !DateTimeOffset.TryParse(timestampElement.GetString(), out var timestamp))
            {
                return;
            }

            var inputDelta = CalculateDelta(state.PreviousInput, currentInput);
            var outputDelta = CalculateDelta(state.PreviousOutput, currentOutput);
            state.PreviousInput = currentInput;
            state.PreviousOutput = currentOutput;
            var isPriced = TokenUsagePricing.TryEstimateUsd(
                state.CurrentModel,
                inputDelta,
                outputDelta,
                out var estimatedPrice);

            var localDate = timestamp.ToLocalTime().Date;
            state.ByDate.TryGetValue(localDate, out var previous);
            state.ByDate[localDate] = new TokenUsagePeriod(
                previous.InputTokens + inputDelta,
                previous.OutputTokens + outputDelta,
                previous.EstimatedPriceUsd + estimatedPrice,
                previous.UnpricedTokens + (isPriced ? 0 : inputDelta + outputDelta));
        }
        catch (JsonException)
        {
            // 单条损坏记录不应影响其他会话统计。
        }
        catch (DecoderFallbackException)
        {
            // 非法 UTF-8 行按损坏记录处理。
        }
    }

    private static long CalculateDelta(long? previous, long current)
    {
        if (previous is null || current < previous.Value)
        {
            return Math.Max(0, current);
        }

        return current - previous.Value;
    }

    private static bool TryGetInt64(JsonElement element, string propertyName, out long value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out value);
    }

    private sealed class FileUsageState
    {
        public long Offset { get; set; }
        public long? PreviousInput { get; set; }
        public long? PreviousOutput { get; set; }
        public string? CurrentModel { get; set; }
        public Dictionary<DateTime, TokenUsagePeriod> ByDate { get; } = [];

        public void Reset()
        {
            Offset = 0;
            PreviousInput = null;
            PreviousOutput = null;
            CurrentModel = null;
            ByDate.Clear();
        }
    }
}
