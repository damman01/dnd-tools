# DnD Aktions- & Zauberkarten-Generator

Erzeugt druckfertige DnD-Karten als PDF – aus JSON-Dateien, über die Kommandozeile oder
über eine Web-UI mit Upload und Formular-Eingabe.

## Installation

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -e ".[ui,dev]"
```

> WeasyPrint benötigt unter Windows zusätzlich die GTK-Runtime
> (siehe <https://doc.courtbouillon.org/weasyprint/stable/first_steps.html>).
> Ohne GTK funktionieren CLI/UI weiterhin für den HTML-Export.

## Kommandozeile

```powershell
dnd-cards examples/wiwaka.json -o out/Wiwaka.pdf
dnd-cards examples/wiwaka.json --html          # nur HTML-Vorschau
```

## Web-UI

```powershell
streamlit run src/dndcards/app.py
```

- JSON-Dateien hochladen (auch mehrere – die Karten werden zusammengeführt)
- Karten im Formular anlegen und bearbeiten
- Export als PDF, HTML oder JSON

## JSON-Format

```json
{
  "title": "Mein Deck",
  "page": { "width": "70mm", "height": "120mm", "margin": "4mm" },
  "cards": [
    {
      "name": "Heilendes Wort",
      "type": "green",
      "cost": "Bonusaktion",
      "range": "18 Meter",
      "effect": "Ein Verbündeter heilt 1W4 + 4 Trefferpunkte.",
      "fluff": "Ich rufe die tröstende Kraft der Sterne an.",
      "tactic": "Rette Verbündete auf 0 Trefferpunkten aus sicherer Distanz.",
      "tracker": { "label": "Pro Tag:", "count": 3 },
      "tracker_rows": [{ "label": "Zauberplätze Grad 1", "count": 4 }]
    }
  ]
}
```

| Feld | Bedeutung |
| --- | --- |
| `name` | Kartentitel (Pflichtfeld) |
| `type` | Farbschema: `red`, `blue`, `green`, `gold`, `purple`, `grey` |
| `cost` / `range` | Kopfzeilen-Statistiken |
| `effect` | Regeltext im hervorgehobenen Kasten |
| `fluff` | Kursiver Erzähltext |
| `tactic` | Hinweis „Wann man es nutzt" |
| `tracker` | Eine Zeile abstreichbarer Kreise |
| `tracker_rows` | Mehrere Ressourcen-Zeilen (z. B. Zauberplätze) |

Alle Felder außer `name` sind optional. Eine JSON-Datei darf auch nur aus einer
Kartenliste bestehen; das alte Notebook-Format mit `trackers`/`tracker_label`
wird weiterhin gelesen.

## Tests

```powershell
pytest
```
