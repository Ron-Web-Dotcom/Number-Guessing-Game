using System;

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
    }
}
