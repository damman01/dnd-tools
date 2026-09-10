using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CardStudio.Shared.Models;

namespace CardStudio.Web.Services;

/// <summary>
/// Mapper from a real D&amp;D Beyond character JSON export (the unofficial
/// character-service v5 response shape, root or "data" object) to printable
/// cards. Field names were verified against a real export - see
/// character-service.dndbeyond.com/character/v5/character/{id}.
/// </summary>
public class DndBeyondImportService
{
    public CardDeck Import(string json, UnitSystem unitSystem = UnitSystem.Metric)
    {
        var root = JsonNode.Parse(json) ?? throw new InvalidOperationException("Ungültiges JSON.");
        var data = root["data"] ?? root;

        var deck = new CardDeck
        {
            Title = data["name"]?.ToString() ?? "D&D Beyond Import"
        };

        var proficiencyBonus = CalculateProficiencyBonus(data);

        deck.Cards.AddRange(ImportSpells(data, unitSystem));
        deck.Cards.AddRange(ImportActions(data, proficiencyBonus, unitSystem));

        var backgroundCard = BuildBackgroundCard(data);
        if (backgroundCard is not null)
        {
            deck.Cards.Add(backgroundCard);
        }

        deck.Cards.AddRange(ImportCreatures(data, unitSystem));

        var resourceCard = BuildSpellSlotResourceCard(data);
        if (resourceCard is not null)
        {
            deck.Cards.Add(resourceCard);
        }

        var classResourceCard = BuildClassResourcesCard(data, proficiencyBonus);
        if (classResourceCard is not null)
        {
            deck.Cards.Add(classResourceCard);
        }

        return deck;
    }

    private static IEnumerable<CardModel> ImportSpells(JsonNode data, UnitSystem unitSystem)
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
            var range = BuildSpellRangeText(definition["range"], unitSystem);
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

    // Spell slot totals come from each class's level-indexed table (classes[].definition.spellRules.levelSpellSlots),
    // only if the class or subclass is an actual spellcaster (canCastSpells == true).
    private static CardModel? BuildSpellSlotResourceCard(JsonNode data)
    {
        if (data["classes"] is not JsonArray classes)
        {
            return null;
        }

        var totalsBySpellLevel = new int[10];
        foreach (var cls in classes)
        {
            var level = cls?["level"]?.GetValue<int>() ?? 0;
            if (level <= 0) continue;

            var canCast = cls?["definition"]?["canCastSpells"]?.GetValue<bool>() == true
                       || cls?["subclassDefinition"]?["canCastSpells"]?.GetValue<bool>() == true;
            if (!canCast) continue;

            if (cls?["definition"]?["spellRules"]?["levelSpellSlots"] is not JsonArray table || level >= table.Count)
            {
                continue;
            }

            if (table[level] is not JsonArray row) continue;
            for (var i = 0; i < row.Count && i < totalsBySpellLevel.Length - 1; i++)
            {
                totalsBySpellLevel[i + 1] += row[i]?.GetValue<int>() ?? 0;
            }
        }

        var rows = new List<CardTracker>();
        for (var spellLevel = 1; spellLevel <= 9; spellLevel++)
        {
            if (totalsBySpellLevel[spellLevel] > 0)
            {
                rows.Add(new CardTracker { Label = $"Zauberpl\u00e4tze Grad {spellLevel} (T\u00e4glich)", Count = totalsBySpellLevel[spellLevel] });
            }
        }

        if (rows.Count == 0)
        {
            return null;
        }

        return new CardModel
        {
            Name = "Zauberpl\u00e4tze",
            Type = CardColor.Gold,
            Fluff = "Ressourcen f\u00fcr Zauber",
            TrackerRows = rows
        };
    }

    private static CardModel? BuildClassResourcesCard(JsonNode data, int proficiencyBonus)
    {
        if (data["actions"] is not JsonObject actionGroups)
        {
            return null;
        }

        var rows = new List<CardTracker>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, group) in actionGroups)
        {
            if (group is not JsonArray arr) continue;

            foreach (var action in arr)
            {
                if (action is null) continue;
                var rawName = action["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(rawName)) continue;

                var limitedUse = action["limitedUse"];
                if (limitedUse is null) continue;

                var maxUses = limitedUse["maxUses"]?.GetValue<int>() ?? 0;
                if (maxUses <= 0 && limitedUse["useProficiencyBonus"]?.GetValue<bool>() == true)
                {
                    maxUses = proficiencyBonus;
                }
                if (maxUses <= 0) continue;

                // Clean up suffixes like "(Enter)" in "Rage (Enter)"
                var cleanName = rawName.Replace(" (Enter)", "").Trim();
                if (!seenNames.Add(cleanName)) continue;

                var restType = limitedUse["resetType"]?.GetValue<int>() == 1 ? "Kurze Rast" : "Lange Rast";
                rows.Add(new CardTracker
                {
                    Label = $"{cleanName} ({restType}):",
                    Count = maxUses
                });
            }
        }

        if (rows.Count == 0)
        {
            return null;
        }

        return new CardModel
        {
            Name = "Klassenressourcen",
            Type = CardColor.Gold,
            Fluff = "T\u00e4gliche F\u00e4higkeiten & Z\u00e4hler",
            TrackerRows = rows
        };
    }

    private static IEnumerable<CardModel> ImportActions(JsonNode data, int proficiencyBonus, UnitSystem unitSystem)
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
                var range = BuildActionRangeText(action["range"], unitSystem);
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

    private static string BuildSpellRangeText(JsonNode? range, UnitSystem unitSystem = UnitSystem.Metric)
    {
        if (range is null) return "Selbst";
        var origin = range["origin"]?.ToString() ?? "";
        var feet = range["rangeValue"]?.GetValue<int>() ?? 0;

        return origin switch
        {
            "Self" => "Du selbst",
            "Touch" => "Berührung",
            _ when feet > 0 => FormatRangeDistance(feet, unitSystem),
            _ => origin
        };
    }

    private static string? BuildActionRangeText(JsonNode? range, UnitSystem unitSystem = UnitSystem.Metric)
    {
        var feet = range?["range"]?.GetValue<int>() ?? 0;
        return feet > 0 ? FormatRangeDistance(feet, unitSystem) : null;
    }

    // Official German D&D translations convert 1 foot to 0.3 meters (5 ft square = 1.5 m).
    private static string FormatRangeDistance(int feet, UnitSystem unitSystem)
    {
        if (unitSystem == UnitSystem.Imperial)
        {
            return $"{feet} ft";
        }
        var meters = feet * 0.3;
        var meterStr = $"{meters:0.#} m".Replace(".", ",");

        if (unitSystem == UnitSystem.Both)
        {
            return $"{meterStr} ({feet} ft)";
        }

        return meterStr;
    }

    private static CardModel? BuildBackgroundCard(JsonNode data)
    {
        var bg = data["background"];
        var bgDef = bg?["definition"];
        if (bgDef is null) return null;

        var bgName = bgDef["name"]?.ToString() ?? "Hintergrund";
        var featureName = bgDef["featureName"]?.ToString();
        var featureDesc = StripHtml(bgDef["featureDescription"]?.ToString() ?? bgDef["description"]?.ToString() ?? "");

        var effectText = !string.IsNullOrWhiteSpace(featureName)
            ? $"Merkmal '{featureName}': {featureDesc}"
            : featureDesc;

        var traits = data["traits"];
        var personality = traits?["personalityTraits"]?.ToString();
        var flaw = traits?["flaws"]?.ToString();
        var fluff = personality switch
        {
            not null when flaw is not null => $"{personality} {flaw}",
            not null => personality,
            _ => flaw ?? $"Hintergrund: {bgName}"
        };

        return new CardModel
        {
            Name = $"Hintergrund: {bgName}",
            Type = CardColor.Gold,
            Cost = "Passiv",
            Range = "Selbst",
            Effect = Truncate(effectText, 450),
            Fluff = Truncate(fluff, 250),
            CustomEffectLimit = 450
        };
    }

    private static IEnumerable<CardModel> ImportCreatures(JsonNode data, UnitSystem unitSystem)
    {
        if (data["creatures"] is not JsonArray creatures || creatures.Count == 0)
        {
            yield break;
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var creature in creatures)
        {
            if (creature is null) continue;

            var def = creature["definition"];
            if (def is null) continue;

            var name = creature["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = def["name"]?.ToString();
            }
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!seenNames.Add(name)) continue;

            var ac = def["armorClass"]?.GetValue<int>() ?? 10;
            var hp = def["averageHitPoints"]?.GetValue<int>() ?? 1;

            var speedParts = new List<string>();
            if (def["movements"] is JsonArray movements)
            {
                foreach (var mov in movements)
                {
                    var speed = mov?["speed"]?.GetValue<int>() ?? 0;
                    var movId = mov?["movementId"]?.GetValue<int>();
                    var typeStr = movId switch
                    {
                        1 => "Gehen",
                        2 => "Graben",
                        3 => "Klettern",
                        4 => "Fliegen",
                        5 => "Schwimmen",
                        _ => null
                    };

                    if (speed > 0)
                    {
                        var formatted = FormatRangeDistance(speed, unitSystem);
                        speedParts.Add(typeStr is not null ? $"{typeStr} {formatted}" : formatted);
                    }
                }
            }
            var speedStr = speedParts.Count > 0 ? string.Join(", ", speedParts) : "9 m";

            var crId = def["challengeRatingId"]?.GetValue<int>() ?? 0;
            var crStr = crId switch
            {
                1 => "0",
                2 => "1/8",
                3 => "1/4",
                4 => "1/2",
                5 => "1",
                6 => "2",
                7 => "3",
                _ => $"{crId}"
            };

            var actionsDesc = StripHtml(def["actionsDescription"]?.ToString() ?? "");
            var traitsDesc = StripHtml(def["specialTraitsDescription"]?.ToString() ?? "");

            var statsSummary = $"RK: {ac} | TP: {hp} | Tempo: {speedStr} | HG: {crStr}";
            var combinedEffect = $"{statsSummary}\n";
            if (!string.IsNullOrWhiteSpace(traitsDesc))
            {
                combinedEffect += $"{traitsDesc}\n";
            }
            if (!string.IsNullOrWhiteSpace(actionsDesc))
            {
                combinedEffect += $"{actionsDesc}";
            }

            var groupId = creature["groupId"]?.GetValue<int>() ?? 0;
            var categoryPrefix = groupId switch
            {
                4 => "Reittier",
                13 => "Tiergestalt",
                _ => "Begleiter"
            };

            yield return new CardModel
            {
                Name = $"{categoryPrefix}: {name}",
                Type = CardColor.Blue,
                Cost = "Aktion",
                Range = "Selbst",
                Effect = Truncate(combinedEffect.Trim(), 450),
                Fluff = $"{name} (Herausforderungsgrad {crStr})",
                CustomEffectLimit = 450
            };
        }
    }

    private static string StripHtml(string html) =>
        Regex.Replace(html, "<.*?>", " ").Replace("&nbsp;", " ").Trim();

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
}

