using UsageLens.Models;

namespace UsageLens.Services;

public sealed class LocalTokenUsageReader : ITokenUsageReader, IDisposable
{
    private readonly LocalTokenUsageService _service;
    private bool _disposed;

    public LocalTokenUsageReader(LocalTokenUsageService? service = null)
    {
        _service = service ?? new LocalTokenUsageService();
    }

    public Task<TokenUsageState> ReadAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _service.ReadAsync(now, cancellationToken);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
