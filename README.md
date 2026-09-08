# DnD Tools – Action & Spell Card Generator

A self-hostable tool that turns D&D character abilities into small, printable
reference cards (70×120 mm by default) as a PDF. Card content itself can be
authored in any language (the bundled UI defaults to German cards), but the
codebase, commits, issues, and docs are kept in English.

## Status

Actively evolving prototype. The original idea started as a Jupyter notebook
([DnD_Action_and_Spell_Card_Generator.ipynb](DnD_Action_and_Spell_Card_Generator.ipynb))
and is being rebuilt as a containerizable .NET web app so it can eventually be
hosted as a small paid service.

## Architecture

| Project | Purpose |
|---|---|
| [DndTools.AppHost](DndTools.AppHost/) | [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) orchestration for local development |
| [src/DndCards.Web](src/DndCards.Web/) | Blazor Web App (interactive server) – the actual product: card editor + PDF export |
| [src/DndTools.ServiceDefaults](src/DndTools.ServiceDefaults/) | Shared OpenTelemetry / health-check / resilience defaults |

Key building blocks inside `DndCards.Web`:

- `Models/CardModel.cs` – `CardDeck` / `CardModel` / `CardTracker` data model, JSON-serializable.
- `Services/CardHtmlBuilder.cs` – renders a deck into printable HTML/CSS (only fields that are actually filled in are shown on a card).
- `Services/PdfCardService.cs` – turns that HTML into a PDF via [PeachPDF](https://peachpdf.net/) (pure .NET, no headless browser or wkhtmltopdf dependency).
- `Services/DndBeyondImportService.cs` – best-effort mapper from an uploaded D&D Beyond character JSON export (spells/actions) into cards.
- `Services/UsageLimitService.cs` – free-tier usage gate scaffold for a future paid tier (not wired to a payment provider yet, see [Monetization](#monetization)).
- `Components/Pages/Cards.razor` – the `/cards` UI: manual card editor, deck JSON upload/download, PDF export.

## Getting started

Prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/cli/overview) (`dotnet tool install -g Aspire.Cli`)

Run the app (always through Aspire, not `dotnet run`, so the dashboard and
telemetry pipeline come up correctly):

```powershell
aspire start
aspire wait web
```

Then open the `web` resource's URL from `aspire ps` / the Aspire dashboard and
navigate to `/cards`.

## Card deck JSON format

Decks can be authored by hand as JSON and uploaded via the "Karten-JSON laden"
button, or exported back out for reuse. See
[examples/sample-deck.json](examples/sample-deck.json) for a complete example.

```jsonc
{
  "title": "My Deck",
  "page": { "width": "70mm", "height": "120mm", "margin": "4mm" },
  "cards": [
    {
      "name": "Fire Bolt",      // required
      "type": "red",            // required: red | blue | green | gold
      "cost": "Action",         // optional
      "range": "120 feet",      // optional
      "effect": "...",          // optional
      "fluff": "...",           // optional, shown in italics
      "tactic": "...",          // optional
      "tracker": { "label": "Uses:", "count": 3 },       // optional, single tracker row
      "tracker_rows": [ { "label": "...", "count": 4 } ] // optional, multi-row resource card
    }
  ]
}
```

Only fields that are present end up on the printed card.

## Importing a D&D Beyond character

- **Supported:** uploading a D&D Beyond character JSON export (the unofficial
  `character-service` response shape) via the "D&D Beyond Charakter
  importieren" upload. The app never calls the D&D Beyond API itself — that
  endpoint is undocumented/unofficial and could change or break at any time,
  so fetching it server-side was deliberately not implemented. Users who want
  this can retrieve the JSON themselves (e.g. via browser dev tools) and
  upload it.
- **Not supported:** D&D Beyond's PDF character sheet export. It only embeds
  real text for the static form labels — all character-specific content
  (name, stats, spells, actions) is rendered as a rasterized image per page,
  so there is no text layer to parse without OCR. This was verified against a
  real export and intentionally not built to avoid shipping an unreliable
  import path.

## Monetization

`UsageLimitService` contains a free-tier scaffold (max cards per deck, max PDF
generations per day, configurable under `Monetization` in `appsettings.json`)
with an `IsPremium()` extension point. Wiring this up to a real payment
provider (e.g. Stripe) is intentionally deferred — see open issues.

## Container hosting

A production [Dockerfile](src/DndCards.Web/Dockerfile) is provided for
`DndCards.Web` (multi-stage build on `mcr.microsoft.com/dotnet/aspnet:10.0`,
listens on port 8080).

```powershell
docker build -f src/DndCards.Web/Dockerfile -t dndcards-web .
docker run -p 8080:8080 dndcards-web
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE)
