using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CardStudio.Shared.Models;

namespace CardStudio.Api.Modules.Import;

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
            var def = spellEntry["definition"];
            if (def is null) continue;

            var id = def["id"]?.ToString() ?? def["name"]?.ToString() ?? "";
            if (!seenIds.Add(id)) continue;

            var name = def["name"]?.ToString() ?? "Unbenannter Zauber";
            var level = def["level"]?.GetValue<int>() ?? 0;
            var description = StripHtml(def["description"]?.ToString() ?? "");

            var activationType = def["activation"]?["activationType"]?.GetValue<int>();
            var cost = activationType switch
            {
                1 => "Aktion",
                3 => "Bonusaktion",
                4 => "Reaktion",
                _ => def["activation"]?["activationType"]?.ToString()
            };

            var rangeValue = def["range"]?["rangeValue"]?.GetValue<int>();
            var rawRange = rangeValue switch
            {
                null or 0 => def["range"]?["origin"]?.ToString(),
                _ => $"{rangeValue} ft"
            };
            var range = FormatDistance(rawRange, unitSystem);

            var school = def["school"]?.ToString();
            var type = school switch
            {
                "Evocation" or "Necromancy" => CardColor.Red,
                "Abjuration" => CardColor.Blue,
                "Conjuration" when description.Contains("heal", StringComparison.OrdinalIgnoreCase) => CardColor.Green,
                "Divination" or "Enchantment" or "Illusion" or "Transmutation" => CardColor.Blue,
                _ => CardColor.Blue
            };

            yield return new CardModel
            {
                Name = name,
                Type = type,
                Cost = cost,
                Range = range,
                Effect = Truncate(description, CardTextLimits.Effect),
                Fluff = level == 0 ? "Ein Zaubertrick, jederzeit einsetzbar." : $"Zauber des Grades {level}."
            };
        }
    }

    private static IEnumerable<JsonNode> EnumerateSpellEntries(JsonNode data)
    {
        if (data["spells"] is JsonObject spellsObj)
        {
            foreach (var (_, list) in spellsObj)
            {
                if (list is not JsonArray arr) continue;
                foreach (var item in arr)
                {
                    if (item is not null) yield return item;
                }
            }
        }

        if (data["classSpells"] is JsonArray classSpells)
        {
            foreach (var cls in classSpells)
            {
                if (cls?["spells"] is not JsonArray list) continue;
                foreach (var item in list)
                {
                    if (item is not null) yield return item;
                }
            }
        }
    }

    private static CardModel? BuildSpellSlotResourceCard(JsonNode data)
    {
        if (data["classes"] is not JsonArray classes)
        {
            return null;
        }

        var totalSlotsByLevel = new int[10];

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

            if (table[level] is not JsonArray slotsForLevel)
            {
                continue;
            }

            for (var slotLvl = 1; slotLvl < slotsForLevel.Count && slotLvl < totalSlotsByLevel.Length; slotLvl++)
            {
                var count = slotsForLevel[slotLvl]?.GetValue<int>() ?? 0;
                totalSlotsByLevel[slotLvl] += count;
            }
        }

        var rows = new List<CardTracker>();
        for (var lvl = 1; lvl < totalSlotsByLevel.Length; lvl++)
        {
            if (totalSlotsByLevel[lvl] > 0)
            {
                rows.Add(new CardTracker
                {
                    Label = $"Grad {lvl}:",
                    Count = totalSlotsByLevel[lvl]
                });
            }
        }

        if (rows.Count == 0)
        {
            return null;
        }

        return new CardModel
        {
            Name = "Zauberplätze",
            Type = CardColor.Gold,
            Fluff = "Ressourcen für Zauber",
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
            Fluff = "Tägliche Fähigkeiten & Zähler",
            TrackerRows = rows
        };
    }

    private static IEnumerable<CardModel> ImportActions(JsonNode data, int proficiencyBonus, UnitSystem unitSystem)
    {
        if (data["actions"] is not JsonObject actionGroups)
        {
            yield break;
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, group) in actionGroups)
        {
            if (group is not JsonArray arr) continue;

            foreach (var action in arr)
            {
                if (action is null) continue;
                var name = action["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name) || !seenNames.Add(name)) continue;

                var description = StripHtml(action["description"]?.ToString() ?? action["snippet"]?.ToString() ?? "");

                var activationType = action["activation"]?["activationType"]?.GetValue<int>();
                var cost = activationType switch
                {
                    1 => "Aktion",
                    3 => "Bonusaktion",
                    4 => "Reaktion",
                    _ => null
                };

                var rangeValue = action["range"]?["range"]?.GetValue<int>();
                var rawRange = rangeValue switch
                {
                    null or 0 => action["range"]?["aoeType"]?.ToString(),
                    _ => $"{rangeValue} ft"
                };
                var range = FormatDistance(rawRange, unitSystem);

                var limitedUse = action["limitedUse"];
                CardTracker? tracker = null;
                if (limitedUse is not null)
                {
                    var maxUses = limitedUse["maxUses"]?.GetValue<int>() ?? 0;
                    if (maxUses <= 0 && limitedUse["useProficiencyBonus"]?.GetValue<bool>() == true)
                    {
                        maxUses = proficiencyBonus;
                    }
                    if (maxUses > 0)
                    {
                        var resetType = limitedUse["resetType"]?.GetValue<int>() == 1 ? "Kurze Rast" : "Tag";
                        tracker = new CardTracker
                        {
                            Label = $"Pro {resetType}:",
                            Count = maxUses
                        };
                    }
                }

                yield return new CardModel
                {
                    Name = name,
                    Type = CardColor.Red,
                    Cost = cost,
                    Range = range,
                    Effect = Truncate(description, CardTextLimits.Effect),
                    Tracker = tracker
                };
            }
        }
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
        return totalLevel switch
        {
            >= 17 => 6,
            >= 13 => 5,
            >= 9 => 4,
            >= 5 => 3,
            _ => 2
        };
    }

    private static string? FormatDistance(string? raw, UnitSystem unitSystem)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var ftMatch = Regex.Match(raw, @"^(\d+)\s*(?:ft|feet)$", RegexOptions.IgnoreCase);
        if (!ftMatch.Success || !int.TryParse(ftMatch.Groups[1].Value, out var feet))
        {
            return raw;
        }

        var meters = Math.Round(feet * 0.3048, 1);
        var mStr = meters % 1 == 0 ? $"{meters:0} m" : $"{meters:0.#} m";

        return unitSystem switch
        {
            UnitSystem.Metric => mStr,
            UnitSystem.Imperial => $"{feet} ft",
            _ => $"{mStr} ({feet} ft)"
        };
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
                        var formatted = FormatDistance($"{speed} ft", unitSystem) ?? $"{speed} ft";
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

    private static string StripHtml(string html)
    {
        var text = Regex.Replace(html, "<.*?>", string.Empty);
        return System.Net.WebUtility.HtmlDecode(text).Trim();
    }

    private static string Truncate(string text, int max)
    {
        if (text.Length <= max) return text;
        return text[..max].TrimEnd() + "…";
    }
}
