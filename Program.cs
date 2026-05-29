using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Number_Guessing
{
    class Program
    {
        static readonly (string Name, int Min, int Max, int MaxAttempts)[] Difficulties =
        {
            ("Easy",   1,  10, int.MaxValue),
            ("Medium", 1,  30, int.MaxValue),
            ("Hard",   1, 100, 7),
        };

        static readonly string ScoreFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scores.txt");

        static readonly Random rng = new Random();
        static readonly HttpClient http = new HttpClient();

        // Session stats
        static int gamesPlayed = 0;
        static int currentStreak = 0;
        static int sessionBestStreak = 0;

        // Persistent data: top-5 scores and best streak per difficulty
        static readonly Dictionary<string, List<int>> leaderboard = new Dictionary<string, List<int>>();
        static readonly Dictionary<string, int> bestStreaks = new Dictionary<string, int>();

        static Program() { LoadScores(); }

        static void Main(string[] args)
        {
            WriteColor("\n=== Number Guessing Game ===\n", ConsoleColor.Cyan);
            Console.WriteLine("1. You guess the computer's number");
            Console.WriteLine("2. Computer guesses your number");
            WriteColor("\nChoose a mode (1 or 2): ", ConsoleColor.White, newLine: false);
            string modeChoice = (Console.ReadLine() ?? "1").Trim();
            Console.WriteLine();

            var diff = ChooseDifficulty();
            Console.WriteLine();

            if (modeChoice == "2")
                PlayReverseMode(diff);
            else
                PlayNormalMode(diff);
        }

        // ── Difficulty selection ─────────────────────────────────────────────────

        static (string Name, int Min, int Max, int MaxAttempts) ChooseDifficulty()
        {
            WriteColor("Select difficulty:", ConsoleColor.Cyan);
            Console.WriteLine("1. Easy   (1-10,  unlimited guesses)");
            Console.WriteLine("2. Medium (1-30,  unlimited guesses)");
            Console.WriteLine("3. Hard   (1-100, 7 guesses max)");
            WriteColor("Choose difficulty (1-3): ", ConsoleColor.White, newLine: false);
            string choice = (Console.ReadLine() ?? "2").Trim();
            return choice == "1" ? Difficulties[0] : choice == "3" ? Difficulties[2] : Difficulties[1];
        }

        // ── Normal mode ─────────────────────────────────────────────────────────

        static void PlayNormalMode((string Name, int Min, int Max, int MaxAttempts) diff)
        {
            bool keepPlaying = true;
            do
            {
                int realNumber = rng.Next(diff.Min, diff.Max + 1);
                bool limited = diff.MaxAttempts != int.MaxValue;

                string firstPrompt = "Guess a number between " + diff.Min + " and " + diff.Max
                    + (limited ? " (" + diff.MaxAttempts + " guesses allowed)" : "") + ": ";

                int guess = readIntInRange(firstPrompt, diff.Min, diff.Max);
                int amountGuesses = 1;
                bool won = false;

                while (guess != realNumber)
                {
                    if (limited && amountGuesses >= diff.MaxAttempts)
                    {
                        WriteColor("\nOut of guesses! The number was " + realNumber + ".", ConsoleColor.Red);
                        break;
                    }

                    string direction = guess < realNumber ? "higher" : "lower";
                    int remaining = limited ? diff.MaxAttempts - amountGuesses : int.MaxValue;
                    string proximity = GetProximity(guess, realNumber, diff.Min, diff.Max);

                    WriteColor(proximity + " — " + GetAiHint(guess, direction, amountGuesses, diff.Min, diff.Max, remaining, proximity), ConsoleColor.Yellow);
                    amountGuesses++;
                    guess = readIntInRange("Your next guess: ", diff.Min, diff.Max);
                }

                if (guess == realNumber)
                {
                    won = true;
                    RecordScore(diff.Name, amountGuesses, won: true);
                    WriteColor(
                        "\nYou guessed it! It took you " + amountGuesses + " attempt" + (amountGuesses == 1 ? "" : "s") + ".",
                        ConsoleColor.Green);
                }
                else
                {
                    RecordScore(diff.Name, amountGuesses, won: false);
                }

                PrintStats(diff.Name);

                WriteColor("\nPlay again? (y/n): ", ConsoleColor.White, newLine: false);
                keepPlaying = (Console.ReadLine() ?? "").ToLower().Trim() != "n";
                Console.WriteLine();
            } while (keepPlaying);

            WriteColor("Thanks for playing!", ConsoleColor.Cyan);
            Console.ReadLine();
        }

        // ── Reverse mode (computer guesses) ─────────────────────────────────────

        static void PlayReverseMode((string Name, int Min, int Max, int MaxAttempts) diff)
        {
            bool keepPlaying = true;
            do
            {
                WriteColor("Think of a number between " + diff.Min + " and " + diff.Max + ", then press Enter.", ConsoleColor.Cyan);
                Console.ReadLine();

                int low = diff.Min;
                int high = diff.Max;
                int attempts = 0;
                bool solved = false;

                while (low <= high)
                {
                    int mid = (low + high) / 2;
                    attempts++;
                    WriteColor("Is your number " + mid + "? (higher / lower / correct): ", ConsoleColor.White, newLine: false);
                    string response = (Console.ReadLine() ?? "").ToLower().Trim();

                    if (response == "correct")
                    {
                        RecordScore(diff.Name, attempts, won: true);
                        WriteColor("\nGot it in " + attempts + " guess" + (attempts == 1 ? "" : "es") + "!", ConsoleColor.Green);
                        solved = true;
                        break;
                    }
                    else if (response == "higher")
                        low = mid + 1;
                    else if (response == "lower")
                        high = mid - 1;
                    else
                    {
                        WriteColor("Please type 'higher', 'lower', or 'correct'.", ConsoleColor.Red);
                        attempts--;
                    }
                }

                if (!solved)
                    WriteColor("\nI ran out of possibilities — are you sure the number was between " + diff.Min + " and " + diff.Max + "?", ConsoleColor.Red);

                PrintStats(diff.Name);

                WriteColor("\nPlay again? (y/n): ", ConsoleColor.White, newLine: false);
                keepPlaying = (Console.ReadLine() ?? "").ToLower().Trim() != "n";
                Console.WriteLine();
            } while (keepPlaying);

            WriteColor("Thanks for playing!", ConsoleColor.Cyan);
            Console.ReadLine();
        }

        // ── Proximity indicator ──────────────────────────────────────────────────

        static string GetProximity(int guess, int answer, int min, int max)
        {
            double pct = Math.Abs(guess - answer) / (double)(max - min);
            if (pct <= 0.05) return "BURNING HOT";
            if (pct <= 0.15) return "Hot!";
            if (pct <= 0.35) return "Warm";
            if (pct <= 0.60) return "Cold";
            return "Freezing cold";
        }

        // ── Score tracking ───────────────────────────────────────────────────────

        static void RecordScore(string difficulty, int attempts, bool won)
        {
            gamesPlayed++;

            if (won)
            {
                currentStreak++;
                if (currentStreak > sessionBestStreak)
                    sessionBestStreak = currentStreak;

                if (!leaderboard.ContainsKey(difficulty))
                    leaderboard[difficulty] = new List<int>();

                leaderboard[difficulty].Add(attempts);
                leaderboard[difficulty].Sort();
                if (leaderboard[difficulty].Count > 5)
                    leaderboard[difficulty].RemoveAt(leaderboard[difficulty].Count - 1);

                int allTimeBest = bestStreaks.ContainsKey(difficulty) ? bestStreaks[difficulty] : 0;
                if (currentStreak > allTimeBest)
                    bestStreaks[difficulty] = currentStreak;

                SaveScores();
            }
            else
            {
                currentStreak = 0;
            }
        }

        static void PrintStats(string difficulty)
        {
            Console.WriteLine();
            WriteColor("  ── Stats ──────────────────────────────", ConsoleColor.Magenta);

            // Session
            WriteColor(
                "  Session : " + gamesPlayed + " game" + (gamesPlayed == 1 ? "" : "s") +
                " | Streak: " + currentStreak +
                " (best this session: " + sessionBestStreak + ")",
                ConsoleColor.Magenta);

            // All-time streak
            int allTimeStreak = bestStreaks.ContainsKey(difficulty) ? bestStreaks[difficulty] : 0;
            WriteColor("  All-time best streak on " + difficulty + ": " + allTimeStreak, ConsoleColor.Magenta);

            // Top-5 leaderboard
            if (leaderboard.ContainsKey(difficulty) && leaderboard[difficulty].Count > 0)
            {
                WriteColor("  Top scores on " + difficulty + ":", ConsoleColor.Magenta);
                var scores = leaderboard[difficulty];
                for (int i = 0; i < scores.Count; i++)
                    WriteColor(
                        "    " + (i + 1) + ". " + scores[i] + " attempt" + (scores[i] == 1 ? "" : "s"),
                        ConsoleColor.Magenta);
            }

            WriteColor("  ────────────────────────────────────────", ConsoleColor.Magenta);
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
                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    if (key.EndsWith("_streak"))
                    {
                        string diff = key.Substring(0, key.Length - 7);
                        if (int.TryParse(val, out int s))
                            bestStreaks[diff] = s;
                    }
                    else
                    {
                        var scores = new List<int>();
                        foreach (string token in val.Split(','))
                            if (int.TryParse(token.Trim(), out int v))
                                scores.Add(v);
                        if (scores.Count > 0)
                            leaderboard[key] = scores;
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
                foreach (var kv in leaderboard)
                    lines.Add(kv.Key + "=" + string.Join(",", kv.Value));
                foreach (var kv in bestStreaks)
                    lines.Add(kv.Key + "_streak=" + kv.Value);
                File.WriteAllLines(ScoreFile, lines);
            }
            catch { }
        }

        // ── Claude API hint ──────────────────────────────────────────────────────

        static string GetAiHint(int guess, string direction, int attemptCount, int min, int max, int remaining, string proximity)
        {
            string apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                return BuildFallbackHint(direction, remaining);

            try
            {
                string remainingClause = remaining != int.MaxValue
                    ? " They have " + remaining + " guess" + (remaining == 1 ? "" : "es") + " left."
                    : "";

                string prompt =
                    "A player is guessing a secret number between " + min + " and " + max + ". " +
                    "They just guessed " + guess + ". The right direction is " + direction + ". " +
                    "Proximity to the answer: " + proximity + ". " +
                    "This is attempt number " + attemptCount + "." + remainingClause + " " +
                    "Give one short, fun, encouraging hint in under 15 words. Do NOT reveal the number.";

                string body =
                    "{\"model\":\"claude-haiku-4-5-20251001\"," +
                    "\"max_tokens\":60," +
                    "\"messages\":[{\"role\":\"user\",\"content\":\"" + EscapeJson(prompt) + "\"}]}";

                using (var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"))
                {
                    req.Headers.Add("x-api-key", apiKey);
                    req.Headers.Add("anthropic-version", "2023-06-01");
                    req.Content = new StringContent(body, Encoding.UTF8, "application/json");

                    var resp = http.SendAsync(req).GetAwaiter().GetResult();
                    string json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    int contentIdx = json.IndexOf("\"content\"");
                    if (contentIdx >= 0)
                    {
                        var m = Regex.Match(
                            json.Substring(contentIdx),
                            "\"text\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
                        if (m.Success)
                            return UnescapeJson(m.Groups[1].Value);
                    }
                }
            }
            catch { }

            return BuildFallbackHint(direction, remaining);
        }

        static string BuildFallbackHint(string direction, int remaining)
        {
            string hint = "Try going " + direction + "!";
            if (remaining != int.MaxValue && remaining <= 3)
                hint += " (" + remaining + " guess" + (remaining == 1 ? "" : "es") + " left)";
            return hint;
        }

        static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

        static string UnescapeJson(string s) =>
            s.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "").Replace("\\t", "\t");

        // ── Console color helper ─────────────────────────────────────────────────

        static void WriteColor(string text, ConsoleColor color, bool newLine = true)
        {
            Console.ForegroundColor = color;
            if (newLine) Console.WriteLine(text);
            else Console.Write(text);
            Console.ResetColor();
        }

        // ── Input helper ─────────────────────────────────────────────────────────

        private static int readIntInRange(string message, int min, int max)
        {
            while (true)
            {
                WriteColor(message, ConsoleColor.White, newLine: false);
                if (int.TryParse(Console.ReadLine(), out int result) && result >= min && result <= max)
                    return result;
                WriteColor("Please enter a number between " + min + " and " + max + ".", ConsoleColor.Red);
            }
        }
    }
}
