using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CardStudio.Shared.Models;

namespace CardStudio.Api.Modules.Translation;

public class D3TranslationProvider(IHttpClientFactory httpClientFactory) : ITranslationProvider
{
    public string ProviderName => "dnddeutsch.de (D3)";

    private static readonly (string English, string German)[] Terms =
    [
        // Action costs
        ("1 Action", "1 Aktion"),
        ("Action", "Aktion"),
        ("1 Bonus Action", "1 Bonusaktion"),
        ("Bonus Action", "Bonusaktion"),
        ("Reaction", "Reaktion"),
        ("Free Action", "Freie Aktion"),
        ("Special", "Spezial"),

        // Ranges & Units
        ("Self", "Selbst"),
        ("Touch", "Berührung"),
        ("Sight", "Sichtweite"),
        ("Unlimited", "Unbegrenzt"),
        ("feet radius", "Fuß Radius"),
        ("ft. radius", "Fuß Radius"),
        ("ft cone", "Fuß Kegel"),
        ("ft cube", "Fuß Würfel"),
        ("ft line", "Fuß Linie"),
        ("feet", "Fuß"),
        ("foot", "Fuß"),
        ("ft.", "Fuß"),
        ("ft", "Fuß"),
        ("miles", "Meilen"),
        ("mile", "Meile"),

        // Duration & Components
        ("Instantaneous", "Augenblicklich"),
        ("Concentration", "Konzentration"),
        ("up to 1 minute", "bis zu 1 Minute"),
        ("up to 10 minutes", "bis zu 10 Minuten"),
        ("up to 1 hour", "bis zu 1 Stunde"),
        ("up to 8 hours", "bis zu 8 Stunden"),
        ("up to 24 hours", "bis zu 24 Stunden"),
        ("1 minute", "1 Minute"),
        ("10 minutes", "10 Minuten"),
        ("1 hour", "1 Stunde"),
        ("8 hours", "8 Stunden"),
        ("24 hours", "24 Stunden"),

        // Damage & Mechanics
        ("damage", "Schaden"),
        ("fire damage", "Feuerschaden"),
        ("cold damage", "Kälteschaden"),
        ("lightning damage", "Blitzschaden"),
        ("thunder damage", "Donnerschaden"),
        ("acid damage", "Säureschaden"),
        ("poison damage", "Giftschaden"),
        ("necrotic damage", "Nekrotischer Schaden"),
        ("radiant damage", "Gleißender Schaden"),
        ("force damage", "Wuchtenergie-Schaden"),
        ("psychic damage", "Psychischer Schaden"),
        ("bludgeoning damage", "Wuchtschaden"),
        ("piercing damage", "Stichschaden"),
        ("slashing damage", "Hiebschaden"),

        // Saving Throws & Attacks
        ("Strength saving throw", "Stärke-Rettungswurf"),
        ("Dexterity saving throw", "Geschicklichkeits-Rettungswurf"),
        ("Constitution saving throw", "Konstitutions-Rettungswurf"),
        ("Intelligence saving throw", "Intelligenz-Rettungswurf"),
        ("Wisdom saving throw", "Weisheits-Rettungswurf"),
        ("Charisma saving throw", "Charisma-Rettungswurf"),
        ("saving throw", "Rettungswurf"),
        ("melee weapon attack", "Nahkampf-Waffenangriff"),
        ("ranged weapon attack", "Fernkampf-Waffenangriff"),
        ("melee spell attack", "Nahkampf-Zauberangriff"),
        ("ranged spell attack", "Fernkampf-Zauberangriff"),
        ("advantage", "Vorteil"),
        ("disadvantage", "Nachteil"),
        ("hit points", "Trefferpunkte"),
        ("temporary hit points", "temporäre Trefferpunkte"),
        ("level", "Grad"),
        ("spell slot", "Zauberplatz"),
        ("spell slots", "Zauberplätze"),

        // Trackers & Rests
        ("Per Day:", "Pro Tag:"),
        ("Per Day", "Pro Tag"),
        ("Free / Day:", "Gratis / Tag:"),
        ("Free / Day", "Gratis / Tag"),
        ("Per Long Rest:", "Pro lange Rast:"),
        ("Per Long Rest", "Pro lange Rast"),
        ("Per Short Rest:", "Pro kurze Rast:"),
        ("Per Short Rest", "Pro kurze Rast"),
        ("Charges:", "Ladungen:"),
        ("Charges", "Ladungen"),
        ("Uses:", "Nutzungen:"),
        ("Uses", "Nutzungen"),
        ("Ki Points:", "Ki-Punkte:"),
        ("Hit Dice:", "Trefferwürfel:"),

        // Common Core Spells
        ("Fire Bolt", "Feuerpfeil"),
        ("Eldritch Blast", "Schauerlicher Strahl"),
        ("Mage Hand", "Magierhand"),
        ("Guidance", "Göttliche Führung"),
        ("Vicious Mockery", "Bösartiger Spott"),
        ("Cure Wounds", "Wunden heilen"),
        ("Healing Word", "Heilendes Wort"),
        ("Bless", "Segnen"),
        ("Shield", "Schild"),
        ("Magic Missile", "Magisches Geschoss"),
        ("Misty Step", "Nebelschritt"),
        ("Hold Person", "Person festhalten"),
        ("Spiritual Weapon", "Geisterwaffe"),
        ("Fireball", "Feuerball"),
        ("Lightning Bolt", "Blitz"),
        ("Counterspell", "Gegenzauber"),
        ("Revivify", "Wiederbeleben"),
        ("Haste", "Hast"),
        ("Spirit Guardians", "Geisterhafte Hüter")
    ];

    public async Task<CardDeck> TranslateDeckAsync(CardDeck deck, string targetLanguage, CancellationToken cancellationToken = default)
    {
        foreach (var card in deck.Cards)
        {
            await TranslateCardAsync(card, targetLanguage, cancellationToken);
        }
        return deck;
    }

    public async Task<CardModel> TranslateCardAsync(CardModel card, string targetLanguage, CancellationToken cancellationToken = default)
    {
        var toGerman = targetLanguage.Equals("de", StringComparison.OrdinalIgnoreCase);

        // 1. D3 API Abgleich
        await TryTranslateViaD3ApiAsync(card, toGerman, cancellationToken);

        // 2. Glossar-Übersetzung
        TranslateCardFields(card, toGerman);

        return card;
    }

    private async Task TryTranslateViaD3ApiAsync(CardModel card, bool toGerman, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(card.Name)) return;

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);

            var queryField = toGerman ? "name_en" : "name_de";
            var url = $"https://www.dnddeutsch.de/api/?spells&search={Uri.EscapeDataString(card.Name)}&limit=1";
            var response = await client.GetStringAsync(url, cancellationToken);

            var node = JsonNode.Parse(response);
            if (node is JsonArray arr && arr.Count > 0 && arr[0] is JsonObject spellObj)
            {
                var deName = spellObj["name_de"]?.ToString();
                var enName = spellObj["name_en"]?.ToString();

                if (toGerman && !string.IsNullOrWhiteSpace(deName))
                {
                    card.Name = deName;
                }
                else if (!toGerman && !string.IsNullOrWhiteSpace(enName))
                {
                    card.Name = enName;
                }

                var deRange = spellObj["range_de"]?.ToString();
                var enRange = spellObj["range_en"]?.ToString();
                if (toGerman && !string.IsNullOrWhiteSpace(deRange) && !string.IsNullOrWhiteSpace(card.Range))
                {
                    card.Range = deRange;
                }
                else if (!toGerman && !string.IsNullOrWhiteSpace(enRange) && !string.IsNullOrWhiteSpace(card.Range))
                {
                    card.Range = enRange;
                }
            }
        }
        catch
        {
            // API graceful fallback to dictionary
        }
    }

    private static void TranslateCardFields(CardModel card, bool toGerman)
    {
        card.Name = TranslateText(card.Name, toGerman);
        card.Cost = TranslateText(card.Cost, toGerman);
        card.Range = TranslateText(card.Range, toGerman);
        card.Effect = TranslateText(card.Effect, toGerman);
        card.Fluff = TranslateText(card.Fluff, toGerman);
        card.Tactic = TranslateText(card.Tactic, toGerman);

        if (card.Tracker is not null)
        {
            card.Tracker.Label = TranslateText(card.Tracker.Label, toGerman);
        }

        if (card.TrackerRows is not null)
        {
            foreach (var row in card.TrackerRows)
            {
                row.Label = TranslateText(row.Label, toGerman);
            }
        }
    }

    private static string? TranslateText(string? input, bool toGerman)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var result = input;
        foreach (var (english, german) in Terms)
        {
            if (toGerman)
            {
                var pattern = $@"\b{Regex.Escape(english)}\b";
                result = Regex.Replace(result, pattern, german, RegexOptions.IgnoreCase);
            }
            else
            {
                var pattern = $@"\b{Regex.Escape(german)}\b";
                result = Regex.Replace(result, pattern, english, RegexOptions.IgnoreCase);
            }
        }

        return result;
    }

    public static void ConvertCardUnits(CardModel card, UnitSystem unitSystem)
    {
        card.Range = ConvertStringUnits(card.Range, unitSystem);
        card.Effect = ConvertStringUnits(card.Effect, unitSystem);
        card.Tactic = ConvertStringUnits(card.Tactic, unitSystem);
    }

    public static void ConvertDeckUnits(CardDeck deck, UnitSystem unitSystem)
    {
        foreach (var card in deck.Cards)
        {
            ConvertCardUnits(card, unitSystem);
        }
    }

    private static string? ConvertStringUnits(string? input, UnitSystem unitSystem)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var result = Regex.Replace(input, @"(\d+(?:[.,]\d+)?)\s*m(?:\s*\((?:\d+(?:[.,]\d+)?)\s*(?:ft|feet|Fuß)\))?", m =>
        {
            if (!double.TryParse(m.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var meters))
            {
                return m.Value;
            }
            var feet = (int)Math.Round(meters / 0.3048 / 5.0) * 5;
            var mFormatted = meters % 1 == 0 ? $"{meters:0} m" : $"{meters:0.#} m";
            return unitSystem switch
            {
                UnitSystem.Metric => mFormatted,
                UnitSystem.Imperial => $"{feet} ft",
                _ => $"{mFormatted} ({feet} ft)"
            };
        }, RegexOptions.IgnoreCase);

        result = Regex.Replace(result, @"(\d+(?:[.,]\d+)?)\s*(?:ft|feet|Fuß)(?!\s*\))", m =>
        {
            if (!double.TryParse(m.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var feet))
            {
                return m.Value;
            }
            var meters = Math.Round(feet * 0.3048, 1);
            var mFormatted = meters % 1 == 0 ? $"{meters:0} m" : $"{meters:0.#} m";
            return unitSystem switch
            {
                UnitSystem.Metric => mFormatted,
                UnitSystem.Imperial => $"{feet:0} ft",
                _ => $"{mFormatted} ({feet:0} ft)"
            };
        }, RegexOptions.IgnoreCase);

        return result;
    }
}
