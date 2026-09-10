namespace DndCards.Web.Services;

public enum UnitSystem
{
    Imperial, // ft, miles
    Metric,   // m, km
    Both      // e.g. 18 m (60 ft)
}

public class LocalizationService
{
    public string CurrentLanguage { get; private set; } = "de";
    public UnitSystem CurrentUnitSystem { get; private set; } = UnitSystem.Metric;

    public event Action? OnLanguageChanged;
    public event Action? OnUnitSystemChanged;

    public void SetLanguage(string language)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        CurrentLanguage = language switch
        {
            "en" => "en",
            _ => "de"
        };

        OnLanguageChanged?.Invoke();
    }

    public void SetUnitSystem(UnitSystem unitSystem)
    {
        if (CurrentUnitSystem == unitSystem)
        {
            return;
        }

        CurrentUnitSystem = unitSystem;
        OnUnitSystemChanged?.Invoke();
    }

    public string T(string key)
    {
        if (Translations.TryGetValue(key, out var dict) && dict.TryGetValue(CurrentLanguage, out var value))
        {
            return value;
        }

        return key;
    }

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        // Navigation & Layout
        ["app.title"] = new() { ["de"] = "TTRPG Card Studio", ["en"] = "TTRPG Card Studio" },
        ["app.compatible_badge"] = new() { ["de"] = "5e-kompatibles Utility", ["en"] = "5e Compatible Utility" },
        ["nav.dashboard"] = new() { ["de"] = "Dashboard", ["en"] = "Dashboard" },
        ["nav.generator"] = new() { ["de"] = "Kartengenerator", ["en"] = "Card Generator" },
        ["topbar.legal_button"] = new() { ["de"] = "Rechtliches & Lizenz", ["en"] = "Legal & License" },
        ["footer.tagline"] = new() { ["de"] = "Ein unabhängiges Formatierungswerkzeug für Spieltische.", ["en"] = "An independent formatting utility for gaming tables." },
        ["footer.legal_link"] = new() { ["de"] = "Rechtliche Hinweise & Disclaimer", ["en"] = "Legal Notices & Disclaimer" },

        // Home Dashboard
        ["home.hero.pill"] = new() { ["de"] = "5e Kompatibel", ["en"] = "5e Compatible" },
        ["home.hero.subpill"] = new() { ["de"] = "Tabletop RPG Karten-Utility", ["en"] = "Tabletop RPG Card Utility" },
        ["home.hero.title"] = new() { ["de"] = "Druckfertige Karten für deinen Spieltisch", ["en"] = "Print-ready cards for your tabletop session" },
        ["home.hero.desc"] = new() {
            ["de"] = "Erstelle professionelle Zauber-, Aktions- und Referenzkarten für deine TTRPG-Runden. Nutze flexible Zähler, importiere Daten aus D&D Beyond oder erstelle eigene Homebrew-Sets – alles druckoptimiert und sofort einsatzbereit.",
            ["en"] = "Create professional spell, action, and reference cards for your TTRPG sessions. Use flexible trackers, import data from D&D Beyond, or craft custom homebrew decks – print-optimized and ready for play."
        },
        ["home.hero.cta_start"] = new() { ["de"] = "Zum Kartengenerator", ["en"] = "Open Card Generator" },
        ["home.hero.cta_legal"] = new() { ["de"] = "Lizenz & Kompatibilität", ["en"] = "License & Compatibility" },
        ["home.feat1.title"] = new() { ["de"] = "D&D Beyond Import", ["en"] = "D&D Beyond Import" },
        ["home.feat1.desc"] = new() {
            ["de"] = "Importiere deinen Charakterbogen direkt via JSON-Export. Alle Zauber, Aktionen und Zauberplatz-Tracker werden automatisch in bearbeitbare Karten konvertiert.",
            ["en"] = "Import your character sheet directly via JSON export. Spells, actions, and spell slot trackers are automatically converted into editable cards."
        },
        ["home.feat2.title"] = new() { ["de"] = "Druckoptimiert & Bemaßt", ["en"] = "Print-Ready & Sized" },
        ["home.feat2.desc"] = new() {
            ["de"] = "Automatische Einhaltung von Textlängen und Standard-Kartengrößen (70x120mm). Keine überlappenden Texte, saubere Rahmen und direkter PDF- & PNG-Export.",
            ["en"] = "Automatic enforcement of text limits and standard card dimensions (70x120mm). No overflowing text, clean borders, and instant PDF & PNG export."
        },
        ["home.feat3.title"] = new() { ["de"] = "Ressourcen-Tracker", ["en"] = "Resource Trackers" },
        ["home.feat3.desc"] = new() {
            ["de"] = "Integrierte Tracker-Reihen für Zauberplätze, Trefferwürfel, Ki-Punkte oder Klassenfähigkeiten. Perfekt für abwischbare Hüllen oder Markierungen am Tisch.",
            ["en"] = "Built-in tracker rows for spell slots, hit dice, ki points, or class features. Ideal for dry-erase sleeves or table-side tokens."
        },
        ["home.footer_note"] = new() {
            ["de"] = "Unabhängiges Software-Dienstprogramm. 5e-kompatibel gemäß CC-BY-4.0 SRD 5.1 Bestimmungen.",
            ["en"] = "Independent software utility. 5e-compatible under CC-BY-4.0 SRD 5.1 terms."
        },
        ["home.link_create"] = new() { ["de"] = "Jetzt Karten gestalten →", ["en"] = "Design cards now →" },

        // Card Generator
        ["gen.title"] = new() { ["de"] = "TTRPG Kartengenerator", ["en"] = "TTRPG Card Generator" },
        ["gen.subtitle"] = new() {
            ["de"] = "5e-kompatible Aktions-, Zauber- & Referenzkarten gestalten. Texte passen garantiert auf 70×120mm Karten.",
            ["en"] = "Design 5e-compatible action, spell & reference cards. Content is guaranteed to fit 70×120mm cards."
        },
        ["gen.btn.add"] = new() { ["de"] = "Karte hinzufügen", ["en"] = "Add Card" },
        ["gen.btn.save_json"] = new() { ["de"] = "JSON sichern", ["en"] = "Save JSON" },
        ["gen.btn.export_html"] = new() { ["de"] = "HTML Export", ["en"] = "HTML Export" },
        ["gen.btn.export_png"] = new() { ["de"] = "PNG erstellen", ["en"] = "Generate PNG" },
        ["gen.btn.export_pdf"] = new() { ["de"] = "PDF exportieren", ["en"] = "Export PDF" },
        ["gen.btn.translate_deck"] = new() { ["de"] = "Deck übersetzen", ["en"] = "Translate Deck" },
        ["gen.btn.translate_card"] = new() { ["de"] = "Karte übersetzen", ["en"] = "Translate Card" },
        ["gen.btn.convert_units"] = new() { ["de"] = "Einheiten umrechnen", ["en"] = "Convert Units" },
        ["gen.units.imperial"] = new() { ["de"] = "Imperial (ft)", ["en"] = "Imperial (ft)" },
        ["gen.units.metric"] = new() { ["de"] = "Metrisch (m)", ["en"] = "Metric (m)" },
        ["gen.units.both"] = new() { ["de"] = "Beide (m & ft)", ["en"] = "Both (m & ft)" },
        ["gen.units.label"] = new() { ["de"] = "Maßeinheit:", ["en"] = "Units:" },

        ["gen.import.deck_json"] = new() { ["de"] = "Deck-JSON importieren", ["en"] = "Import Deck JSON" },
        ["gen.import.dndbeyond"] = new() { ["de"] = "D&D Beyond Charakter importieren", ["en"] = "Import D&D Beyond Character" },
        ["gen.deck_title"] = new() { ["de"] = "Deck-Titel:", ["en"] = "Deck Title:" },
        ["gen.cards_count"] = new() { ["de"] = "Karten im Deck", ["en"] = "cards in deck" },
        ["gen.no_cards"] = new() { ["de"] = "Noch keine Karten vorhanden", ["en"] = "No cards available yet" },
        ["gen.no_cards_desc"] = new() { ["de"] = "Füge eine neue Karte hinzu oder importiere ein existierendes Deck.", ["en"] = "Add a new card or import an existing deck." },

        ["table.th.num"] = new() { ["de"] = "#", ["en"] = "#" },
        ["table.th.name"] = new() { ["de"] = "Kartenname", ["en"] = "Card Name" },
        ["table.th.type"] = new() { ["de"] = "Farbe / Typ", ["en"] = "Color / Type" },
        ["table.th.cost"] = new() { ["de"] = "Kosten", ["en"] = "Cost" },
        ["table.th.range"] = new() { ["de"] = "Reichweite", ["en"] = "Range" },
        ["table.th.trackers"] = new() { ["de"] = "Zähler", ["en"] = "Trackers" },
        ["table.th.actions"] = new() { ["de"] = "Aktionen", ["en"] = "Actions" },

        ["edit.title"] = new() { ["de"] = "Karte bearbeiten:", ["en"] = "Edit Card:" },
        ["edit.live_hint"] = new() { ["de"] = "Änderungen spiegeln sich sofort in der Vorschau wider", ["en"] = "Changes update immediately in live preview" },
        ["edit.name"] = new() { ["de"] = "Kartenname", ["en"] = "Card Name" },
        ["edit.type"] = new() { ["de"] = "Rahmen / Typ", ["en"] = "Border / Type" },
        ["edit.cost"] = new() { ["de"] = "Kosten", ["en"] = "Cost" },
        ["edit.cost_placeholder"] = new() { ["de"] = "z. B. 1 Aktion", ["en"] = "e.g. 1 Action" },
        ["edit.range"] = new() { ["de"] = "Reichweite / Wirkungsbereich", ["en"] = "Range / Area" },
        ["edit.range_placeholder"] = new() { ["de"] = "z. B. 18 m / 60 ft", ["en"] = "e.g. 60 ft" },
        ["edit.tracker"] = new() { ["de"] = "Hauptzähler / Tracker", ["en"] = "Main Tracker" },
        ["edit.tracker_label_placeholder"] = new() { ["de"] = "Label (z. B. Pro Tag:)", ["en"] = "Label (e.g. Per Day:)" },
        ["edit.effect"] = new() { ["de"] = "Effekt / Beschreibung", ["en"] = "Effect / Description" },
        ["edit.effect_size.auto"] = new() { ["de"] = "Auto", ["en"] = "Auto" },
        ["edit.effect_size.standard"] = new() { ["de"] = "Standard (220)", ["en"] = "Standard (220)" },
        ["edit.effect_size.medium"] = new() { ["de"] = "Mittel (360)", ["en"] = "Medium (360)" },
        ["edit.effect_size.full"] = new() { ["de"] = "Voll (450)", ["en"] = "Full (450)" },
        ["edit.effect_size.hint"] = new() { ["de"] = "Textblockgröße anpassen", ["en"] = "Adjust text block size" },
        ["edit.effect_placeholder"] = new() { ["de"] = "Regelmechanischer Effekt der Karte...", ["en"] = "Mechanic effect of the card..." },
        ["edit.fluff"] = new() { ["de"] = "Fluff / Zitat", ["en"] = "Fluff / Quote" },
        ["edit.fluff_placeholder"] = new() { ["de"] = "Atmosphärischer Flavour-Text...", ["en"] = "Atmospheric flavour text..." },
        ["edit.tactic"] = new() { ["de"] = "Taktik / Wann man es nutzt", ["en"] = "Tactics / When to use" },
        ["edit.tactic_placeholder"] = new() { ["de"] = "Kurzer Taktik-Hinweis für den Spieltisch...", ["en"] = "Tactical note for tabletop play..." },
        ["edit.rows_title"] = new() { ["de"] = "Zusätzliche Zähler-Zeilen (z. B. Zauberplätze nach Grad):", ["en"] = "Additional Tracker Rows (e.g. Spell slots by level):" },
        ["edit.rows_add"] = new() { ["de"] = "+ Zeile hinzufügen", ["en"] = "+ Add Row" },

        ["preview.title"] = new() { ["de"] = "Live-Druckvorschau", ["en"] = "Live Print Preview" },
        ["preview.unnamed"] = new() { ["de"] = "Unbenannte Karte", ["en"] = "Unnamed Card" },
        ["preview.when_to_use"] = new() { ["de"] = "Wann man es nutzt:", ["en"] = "When to use:" },
        ["preview.cost_label"] = new() { ["de"] = "Kosten:", ["en"] = "Cost:" },
        ["preview.range_label"] = new() { ["de"] = "Reichweite:", ["en"] = "Range:" },
        ["preview.empty_hint"] = new() { ["de"] = "Wähle eine Karte aus der Liste aus, um die Live-Vorschau anzuzeigen.", ["en"] = "Select a card from the list to display the live preview." },
        ["preview.ratio_hint"] = new() { ["de"] = "Druckfertiges Seitenverhältnis mit automatischer Rand- und Schriftgrößenanpassung.", ["en"] = "Print-ready aspect ratio with automatic margin and font adaptation." },

        // Legal Modal
        ["legal.modal_title"] = new() { ["de"] = "Rechtliche Hinweise & Kompatibilität", ["en"] = "Legal Notices & Compatibility" },
        ["legal.trademark_title"] = new() { ["de"] = "Marken- & Urheberrechtshinweis", ["en"] = "Trademark & Copyright Notice" },
        ["legal.trademark_body"] = new() {
            ["de"] = "TTRPG Card Studio ist ein unabhängiges Software-Dienstprogramm. Alle Produktnamen, Logos und Marken sind Eigentum der jeweiligen Inhaber. Die Verwendung der Begriffe „D&D“ oder „5e“ erfolgt ausschließlich zu nominativen Zwecken zur Beschreibung der Systemkompatibilität und impliziert keinerlei Zugehörigkeit, Sponsoring oder Billigung durch Wizards of the Coast LLC.",
            ["en"] = "TTRPG Card Studio is an independent software utility. All product names, logos, and brands are property of their respective owners. Use of the terms 'D&D' or '5e' is strictly for nominative purposes to describe system compatibility and does not imply any affiliation with, sponsorship, or endorsement by Wizards of the Coast LLC."
        },
        ["legal.ugc_title"] = new() { ["de"] = "Nutzergenerierte Inhalte (UGC)", ["en"] = "User-Generated Content (UGC)" },
        ["legal.ugc_body"] = new() {
            ["de"] = "Dieses Werkzeug dient rein als Formatierungs- und Layout-Prozessor für Texte und Daten, die vom Nutzer selbst eingegeben oder importiert werden. Nutzer sind selbst dafür verantwortlich sicherzustellen, dass sie über die notwendigen Rechte oder Lizenzen für eingegebene Texte, Bilder und Homebrew-Inhalte verfügen.",
            ["en"] = "This tool operates solely as a blank formatting and layout processor for text and data supplied or imported by the user. Users are solely responsible for ensuring they hold the necessary rights or licenses for any text, artwork, or homebrew content they input."
        },
        ["legal.srd_title"] = new() { ["de"] = "System Reference Document (SRD 5.1 / CC-BY-4.0)", ["en"] = "System Reference Document (SRD 5.1 / CC-BY-4.0)" },
        ["legal.srd_body"] = new() {
            ["de"] = "Soweit Regelinhalte aus dem offiziellen System Reference Document verwendet werden: This work includes material taken from the System Reference Document 5.1 („SRD 5.1“) by Wizards of the Coast LLC, available at https://dnd.wizards.com/resources/systems-reference-document, under the Creative Commons Attribution 4.0 International License.",
            ["en"] = "Where rule content from the official System Reference Document is utilized: This work includes material taken from the System Reference Document 5.1 (\"SRD 5.1\") by Wizards of the Coast LLC, available at https://dnd.wizards.com/resources/systems-reference-document, under the Creative Commons Attribution 4.0 International License."
        },
        ["legal.close"] = new() { ["de"] = "Schließen", ["en"] = "Close" },
    };
}
