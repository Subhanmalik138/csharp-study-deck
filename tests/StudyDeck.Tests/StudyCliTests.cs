using Xunit;

namespace StudyDeck.Tests;

public sealed class StudyCliTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    private static (StudyCli Cli, StringWriter Out, StringWriter Err, InMemoryStudyStore Store) MakeCli(string input = "")
    {
        var store = new InMemoryStudyStore();
        var output = new StringWriter();
        var error = new StringWriter();
        var cli = new StudyCli(store, output, error, new StringReader(input), () => Now);
        return (cli, output, error, store);
    }

    [Fact]
    public void DeckAddThenDuplicateFails()
    {
        var (cli, _, err, store) = MakeCli();
        Assert.Equal(0, cli.Run(new[] { "deck", "add", "Bio" }));
        Assert.Single(store.Data.Decks);
        Assert.Equal(1, cli.Run(new[] { "deck", "add", "Bio" }));
        Assert.Contains("already exists", err.ToString());
    }

    [Fact]
    public void CardAddListAndShowRoundTrip()
    {
        var (cli, output, _, store) = MakeCli();
        cli.Run(new[] { "deck", "add", "Bio" });
        Assert.Equal(0, cli.Run(new[] { "card", "add", "Bio", "What is ATP?", "Cellular energy", "-t", "cell" }));
        Assert.Single(store.Data.Cards);

        output.GetStringBuilder().Clear();
        Assert.Equal(0, cli.Run(new[] { "card", "list", "--due" }));
        Assert.Contains("What is ATP?", output.ToString());

        var id = StudyCli.ShortId(store.Data.Cards[0].Id);
        output.GetStringBuilder().Clear();
        Assert.Equal(0, cli.Run(new[] { "card", "show", id }));
        Assert.Contains("Cellular energy", output.ToString());
    }

    [Fact]
    public void ReviewGradesACardAndReschedulesIt()
    {
        // One Enter to reveal, then grade 5.
        var (cli, output, _, store) = MakeCli("\n5\n");
        cli.Run(new[] { "deck", "add", "Bio" });
        cli.Run(new[] { "card", "add", "Bio", "Q?", "A." });
        Assert.Equal(0, cli.Run(new[] { "review" }));

        var card = store.Data.Cards[0];
        Assert.Equal(1, card.Repetitions);
        Assert.Equal(1, card.IntervalDays);
        Assert.Equal(Now.AddDays(1), card.DueUtc);
        Assert.Contains("1 reviewed, 1 passed", output.ToString());
        Assert.True(store.SaveCount >= 1); // review saves after every graded card
    }

    [Fact]
    public void ReviewWithNothingDueReportsCleanly()
    {
        var (cli, output, _, _) = MakeCli();
        cli.Run(new[] { "deck", "add", "Bio" });
        Assert.Equal(0, cli.Run(new[] { "review" }));
        Assert.Contains("Nothing due", output.ToString());
    }

    [Fact]
    public void StatsCommandPrintsTotals()
    {
        var (cli, output, _, _) = MakeCli();
        cli.Run(new[] { "deck", "add", "Bio" });
        cli.Run(new[] { "card", "add", "Bio", "Q?", "A." });
        Assert.Equal(0, cli.Run(new[] { "stats" }));
        Assert.Contains("Cards: 1", output.ToString());
        Assert.Contains("Due now: 1", output.ToString());
    }
}
