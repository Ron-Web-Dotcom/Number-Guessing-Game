using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Number_Guessing
{
    class Program
    {
        // ── Configuration ────────────────────────────────────────────────────────

        static readonly (string Name, int Min, int Max, int MaxAttempts)[] Difficulties =
        {
            ("Easy",   1,  10, int.MaxValue),
            ("Medium", 1,  30, int.MaxValue),
            ("Hard",   1, 100, 7),
        };

        static readonly Dictionary<string, (string Title, string Desc)> AchievementInfo =
            new Dictionary<string, (string, string)>
        {
            ["first_blood"]  = ("First Blood",   "Win your first game"),
            ["one_shot"]     = ("One Shot",       "Win in exactly 1 guess"),
            ["speed_demon"]  = ("Speed Demon",    "Win in under 5 seconds"),
            ["hot_streak"]   = ("Hot Streak",     "Win 5 games in a row"),
            ["comeback"]     = ("Comeback Kid",   "Win Hard mode with only 1 guess remaining"),
            ["daily_player"] = ("Daily Player",   "Complete a daily challenge"),
            ["hard_boiled"]  = ("Hard Boiled",    "Win Hard mode"),
            ["veteran"]      = ("Veteran",        "Play 20 games total"),
        };

        static readonly string ScoreFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scores.txt");

        static readonly Random rng = new Random();
        static readonly HttpClient http = new HttpClient();

        // Session stats
        static int gamesPlayed = 0;
        static int currentStreak = 0;
        static int sessionBestStreak = 0;

        // Persistent data
        static readonly Dictionary<string, List<int>> leaderboard             = new Dictionary<string, List<int>>();
        static readonly Dictionary<string, int> bestStreaks                   = new Dictionary<string, int>();
        static readonly Dictionary<string, (int Total, int Wins)> scoreAverages = new Dictionary<string, (int, int)>();
        static readonly Dictionary<string, int> dailyResults                  = new Dictionary<string, int>();
        static readonly HashSet<string> unlockedAchievements                  = new HashSet<string>();

        static Program() { LoadScores(); }

        // ── Entry point ──────────────────────────────────────────────────────────

        static void Main(string[] args)
        {
            WriteColor("\n=== Number Guessing Game ===\n", ConsoleColor.Cyan);
            Console.WriteLine("1. You guess the computer's number");
            Console.WriteLine("2. Computer guesses your number");
            Console.WriteLine("3. Daily challenge  (same number for everyone today)");
            WriteColor("\nChoose a mode (1-3): ", ConsoleColor.White, newLine: false);
            string modeChoice = (Console.ReadLine() ?? "1").Trim();
            Console.WriteLine();

            var diff = ChooseDifficulty();
            Console.WriteLine();

            switch (modeChoice)
            {
                case "2":
                    PlayReverseMode(diff);
                    break;
                case "3":
                    PlayDailyChallenge(diff);
                    break;
                default:
                    int playerCount = AskPlayerCount();
                    string[] players = GetPlayerNames(playerCount);
                    int tournamentRounds = playerCount > 1 ? AskTournamentRounds() : 0;
                    Console.WriteLine();
                    if (tournamentRounds > 0)
                        PlayTournament(diff, players, tournamentRounds);
                    else
                        PlayNormalMode(diff, players);
                    break;
            }
        }

        // ── Menus ────────────────────────────────────────────────────────────────

        static (string Name, int Min, int Max, int MaxAttempts) ChooseDifficulty()
        {
            WriteColor("Select difficulty:", ConsoleColor.Cyan);
            Console.WriteLine("1. Easy   (1-10,  unlimited guesses)");
            Console.WriteLine("2. Medium (1-30,  unlimited guesses)");
            Console.WriteLine("3. Hard   (1-100, 7 guesses max)");
            Console.WriteLine("4. Custom");
            WriteColor("Choose difficulty (1-4): ", ConsoleColor.White, newLine: false);
            string c = (Console.ReadLine() ?? "2").Trim();

            if (c == "4") return BuildCustomDifficulty();
            return c == "1" ? Difficulties[0] : c == "3" ? Difficulties[2] : Difficulties[1];
        }

        static (string Name, int Min, int Max, int MaxAttempts) BuildCustomDifficulty()
        {
            int min = ReadMenuInt("  Min value (1-9998): ", 1, 9998);
            int max = ReadMenuInt("  Max value (" + (min + 1) + "-9999): ", min + 1, 9999);
            WriteColor("  Guess limit (0 = unlimited): ", ConsoleColor.White, newLine: false);
            int limit = int.TryParse(Console.ReadLine(), out int l) && l > 0 ? l : int.MaxValue;
            return ("Custom(" + min + "-" + max + ")", min, max, limit);
        }

        static int AskPlayerCount()
        {
            WriteColor("How many players? (1-4): ", ConsoleColor.White, newLine: false);
            if (int.TryParse(Console.ReadLine(), out int n) && n >= 1 && n <= 4) return n;
            return 1;
        }

        static int AskTournamentRounds()
        {
            WriteColor("Tournament? Rounds (0 = off, 3 / 5 / 7): ", ConsoleColor.White, newLine: false);
            if (int.TryParse(Console.ReadLine(), out int r) && (r == 3 || r == 5 || r == 7)) return r;
            return 0;
        }

        static string[] GetPlayerNames(int count)
        {
            var names = new string[count];
            for (int i = 0; i < count; i++)
            {
                WriteColor("Player " + (i + 1) + " name: ", ConsoleColor.White, newLine: false);
                string name = (Console.ReadLine() ?? "").Trim();
                names[i] = string.IsNullOrEmpty(name) ? "Player " + (i + 1) : name;
            }
            return names;
        }

        // ── Normal mode (single or multiplayer) ──────────────────────────────────

        static void PlayNormalMode((string Name, int Min, int Max, int MaxAttempts) diff, string[] players)
        {
            bool keepPlaying = true;
            do
            {
                int target = rng.Next(diff.Min, diff.Max + 1);
                var results = new (string Name, int Attempts, double Seconds, bool Won, bool Quit)[players.Length];

                for (int p = 0; p < players.Length; p++)
                {
                    if (players.Length > 1)
                    {
                        WriteColor("\n--- " + players[p] + "'s turn ---", ConsoleColor.Cyan);
                        WriteColor("Press Enter when ready (others look away)...", ConsoleColor.White, newLine: false);
                        Console.ReadLine();
                        Console.WriteLine();
                    }
                    results[p] = RunGuessSession(diff, target, players[p]);
                }

                if (players.Length > 1)
                {
                    ShowRoundResults(results, diff.Name);
                }
                else
                {
                    var r = results[0];
                    if (!r.Quit)
                    {
                        if (r.Won)
                        {
                            RecordWin(diff.Name, r.Attempts);
                            int rem = diff.MaxAttempts == int.MaxValue ? int.MaxValue : diff.MaxAttempts - r.Attempts;
                            AwardAchievements(diff.Name, r.Attempts, r.Seconds, rem, isDaily: false);
                        }
                        else RecordLoss(diff.Name);
                    }
                }

                PrintStats(diff.Name);

                WriteColor("\nPlay again? (y/n): ", ConsoleColor.White, newLine: false);
                keepPlaying = (Console.ReadLine() ?? "").ToLower().Trim() != "n";
                Console.WriteLine();
            } while (keepPlaying);

            WriteColor("Thanks for playing!", ConsoleColor.Cyan);
            Console.ReadLine();
        }

        static void ShowRoundResults(
            (string Name, int Attempts, double Seconds, bool Won, bool Quit)[] results, string difficulty)
        {
            WriteColor("\n--- Round results ---", ConsoleColor.Cyan);
            var winners = results.Where(r => r.Won && !r.Quit)
                                 .OrderBy(r => r.Attempts).ThenBy(r => r.Seconds).ToArray();
            var losers  = results.Where(r => !r.Won || r.Quit).ToArray();

            int rank = 1;
            foreach (var r in winners)
            {
                WriteColor("  " + rank++ + ". " + r.Name + ": " + r.Attempts
                    + " attempt" + (r.Attempts == 1 ? "" : "s")
                    + " (" + r.Seconds.ToString("F1") + "s)", ConsoleColor.Green);
                RecordWin(difficulty, r.Attempts);
            }
            foreach (var r in losers)
            {
                WriteColor("  — " + r.Name + ": " + (r.Quit ? "quit" : "did not guess it"), ConsoleColor.Red);
                if (!r.Quit) RecordLoss(difficulty);
            }
        }

        // ── Tournament mode ──────────────────────────────────────────────────────

        static void PlayTournament(
            (string Name, int Min, int Max, int MaxAttempts) diff, string[] players, int totalRounds)
        {
            var wins          = new int[players.Length];
            var totalAttempts = new int[players.Length];

            WriteColor("\n=== Tournament: Best of " + totalRounds + " ===", ConsoleColor.Cyan);

            for (int round = 1; round <= totalRounds; round++)
            {
                WriteColor("\n--- Round " + round + " of " + totalRounds + " ---", ConsoleColor.Cyan);
                int target = rng.Next(diff.Min, diff.Max + 1);
                var roundResults = new (string Name, int Attempts, double Seconds, bool Won, bool Quit)[players.Length];

                for (int p = 0; p < players.Length; p++)
                {
                    WriteColor("\n" + players[p] + "'s turn — press Enter when ready...", ConsoleColor.White, newLine: false);
                    Console.ReadLine();
                    Console.WriteLine();
                    roundResults[p] = RunGuessSession(diff, target, players[p]);
                }

                // Round winner = fewest attempts (time as tiebreaker)
                var roundWinners = roundResults
                    .Select((r, i) => (Result: r, Index: i))
                    .Where(x => x.Result.Won && !x.Result.Quit)
                    .OrderBy(x => x.Result.Attempts).ThenBy(x => x.Result.Seconds)
                    .ToArray();

                if (roundWinners.Length > 0)
                {
                    int bestAtt  = roundWinners[0].Result.Attempts;
                    double bestT = roundWinners[0].Result.Seconds;
                    foreach (var w in roundWinners.Where(
                        x => x.Result.Attempts == bestAtt && Math.Abs(x.Result.Seconds - bestT) <= 0.5))
                    {
                        wins[w.Index]++;
                        WriteColor("  Round " + round + " won by: " + players[w.Index] + "!", ConsoleColor.Green);
                    }
                }
                else WriteColor("  No winner this round.", ConsoleColor.Yellow);

                foreach (var (r, i) in roundResults.Select((r, i) => (r, i)))
                    totalAttempts[i] += r.Won ? r.Attempts : (diff.MaxAttempts == int.MaxValue ? 999 : diff.MaxAttempts + 1);

                // Standings after each round
                WriteColor("\n  Standings:", ConsoleColor.Magenta);
                var standings = players.Select((p, i) => (Name: p, Wins: wins[i], Att: totalAttempts[i]))
                                       .OrderByDescending(x => x.Wins).ThenBy(x => x.Att).ToArray();
                for (int i = 0; i < standings.Length; i++)
                    WriteColor("    " + (i + 1) + ". " + standings[i].Name
                        + "  " + standings[i].Wins + " win" + (standings[i].Wins == 1 ? "" : "s"), ConsoleColor.Magenta);
            }

            // Final results
            WriteColor("\n=== Tournament Results ===", ConsoleColor.Cyan);
            var final = players.Select((p, i) => (Name: p, Wins: wins[i], Att: totalAttempts[i]))
                               .OrderByDescending(x => x.Wins).ThenBy(x => x.Att).ToArray();

            for (int i = 0; i < final.Length; i++)
            {
                ConsoleColor c = i == 0 ? ConsoleColor.Green : ConsoleColor.White;
                WriteColor("  " + (i + 1) + ". " + final[i].Name
                    + "  " + final[i].Wins + " win" + (final[i].Wins == 1 ? "" : "s")
                    + (i == 0 ? "  <-- Champion!" : ""), c);
                if (final[i].Wins > 0)
                    RecordWin(diff.Name, final[i].Att / Math.Max(1, final[i].Wins));
            }

            WriteColor("\nTournament complete! Press Enter to exit.", ConsoleColor.Cyan);
            Console.ReadLine();
        }

        // ── Daily challenge ──────────────────────────────────────────────────────

        static void PlayDailyChallenge((string Name, int Min, int Max, int MaxAttempts) diff)
        {
            DateTime today  = DateTime.Today;
            string dateKey  = diff.Name + "_" + today.ToString("yyyy-MM-dd");

            WriteColor("\n=== Daily Challenge — " + today.ToString("MMMM d, yyyy") + " ===", ConsoleColor.Cyan);
            WriteColor("Difficulty: " + diff.Name + " (" + diff.Min + "-" + diff.Max + ")\n", ConsoleColor.White);

            if (dailyResults.ContainsKey(dateKey))
            {
                WriteColor("You already completed today's challenge!", ConsoleColor.Green);
                WriteColor("Result: " + dailyResults[dateKey] + " attempt"
                    + (dailyResults[dateKey] == 1 ? "" : "s") + ".", ConsoleColor.Magenta);
                Console.ReadLine();
                return;
            }

            int target = GameLogic.GetDailyNumber(diff.Min, diff.Max, today);
            WriteColor("Today's number is the same for everyone. Good luck!\n", ConsoleColor.White);

            var result = RunGuessSession(diff, target, "You");

            if (!result.Quit && result.Won)
            {
                dailyResults[dateKey] = result.Attempts;
                RecordWin(diff.Name, result.Attempts);
                int rem = diff.MaxAttempts == int.MaxValue ? int.MaxValue : diff.MaxAttempts - result.Attempts;
                AwardAchievements(diff.Name, result.Attempts, result.Seconds, rem, isDaily: true);
                WriteColor("\nDaily complete! " + result.Attempts + " attempt"
                    + (result.Attempts == 1 ? "" : "s") + " in " + result.Seconds.ToString("F1") + "s.", ConsoleColor.Green);
            }

            PrintStats(diff.Name);
            Console.ReadLine();
        }

        // ── Core guessing session ────────────────────────────────────────────────

        static (string Name, int Attempts, double Seconds, bool Won, bool Quit) RunGuessSession(
            (string Name, int Min, int Max, int MaxAttempts) diff, int target, string playerName)
        {
            bool limited    = diff.MaxAttempts != int.MaxValue;
            string prompt   = playerName + ", guess a number between " + diff.Min + " and " + diff.Max
                + (limited ? " (" + diff.MaxAttempts + " guesses allowed)" : "")
                + " (or 'quit'): ";

            var history = new List<(int Guess, string Proximity, string Direction)>();
            var sw      = Stopwatch.StartNew();

            int? first = readGameInput(prompt, diff.Min, diff.Max);
            if (first == null) { sw.Stop(); return (playerName, 0, 0, Won: false, Quit: true); }

            int guess   = first.Value;
            int attempts = 1;

            while (guess != target)
            {
                if (limited && attempts >= diff.MaxAttempts)
                {
                    sw.Stop();
                    history.Add((guess, GameLogic.GetProximity(guess, target, diff.Min, diff.Max),
                        guess < target ? "↑" : "↓"));
                    WriteColor("\nOut of guesses! The number was " + target + ".", ConsoleColor.Red);
                    PrintGuessHistory(history, won: false);
                    return (playerName, attempts, sw.Elapsed.TotalSeconds, Won: false, Quit: false);
                }

                string direction = guess < target ? "higher" : "lower";
                string arrow     = guess < target ? "↑" : "↓";
                int remaining    = limited ? diff.MaxAttempts - attempts : int.MaxValue;
                string proximity = GameLogic.GetProximity(guess, target, diff.Min, diff.Max);
                history.Add((guess, proximity, arrow));
                WriteColor(proximity + " — "
                    + GetAiHint(guess, direction, attempts, diff.Min, diff.Max, remaining, proximity),
                    ConsoleColor.Yellow);

                attempts++;
                int? next = readGameInput("Your next guess (or 'quit'): ", diff.Min, diff.Max);
                if (next == null)
                {
                    sw.Stop();
                    WriteColor("Game abandoned.", ConsoleColor.DarkGray);
                    return (playerName, attempts, sw.Elapsed.TotalSeconds, Won: false, Quit: true);
                }
                guess = next.Value;
            }

            sw.Stop();
            history.Add((guess, "✓", ""));
            WriteColor(
                "\n" + playerName + " guessed it in " + attempts + " attempt" + (attempts == 1 ? "" : "s")
                + " (" + sw.Elapsed.TotalSeconds.ToString("F1") + "s)!",
                ConsoleColor.Green);
            PrintGuessHistory(history, won: true);
            return (playerName, attempts, sw.Elapsed.TotalSeconds, Won: true, Quit: false);
        }

        static void PrintGuessHistory(List<(int Guess, string Proximity, string Direction)> history, bool won)
        {
            if (history.Count == 0) return;
            Console.WriteLine();
            WriteColor("  Guess history:", ConsoleColor.Cyan);
            for (int i = 0; i < history.Count; i++)
            {
                var (g, prox, dir) = history[i];
                bool isLast = i == history.Count - 1;
                ConsoleColor color = isLast && won ? ConsoleColor.Green : ConsoleColor.DarkYellow;
                string label = isLast && won ? "  v" : "  " + (i + 1) + ".";
                WriteColor(label + " " + g.ToString().PadLeft(4) + "  " + (dir + " " + prox).TrimEnd(), color);
            }
        }

        // ── Reverse mode ─────────────────────────────────────────────────────────

        static void PlayReverseMode((string Name, int Min, int Max, int MaxAttempts) diff)
        {
            bool keepPlaying = true;
            do
            {
                WriteColor("Think of a number between " + diff.Min + " and " + diff.Max + ", then press Enter.", ConsoleColor.Cyan);
                Console.ReadLine();

                var sw = Stopwatch.StartNew();
                int low = diff.Min, high = diff.Max, attempts = 0;
                bool solved = false;

                while (low <= high)
                {
                    int mid = GameLogic.BinarySearchMid(low, high);
                    attempts++;
                    WriteColor("Is your number " + mid + "? (higher / lower / correct): ", ConsoleColor.White, newLine: false);
                    string response = (Console.ReadLine() ?? "").ToLower().Trim();

                    if (response == "correct")
                    {
                        sw.Stop();
                        RecordWin(diff.Name, attempts);
                        WriteColor("\nGot it in " + attempts + " guess" + (attempts == 1 ? "" : "es")
                            + " (" + sw.Elapsed.TotalSeconds.ToString("F1") + "s)!", ConsoleColor.Green);
                        solved = true;
                        break;
                    }
                    else if (response == "higher") low = mid + 1;
                    else if (response == "lower")  high = mid - 1;
                    else
                    {
                        WriteColor("Please type 'higher', 'lower', or 'correct'.", ConsoleColor.Red);
                        attempts--;
                    }
                }

                if (!solved)
                    WriteColor("\nI ran out of possibilities — are you sure it was between "
                        + diff.Min + " and " + diff.Max + "?", ConsoleColor.Red);

                PrintStats(diff.Name);

                WriteColor("\nPlay again? (y/n): ", ConsoleColor.White, newLine: false);
                keepPlaying = (Console.ReadLine() ?? "").ToLower().Trim() != "n";
                Console.WriteLine();
            } while (keepPlaying);

            WriteColor("Thanks for playing!", ConsoleColor.Cyan);
            Console.ReadLine();
        }

        // ── Achievement system ───────────────────────────────────────────────────

        static void AwardAchievements(string difficulty, int attempts, double seconds,
            int remainingGuesses, bool isDaily)
        {
            var earned = new List<string>();

            void Check(string id, bool cond)
            {
                if (cond && unlockedAchievements.Add(id)) earned.Add(id);
            }

            Check("first_blood",  gamesPlayed == 1);
            Check("one_shot",     attempts == 1);
            Check("speed_demon",  seconds < 5.0);
            Check("hot_streak",   currentStreak >= 5);
            Check("comeback",     difficulty == "Hard" && remainingGuesses == 1);
            Check("daily_player", isDaily);
            Check("hard_boiled",  difficulty == "Hard");
            Check("veteran",      gamesPlayed >= 20);

            if (earned.Count > 0) SaveScores();

            foreach (string id in earned)
            {
                var (title, desc) = AchievementInfo[id];
                WriteColor("\n  [Achievement unlocked] " + title + " — " + desc, ConsoleColor.DarkYellow);
            }
        }

        // ── Score management ─────────────────────────────────────────────────────

        static void RecordWin(string difficulty, int attempts)
        {
            gamesPlayed++;
            currentStreak++;
            if (currentStreak > sessionBestStreak) sessionBestStreak = currentStreak;

            if (!leaderboard.ContainsKey(difficulty)) leaderboard[difficulty] = new List<int>();
            leaderboard[difficulty].Add(attempts);
            leaderboard[difficulty].Sort();
            if (leaderboard[difficulty].Count > 5) leaderboard[difficulty].RemoveAt(5);

            if (!bestStreaks.ContainsKey(difficulty) || currentStreak > bestStreaks[difficulty])
                bestStreaks[difficulty] = currentStreak;

            var avg = scoreAverages.ContainsKey(difficulty) ? scoreAverages[difficulty] : (0, 0);
            scoreAverages[difficulty] = (avg.Total + attempts, avg.Wins + 1);

            SaveScores();
        }

        static void RecordLoss(string difficulty)
        {
            gamesPlayed++;
            currentStreak = 0;
        }

        static void PrintStats(string difficulty)
        {
            double avg = scoreAverages.ContainsKey(difficulty)
                ? GameLogic.ComputeAverage(scoreAverages[difficulty].Total, scoreAverages[difficulty].Wins)
                : 0;
            int allTimeStreak = bestStreaks.ContainsKey(difficulty) ? bestStreaks[difficulty] : 0;

            Console.WriteLine();
            WriteColor("  ── Stats ──────────────────────────────────────", ConsoleColor.Magenta);
            WriteColor("  Session : " + gamesPlayed + " game" + (gamesPlayed == 1 ? "" : "s")
                + " | Streak: " + currentStreak
                + " (session best: " + sessionBestStreak + ")", ConsoleColor.Magenta);
            WriteColor("  All-time: best streak " + allTimeStreak
                + (avg > 0 ? " | avg " + avg.ToString("F1") + " attempts on " + difficulty : ""),
                ConsoleColor.Magenta);

            if (leaderboard.ContainsKey(difficulty) && leaderboard[difficulty].Count > 0)
            {
                WriteColor("  Top-5 on " + difficulty + ":", ConsoleColor.Magenta);
                for (int i = 0; i < leaderboard[difficulty].Count; i++)
                    WriteColor("    " + (i + 1) + ". " + leaderboard[difficulty][i]
                        + " attempt" + (leaderboard[difficulty][i] == 1 ? "" : "s"), ConsoleColor.Magenta);
            }

            if (unlockedAchievements.Count > 0)
            {
                WriteColor("  Achievements (" + unlockedAchievements.Count
                    + "/" + AchievementInfo.Count + "):", ConsoleColor.DarkYellow);
                foreach (string id in unlockedAchievements)
                    if (AchievementInfo.ContainsKey(id))
                        WriteColor("    [" + AchievementInfo[id].Title + "]", ConsoleColor.DarkYellow);
            }

            WriteColor("  ────────────────────────────────────────────────", ConsoleColor.Magenta);
        }

        // ── Persistence ──────────────────────────────────────────────────────────

        static void LoadScores()
        {
            if (!File.Exists(ScoreFile)) return;
            try
            {
                foreach (string line in File.ReadAllLines(ScoreFile))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue;
                    string key = parts[0].Trim(), val = parts[1].Trim();

                    if (key == "achievements")
                    {
                        foreach (string a in val.Split(','))
                            if (!string.IsNullOrWhiteSpace(a)) unlockedAchievements.Add(a.Trim());
                    }
                    else if (key.EndsWith("_streak"))
                    {
                        if (int.TryParse(val, out int s))
                            bestStreaks[key.Substring(0, key.Length - 7)] = s;
                    }
                    else if (key.EndsWith("_avg"))
                    {
                        string[] ap = val.Split(',');
                        if (ap.Length == 2 && int.TryParse(ap[0], out int t) && int.TryParse(ap[1], out int w))
                            scoreAverages[key.Substring(0, key.Length - 4)] = (t, w);
                    }
                    else if (key.Contains("_20"))
                    {
                        if (int.TryParse(val, out int d)) dailyResults[key] = d;
                    }
                    else
                    {
                        var scores = new List<int>();
                        foreach (string token in val.Split(','))
                            if (int.TryParse(token.Trim(), out int v)) scores.Add(v);
                        if (scores.Count > 0) leaderboard[key] = scores;
                    }
                }
            }
            catch { }
        }

        static void SaveScores()
        {
            try
            {
                var lines = new List<string>();
                if (unlockedAchievements.Count > 0)
                    lines.Add("achievements=" + string.Join(",", unlockedAchievements));
                foreach (var kv in leaderboard)    lines.Add(kv.Key + "=" + string.Join(",", kv.Value));
                foreach (var kv in bestStreaks)    lines.Add(kv.Key + "_streak=" + kv.Value);
                foreach (var kv in scoreAverages)  lines.Add(kv.Key + "_avg=" + kv.Value.Total + "," + kv.Value.Wins);
                foreach (var kv in dailyResults)   lines.Add(kv.Key + "=" + kv.Value);
                File.WriteAllLines(ScoreFile, lines);
            }
            catch { }
        }

        // ── Claude API hint ──────────────────────────────────────────────────────

        static string GetAiHint(int guess, string direction, int attemptCount,
            int min, int max, int remaining, string proximity)
        {
            string apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                return GameLogic.BuildFallbackHint(direction, remaining);

            try
            {
                string remainingClause = remaining != int.MaxValue
                    ? " They have " + remaining + " guess" + (remaining == 1 ? "" : "es") + " left."
                    : "";

                string prompt =
                    "A player is guessing a secret number between " + min + " and " + max + ". " +
                    "They just guessed " + guess + ". The right direction is " + direction + ". " +
                    "Proximity: " + proximity + ". This is attempt " + attemptCount + "." + remainingClause + " " +
                    "Give one short, fun, encouraging hint in under 15 words. Do NOT reveal the number.";

                string body =
                    "{\"model\":\"claude-haiku-4-5-20251001\",\"max_tokens\":60," +
                    "\"messages\":[{\"role\":\"user\",\"content\":\"" + EscapeJson(prompt) + "\"}]}";

                using (var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"))
                {
                    req.Headers.Add("x-api-key", apiKey);
                    req.Headers.Add("anthropic-version", "2023-06-01");
                    req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    var resp = http.SendAsync(req).GetAwaiter().GetResult();
                    string json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    int ci = json.IndexOf("\"content\"");
                    if (ci >= 0)
                    {
                        var m = Regex.Match(json.Substring(ci), "\"text\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
                        if (m.Success) return UnescapeJson(m.Groups[1].Value);
                    }
                }
            }
            catch { }

            return GameLogic.BuildFallbackHint(direction, remaining);
        }

        static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

        static string UnescapeJson(string s) =>
            s.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "").Replace("\\t", "\t");

        // ── Console helpers ──────────────────────────────────────────────────────

        static void WriteColor(string text, ConsoleColor color, bool newLine = true)
        {
            Console.ForegroundColor = color;
            if (newLine) Console.WriteLine(text); else Console.Write(text);
            Console.ResetColor();
        }

        // For menus — never returns null
        private static int ReadMenuInt(string message, int min, int max)
        {
            while (true)
            {
                WriteColor(message, ConsoleColor.White, newLine: false);
                if (int.TryParse(Console.ReadLine(), out int result) && result >= min && result <= max)
                    return result;
                WriteColor("Please enter a number between " + min + " and " + max + ".", ConsoleColor.Red);
            }
        }

        // For game sessions — null means the player typed 'quit'
        private static int? readGameInput(string message, int min, int max)
        {
            while (true)
            {
                WriteColor(message, ConsoleColor.White, newLine: false);
                string input = (Console.ReadLine() ?? "").Trim();
                if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    return null;
                if (int.TryParse(input, out int result) && result >= min && result <= max)
                    return result;
                WriteColor("Enter a number between " + min + " and " + max + ", or 'quit' to exit.", ConsoleColor.Red);
            }
        }
    }
}
