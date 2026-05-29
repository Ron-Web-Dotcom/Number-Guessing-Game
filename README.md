# Number Guessing Game

A C# console number guessing game with two game modes and AI-powered hints via the Claude API.

## Features

- **Mode 1 — You guess**: The computer picks a random number between 1 and 30. After each wrong guess, a Claude AI hint encourages you with a fun nudge in the right direction.
- **Mode 2 — Computer guesses**: You think of a number and the computer uses binary search to find it (guaranteed in ≤ 5 guesses).
- **Session stats**: Tracks games played and your best score across rounds.

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

The game works without a key — it falls back to plain directional hints.
