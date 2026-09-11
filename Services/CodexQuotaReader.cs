using UsageLens.Models;

namespace UsageLens.Services;

public sealed class CodexQuotaReader : IQuotaReader, IDisposable
{
    private readonly CodexAppServerClient _client;
    private readonly QuotaParser _parser;
    private bool _disposed;

    public CodexQuotaReader(CodexAppServerClient? client = null, QuotaParser? parser = null)
    {
        _client = client ?? new CodexAppServerClient();
        _parser = parser ?? new QuotaParser();
    }

    public async Task<QuotaState> ReadAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var result = await _client.ReadRateLimitsAsync(cancellationToken);
        return _parser.Parse(result, DateTimeOffset.Now);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _client.Dispose();
    }
}
