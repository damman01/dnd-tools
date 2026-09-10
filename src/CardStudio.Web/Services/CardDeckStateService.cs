using CardStudio.Shared.Models;

namespace CardStudio.Web.Services;

/// <summary>
/// Scoped session state container so card data and selected card remain intact
/// when navigating between Dashboard and Kartengenerator.
/// </summary>
public class CardDeckStateService
{
    public CardDeck Deck { get; set; }

    public CardModel? SelectedCard { get; set; }

    public CardDeckStateService()
    {
        var demoCard = new CardModel
        {
            Name = "Feuerball",
            Type = CardColor.Red,
            Cost = "1 Aktion",
            Range = "150 Fuß (20-Fuß Kugel)",
            Effect = "Jede Kreatur im Radius erleidet 8d6 Feuerschaden (Geschicklichkeitswurf halbiert). Entzündet brennbare Gegenstände.",
            Fluff = "Ein heller Funke schießt aus deinen Fingerspitzen hervor und detoniert im lodernden Inferno.",
            Tactic = "Gegen Gruppen eng stehender Feinde auf Distanz einsetzen.",
            Tracker = new CardTracker { Label = "Zauberplatz Grad 3:", Count = 3 }
        };

        Deck = new CardDeck
        {
            Title = "Aktions- & Zauberkarten",
            Cards = [demoCard]
        };

        SelectedCard = demoCard;
    }
}
