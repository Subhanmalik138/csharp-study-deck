namespace StudyDeck;

/// <summary>A single flashcard. Scheduling state is pure SM-2 data; the
/// scheduler owns every transition, so a card can never be left in an
/// inconsistent state by a caller mutating a field by hand.</summary>
public sealed class Card
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeckId { get; set; }
    public string Front { get; set; } = "";
    public string Back { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // SM-2 state. A brand-new card is due immediately.
    public double EaseFactor { get; set; } = 2.5;
    public int IntervalDays { get; set; } = 0;
    public int Repetitions { get; set; } = 0;
    public DateTime DueUtc { get; set; } = DateTime.UtcNow;
    public int ReviewCount { get; set; } = 0;
    public int LapseCount { get; set; } = 0;

    public bool IsDue(DateTime now) => DueUtc <= now;
    public bool IsNew => ReviewCount == 0;
}
