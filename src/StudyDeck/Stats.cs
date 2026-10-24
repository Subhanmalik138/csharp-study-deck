namespace StudyDeck;

/// <summary>Read-only study statistics computed from card state. No review
/// history table is kept, so retention is derived from what cards do
/// record: total reviews and total lapses.</summary>
public static class Stats
{
    public sealed record DeckStats(string Name, int Cards, int Due, int New, double AverageEase, int Reviews, int Lapses);
    public sealed record Overall(int Decks, int Cards, int Due, int New, int DueTomorrow, int DueIn7Days, double AverageEase, int Reviews, int Lapses, List<DeckStats> PerDeck);

    public static Overall Compute(StudyData data, DateTime now)
    {
        var cards = data.Cards;
        var perDeck = data.Decks.Select(d =>
        {
            var dc = cards.Where(c => c.DeckId == d.Id).ToList();
            return new DeckStats(d.Name, dc.Count, dc.Count(c => c.IsDue(now)), dc.Count(c => c.IsNew),
                dc.Count == 0 ? 0 : dc.Average(c => c.EaseFactor),
                dc.Sum(c => c.ReviewCount), dc.Sum(c => c.LapseCount));
        }).ToList();

        return new Overall(
            data.Decks.Count, cards.Count, cards.Count(c => c.IsDue(now)), cards.Count(c => c.IsNew),
            cards.Count(c => c.DueUtc <= now.AddDays(1)), cards.Count(c => c.DueUtc <= now.AddDays(7)),
            cards.Count == 0 ? 0 : cards.Average(c => c.EaseFactor),
            cards.Sum(c => c.ReviewCount), cards.Sum(c => c.LapseCount), perDeck);
    }
}
