using System;
using System.Collections.Generic;

namespace Number_Guessing
{
    public static class GameLogic
    {
        public static string GetProximity(int guess, int answer, int min, int max)
        {
            if (max <= min) return "On it!";
            double pct = Math.Abs(guess - answer) / (double)(max - min);
            if (pct <= 0.05) return "BURNING HOT";
            if (pct <= 0.15) return "Hot!";
            if (pct <= 0.35) return "Warm";
            if (pct <= 0.60) return "Cold";
            return "Freezing cold";
        }

        public static string BuildFallbackHint(string direction, int remaining)
        {
            string hint = "Try going " + direction + "!";
            if (remaining != int.MaxValue && remaining <= 3)
                hint += " (" + remaining + " guess" + (remaining == 1 ? "" : "es") + " left)";
            return hint;
        }

        public static int GetDailySeed(DateTime date) =>
            date.Year * 10000 + date.Month * 100 + date.Day;

        public static int GetDailyNumber(int min, int max, DateTime date) =>
            new Random(GetDailySeed(date)).Next(min, max + 1);

        public static int BinarySearchMid(int low, int high) => (low + high) / 2;

        public static double ComputeAverage(int totalAttempts, int totalWins) =>
            totalWins == 0 ? 0.0 : Math.Round((double)totalAttempts / totalWins, 1);

        public static string FormatAttempts(int count) =>
            count + " attempt" + (count == 1 ? "" : "s");

        public static List<int> ParseScoreList(string csv)
        {
            var result = new List<int>();
            if (string.IsNullOrWhiteSpace(csv)) return result;
            foreach (string token in csv.Split(','))
                if (int.TryParse(token.Trim(), out int v)) result.Add(v);
            return result;
        }

        public static bool TryParseAverage(string csv, out int total, out int wins)
        {
            total = wins = 0;
            if (string.IsNullOrWhiteSpace(csv)) return false;
            string[] parts = csv.Split(',');
            return parts.Length == 2
                && int.TryParse(parts[0].Trim(), out total)
                && int.TryParse(parts[1].Trim(), out wins);
        }
    }
}
