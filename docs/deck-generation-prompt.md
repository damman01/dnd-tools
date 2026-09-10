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
2. "cost": Aktion, Bonusaktion oder Reaktion (max. 30 Zeichen).
3. "range": Reichweite in Metern (z. B. "9 m (30 ft)", "Selbst", "Berührung") (max. 40 Zeichen).
4. "effect": Ein klarer, direkter Regeltext mit bereits ausgerechneten Boni (z.B. "Wirf den W20 (+7). Trifft der Schlag, machst du 1W12+4 Hiebschaden.").
   - Standard-Länge bei Karten mit Fluff & Taktik: max. 220 Zeichen.
   - Karten ohne Fluff & Taktik können den gesamten Platz nutzen: bis zu 450 Zeichen. Setze bei solchen Karten optional `"custom_effect_limit": 450`.
5. "fluff": Optional. Ein atmosphärischer Satz aus der ICH-Perspektive des Charakters (max. 180 Zeichen). Kann weggelassen werden, wenn der Effekttext mehr Platz benötigt.
6. "tactic": Optional. Ein kurzer Tipp für den Spieltisch (max. 200 Zeichen). Kann weggelassen werden, wenn der Effekttext mehr Platz benötigt.
7. "tracker": Optional für limitierte Fähigkeiten (z.B. `{"label": "Pro Tag:", "count": 3}`).
8. "custom_effect_limit": Optional. Erlaubte Werte: 220, 360 oder 450. Wenn weggelassen, berechnet das Tool die optimale Größe automatisch.
9. Füge als letzte Karte IMMER eine "Ressourcen & Fähigkeiten"-Karte vom Typ "gold" hinzu, die unter "tracker_rows" alle relevanten Ressourcen des Charakters auflistet:
   - Zauberplätze nach Grad (nur für zauberwirkende Klassen wie Magier, Kleriker, Barde etc.)
   - Reine Kampffähigkeiten für Nicht-Zauberer (z.B. Barbar: "Kampfrausch", "Erholung"; Kämpfer: "Tatendrang / Action Surge", "Zweite Luft / Second Wind"; Mönch: "Ki-Punkte"; etc.)
   - Gib Klassen OHNE Zauberplätze KEINE Zauberplatz-Tracker!

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
      "range": "9 m (30 ft) oder Selbst",
      "effect": "Simpler Regeltext mit bereits ausgerechneten Boni.",
      "fluff": "Flufftext in der Ich-Perspektive.",
      "tactic": "Taktischer Tipp für den Einsatz."
    },
    {
      "name": "Reine Effektkarte (großer Textblock)",
      "type": "blue",
      "cost": "Reaktion",
      "range": "Selbst",
      "effect": "Ausführlicherer Zauber- oder Regeltext ohne Fluff/Taktik, der den gesamten Platz der Karte nutzt (bis zu 450 Zeichen).",
      "custom_effect_limit": 450
    },
    {
      "name": "Limitierte Fähigkeit",
      "type": "red|blue|green",
      "cost": "Bonusaktion",
      "range": "Selbst",
      "effect": "Effekttext...",
      "fluff": "Flufftext...",
      "tactic": "Taktik...",
      "tracker": { "label": "Pro Tag:", "count": 3 }
    },
    {
      "name": "Ressourcen & Fähigkeiten",
      "type": "gold",
      "fluff": "Allgemeiner Fluff-Text zur Herkunft der Kraft des Charakters.",
      "tracker_rows": [
        { "label": "Kampfrausch / Rage (Täglich)", "count": 3 },
        { "label": "Zweite Luft (Kurze Rast)", "count": 1 }
      ]
    }
  ]
}
```

Hier sind die Daten des Charakters. Erstelle daraus die 6-8 wichtigsten Karten plus die Ressourcen-Karte:

[FÜGE HIER DEN CHARAKTERBOGEN EIN]

<!-- markdownlint-enable -->
