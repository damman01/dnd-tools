using CardStudio.Shared.Models;

namespace CardStudio.Api.Modules.Translation;

public interface ITranslationProvider
{
    string ProviderName { get; }
    Task<CardModel> TranslateCardAsync(CardModel card, string targetLanguage, CancellationToken cancellationToken = default);
    Task<CardDeck> TranslateDeckAsync(CardDeck deck, string targetLanguage, CancellationToken cancellationToken = default);
}
