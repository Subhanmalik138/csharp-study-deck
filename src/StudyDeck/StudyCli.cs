namespace StudyDeck;

/// <summary>Command dispatcher. All output goes through injectable writers
/// and the clock is injected, so every command is directly testable
/// without touching the console or the real clock.</summary>
public sealed class StudyCli
{
    private readonly IStudyStore _store;
    private readonly TextWriter _out;
    private readonly TextWriter _err;
    private readonly TextReader _in;
    private readonly Func<DateTime> _nowUtc;

    public StudyCli(IStudyStore store, TextWriter output, TextWriter error, TextReader input, Func<DateTime> nowUtc)
    {
        _store = store;
        _out = output;
        _err = error;
        _in = input;
        _nowUtc = nowUtc;
    }

    public int Run(string[] args)
    {
        var tokens = args.ToList();
        if (tokens.Count == 0)
        {
            PrintUsage();
            return 0;
        }

        var command = tokens[0].ToLowerInvariant();
        var rest = tokens.Skip(1).ToList();
        return command switch
        {
            "deck" => Deck(rest),
            "card" => CardCmd(rest),
            "review" => Review(rest),
            "help" or "--help" or "-h" => Usage(),
            _ => Fail($"Unknown command '{tokens[0]}'. Run 'help' for usage."),
        };
    }

    // ---------- decks ----------

    private int Deck(List<string> args)
    {
        if (args.Count == 0) return Fail("Usage: deck <add|list|delete> ...");
        var sub = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToList();
        return sub switch
        {
            "add" => DeckAdd(rest),
            "list" => DeckList(),
            "delete" => DeckDelete(rest),
            _ => Fail($"Unknown deck command '{args[0]}'."),
        };
    }

    private int DeckAdd(List<string> args)
    {
        var opts = CliOptions.Parse(args);
        if (opts.Positionals.Count == 0)
            return Fail("Usage: deck add \"Name\" [--desc \"...\"]");
        var name = opts.Positionals[0];
        if (_store.Data.Decks.Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Fail($"A deck named '{name}' already exists.");
        var deck = new Deck { Name = name, Description = opts.Get("desc", "d") ?? "" };
        _store.Data.Decks.Add(deck);
        _store.Save();
        _out.WriteLine($"Added deck '{deck.Name}' ({ShortId(deck.Id)}).");
        return 0;
    }

    private int DeckList()
    {
        var decks = _store.Data.Decks;
        if (decks.Count == 0)
        {
            _out.WriteLine("No decks yet. Add one with: deck add \"Name\"");
            return 0;
        }
        foreach (var d in decks)
        {
            var cards = _store.Data.Cards.Count(c => c.DeckId == d.Id);
            var due = _store.Data.Cards.Count(c => c.DeckId == d.Id && c.IsDue(_nowUtc()));
            _out.WriteLine($"{ShortId(d.Id)}  {d.Name} — {cards} cards, {due} due" +
                           (string.IsNullOrWhiteSpace(d.Description) ? "" : $" — {d.Description}"));
        }
        return 0;
    }

    private int DeckDelete(List<string> args)
    {
        var opts = CliOptions.Parse(args);
        if (opts.Positionals.Count == 0) return Fail("Usage: deck delete <name-or-id> --force");
        if (!opts.Has("force")) return Fail("Refusing to delete a deck without --force.");
        var deck = FindDeck(opts.Positionals[0]);
        if (deck is null) return Fail($"No deck '{opts.Positionals[0]}'.");
        _store.Data.Cards.RemoveAll(c => c.DeckId == deck.Id);
        _store.Data.Decks.Remove(deck);
        _store.Save();
        _out.WriteLine($"Deleted deck '{deck.Name}' and its cards.");
        return 0;
    }

    // ---------- cards ----------

    private int CardCmd(List<string> args)
    {
        if (args.Count == 0) return Fail("Usage: card <add|list|show|delete> ...");
        var sub = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToList();
        return sub switch
        {
            "add" => CardAdd(rest),
            "list" => CardList(rest),
            "show" => CardShow(rest),
            "delete" => CardDelete(rest),
            _ => Fail($"Unknown card command '{args[0]}'."),
        };
    }

    private int CardAdd(List<string> args)
    {
        var opts = CliOptions.Parse(args);
        if (opts.Positionals.Count < 3)
            return Fail("Usage: card add <deck> \"front\" \"back\" [-t tag1,tag2]");
        var deck = FindDeck(opts.Positionals[0]);
        if (deck is null) return Fail($"No deck '{opts.Positionals[0]}'.");
        var card = new Card
        {
            DeckId = deck.Id,
            Front = opts.Positionals[1],
            Back = opts.Positionals[2],
            Tags = ParseTags(opts.Get("tags", "t")),
            DueUtc = _nowUtc(), // new cards are due immediately
        };
        _store.Data.Cards.Add(card);
        _store.Save();
        _out.WriteLine($"Added card {ShortId(card.Id)} to '{deck.Name}'.");
        return 0;
    }

    private int CardList(List<string> args)
    {
        var opts = CliOptions.Parse(args);
        var now = _nowUtc();
        IEnumerable<Card> cards = _store.Data.Cards;
        var deckName = opts.Get("deck");
        if (deckName is not null)
        {
            var deck = FindDeck(deckName);
            if (deck is null) return Fail($"No deck '{deckName}'.");
            cards = cards.Where(c => c.DeckId == deck.Id);
        }
        if (opts.Has("due")) cards = cards.Where(c => c.IsDue(now));
        if (opts.Has("new")) cards = cards.Where(c => c.IsNew);
        var tag = opts.Get("tag");
        if (tag is not null)
            cards = cards.Where(c => c.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
        var list = cards.OrderBy(c => c.DueUtc).ToList();
        if (list.Count == 0)
        {
            _out.WriteLine("No cards match.");
            return 0;
        }
        foreach (var c in list)
        {
            var deck = _store.Data.Decks.FirstOrDefault(d => d.Id == c.DeckId);
            var tags = c.Tags.Count == 0 ? "" : $" [{string.Join(",", c.Tags)}]";
            _out.WriteLine($"{ShortId(c.Id)}  {(deck?.Name ?? "?"),-16} due {c.DueUtc:yyyy-MM-dd} " +
                           $"x{c.Repetitions} ease {c.EaseFactor:F2}{tags}  {Truncate(c.Front, 48)}");
        }
        return 0;
    }

    private int CardShow(List<string> args)
    {
        if (!TryGetCard(args, out var card)) return 1;
        var deck = _store.Data.Decks.FirstOrDefault(d => d.Id == card.DeckId);
        _out.WriteLine($"Card {ShortId(card.Id)}  (deck: {deck?.Name ?? "?"})");
        _out.WriteLine($"  Front: {card.Front}");
        _out.WriteLine($"  Back:  {card.Back}");
        _out.WriteLine($"  Tags:  {(card.Tags.Count == 0 ? "-" : string.Join(", ", card.Tags))}");
        _out.WriteLine($"  Reviews: {card.ReviewCount}  Repetitions: {card.Repetitions}  " +
                       $"Interval: {card.IntervalDays}d  Ease: {card.EaseFactor:F2}  Lapses: {card.LapseCount}");
        _out.WriteLine($"  Due: {card.DueUtc:yyyy-MM-dd HH:mm} UTC  Created: {card.CreatedUtc:yyyy-MM-dd}");
        return 0;
    }

    private int CardDelete(List<string> args)
    {
        var opts = CliOptions.Parse(args);
        if (opts.Positionals.Count == 0) return Fail("Usage: card delete <id> --force");
        if (!opts.Has("force")) return Fail("Refusing to delete a card without --force.");
        if (!TryGetCard(args, out var card)) return 1;
        _store.Data.Cards.Remove(card);
        _store.Save();
        _out.WriteLine($"Deleted card {ShortId(card.Id)}.");
        return 0;
    }

    // ---------- review ----------

    private int Review(List<string> args)
    {
        var opts = CliOptions.Parse(args);
        Deck? deck = null;
        var deckName = opts.Get("deck");
        if (deckName is not null)
        {
            deck = FindDeck(deckName);
            if (deck is null) return Fail($"No deck '{deckName}'.");
        }
        int? limit = null;
        var limitRaw = opts.Get("limit");
        if (limitRaw is not null && (!int.TryParse(limitRaw, out var l) || l <= 0))
            return Fail("--limit must be a positive integer.");
        if (limitRaw is not null) limit = int.Parse(limitRaw);

        var now = _nowUtc();
        var due = ReviewSession.DueCards(_store.Data, deck, now, limit);
        if (due.Count == 0)
        {
            _out.WriteLine("Nothing due. Enjoy the break — new cards become due as their intervals elapse.");
            return 0;
        }

        _out.WriteLine($"Reviewing {due.Count} card(s) due.");
        var grades = new List<int>();
        foreach (var card in due)
        {
            var deckName2 = _store.Data.Decks.FirstOrDefault(d => d.Id == card.DeckId)?.Name ?? "?";
            _out.WriteLine();
            _out.WriteLine($"[{deckName2}] {card.Front}");
            _out.Write("Press Enter to reveal the answer... ");
            if (_in.ReadLine() is null) break; // EOF: keep progress saved so far
            _out.WriteLine($"  Answer: {card.Back}");
            int grade;
            while (true)
            {
                _out.Write("Grade yourself 0-5 (0 = blank, 5 = perfect): ");
                var line = _in.ReadLine();
                if (line is null) goto Done; // EOF mid-card: card stays ungraded
                if (int.TryParse(line.Trim(), out grade) && grade >= 0 && grade <= 5) break;
                _out.WriteLine("Please enter a number from 0 to 5.");
            }
            Sm2Scheduler.Review(card, grade, _nowUtc());
            grades.Add(grade);
            _store.Save(); // save after every card, so an interrupted session keeps its progress
            _out.WriteLine(grade >= 3
                ? $"  ✓ Next review in {card.IntervalDays} day(s) ({card.DueUtc:yyyy-MM-dd})."
                : $"  ✗ Lapsed — back tomorrow ({card.DueUtc:yyyy-MM-dd}).");
        }
    Done:
        var summary = ReviewSession.Summary.From(grades);
        _out.WriteLine();
        _out.WriteLine($"Session done: {summary.Reviewed} reviewed, {summary.Passed} passed, " +
                       $"{summary.Failed} lapsed, average grade {summary.AverageQuality:F1}.");
        return 0;
    }

    // ---------- helpers ----------

    internal Deck? FindDeck(string nameOrId) =>
        _store.Data.Decks.FirstOrDefault(d =>
            d.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) ||
            d.Id.ToString().StartsWith(nameOrId, StringComparison.OrdinalIgnoreCase));

    internal bool TryGetCard(List<string> args, out Card card)
    {
        card = null!;
        var opts = CliOptions.Parse(args);
        if (opts.Positionals.Count == 0)
        {
            Fail("Usage: card <command> <id>");
            return false;
        }
        var id = opts.Positionals[0];
        var matches = _store.Data.Cards
            .Where(c => c.Id.ToString().StartsWith(id, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0)
        {
            Fail($"No card '{id}'.");
            return false;
        }
        if (matches.Count > 1)
        {
            Fail($"Ambiguous card id '{id}' — use more characters.");
            return false;
        }
        card = matches[0];
        return true;
    }

    internal static string ShortId(Guid id) => id.ToString()[..8];

    internal static List<string> ParseTags(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? new List<string>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    internal static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";

    private int Usage()
    {
        PrintUsage();
        return 0;
    }

    private int Fail(string message)
    {
        _err.WriteLine($"Error: {message}");
        return 1;
    }

    private void PrintUsage()
    {
        _out.WriteLine("""
            StudyDeck — CLI flashcards with SM-2 spaced repetition.

            Usage: studydeck <command> [options]

              deck add "Name" [--desc "..."]
              deck list
              deck delete <name-or-id> --force
              card add <deck> "front" "back" [-t tag1,tag2]
              card list [--deck name] [--due] [--new] [--tag x]
              card show <id>
              card delete <id> --force
              review [--deck name] [--limit N]
              help
            """);
    }
}
