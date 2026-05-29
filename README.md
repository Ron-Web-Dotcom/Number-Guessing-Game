# Number Guessing Game

A C# console number guessing game with two game modes, three difficulty levels, AI-powered hints via the Claude API, and persistent high scores.

## Features

- **Mode 1 — You guess**: The computer picks a random number. After each wrong guess a Claude AI hint nudges you in the right direction. Hard mode adds a 7-guess limit.
- **Mode 2 — Computer guesses**: You think of a number and the computer uses binary search to find it (guaranteed in ≤ 7 guesses on Hard).
- **Difficulty levels**

  | Level  | Range  | Guess limit |
  |--------|--------|-------------|
  | Easy   | 1–10   | Unlimited   |
  | Medium | 1–30   | Unlimited   |
  | Hard   | 1–100  | 7           |

- **Persistent high scores**: Best score per difficulty is saved to `scores.txt` next to the executable and loaded on every launch.
- **Session stats**: Games played and best score are shown after each completed game.

## Requirements

- [.NET Framework 4.7.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472) or Visual Studio 2017+

## Running the game

Open `Number Guessing.sln` in Visual Studio and press **F5**, or build from the command line:

```
msbuild "Number Guessing.sln" /p:Configuration=Release
"bin\Release\Number Guessing.exe"
```

## AI hints (optional)

AI hints require an Anthropic API key. Set the environment variable before running:

**Windows (Command Prompt)**
```
set ANTHROPIC_API_KEY=your-key-here
```

**Windows (PowerShell)**
```
$env:ANTHROPIC_API_KEY="your-key-here"
```

The game works without a key — it falls back to plain directional hints. On Hard mode the fallback also shows remaining guesses when 3 or fewer are left.
