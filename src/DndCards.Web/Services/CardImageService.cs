using System.IO.Compression;
using DndCards.Web.Models;
using PDFtoImage;
using SkiaSharp;

namespace DndCards.Web.Services;

// Rasterizes cards to PNG via the already-generated PDF (PDFium under the hood).
// PDFium is not thread-safe, so only one render runs at a time per process.
public class CardImageService(PdfCardService pdfCardService)
{
    private static readonly SemaphoreSlim RenderLock = new(1, 1);

    // Single-card decks download as one PNG; multi-card decks as a ZIP of PNGs.
    public async Task<(byte[] Bytes, string FileName, string ContentType)> GeneratePngAsync(CardDeck deck)
    {
        var pdfBytes = await pdfCardService.GeneratePdfAsync(deck);

        await RenderLock.WaitAsync();
        try
        {
            var pngPages = new List<byte[]>();
            for (var i = 0; i < deck.Cards.Count; i++)
            {
                // PDFtoImage's platform attributes cover Windows/Linux/macOS, all of which this app targets.
#pragma warning disable CA1416
                using var bitmap = Conversion.ToImage(pdfBytes, page: i);
#pragma warning restore CA1416
                using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                pngPages.Add(encoded.ToArray());
            }

            if (pngPages.Count == 1)
            {
                return (pngPages[0], $"{SanitizeFileName(deck.Title)}.png", "image/png");
            }

            using var zipStream = new MemoryStream();
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (var i = 0; i < pngPages.Count; i++)
                {
                    var cardName = SanitizeFileName(deck.Cards[i].Name);
                    var entry = zip.CreateEntry($"{i + 1:00}_{cardName}.png", CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await entryStream.WriteAsync(pngPages[i]);
                }
            }

            return (zipStream.ToArray(), $"{SanitizeFileName(deck.Title)}.zip", "application/zip");
        }
        finally
        {
            RenderLock.Release();
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "karten" : cleaned;
    }
}
