using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DndCards.Web.Models;

namespace DndCards.Web.Services;

/// <summary>
/// Best-effort mapper from a D&amp;D Beyond character JSON export (the unofficial
/// character-service response shape, root or "data" object) to printable cards.
/// DDB does not offer an official export format, so field names are matched
/// defensively and missing sections are simply skipped.
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

        deck.Cards.AddRange(ImportSpells(data));
        deck.Cards.AddRange(ImportActions(data));

        return deck;
    }

    private static IEnumerable<CardModel> ImportSpells(JsonNode data)
    {
        var seenIds = new HashSet<string>();

        foreach (var spellEntry in EnumerateSpellEntries(data))
        {
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
            var castingTime = definition["castingTime"]?.ToString() ?? (level == 0 ? "1 Aktion" : "1 Aktion");
            var range = BuildRangeText(definition["range"]);
            var description = StripHtml(definition["description"]?.ToString() ?? "");
            var hasDamage = definition["damageEffect"] is not null
                || description.Contains("damage", StringComparison.OrdinalIgnoreCase)
                || description.Contains("schaden", StringComparison.OrdinalIgnoreCase);

            yield return new CardModel
            {
                Name = name,
                Type = hasDamage ? CardColor.Red : CardColor.Blue,
                Cost = castingTime,
                Range = range,
                Effect = Truncate(description, 260),
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

    private static IEnumerable<CardModel> ImportActions(JsonNode data)
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

                var name = action["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var description = StripHtml(action["description"]?.ToString() ?? action["snippet"]?.ToString() ?? "");
                var limitedUse = action["limitedUse"];
                var maxUses = limitedUse?["maxUses"]?.GetValue<int>() ?? 0;
                var resetType = limitedUse?["resetType"]?.ToString() ?? "";

                var trackerLabel = resetType.Contains("short", StringComparison.OrdinalIgnoreCase)
                    ? "Pro kurze Rast:"
                    : resetType.Contains("long", StringComparison.OrdinalIgnoreCase)
                        ? "Pro lange Rast:"
                        : "Nutzungen:";

                var hasAttack = description.Contains("attack roll", StringComparison.OrdinalIgnoreCase)
                    || description.Contains("angriffswurf", StringComparison.OrdinalIgnoreCase);

                yield return new CardModel
                {
                    Name = name,
                    Type = maxUses > 0 ? CardColor.Gold : (hasAttack ? CardColor.Red : CardColor.Green),
                    Cost = "Aktion",
                    Range = "Siehe Beschreibung",
                    Effect = Truncate(description, 260),
                    Fluff = "Aus dem Charakterbogen importiert.",
                    Tactic = "Taktiktext bei Bedarf anpassen.",
                    Tracker = maxUses > 0 ? new CardTracker { Label = trackerLabel, Count = maxUses } : null
                };
            }
        }
    }

    private static string BuildRangeText(JsonNode? range)
    {
        if (range is null) return "Selbst";
        var value = range["rangeValue"]?.GetValue<int>();
        var origin = range["origin"]?.ToString() ?? "";
        return value is > 0 ? $"{value} Fuß ({origin})" : origin;
    }

    private static string StripHtml(string html) =>
        Regex.Replace(html, "<.*?>", " ").Replace("&nbsp;", " ").Trim();

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
}
