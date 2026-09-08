using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DndCards.Web.Models;

namespace DndCards.Web.Services;

/// <summary>
/// Mapper from a real D&amp;D Beyond character JSON export (the unofficial
/// character-service v5 response shape, root or "data" object) to printable
/// cards. Field names were verified against a real export - see
/// character-service.dndbeyond.com/character/v5/character/{id}.
/// </summary>
public class DndBeyondImportService
{
    public CardDeck Import(string json)
    {
        var root = JsonNode.Parse(json) ?? throw new InvalidOperationException("Ungültiges JSON.");
        var data = root["data"] ?? root;

        var deck = new CardDeck
        {
            Title = data["name"]?.ToString() ?? "D&D Beyond Import"
        };

        var proficiencyBonus = CalculateProficiencyBonus(data);

        deck.Cards.AddRange(ImportSpells(data));
        deck.Cards.AddRange(ImportActions(data, proficiencyBonus));

        return deck;
    }

    private static IEnumerable<CardModel> ImportSpells(JsonNode data)
    {
        var seenIds = new HashSet<string>();

        foreach (var spellEntry in EnumerateSpellEntries(data))
        {
            // Spells are wrapped in a "definition" object; actions (below) are not.
            var definition = spellEntry["definition"] ?? spellEntry;
            var name = definition["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var id = definition["id"]?.ToString() ?? name;
            if (!seenIds.Add(id))
            {
                continue;
            }

            var level = definition["level"]?.GetValue<int>() ?? 0;
            var cost = BuildCostText(definition["activation"] ?? spellEntry["activation"]);
            var range = BuildSpellRangeText(definition["range"]);
            var description = StripHtml(definition["description"]?.ToString() ?? "");
            var hasDamage = definition["requiresAttackRoll"]?.GetValue<bool>() == true
                || description.Contains("damage", StringComparison.OrdinalIgnoreCase)
                || description.Contains("schaden", StringComparison.OrdinalIgnoreCase);

            yield return new CardModel
            {
                Name = name,
                Type = hasDamage ? CardColor.Red : CardColor.Blue,
                Cost = cost,
                Range = range,
                Effect = Truncate(description, CardTextLimits.Effect),
                Fluff = level == 0 ? "Ein Zaubertrick, jederzeit einsetzbar." : $"Zauber des Grades {level}.",
                Tactic = "Aus dem Charakterbogen importiert – Taktiktext bei Bedarf anpassen."
            };
        }
    }

    private static IEnumerable<JsonNode> EnumerateSpellEntries(JsonNode data)
    {
        if (data["spells"] is JsonObject spellGroups)
        {
            foreach (var (_, group) in spellGroups)
            {
                if (group is JsonArray arr)
                {
                    foreach (var entry in arr)
                    {
                        if (entry is not null) yield return entry;
                    }
                }
            }
        }

        if (data["classSpells"] is JsonArray classSpells)
        {
            foreach (var classEntry in classSpells)
            {
                if (classEntry?["spells"] is JsonArray arr)
                {
                    foreach (var entry in arr)
                    {
                        if (entry is not null) yield return entry;
                    }
                }
            }
        }
    }

    private static IEnumerable<CardModel> ImportActions(JsonNode data, int proficiencyBonus)
    {
        if (data["actions"] is not JsonObject actionGroups)
        {
            yield break;
        }

        foreach (var (_, group) in actionGroups)
        {
            if (group is not JsonArray arr) continue;

            foreach (var action in arr)
            {
                if (action is null) continue;

                // Actions (unlike spells) carry name/description directly, no "definition" wrapper.
                var name = action["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var description = StripHtml(action["description"]?.ToString() ?? action["snippet"]?.ToString() ?? "");
                var cost = BuildCostText(action["activation"]);
                var range = BuildActionRangeText(action["range"]);
                var tracker = BuildActionTracker(action["limitedUse"], proficiencyBonus);

                var hasAttack = action["displayAsAttack"]?.GetValue<bool>() == true
                    || description.Contains("attack roll", StringComparison.OrdinalIgnoreCase)
                    || description.Contains("angriffswurf", StringComparison.OrdinalIgnoreCase);

                yield return new CardModel
                {
                    Name = name,
                    Type = tracker is not null ? CardColor.Gold : (hasAttack ? CardColor.Red : CardColor.Green),
                    Cost = cost,
                    Range = range,
                    Effect = Truncate(description, CardTextLimits.Effect),
                    Fluff = "Aus dem Charakterbogen importiert.",
                    Tactic = "Taktiktext bei Bedarf anpassen.",
                    Tracker = tracker
                };
            }
        }
    }

    private static CardTracker? BuildActionTracker(JsonNode? limitedUse, int proficiencyBonus)
    {
        if (limitedUse is null) return null;

        var maxUses = limitedUse["maxUses"]?.GetValue<int>() ?? 0;
        if (maxUses <= 0 && limitedUse["useProficiencyBonus"]?.GetValue<bool>() == true)
        {
            // DDB computes some limits (e.g. Rabbit Hop) from proficiency bonus instead of a fixed number.
            maxUses = proficiencyBonus;
        }
        if (maxUses <= 0) return null;

        // Known values: 1 = Short Rest, 2 = Long Rest. Anything else falls back to a generic label.
        var label = limitedUse["resetType"]?.GetValue<int>() switch
        {
            1 => "Pro kurze Rast:",
            2 => "Pro lange Rast:",
            _ => "Nutzungen:"
        };

        return new CardTracker { Label = label, Count = maxUses };
    }

    private static int CalculateProficiencyBonus(JsonNode data)
    {
        var totalLevel = 0;
        if (data["classes"] is JsonArray classes)
        {
            foreach (var cls in classes)
            {
                totalLevel += cls?["level"]?.GetValue<int>() ?? 0;
            }
        }
        return totalLevel <= 0 ? 2 : 2 + (totalLevel - 1) / 4;
    }

    // Known values: 1 = Action, 2 = No Action, 3 = Bonus Action, 4 = Reaction.
    // Other values (minutes/hours/special/legendary) vary and are mapped generically.
    private static string BuildCostText(JsonNode? activation) =>
        activation?["activationType"]?.GetValue<int>() switch
        {
            1 => "Aktion",
            2 => "Keine Aktion",
            3 => "Bonusaktion",
            4 => "Reaktion",
            _ => "Spezial"
        };

    private static string BuildSpellRangeText(JsonNode? range)
    {
        if (range is null) return "Selbst";
        var origin = range["origin"]?.ToString() ?? "";
        var feet = range["rangeValue"]?.GetValue<int>() ?? 0;

        return origin switch
        {
            "Self" => "Du selbst",
            "Touch" => "Berührung",
            _ when feet > 0 => FeetToMeters(feet),
            _ => origin
        };
    }

    private static string? BuildActionRangeText(JsonNode? range)
    {
        var feet = range?["range"]?.GetValue<int>() ?? 0;
        return feet > 0 ? FeetToMeters(feet) : null;
    }

    // Official German D&D translations convert 1 foot to 0.3 meters (5 ft square = 1.5 m).
    private static string FeetToMeters(int feet)
    {
        var meters = feet * 0.3;
        return $"{meters:0.#} Meter".Replace(".", ",");
    }

    private static string StripHtml(string html) =>
        Regex.Replace(html, "<.*?>", " ").Replace("&nbsp;", " ").Trim();

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
}

