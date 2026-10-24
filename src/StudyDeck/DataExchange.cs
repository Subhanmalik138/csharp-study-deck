using System.Text.Json;

namespace StudyDeck;

/// <summary>Deck export/import in the same JSON schema as the data file, so
/// an export is just a portable copy of a library. Import merges by id:
/// existing decks/cards are left untouched, and a card whose deck is
/// missing from both the file and the library is skipped rather than
/// orphaned.</summary>
public static class DataExchange
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void Export(StudyData data, string path) =>
        File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));

    public static (int DecksAdded, int CardsAdded, int Skipped) Import(StudyData data, string path)
    {
        var incoming = JsonSerializer.Deserialize<StudyData>(File.ReadAllText(path), JsonOptions)
                       ?? new StudyData();
        var deckIds = data.Decks.Select(d => d.Id).ToHashSet();
        var cardIds = data.Cards.Select(c => c.Id).ToHashSet();
        var decksAdded = 0;
        var cardsAdded = 0;
        var skipped = 0;

        foreach (var deck in incoming.Decks)
        {
            if (deckIds.Add(deck.Id)) { data.Decks.Add(deck); decksAdded++; }
        }
        foreach (var card in incoming.Cards)
        {
            if (!deckIds.Contains(card.DeckId)) { skipped++; continue; }
            if (cardIds.Add(card.Id)) { data.Cards.Add(card); cardsAdded++; }
        }
        return (decksAdded, cardsAdded, skipped);
    }
}
