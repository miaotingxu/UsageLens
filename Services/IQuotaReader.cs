using UsageLens.Models;

namespace UsageLens.Services;

public interface IQuotaReader
{
    Task<QuotaState> ReadAsync(CancellationToken cancellationToken);
}
