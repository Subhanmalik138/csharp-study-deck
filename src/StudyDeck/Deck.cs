namespace StudyDeck;

public sealed class Deck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
