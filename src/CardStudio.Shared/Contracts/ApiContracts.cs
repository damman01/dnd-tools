using CardStudio.Shared.Models;

namespace CardStudio.Shared.Contracts;

public class RenderPdfRequest
{
    public CardDeck Deck { get; set; } = new();
}

public class RenderPngRequest
{
    public CardDeck Deck { get; set; } = new();
}

public class TranslateDeckRequest
{
    public CardDeck Deck { get; set; } = new();
    public string TargetLanguage { get; set; } = "de";
}

public class TranslateCardRequest
{
    public CardModel Card { get; set; } = new();
    public string TargetLanguage { get; set; } = "de";
}

public class ConvertUnitsRequest
{
    public CardDeck Deck { get; set; } = new();
    public string UnitSystem { get; set; } = "Metric";
}

public class UsageQuotaDto
{
    public int MaxCardsPerDeck { get; set; }
    public int RemainingGenerationsToday { get; set; }
    public bool IsPremium { get; set; }
}
