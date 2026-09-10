using System.Text;
using DndCards.Web.Models;

namespace DndCards.Web.Services;

// Builds the printable HTML card layout from card data. Page size comes from
// CardDeck.Page; only fields the user actually filled in are rendered.
public class CardHtmlBuilder
{
    public string BuildHtml(CardDeck deck)
    {
        var page = deck.Page;
        var sb = new StringBuilder();
        sb.Append($$"""
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8">
        <title>{{Html(deck.Title)}}</title>
        <style>
        @page { size: {{page.Width}} {{page.Height}}; margin: {{page.Margin}}; }
        html, body { height: 100%; }
        body { font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; margin: 0; padding: 0; box-sizing: border-box; }
        *, *::before, *::after { box-sizing: border-box; }
        /* Fixed (not min-) height: combined with the CardTextLimits truncation below,
           content always fits in one page, so the border is never cut off/fragmented. */
        .card { width: 100%; height: calc({{page.Height}} - {{page.Margin}} - {{page.Margin}}); border-radius: 8px; position: relative; overflow: hidden; page-break-before: always; page-break-after: always; }
        .red { background-color: #fffafa; border: 2px solid #8b0000; }
        .blue { background-color: #f8fbff; border: 2px solid #003399; }
        .green { background-color: #f6fff6; border: 2px solid #006400; }
        .gold { background-color: #fffdf5; border: 2px solid #b8860b; }
        .header { color: white; padding: 6px 4px; text-align: center; font-weight: bold; font-size: 10pt; text-transform: uppercase; }
        .red .header { background-color: #b30000; border-bottom: 2px solid #8b0000; }
        .blue .header { background-color: #0044cc; border-bottom: 2px solid #003399; }
        .green .header { background-color: #008000; border-bottom: 2px solid #006400; }
        .gold .header { background-color: #daa520; border-bottom: 2px solid #b8860b; }
        .content { padding: 6px; color: #222; }
        .stat-row { margin-bottom: 4px; font-size: 8pt; line-height: 1.2; }
        .stat-label { font-weight: bold; text-transform: uppercase; font-size: 7pt; color: #555; display: inline-block; width: 23mm; }
        .effect { background: #fff; border-radius: 4px; padding: 6px; margin-top: 4px; margin-bottom: 6px; font-size: 8pt; font-weight: 500; line-height: 1.35; box-shadow: 0 1px 2px rgba(0,0,0,0.1); }
        .effect.expanded { font-size: 7.8pt; line-height: 1.3; min-height: 65mm; }
        .red .effect { border-left: 4px solid #b30000; }
        .blue .effect { border-left: 4px solid #0044cc; }
        .green .effect { border-left: 4px solid #008000; }
        .gold .effect { border-left: 4px solid #daa520; }
        .fluff { background: rgba(0,0,0,0.03); padding: 6px; border-radius: 4px; font-family: 'Times New Roman', serif; font-size: 8pt; text-align: center; font-style: italic; color: #333; border: 1px solid rgba(0,0,0,0.05); margin-bottom: 6px; }
        .tactic-title { font-size: 7.5pt; font-weight: bold; color: #444; margin-bottom: 2px; text-transform: uppercase; }
        .tactic { font-size: 8pt; color: #333; line-height: 1.2; }
        .tracker-container { display: flex; align-items: center; margin-top: 2px; }
        .tracker-label { font-size: 7.5pt; font-weight: bold; margin-right: 6px; color: #555; }
        .circle { display: inline-block; width: 10px; height: 10px; border: 1px solid #555; border-radius: 50%; background-color: white; margin-right: 3px; box-shadow: inset 0 1px 2px rgba(0,0,0,0.1); }
        .tracker-row { display: flex; flex-direction: column; margin-bottom: 8px; background: #fff; padding: 6px; border-radius: 4px; border-left: 4px solid #daa520; box-shadow: 0 1px 2px rgba(0,0,0,0.1); }
        .tracker-row .tracker-label { margin-bottom: 4px; font-size: 8pt; color: #222; }
        .circle-row { display: flex; align-items: center; }
        </style>
        </head>
        <body>
        """);

        foreach (var card in deck.Cards)
        {
            sb.Append(BuildCard(card));
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string BuildCard(CardModel card)
    {
        var colorClass = card.Type.ToString().ToLowerInvariant();
        var sb = new StringBuilder();
        sb.Append($"""<div class="card {colorClass}"><div class="header">{Html(card.Name, CardTextLimits.Name)}</div><div class="content">""");

        if (!string.IsNullOrWhiteSpace(card.Cost))
        {
            sb.Append($"""<div class="stat-row"><span class="stat-label">Kosten:</span> {Html(card.Cost, CardTextLimits.Cost)}</div>""");
        }
        if (!string.IsNullOrWhiteSpace(card.Range))
        {
            sb.Append($"""<div class="stat-row"><span class="stat-label">Reichweite:</span> {Html(card.Range, CardTextLimits.Range)}</div>""");
        }

        if (card.Tracker is { Count: > 0 } tracker)
        {
            var circles = string.Concat(Enumerable.Repeat("<div class=\"circle\"></div>", tracker.Count));
            sb.Append($"""<div class="tracker-container"><span class="tracker-label">{Html(tracker.Label, CardTextLimits.TrackerLabel)}</span>{circles}</div>""");
        }

        if (!string.IsNullOrWhiteSpace(card.Effect))
        {
            var isExpanded = card.EffectiveEffectLimit > 300;
            var effectClass = isExpanded ? "effect expanded" : "effect";
            sb.Append($"""<div class="{effectClass}">{Html(card.Effect, card.EffectiveEffectLimit)}</div>""");
        }
        if (!string.IsNullOrWhiteSpace(card.Fluff))
        {
            sb.Append($"""<div class="fluff">"{Html(card.Fluff, CardTextLimits.Fluff)}"</div>""");
        }
        if (!string.IsNullOrWhiteSpace(card.Tactic))
        {
            sb.Append($"""<div class="tactic-title">Wann man es nutzt:</div><div class="tactic">{Html(card.Tactic, CardTextLimits.Tactic)}</div>""");
        }

        if (card.TrackerRows is { Count: > 0 } rows)
        {
            foreach (var row in rows)
            {
                var circles = string.Concat(Enumerable.Repeat("<div class=\"circle\"></div>", row.Count));
                sb.Append($"""<div class="tracker-row"><span class="tracker-label">{Html(row.Label, CardTextLimits.TrackerLabel)}</span><div class="circle-row">{circles}</div></div>""");
            }
        }

        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static string Html(string? value, int? maxLength = null)
    {
        var text = value ?? "";
        if (maxLength is { } max && text.Length > max)
        {
            text = text[..max].TrimEnd() + "…";
        }
        return System.Net.WebUtility.HtmlEncode(text);
    }
}
