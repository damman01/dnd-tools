using System.Text.RegularExpressions;
using DndCards.Web.Models;

namespace DndCards.Web.Services;

public class CardTranslationService
{
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

        // Saving throws
        ("Strength saving throw", "Stärke-Rettungswurf"),
        ("Dexterity saving throw", "Geschicklichkeits-Rettungswurf"),
        ("Constitution saving throw", "Konstitutions-Rettungswurf"),
        ("Intelligence saving throw", "Intelligenz-Rettungswurf"),
        ("Wisdom saving throw", "Weisheits-Rettungswurf"),
        ("Charisma saving throw", "Charisma-Rettungswurf"),
        ("saving throw", "Rettungswurf"),

        // Trackers & Resources
        ("Per Day:", "Pro Tag:"),
        ("Per Day", "Pro Tag"),
        ("Per Short Rest:", "Pro kurze Rast:"),
        ("Per Short Rest", "Pro kurze Rast"),
        ("Per Long Rest:", "Pro lange Rast:"),
        ("Per Long Rest", "Pro lange Rast"),
        ("Spell Slots", "Zauberplätze"),
        ("Spell Slot", "Zauberplatz"),
        ("Level 1", "Grad 1"),
        ("Level 2", "Grad 2"),
        ("Level 3", "Grad 3"),
        ("Level 4", "Grad 4"),
        ("Level 5", "Grad 5"),
        ("Level 6", "Grad 6"),
        ("Level 7", "Grad 7"),
        ("Level 8", "Grad 8"),
        ("Level 9", "Grad 9"),
        ("Cantrip", "Zaubertrick"),

        // Spell Names
        ("Fireball", "Feuerball"),
        ("Magic Missile", "Magisches Geschoss"),
        ("Cure Wounds", "Wunden heilen"),
        ("Healing Word", "Heilendes Wort"),
        ("Shield", "Schild"),
        ("Mage Armor", "Magierrüstung"),
        ("Thunderwave", "Donnerwoge"),
        ("Eldritch Blast", "Schauerlicher Strahl"),
        ("Guiding Bolt", "Leitblitz"),
        ("Spiritual Weapon", "Geisterwaffe"),
        ("Hold Person", "Person festhalten"),
        ("Misty Step", "Nebelschritt"),
        ("Invisibility", "Unsichtbarkeit"),
        ("Counterspell", "Gegenzauber"),
        ("Dispel Magic", "Magie bannen"),
        ("Lightning Bolt", "Blitz"),
        ("Fly", "Fliegen"),
        ("Haste", "Hast"),
        ("Polymorph", "Verwandlung"),

        // Class Features & Actions
        ("Sneak Attack", "Hinterhältiger Angriff"),
        ("Action Surge", "Tatenendspurt"),
        ("Second Wind", "Zweiter Wind"),
        ("Rage", "Kampfrausch"),
        ("Reckless Attack", "Rücksichtsloser Angriff"),
        ("Divine Smite", "Göttliches Niederstrecken"),
        ("Lay on Hands", "Handauflegen"),
        ("Wild Shape", "Tiergestalt"),
        ("Bardic Inspiration", "Barden-Inspiration"),
        ("Channel Divinity", "Göttliche Macht fokussieren"),
        ("Ki Points", "Ki-Punkte"),
        ("Flurry of Blows", "Schlaghagel"),
        ("Patient Defense", "Geduldige Verteidigung"),
        ("Step of the Wind", "Schritt des Windes"),
        ("Stunning Strike", "Betäubender Schlag"),
        ("Uncanny Dodge", "Unergründliches Ausweichen"),
        ("Evasion", "Entrinnen"),
        ("Extra Attack", "Zusätzlicher Angriff"),

        // Tactical notes
        ("Use against multiple targets", "Gegen mehrere Ziele einsetzen"),
        ("Use when surrounded", "Einsetzen, wenn umzingelt"),
        ("Defensive reaction", "Defensive Reaktion"),
        ("High single target burst", "Hoher Einzelschaden"),
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string De, string En)> ApiCache = new(StringComparer.OrdinalIgnoreCase);

    public CardTranslationService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task TranslateCardAsync(CardModel card, string targetLanguage)
    {
        var toGerman = targetLanguage.Equals("de", StringComparison.OrdinalIgnoreCase);

        // Try API lookup for card name from dnddeutsch.de
        if (!string.IsNullOrWhiteSpace(card.Name))
        {
            var apiTranslation = await LookupD3Async(card.Name, toGerman);
            if (!string.IsNullOrWhiteSpace(apiTranslation))
            {
                card.Name = apiTranslation;
            }
            else
            {
                card.Name = TranslateText(card.Name, toGerman);
            }
        }

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

    public async Task TranslateDeckAsync(CardDeck deck, string targetLanguage)
    {
        var toGerman = targetLanguage.Equals("de", StringComparison.OrdinalIgnoreCase);
        deck.Title = TranslateText(deck.Title, toGerman);

        foreach (var card in deck.Cards)
        {
            await TranslateCardAsync(card, targetLanguage);
        }
    }

    public async Task<string?> LookupD3Async(string searchTerm, bool toGerman)
    {
        var term = searchTerm.Trim();
        if (ApiCache.TryGetValue(term, out var cached))
        {
            return toGerman ? cached.De : cached.En;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TTRPGCardStudio/1.0 (+https://www.dnddeutsch.de)");

            var url = $"https://www.dnddeutsch.de/tools/json.php?apiv=0.7&s={Uri.EscapeDataString(term)}&o=dict&mi=on&mo=on&sp=on&it=on&misc=on";
            var response = await client.GetFromJsonAsync<D3DictResponse>(url);

            if (response?.Result is { Count: > 0 })
            {
                foreach (var item in response.Result)
                {
                    if (string.Equals(item.NameEn, term, StringComparison.OrdinalIgnoreCase))
                    {
                        ApiCache[item.NameEn] = (item.NameDe, item.NameEn);
                        ApiCache[item.NameDe] = (item.NameDe, item.NameEn);
                        return toGerman ? item.NameDe : item.NameEn;
                    }
                    if (string.Equals(item.NameDe, term, StringComparison.OrdinalIgnoreCase))
                    {
                        ApiCache[item.NameEn] = (item.NameDe, item.NameEn);
                        ApiCache[item.NameDe] = (item.NameDe, item.NameEn);
                        return toGerman ? item.NameDe : item.NameEn;
                    }
                }

                // Fallback to first result
                var first = response.Result[0];
                ApiCache[first.NameEn] = (first.NameDe, first.NameEn);
                ApiCache[first.NameDe] = (first.NameDe, first.NameEn);
                return toGerman ? first.NameDe : first.NameEn;
            }
        }
        catch
        {
            // Fallback gracefully on timeout or offline
        }

        return null;
    }

    public void ConvertCardUnits(CardModel card, UnitSystem targetSystem)
    {
        card.Range = ConvertUnitsText(card.Range, targetSystem);
        card.Effect = ConvertUnitsText(card.Effect, targetSystem);
    }

    public void ConvertDeckUnits(CardDeck deck, UnitSystem targetSystem)
    {
        foreach (var card in deck.Cards)
        {
            ConvertCardUnits(card, targetSystem);
        }
    }

    public static string ConvertUnitsText(string? input, UnitSystem targetSystem)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input ?? string.Empty;
        }

        var result = input;

        if (targetSystem == UnitSystem.Both)
        {
            // First strip any existing secondary parenthesized units to prevent nested loops like "18 m (60 ft) (60 ft)"
            result = Regex.Replace(result, @"\s*\(\s*\d+(?:[\.,]\d+)?\s*(?:ft|feet|m|meter|km|miles)\s*\)", "", RegexOptions.IgnoreCase);

            // Now find either meters or feet and format as "X m (Y ft)"
            // Case 1: has feet (e.g. "60 ft" or "60 feet" or "60 Fuß")
            result = Regex.Replace(result, @"\b(?<val>\d+(?:[\.,]\d+)?)\s*(?:[-–])?\s*(?:feet|foot|ft\.?|Fuß)\b", match =>
            {
                var valStr = match.Groups["val"].Value.Replace(',', '.');
                if (double.TryParse(valStr, System.Globalization.CultureInfo.InvariantCulture, out var feet))
                {
                    var meters = Math.Round(feet * 0.3, 1);
                    return $"{meters} m ({feet:0.#} ft)";
                }
                return match.Value;
            }, RegexOptions.IgnoreCase);

            // Case 2: has meters without already being paired (e.g. "18 m" or "18 Meter" not followed by "(...ft)")
            result = Regex.Replace(result, @"\b(?<val>\d+(?:[\.,]\d+)?)\s*(?:[-–])?\s*(?:meter|metres|m)\b(?!\s*\([^)]*ft\))(?!\w)", match =>
            {
                var valStr = match.Groups["val"].Value.Replace(',', '.');
                if (double.TryParse(valStr, System.Globalization.CultureInfo.InvariantCulture, out var meters))
                {
                    var feet = (int)Math.Round(meters / 0.3 / 5.0) * 5;
                    if (feet <= 0 && meters > 0) feet = (int)Math.Round(meters / 0.3);
                    return $"{meters:0.#} m ({feet} ft)";
                }
                return match.Value;
            }, RegexOptions.IgnoreCase);

            // Miles / km both
            result = Regex.Replace(result, @"\b(?<val>\d+(?:[\.,]\d+)?)\s*(?:[-–])?\s*(?:miles|mile|Meilen|Meile)\b", match =>
            {
                var valStr = match.Groups["val"].Value.Replace(',', '.');
                if (double.TryParse(valStr, System.Globalization.CultureInfo.InvariantCulture, out var miles))
                {
                    var km = Math.Round(miles * 1.5, 1);
                    return $"{km} km ({miles:0.#} mi)";
                }
                return match.Value;
            }, RegexOptions.IgnoreCase);

            return result;
        }

        // For Metric or Imperial, first strip any existing dual units like "(60 ft)" or "(18 m)"
        result = Regex.Replace(result, @"\s*\(\s*\d+(?:[\.,]\d+)?\s*(?:ft|feet|m|meter|km|miles|mi)\s*\)", "", RegexOptions.IgnoreCase);

        if (targetSystem == UnitSystem.Metric)
        {
            // Convert ft / feet / fuß -> m / Meter
            // Example: "60 ft.", "60 ft", "60 feet", "60 Fuß", "60-ft."
            result = Regex.Replace(result, @"\b(?<val>\d+(?:[\.,]\d+)?)\s*(?:[-–])?\s*(?:feet|foot|ft\.?|Fuß)\b", match =>
            {
                var valStr = match.Groups["val"].Value.Replace(',', '.');
                if (double.TryParse(valStr, System.Globalization.CultureInfo.InvariantCulture, out var feet))
                {
                    var meters = Math.Round(feet * 0.3, 1);
                    return $"{meters} m";
                }
                return match.Value;
            }, RegexOptions.IgnoreCase);

            // Convert miles / meilen -> km
            result = Regex.Replace(result, @"\b(?<val>\d+(?:[\.,]\d+)?)\s*(?:[-–])?\s*(?:miles|mile|Meilen|Meile)\b", match =>
            {
                var valStr = match.Groups["val"].Value.Replace(',', '.');
                if (double.TryParse(valStr, System.Globalization.CultureInfo.InvariantCulture, out var miles))
                {
                    var km = Math.Round(miles * 1.5, 1);
                    return $"{km} km";
                }
                return match.Value;
            }, RegexOptions.IgnoreCase);
        }
        else // UnitSystem.Imperial
        {
            // Convert m / meter -> ft
            // Example: "18 m", "18 Meter", "1.5 m", "1,5 Meter"
            result = Regex.Replace(result, @"\b(?<val>\d+(?:[\.,]\d+)?)\s*(?:[-–])?\s*(?:meter|metres|m)\b(?!\w)", match =>
            {
                var valStr = match.Groups["val"].Value.Replace(',', '.');
                if (double.TryParse(valStr, System.Globalization.CultureInfo.InvariantCulture, out var meters))
                {
                    // D&D standard: 1.5 m = 5 ft, 9 m = 30 ft, 18 m = 60 ft (divide by 0.3)
                    var feet = (int)Math.Round(meters / 0.3 / 5.0) * 5;
                    if (feet <= 0 && meters > 0) feet = (int)Math.Round(meters / 0.3);
                    return $"{feet} ft";
                }
                return match.Value;
            }, RegexOptions.IgnoreCase);

            // Convert km / kilometer -> miles
            result = Regex.Replace(result, @"\b(?<val>\d+(?:[\.,]\d+)?)\s*(?:[-–])?\s*(?:kilometer|km)\b(?!\w)", match =>
            {
                var valStr = match.Groups["val"].Value.Replace(',', '.');
                if (double.TryParse(valStr, System.Globalization.CultureInfo.InvariantCulture, out var km))
                {
                    var miles = Math.Round(km / 1.5, 1);
                    return $"{miles} miles";
                }
                return match.Value;
            }, RegexOptions.IgnoreCase);
        }

        return result;
    }

    public string TranslateText(string? input, bool toGerman)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input ?? string.Empty;
        }

        var result = input;
        foreach (var (en, de) in Terms)
        {
            var from = toGerman ? en : de;
            var to = toGerman ? de : en;

            result = Regex.Replace(result, $@"\b{Regex.Escape(from)}\b", to, RegexOptions.IgnoreCase);
        }

        return result;
    }

    private class D3DictResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("result")]
        public List<D3DictResult>? Result { get; set; }
    }

    private class D3DictResult
    {
        [System.Text.Json.Serialization.JsonPropertyName("name_de")]
        public string NameDe { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("name_en")]
        public string NameEn { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
