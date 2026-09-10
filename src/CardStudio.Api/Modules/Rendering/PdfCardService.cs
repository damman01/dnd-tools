using CardStudio.Shared.Models;
using PeachPDF;
using PeachPDF.Network;

namespace CardStudio.Api.Modules.Rendering;

public class PdfCardService(CardHtmlBuilder htmlBuilder)
{
    public async Task<byte[]> GeneratePdfAsync(CardDeck deck)
    {
        var html = htmlBuilder.BuildHtml(deck);

        var config = new PdfGenerateConfig
        {
            PageSize = PageSize.Letter // overridden per-card via @page in the HTML
        };

        var generator = new PdfGenerator();
        var document = await generator.GeneratePdf(html, config);

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }
}
