using Xunit;

namespace StudyDeck.Tests;

public sealed class Sm2SchedulerTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PerfectGradesGrowIntervalsOneSixThenScaled()
    {
        var card = new Card { DueUtc = Now };
        Sm2Scheduler.Review(card, 5, Now);
        Assert.Equal(1, card.IntervalDays);
        Assert.Equal(2.6, card.EaseFactor, precision: 5);

        Sm2Scheduler.Review(card, 5, Now);
        Assert.Equal(6, card.IntervalDays);
        Assert.Equal(2.7, card.EaseFactor, precision: 5);

        Sm2Scheduler.Review(card, 5, Now);
        Assert.Equal(16, card.IntervalDays); // round(6 * 2.7)
        Assert.Equal(Now.AddDays(16), card.DueUtc);
    }

    [Fact]
    public void FailingGradeResetsStreakAndCountsALapse()
    {
        var card = new Card { DueUtc = Now, EaseFactor = 2.6, Repetitions = 3, IntervalDays = 16 };
        Sm2Scheduler.Review(card, 2, Now);
        Assert.Equal(0, card.Repetitions);
        Assert.Equal(1, card.IntervalDays);
        Assert.Equal(1, card.LapseCount);
        Assert.Equal(2.6, card.EaseFactor, precision: 5); // ease untouched on a lapse
    }

    [Fact]
    public void EaseFactorNeverDropsBelowFloor()
    {
        var card = new Card { DueUtc = Now, EaseFactor = 1.31 };
        Sm2Scheduler.Review(card, 3, Now); // would drop ease by 0.14
        Assert.Equal(1.3, card.EaseFactor, precision: 5);
    }

    [Fact]
    public void QualityOutsideZeroToFiveThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Sm2Scheduler.Review(new Card(), -1, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => Sm2Scheduler.Review(new Card(), 6, Now));
    }

    [Fact]
    public void DueCardsComeMostOverdueFirst()
    {
        var cards = new[]
        {
            new Card { DueUtc = Now.AddDays(-1) },
            new Card { DueUtc = Now.AddDays(5) }, // not due
            new Card { DueUtc = Now.AddDays(-5) },
        };
        var due = Sm2Scheduler.DueCards(cards, Now).ToList();
        Assert.Equal(2, due.Count);
        Assert.Equal(Now.AddDays(-5), due[0].DueUtc);
        Assert.Equal(Now.AddDays(-1), due[1].DueUtc);
    }
}
