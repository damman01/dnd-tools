using System.Collections.Concurrent;

namespace DndCards.Web.Services;

public class UsageLimitOptions
{
    public int FreeCardLimitPerDeck { get; set; } = 8;
    public int FreeGenerationsPerDay { get; set; } = 5;
}

/// <summary>
/// Minimal in-memory free-tier gate. Placeholder for a future paid-tier check
/// (e.g. Stripe subscription/credit lookup) — swap <see cref="IsPremium"/> for a
/// real entitlement check once payments are wired up.
/// </summary>
public class UsageLimitService(Microsoft.Extensions.Options.IOptions<UsageLimitOptions> options)
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
