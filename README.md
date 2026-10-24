# StudyDeck — CLI flashcards with SM-2 spaced repetition

A dependency-free .NET 8 CLI for studying with flashcards that schedule
themselves. Every review updates the card with the classic SM-2 algorithm
(SuperMemo 2), so cards you know drift weeks apart and cards you miss come
back tomorrow. No database, no account, no dependencies: your whole library
is one JSON file you can back up, export, and share.

## Why this exists

Most flashcard tools either keep everything in memory (gone when the
shell closes) or drag in a database and an account for a problem a flat
file solves well. StudyDeck is the middle ground: decks and cards in a
single JSON file, a review loop that fits in a terminal, and scheduling
math that is exactly the published SM-2 algorithm — not a home-grown
approximation.

## Design decisions

1. **Faithful SM-2, with the ease floor.** Quality 0–2 lapses the card
   (streak resets, back tomorrow); quality 3–5 grows the interval 1 day,
   then 6 days, then interval × ease. Ease is clamped at 1.3, as in the
   original algorithm — without the floor, a repeatedly-failed card's
   interval collapses toward zero.
2. **Atomic JSON persistence.** Saves go to a temp file in the same
   directory and are renamed over the original, so a crash mid-save can't
   corrupt the library. A corrupt data file is quarantined to
   `studydeck.json.corrupt-<utc>` instead of crashing the CLI.
3. **Testable by construction.** The CLI writes to injected writers,
   reads from an injected reader, and takes an injected clock; the store
   has an in-memory fake in the tests. No statics, no hidden I/O in the
   command layer. Review sessions save after *every* graded card, so an
   interrupted session keeps its progress.
4. **Import merges by id, never overwrites.** Re-importing the same file
   adds nothing; a card whose deck is missing is skipped rather than
   orphaned. Export is the same JSON schema as the data file, so an
   export is just a portable copy of a library.

## Requirements

- .NET 8 SDK

## How to run

```bash
dotnet build StudyDeck.sln -c Release
dotnet src/StudyDeck/bin/Release/net8.0/StudyDeck.dll --help
```

Data lives in `~/.studydeck/studydeck.json`; override the full file path
with the `STUDYDECK_DATA` environment variable.

## Usage

```bash
# decks
studydeck deck add "Biology" --desc "Cell biology"
studydeck deck list
studydeck deck delete Biology --force

# cards (new cards are due immediately)
studydeck card add Biology "What organelle produces ATP?" "Mitochondria" -t cell,energy
studydeck card list --due
studydeck card list --deck Biology --tag cell
studydeck card show 08d1f5d1
studydeck card delete 08d1f5d1 --force

# review: front shown, Enter reveals the answer, grade yourself 0-5
studydeck review
studydeck review --deck Biology --limit 20

# stats and sharing
studydeck stats
studydeck stats --deck Biology
studydeck export biology.json
studydeck import biology.json
```

Grading scale: 0 = complete blank, 1 = wrong but familiar, 2 = wrong but
the answer rang a bell, 3 = correct with difficulty, 4 = correct with
hesitation, 5 = perfect recall.

## How to run tests

```bash
dotnet test StudyDeck.sln -c Release
```

14 xUnit tests cover the SM-2 transitions (interval growth, lapse reset,
ease floor, invalid grades), due-card ordering, JSON round-trips and
corrupt-file quarantine, idempotent import, stats counts, and the CLI
commands including a full scripted review session.

## Tech stack

- C# / .NET 8, zero NuGet dependencies in the app itself
- xUnit + Microsoft.NET.Test.Sdk for tests
- System.Text.Json for persistence and exchange
