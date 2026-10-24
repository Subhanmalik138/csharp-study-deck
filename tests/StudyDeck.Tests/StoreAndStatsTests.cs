using Xunit;

namespace StudyDeck.Tests;

public sealed class StoreAndStatsTests
{
    [Fact]
    public void JsonStoreRoundTripsDecksAndCards()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "studydeck.json");
        var store = new JsonStudyStore(path);
        var deck = new Deck { Name = "History" };
        store.Data.Decks.Add(deck);
        store.Data.Cards.Add(new Card { DeckId = deck.Id, Front = "Q", Back = "A" });
        store.Save();

        var reloaded = new JsonStudyStore(path);
        Assert.Single(reloaded.Data.Decks);
        Assert.Equal("History", reloaded.Data.Decks[0].Name);
        Assert.Single(reloaded.Data.Cards);
        Assert.Equal("Q", reloaded.Data.Cards[0].Front);
        Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void CorruptFileIsQuarantinedNotDeletedAndLoadsEmpty()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "studydeck.json");
        File.WriteAllText(path, "{ not json");
        var store = new JsonStudyStore(path);
        Assert.Empty(store.Data.Decks);
        Assert.Single(Directory.GetFiles(dir, "*.corrupt-*"));
        Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void ImportIsIdempotentAndSkipsOrphanedCards()
    {
        var data = new StudyData();
        var deck = new Deck { Name = "Bio" };
        var incoming = new StudyData();
        incoming.Decks.Add(deck);
        incoming.Cards.Add(new Card { DeckId = deck.Id, Front = "Q", Back = "A" });
        incoming.Cards.Add(new Card { DeckId = Guid.NewGuid(), Front = "Orphan", Back = "?" });

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        DataExchange.Export(incoming, path);
        var first = DataExchange.Import(data, path);
        Assert.Equal((1, 1, 1), first);
        var second = DataExchange.Import(data, path);
        Assert.Equal((0, 0, 1), second); // nothing new, orphan still skipped
        File.Delete(path);
    }

    [Fact]
    public void StatsCountDueNewAndForecast()
    {
        var now = new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);
        var deck = new Deck { Name = "Bio" };
        var data = new StudyData();
        data.Decks.Add(deck);
        data.Cards.Add(new Card { DeckId = deck.Id, DueUtc = now.AddDays(-1) });
        data.Cards.Add(new Card { DeckId = deck.Id, DueUtc = now.AddDays(3), ReviewCount = 2, LapseCount = 1 });
        data.Cards.Add(new Card { DeckId = deck.Id, DueUtc = now.AddDays(30) });

        var stats = Stats.Compute(data, now);
        Assert.Equal(3, stats.Cards);
        Assert.Equal(1, stats.Due);
        Assert.Equal(2, stats.New); // cards 1 and 3 have never been reviewed
        Assert.Equal(1, stats.DueTomorrow);
        Assert.Equal(2, stats.DueIn7Days);
        Assert.Equal(2, stats.Reviews);
        Assert.Equal(1, stats.Lapses);
    }
}
