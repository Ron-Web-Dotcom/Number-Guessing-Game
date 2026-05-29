using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Number_Guessing
{
    class Program
    {
        const int minRange = 1;
        const int maxRange = 30;

        static readonly Random rng = new Random();
        static readonly HttpClient http = new HttpClient();

        static void Main(string[] args)
        {
            Console.WriteLine("=== Number Guessing Game ===");
            Console.WriteLine("1. You guess the computer's number");
            Console.WriteLine("2. Computer guesses your number");
            Console.Write("\nChoose a mode (1 or 2): ");

            string choice = (Console.ReadLine() ?? "1").Trim();
            Console.WriteLine();

            if (choice == "2")
                PlayReverseMode();
            else
                PlayNormalMode();
        }

        // ── Normal mode ─────────────────────────────────────────────────────────

        static void PlayNormalMode()
        {
            bool keepPlaying = true;
            do
            {
                int realNumber = rng.Next(minRange, maxRange + 1);
                int guess = readIntInRange("Guess a number between " + minRange + " and " + maxRange + ": ");
                int amountGuesses = 1;

                while (guess != realNumber)
                {
                    string direction = guess < realNumber ? "higher" : "lower";
                    Console.WriteLine(GetAiHint(guess, direction, amountGuesses));
                    amountGuesses++;
                    guess = readIntInRange("Your next guess: ");
                }

                Console.WriteLine(
                    "\nYou guessed it! It took you {0} attempt{1}.",
                    amountGuesses,
                    amountGuesses == 1 ? "" : "s");

                Console.Write("\nPlay again? (y/n): ");
                keepPlaying = (Console.ReadLine() ?? "").ToLower().Trim() != "n";
                Console.WriteLine();
            } while (keepPlaying);

            Console.WriteLine("Thanks for playing!");
            Console.ReadLine();
        }

        // ── Reverse mode (computer guesses) ─────────────────────────────────────

        static void PlayReverseMode()
        {
            bool keepPlaying = true;
            do
            {
                Console.WriteLine("Think of a number between " + minRange + " and " + maxRange + ", then press Enter.");
                Console.ReadLine();

                int low = minRange;
                int high = maxRange;
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
                    Console.WriteLine("\nI ran out of possibilities — are you sure the number was between " + minRange + " and " + maxRange + "?");

                Console.Write("\nPlay again? (y/n): ");
                keepPlaying = (Console.ReadLine() ?? "").ToLower().Trim() != "n";
                Console.WriteLine();
            } while (keepPlaying);

            Console.WriteLine("Thanks for playing!");
            Console.ReadLine();
        }

        // ── Claude API hint ──────────────────────────────────────────────────────

        static string GetAiHint(int guess, string direction, int attemptCount)
        {
            string apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                return "Try going " + direction + "!";

            try
            {
                string prompt =
                    "A player is guessing a secret number between 1 and 30. " +
                    "They just guessed " + guess + ". The right direction is " + direction + ". " +
                    "This is attempt number " + attemptCount + ". " +
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

            return "Try going " + direction + "!";
        }

        static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

        static string UnescapeJson(string s) =>
            s.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "").Replace("\\t", "\t");

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static int readIntInRange(string message)
        {
            while (true)
            {
                Console.Write(message);
                if (int.TryParse(Console.ReadLine(), out int result) && result >= minRange && result <= maxRange)
                    return result;
                Console.WriteLine("Please enter a number between " + minRange + " and " + maxRange + ".");
            }
        }
    }
}
