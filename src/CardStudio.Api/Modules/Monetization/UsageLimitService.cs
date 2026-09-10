using System.Collections.Concurrent;

namespace CardStudio.Api.Modules.Monetization;

public class UsageLimitOptions
{
    public int FreeCardLimitPerDeck { get; set; } = 8;
    public int FreeGenerationsPerDay { get; set; } = 5;
}

public interface IQuotaService
{
    bool IsPremium(string clientId);
    bool TryConsumeGeneration(string clientId, out int remainingToday);
    int GetMaxCardsForClient(string clientId);
}

public class UsageLimitService(Microsoft.Extensions.Options.IOptions<UsageLimitOptions> options) : IQuotaService
{
    private readonly ConcurrentDictionary<string, (DateOnly Day, int Count)> _generationsByClient = new();

    public UsageLimitOptions Options => options.Value;

    public virtual bool IsPremium(string clientId) => false;

    public bool TryConsumeGeneration(string clientId, out int remainingToday)
    {
        if (IsPremium(clientId))
        {
            remainingToday = int.MaxValue;
            return true;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entry = _generationsByClient.AddOrUpdate(
            clientId,
            _ => (today, 1),
            (_, existing) => existing.Day == today ? (today, existing.Count + 1) : (today, 1));

        remainingToday = Math.Max(0, options.Value.FreeGenerationsPerDay - entry.Count);
        return entry.Count <= options.Value.FreeGenerationsPerDay;
    }

    public int GetMaxCardsForClient(string clientId) =>
        IsPremium(clientId) ? int.MaxValue : options.Value.FreeCardLimitPerDeck;
}
