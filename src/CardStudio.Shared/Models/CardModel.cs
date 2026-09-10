using System.Text.Json.Serialization;

namespace CardStudio.Shared.Models;

// Character caps that keep a card's text within the fixed 70x120mm print area at
// 8pt. Enforced both in the UI (maxlength) and defensively in CardHtmlBuilder, since
// hand-authored deck JSON bypasses the UI entirely.
public static class CardTextLimits
{
    public const int Name = 40;
    public const int Cost = 30;
    public const int Range = 40;
    public const int EffectStandard = 220;
    public const int EffectExpanded = 450;
    public const int Effect = EffectExpanded; // Maximum permitted cap
    public const int Fluff = 180;
    public const int Tactic = 200;
    public const int TrackerLabel = 40;
}

public enum CardColor
{
    Red,
    Blue,
    Green,
    Gold
}

public class CardTracker
{
    public string Label { get; set; } = "Pro Tag:";
    public int Count { get; set; }
}

// Only fields the user actually filled in should end up in the PDF - every text
// field besides Name/Type is optional to match the hand-authored deck JSON format.
public class CardModel
{
    public string Name { get; set; } = "";
    public CardColor Type { get; set; } = CardColor.Red;
    public string? Cost { get; set; }
    public string? Range { get; set; }
    public string? Effect { get; set; }
    public string? Fluff { get; set; }
    public string? Tactic { get; set; }
    public CardTracker? Tracker { get; set; }

    [JsonPropertyName("tracker_rows")]
    public List<CardTracker>? TrackerRows { get; set; }

    [JsonPropertyName("custom_effect_limit")]
    public int? CustomEffectLimit { get; set; }

    // Calculates the available capacity for the effect text depending on other fields filled
    [JsonIgnore]
    public int EffectiveEffectLimit
    {
        get
        {
            if (CustomEffectLimit.HasValue && CustomEffectLimit.Value > 0)
            {
                return CustomEffectLimit.Value;
            }

            var hasFluff = !string.IsNullOrWhiteSpace(Fluff);
            var hasTactic = !string.IsNullOrWhiteSpace(Tactic);
            var hasTrackers = (Tracker is { Count: > 0 }) || (TrackerRows is { Count: > 0 });

            // If there's no fluff, no tactic, and no extra trackers, effect text can use the full card
            if (!hasFluff && !hasTactic && !hasTrackers)
            {
                return CardTextLimits.EffectExpanded; // 450 chars
            }
            if (!hasFluff && !hasTactic)
            {
                return 360;
            }
            if (!hasFluff || !hasTactic)
            {
                return 280;
            }
            return CardTextLimits.EffectStandard; // 220 chars
        }
    }
}

public class PageSpec
{
    public string Width { get; set; } = "70mm";
    public string Height { get; set; } = "120mm";
    public string Margin { get; set; } = "4mm";
}

public class CardDeck
{
    public string Title { get; set; } = "Karten";
    public PageSpec Page { get; set; } = new();
    public List<CardModel> Cards { get; set; } = [];
}
