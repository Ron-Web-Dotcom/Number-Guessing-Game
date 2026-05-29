# Number Guessing Game

A C# console number guessing game with two game modes, three difficulty levels, AI-powered hints, hot/cold proximity feedback, win streaks, and a persistent top-5 leaderboard.

## Features

- **Mode 1 — You guess**: The computer picks a random number. After each wrong guess you get a proximity reading (*Burning hot / Hot / Warm / Cold / Freezing cold*) plus a Claude AI hint tailored to how close you are and how many guesses remain.
- **Mode 2 — Computer guesses**: You think of a number and the computer uses binary search to find it (guaranteed in ≤ 7 guesses on Hard).
- **Difficulty levels**

  | Level  | Range  | Guess limit |
  |--------|--------|-------------|
  | Easy   | 1–10   | Unlimited   |
  | Medium | 1–30   | Unlimited   |
  | Hard   | 1–100  | 7           |

- **Hot/cold proximity** — Each wrong guess shows how close you are as a percentage of the range, independent of the Claude API.
- **Win streaks** — Tracks your current streak and best streak for the session; best all-time streak per difficulty is persisted.
- **Top-5 leaderboard** — The 5 best scores per difficulty are saved to `scores.txt` and displayed after each game.
- **Color-coded output** — Cyan for menus, green for wins, red for losses/errors, yellow for hints, magenta for stats.

## Requirements

- [.NET Framework 4.7.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472) or Visual Studio 2017+

## Running the game

Open `Number Guessing.sln` in Visual Studio and press **F5**, or build from the command line:

```
msbuild "Number Guessing.sln" /p:Configuration=Release
"bin\Release\Number Guessing.exe"
```

## AI hints (optional)

AI hints require an Anthropic API key. The hint prompt includes the proximity level and remaining guess count so Claude's response is context-aware.

**Windows (Command Prompt)**
```
set ANTHROPIC_API_KEY=your-key-here
```

**Windows (PowerShell)**
```
$env:ANTHROPIC_API_KEY="your-key-here"
```

The game works without a key — proximity + directional fallback hints are always shown.

## Score file

`scores.txt` is written next to the executable. Format:

```
Easy=3,5,7,8,10
Easy_streak=4
Medium=4,6,8
Medium_streak=2
Hard=6,7
Hard_streak=1
```
