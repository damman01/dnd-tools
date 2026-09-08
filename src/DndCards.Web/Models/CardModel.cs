using System.Text.Json.Serialization;

namespace DndCards.Web.Models;

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
