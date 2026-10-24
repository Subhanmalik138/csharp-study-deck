namespace StudyDeck;

/// <summary>The whole data file: decks and cards in one document, so a
/// backup or export is a single file copy.</summary>
public sealed class StudyData
{
    public List<Deck> Decks { get; set; } = new();
    public List<Card> Cards { get; set; } = new();
}
