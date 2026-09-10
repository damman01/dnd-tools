using CardStudio.Api.Modules.Import;
using CardStudio.Api.Modules.Monetization;
using CardStudio.Api.Modules.Rendering;
using CardStudio.Api.Modules.Translation;
using CardStudio.Shared.Contracts;
using CardStudio.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace CardStudio.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCardStudioEndpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/api/v1");

        // --- Rendering Endpoints ---
        var renderGroup = v1.MapGroup("/render").WithTags("Rendering");

        renderGroup.MapPost("/pdf", async (
            [FromBody] RenderPdfRequest request,
            [FromHeader(Name = "X-Client-Id")] string? clientId,
            PdfCardService pdfService,
            IQuotaService quotaService) =>
        {
            var cid = clientId ?? "anonymous";
            var maxCards = quotaService.GetMaxCardsForClient(cid);
            if (request.Deck.Cards.Count > maxCards)
            {
                return Results.Problem(
                    detail: $"Die Kartenzahl ({request.Deck.Cards.Count}) überschreitet das Limit von {maxCards}.",
                    statusCode: StatusCodes.Status403Forbidden);
            }

            if (!quotaService.TryConsumeGeneration(cid, out _))
            {
                return Results.Problem(
                    detail: "Das tägliche Kontingent für kostenlose Erstellungen ist erreicht.",
                    statusCode: StatusCodes.Status429TooManyRequests);
            }

            var bytes = await pdfService.GeneratePdfAsync(request.Deck);
            return Results.File(bytes, "application/pdf", $"{request.Deck.Title}.pdf");
        })
        .WithName("RenderPdf")
        .WithSummary("Generiert ein druckfertiges PDF (70x120mm Karten)")
        .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        renderGroup.MapPost("/png", async (
            [FromBody] RenderPngRequest request,
            [FromHeader(Name = "X-Client-Id")] string? clientId,
            CardImageService imageService,
            IQuotaService quotaService) =>
        {
            var cid = clientId ?? "anonymous";
            var maxCards = quotaService.GetMaxCardsForClient(cid);
            if (request.Deck.Cards.Count > maxCards)
            {
                return Results.Problem(
                    detail: $"Die Kartenzahl ({request.Deck.Cards.Count}) überschreitet das Limit von {maxCards}.",
                    statusCode: StatusCodes.Status403Forbidden);
            }

            if (!quotaService.TryConsumeGeneration(cid, out _))
            {
                return Results.Problem(
                    detail: "Das tägliche Kontingent für kostenlose Erstellungen ist erreicht.",
                    statusCode: StatusCodes.Status429TooManyRequests);
            }

            var (bytes, fileName, contentType) = await imageService.GeneratePngAsync(request.Deck);
            return Results.File(bytes, contentType, fileName);
        })
        .WithName("RenderPng")
        .WithSummary("Rendert Karten als PNG oder ZIP-Archiv von PNGs")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        // --- Translation & Units Endpoints ---
        var translationGroup = v1.MapGroup("/translate").WithTags("Translation");

        translationGroup.MapPost("/card", async (
            [FromBody] TranslateCardRequest request,
            ITranslationProvider translationProvider,
            CancellationToken ct) =>
        {
            var translated = await translationProvider.TranslateCardAsync(request.Card, request.TargetLanguage, ct);
            return Results.Ok(translated);
        })
        .WithName("TranslateCard")
        .WithSummary("Übersetzt eine einzelne Karte (inkl. D3 API Abgleich)")
        .Produces<CardModel>(StatusCodes.Status200OK);

        translationGroup.MapPost("/deck", async (
            [FromBody] TranslateDeckRequest request,
            ITranslationProvider translationProvider,
            CancellationToken ct) =>
        {
            var translated = await translationProvider.TranslateDeckAsync(request.Deck, request.TargetLanguage, ct);
            return Results.Ok(translated);
        })
        .WithName("TranslateDeck")
        .WithSummary("Übersetzt ein gesamtes Kartendeck")
        .Produces<CardDeck>(StatusCodes.Status200OK);

        translationGroup.MapPost("/convert-units", (
            [FromBody] ConvertUnitsRequest request) =>
        {
            var unitSystem = Enum.TryParse<UnitSystem>(request.UnitSystem, true, out var parsed) ? parsed : UnitSystem.Metric;
            D3TranslationProvider.ConvertDeckUnits(request.Deck, unitSystem);
            return Results.Ok(request.Deck);
        })
        .WithName("ConvertUnits")
        .WithSummary("Konvertiert Maßeinheiten aller Karten im Deck (m, ft oder both)")
        .Produces<CardDeck>(StatusCodes.Status200OK);

        // --- Import Endpoints ---
        var importGroup = v1.MapGroup("/import").WithTags("Import");

        importGroup.MapPost("/dndbeyond", (
            [FromBody] string rawJson,
            [FromQuery] string? units,
            DndBeyondImportService importService) =>
        {
            var unitSystem = Enum.TryParse<UnitSystem>(units, true, out var parsed) ? parsed : UnitSystem.Metric;
            try
            {
                var deck = importService.Import(rawJson, unitSystem);
                return Results.Ok(deck);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        })
        .WithName("ImportDndBeyond")
        .WithSummary("Importiert ein D&D Beyond Character-Service v5 JSON")
        .Produces<CardDeck>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // --- Quota / Monetization Endpoints ---
        var billingGroup = v1.MapGroup("/billing").WithTags("Monetization");

        billingGroup.MapGet("/quota", (
            [FromHeader(Name = "X-Client-Id")] string? clientId,
            IQuotaService quotaService) =>
        {
            var cid = clientId ?? "anonymous";
            var maxCards = quotaService.GetMaxCardsForClient(cid);
            var isPremium = quotaService.IsPremium(cid);

            return Results.Ok(new UsageQuotaDto
            {
                MaxCardsPerDeck = maxCards,
                RemainingGenerationsToday = isPremium ? int.MaxValue : 5,
                IsPremium = isPremium
            });
        })
        .WithName("GetQuota")
        .WithSummary("Gibt das aktuelle Kontingent für den Client zurück")
        .Produces<UsageQuotaDto>(StatusCodes.Status200OK);

        return app;
    }
}
