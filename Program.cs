using System;
using System.Collections.Generic;
using System.IO;
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

        static int gamesPlayed = 0;
        static readonly Dictionary<string, int> bestScores = LoadBestScores();

        static void Main(string[] args)
        {
            Console.WriteLine("=== Number Guessing Game ===");
            Console.WriteLine("1. You guess the computer's number");
            Console.WriteLine("2. Computer guesses your number");
            Console.Write("\nChoose a mode (1 or 2): ");
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
            Console.WriteLine("Select difficulty:");
            Console.WriteLine("1. Easy   (1-10,  unlimited guesses)");
            Console.WriteLine("2. Medium (1-30,  unlimited guesses)");
            Console.WriteLine("3. Hard   (1-100, 7 guesses max)");
            Console.Write("Choose difficulty (1-3): ");
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

                string prompt = "Guess a number between " + diff.Min + " and " + diff.Max;
                if (limited)
                    prompt += " (" + diff.MaxAttempts + " guesses allowed)";
                prompt += ": ";

                int guess = readIntInRange(prompt, diff.Min, diff.Max);
                int amountGuesses = 1;
                bool won = false;

                while (guess != realNumber)
                {
                    if (limited && amountGuesses >= diff.MaxAttempts)
                    {
                        Console.WriteLine("\nOut of guesses! The number was " + realNumber + ".");
                        break;
                    }

                    string direction = guess < realNumber ? "higher" : "lower";
                    int remaining = limited ? diff.MaxAttempts - amountGuesses : int.MaxValue;
                    Console.WriteLine(GetAiHint(guess, direction, amountGuesses, diff.Min, diff.Max, remaining));
                    amountGuesses++;
                    guess = readIntInRange("Your next guess: ", diff.Min, diff.Max);
                }

                if (guess == realNumber)
                {
                    won = true;
                    RecordScore(diff.Name, amountGuesses);
                    Console.WriteLine(
                        "\nYou guessed it! It took you {0} attempt{1}.",
                        amountGuesses,
                        amountGuesses == 1 ? "" : "s");
                }

                if (won) PrintStats(diff.Name);

                Console.Write("\nPlay again? (y/n): ");
                keepPlaying = (Console.ReadLine() ?? "").ToLower().Trim() != "n";
                Console.WriteLine();
            } while (keepPlaying);

            Console.WriteLine("Thanks for playing!");
            Console.ReadLine();
        }

        // ── Reverse mode (computer guesses) ─────────────────────────────────────

        static void PlayReverseMode((string Name, int Min, int Max, int MaxAttempts) diff)
        {
            bool keepPlaying = true;
            do
            {
                Console.WriteLine("Think of a number between " + diff.Min + " and " + diff.Max + ", then press Enter.");
                Console.ReadLine();

                int low = diff.Min;
                int high = diff.Max;
                int attempts = 0;
                bool solved = false;

                while (low <= high)
                {
                    int mid = (low + high) / 2;
                    attempts++;
                    Console.Write("Is your number " + mid + "? (higher / lower / correct): ");
                    string response = (Console.ReadLine() ?? "").ToLower().Trim();

                    if (response == "correct")
                    {
                        RecordScore(diff.Name, attempts);
                        Console.WriteLine("\nGot it in " + attempts + " guess" + (attempts == 1 ? "" : "es") + "!");
                        solved = true;
                        break;
                    }
                    else if (response == "higher")
                        low = mid + 1;
                    else if (response == "lower")
                        high = mid - 1;
                    else
                    {
                        Console.WriteLine("Please type 'higher', 'lower', or 'correct'.");
                        attempts--;
                    }
                }

                if (!solved)
                    Console.WriteLine("\nI ran out of possibilities — are you sure the number was between " + diff.Min + " and " + diff.Max + "?");
                else
                    PrintStats(diff.Name);

                Console.Write("\nPlay again? (y/n): ");
                keepPlaying = (Console.ReadLine() ?? "").ToLower().Trim() != "n";
                Console.WriteLine();
            } while (keepPlaying);

            Console.WriteLine("Thanks for playing!");
            Console.ReadLine();
        }

        // ── Score tracking ───────────────────────────────────────────────────────

        static void RecordScore(string difficulty, int attempts)
        {
            gamesPlayed++;
            if (!bestScores.ContainsKey(difficulty) || attempts < bestScores[difficulty])
            {
                bestScores[difficulty] = attempts;
                SaveBestScores();
            }
        }

        static void PrintStats(string difficulty)
        {
            string best = bestScores.ContainsKey(difficulty)
                ? bestScores[difficulty] + " attempt" + (bestScores[difficulty] == 1 ? "" : "s")
                : "—";
            Console.WriteLine(
                "  Session: {0} game{1} played | Best on {2}: {3}",
                gamesPlayed,
                gamesPlayed == 1 ? "" : "s",
                difficulty,
                best);
        }

        static Dictionary<string, int> LoadBestScores()
        {
            var scores = new Dictionary<string, int>();
            if (!File.Exists(ScoreFile)) return scores;
            try
            {
                foreach (string line in File.ReadAllLines(ScoreFile))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int val))
                        scores[parts[0].Trim()] = val;
                }
            }
            catch { }
            return scores;
        }

        static void SaveBestScores()
        {
            try
            {
                var lines = new List<string>();
                foreach (var kv in bestScores)
                    lines.Add(kv.Key + "=" + kv.Value);
                File.WriteAllLines(ScoreFile, lines);
            }
            catch { }
        }

        // ── Claude API hint ──────────────────────────────────────────────────────

        static string GetAiHint(int guess, string direction, int attemptCount, int min, int max, int remaining)
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

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static int readIntInRange(string message, int min, int max)
        {
            while (true)
            {
                Console.Write(message);
                if (int.TryParse(Console.ReadLine(), out int result) && result >= min && result <= max)
                    return result;
                Console.WriteLine("Please enter a number between " + min + " and " + max + ".");
            }
        }
    }
}
