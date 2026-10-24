namespace StudyDeck;

/// <summary>Classic SM-2 (SuperMemo 2) scheduling.
/// Quality is the 0-5 self-grade from a review: 0-2 is a failure (the card
/// lapses and restarts), 3-5 is a pass with increasing intervals.
/// The ease floor of 1.3 is part of the original algorithm, not a tweak:
/// without it a repeatedly-failed card's interval collapses to zero.</summary>
public static class Sm2Scheduler
{
    public static Card Review(Card card, int quality, DateTime now)
    {
        if (quality < 0 || quality > 5)
            throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be 0-5.");
        if (card is null) throw new ArgumentNullException(nameof(card));

        card.ReviewCount++;

        if (quality < 3)
        {
            card.Repetitions = 0;
            card.IntervalDays = 1;
            card.LapseCount++;
            // Ease is untouched on a lapse in SM-2; only the streak resets.
        }
        else
        {
            card.Repetitions++;
            card.IntervalDays = card.Repetitions switch
            {
                1 => 1,
                2 => 6,
                _ => Math.Max(1, (int)Math.Round(card.IntervalDays * card.EaseFactor)),
            };
            card.EaseFactor = Math.Max(1.3,
                card.EaseFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));
        }

        card.DueUtc = now.AddDays(card.IntervalDays);
        return card;
    }

    /// <summary>Cards due for review, most overdue first; new cards last
    /// within the same due bucket so re-studying never starves reviews.</summary>
    public static IEnumerable<Card> DueCards(IEnumerable<Card> cards, DateTime now) =>
        cards.Where(c => c.IsDue(now))
             .OrderBy(c => c.DueUtc)
             .ThenByDescending(c => c.IsNew ? 0 : 1);
}
