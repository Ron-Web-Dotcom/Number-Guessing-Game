# Number Guessing Game

![Tests](https://github.com/Ron-Web-Dotcom/Number-Guessing-Game/actions/workflows/tests.yml/badge.svg)

A feature-complete C# console number guessing game.

## Game modes

| # | Mode | Description |
|---|------|-------------|
| 1 | **You guess** | Computer picks a number. You get hot/cold proximity + a Claude AI hint after each wrong guess. Supports 1–4 players competitively. Supports tournament mode (best of 3/5/7). |
| 2 | **Computer guesses** | You think of a number; the computer uses binary search to find it (≤ 7 guesses on Hard). |
| 3 | **Daily challenge** | Date-seeded number — same for everyone that day. One attempt per day per difficulty. |

## Difficulty levels

| Level  | Range  | Guess limit |
|--------|--------|-------------|
| Easy   | 1–10   | Unlimited   |
| Medium | 1–30   | Unlimited   |
| Hard   | 1–100  | 7           |
| Custom | You choose | You choose |

## Features

- **Hot/cold proximity** — *BURNING HOT / Hot! / Warm / Cold / Freezing cold* based on distance as % of range.
- **Claude AI hints** — context-aware hint after each wrong guess (proximity + remaining guesses in prompt).
- **Guess history** — compact recap of every guess + proximity shown at game end.
- **Stopwatch** — each game is timed; shown on win/loss and drives multiplayer rankings.
- **Tournament mode** — 2–4 players, best of 3/5/7 rounds; standings updated after each round.
- **Quit mid-game** — type `quit` at any guess prompt to exit cleanly without losing a streak.
- **Achievement system** — 8 unlockable achievements, earned and shown in stats:
  - First Blood, One Shot, Speed Demon, Hot Streak, Comeback Kid, Daily Player, Hard Boiled, Veteran
- **Custom difficulty** — set your own min, max, and guess limit.
- **Win streaks** — current, session best, and all-time best streak per difficulty.
- **Top-5 leaderboard** — best 5 scores per difficulty, persisted.
- **Average score** — historical average attempts per difficulty.
- **Color-coded output** — cyan menus, green wins, red losses, yellow hints, magenta stats, gold achievements.

## Requirements

- [.NET Framework 4.7.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472) or Visual Studio 2017+

## Running the game

Open `Number Guessing.sln` in Visual Studio and press **F5**, or build from the command line:

```
msbuild "Number Guessing.sln" /p:Configuration=Release
"bin\Release\Number Guessing.exe"
```

## Running the tests

```
dotnet test NumberGuessingTests/NumberGuessingTests.csproj
```

Tests run automatically via GitHub Actions on every push and pull request — see the badge above.

## AI hints (optional)

Set `ANTHROPIC_API_KEY` before running. Falls back to plain directional hints if the key is absent.

**Windows (Command Prompt)**
```
set ANTHROPIC_API_KEY=your-key-here
```

**Windows (PowerShell)**
```
$env:ANTHROPIC_API_KEY="your-key-here"
```

## Score file (`scores.txt`)

```
achievements=first_blood,one_shot
Easy=3,5,7,8,10
Easy_streak=4
Easy_avg=45,8
Easy_2026-05-29=4
```

## Game modes

| # | Mode | Description |
|---|------|-------------|
| 1 | **You guess** | The computer picks a random number. You get proximity feedback and a Claude AI hint after each wrong guess. Supports 1–4 players competitively (same target, players take turns). |
| 2 | **Computer guesses** | You think of a number; the computer uses binary search to find it (guaranteed in ≤ 7 guesses on Hard). |
| 3 | **Daily challenge** | A date-seeded number that's the same for everyone that day. One attempt per day per difficulty — come back tomorrow for a new one. |

## Difficulty levels

| Level  | Range  | Guess limit |
|--------|--------|-------------|
| Easy   | 1–10   | Unlimited   |
| Medium | 1–30   | Unlimited   |
| Hard   | 1–100  | 7           |

## Features

- **Hot/cold proximity** — Every wrong guess shows *BURNING HOT / Hot! / Warm / Cold / Freezing cold* based on how close you are as a percentage of the range.
- **Stopwatch** — Each game session is timed; time is shown on win/loss and in multiplayer rankings.
- **Multiplayer** — 1–4 players take turns on the same machine guessing the same target. Results are ranked by attempts then time.
- **Average score** — Tracks total attempts across all wins per difficulty and displays your historical average.
- **Win streaks** — Current streak, session best, and all-time best streak per difficulty.
- **Top-5 leaderboard** — Best 5 scores per difficulty, persisted between sessions.
- **Color-coded output** — Cyan for menus, green for wins, red for losses, yellow for hints, magenta for stats.

## Requirements

- [.NET Framework 4.7.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472) or Visual Studio 2017+

## Running the game

Open `Number Guessing.sln` in Visual Studio and press **F5**, or build from the command line:

```
msbuild "Number Guessing.sln" /p:Configuration=Release
"bin\Release\Number Guessing.exe"
```

## Running the tests

```
dotnet test NumberGuessingTests/NumberGuessingTests.csproj
```

The test project targets `.NET 8` and compiles `GameLogic.cs` directly (no exe-reference needed). Tests cover `GetProximity`, `BuildFallbackHint`, `GetDailyNumber`, `BinarySearchMid`, and `ComputeAverage`.

Tests run automatically via GitHub Actions on every push and pull request — see the badge at the top of this file.

## AI hints (optional)

Set `ANTHROPIC_API_KEY` before running to enable Claude-powered hints. The hint prompt includes proximity level and remaining guess count for context-aware responses.

**Windows (Command Prompt)**
```
set ANTHROPIC_API_KEY=your-key-here
```

**Windows (PowerShell)**
```
$env:ANTHROPIC_API_KEY="your-key-here"
```

The game works without a key — proximity + directional fallback hints are always shown.

## Score file (`scores.txt`)

Written next to the executable. Format:

```
Easy=3,5,7,8,10              (top-5 scores)
Easy_streak=4                (all-time best streak)
Easy_avg=45,8                (totalAttempts,totalWins for average)
Easy_2026-05-29=4            (daily challenge result)
```
