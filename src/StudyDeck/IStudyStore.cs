namespace StudyDeck;

public interface IStudyStore
{
    StudyData Data { get; }
    void Save();
}

/// <summary>An in-memory store for tests: no file I/O, Save() is a no-op
/// that just records it was called.</summary>
public sealed class InMemoryStudyStore : IStudyStore
{
    public StudyData Data { get; } = new();
    public int SaveCount { get; private set; }
    public void Save() => SaveCount++;
}
