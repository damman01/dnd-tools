using System.Net.Http.Json;
using CardStudio.Shared.Contracts;
using CardStudio.Shared.Models;

namespace CardStudio.Web.Services;

public interface ICardApiClient
{
    Task<(byte[] Bytes, string FileName)> RenderPdfAsync(CardDeck deck, string clientId, CancellationToken cancellationToken = default);
    Task<(byte[] Bytes, string FileName, string ContentType)> RenderPngAsync(CardDeck deck, string clientId, CancellationToken cancellationToken = default);
    Task<CardModel> TranslateCardAsync(CardModel card, string targetLanguage, CancellationToken cancellationToken = default);
    Task<CardDeck> TranslateDeckAsync(CardDeck deck, string targetLanguage, CancellationToken cancellationToken = default);
    Task<CardDeck> ConvertUnitsAsync(CardDeck deck, string unitSystem, CancellationToken cancellationToken = default);
    Task<CardDeck> ImportDndBeyondAsync(string rawJson, string unitSystem, CancellationToken cancellationToken = default);
    Task<UsageQuotaDto> GetQuotaAsync(string clientId, CancellationToken cancellationToken = default);
}

public class CardApiClient(HttpClient httpClient) : ICardApiClient
{
    public async Task<(byte[] Bytes, string FileName)> RenderPdfAsync(CardDeck deck, string clientId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/render/pdf")
        {
            Content = JsonContent.Create(new RenderPdfRequest { Deck = deck })
        };
        request.Headers.Add("X-Client-Id", clientId);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"PDF-Erstellung fehlgeschlagen: {response.StatusCode} - {problem}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"{deck.Title}.pdf";
        return (bytes, fileName);
    }

    public async Task<(byte[] Bytes, string FileName, string ContentType)> RenderPngAsync(CardDeck deck, string clientId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/render/png")
        {
            Content = JsonContent.Create(new RenderPngRequest { Deck = deck })
        };
        request.Headers.Add("X-Client-Id", clientId);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"PNG-Erstellung fehlgeschlagen: {response.StatusCode} - {problem}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"{deck.Title}.png";
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
        return (bytes, fileName, contentType);
    }

    public async Task<CardModel> TranslateCardAsync(CardModel card, string targetLanguage, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/v1/translate/card", new TranslateCardRequest
        {
            Card = card,
            TargetLanguage = targetLanguage
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardModel>(cancellationToken: cancellationToken))!;
    }

    public async Task<CardDeck> TranslateDeckAsync(CardDeck deck, string targetLanguage, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/v1/translate/deck", new TranslateDeckRequest
        {
            Deck = deck,
            TargetLanguage = targetLanguage
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardDeck>(cancellationToken: cancellationToken))!;
    }

    public async Task<CardDeck> ConvertUnitsAsync(CardDeck deck, string unitSystem, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/v1/translate/convert-units", new ConvertUnitsRequest
        {
            Deck = deck,
            UnitSystem = unitSystem
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardDeck>(cancellationToken: cancellationToken))!;
    }

    public async Task<CardDeck> ImportDndBeyondAsync(string rawJson, string unitSystem, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/v1/import/dndbeyond?units={Uri.EscapeDataString(unitSystem)}", rawJson, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardDeck>(cancellationToken: cancellationToken))!;
    }

    public async Task<UsageQuotaDto> GetQuotaAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/billing/quota");
        request.Headers.Add("X-Client-Id", clientId);

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UsageQuotaDto>(cancellationToken: cancellationToken))!;
    }
}
