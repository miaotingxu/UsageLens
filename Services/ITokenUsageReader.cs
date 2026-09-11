using UsageLens.Models;

namespace UsageLens.Services;

public interface ITokenUsageReader
{
    Task<TokenUsageState> ReadAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
