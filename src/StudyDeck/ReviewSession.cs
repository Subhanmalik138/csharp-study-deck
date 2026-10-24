namespace StudyDeck;

/// <summary>Pure review-session helpers: which cards are up, and the
/// running summary. The interactive prompting lives in StudyCli so the
/// selection logic stays testable without a terminal.</summary>
public static class ReviewSession
{
    public static List<Card> DueCards(StudyData data, Deck? deck, DateTime now, int? limit = null)
    {
        IEnumerable<Card> cards = data.Cards;
        if (deck is not null) cards = cards.Where(c => c.DeckId == deck.Id);
        var due = Sm2Scheduler.DueCards(cards, now).ToList();
        return limit is null ? due : due.Take(limit.Value).ToList();
    }

    public sealed record Summary(int Reviewed, int Passed, int Failed, double AverageQuality)
    {
        public static Summary From(IReadOnlyList<int> grades) =>
            grades.Count == 0
                ? new Summary(0, 0, 0, 0)
                : new Summary(grades.Count, grades.Count(g => g >= 3), grades.Count(g => g < 3),
                    grades.Average());
    }
}
