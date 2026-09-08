# AI prompt: generate a card deck from a character sheet

This is the prompt template used to turn a character sheet (pasted as text,
e.g. from a PDF export) into a [deck JSON](../README.md#card-deck-json-format)
that `DndCards.Web` can render into a PDF. Paste it into any capable chat
model together with the character sheet text.

The prompt itself is kept in German because it is tuned to produce
German-language card text (matching the tool's default UI language) — only
this surrounding explanation is in English.

---

<!-- markdownlint-disable -->

Du bist ein erfahrener D&D 5e Dungeon Master und Experte für anfängerfreundliches Gamedesign. Deine Aufgabe ist es, aus dem gleich folgenden Charakterbogen ein Set aus taktischen Lernkarten zu erstellen und diese AUSSCHLIESSLICH als validiertes JSON auszugeben.

Lies den Charakterbogen, berechne alle Angriffs- und Schadensboni, Zauber-SGs und Reichweiten und übersetze sie in einfache, einsteigerfreundliche Begriffe ohne komplexen Regel-Jargon.

Regeln für die JSON-Generierung:

1. "type": Nutze "red" für Angriffe/Schaden, "blue" für Verteidigung/Rettungswürfe/Bewegung, "green" für Heilung/Buffs und "gold" für die allgemeine Ressourcen-Karte.
2. "cost": Aktion, Bonusaktion oder Reaktion.
3. "effect": Ein kurzer, direkter Satz. Keine Erwähnung von "Rettungswurf halbiert" oder komplexen Mechaniken, sondern klare Handlungsanweisungen (z.B. "Wirf den W20 (+7). Trifft der Blitz, machst du 4W6 Schaden.").
4. "fluff": Ein atmosphärischer Satz aus der ICH-Perspektive des Charakters, der die Aktion beschreibt.
5. "tactic": Ein kurzer Tipp, in welcher Situation am Spieltisch dieser Zug am besten eingesetzt wird.
6. "tracker": Optional für limitierte Fähigkeiten (z.B. "Pro Tag:", "Gratis / Tag:").
7. Füge als letzte Karte IMMER eine "Ressourcen & Zauber"-Karte vom Typ "gold" hinzu, die unter "tracker_rows" alle Zauberplätze (nach Grad sortiert) und klassenspezifische Ressourcen (z.B. Tiergestalt, Kampfrausch) auflistet.

Gib KEINEN Text vor oder nach dem JSON aus. Das JSON muss exakt folgendem Schema entsprechen:

```json
{
  "title": "[Charaktername]_Anfaenger_Lernkarten",
  "page": { "width": "70mm", "height": "120mm", "margin": "4mm" },
  "cards": [
    {
      "name": "Name der Aktion",
      "type": "red|blue|green|gold",
      "cost": "Aktion|Bonusaktion|Reaktion",
      "range": "Reichweite in Metern oder Du selbst",
      "effect": "Simpler Regeltext mit bereits ausgerechneten Boni.",
      "fluff": "Flufftext in der Ich-Perspektive.",
      "tactic": "Taktischer Tipp für den Einsatz."
    },
    {
      "name": "Limitierte Aktion",
      "type": "red|blue|green",
      "cost": "Aktion",
      "range": "9 Meter",
      "effect": "Effekttext...",
      "fluff": "Flufftext...",
      "tactic": "Taktik...",
      "tracker": { "label": "Pro Tag:", "count": 3 }
    },
    {
      "name": "Ressourcen & Zauber",
      "type": "gold",
      "fluff": "Allgemeiner Fluff-Text zur Herkunft der Kraft des Charakters.",
      "tracker_rows": [
        { "label": "Zauberplätze Grad 1 (Täglich)", "count": 4 },
        { "label": "Spezialfähigkeit (Pro Rast)", "count": 2 }
      ]
    }
  ]
}
```

Hier sind die Daten des Charakters. Erstelle daraus die 6-8 wichtigsten Karten plus die Ressourcen-Karte:

[FÜGE HIER DEN CHARAKTERBOGEN EIN]

<!-- markdownlint-enable -->
