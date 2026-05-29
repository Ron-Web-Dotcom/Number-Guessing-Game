using System;

namespace Number_Guessing
{
    class Program
    {
        const int minRange = 1;
        const int maxRange = 30;

        static readonly Random rng = new Random();

        static void Main(string[] args)
        {
            bool keepPlaying = true;

            do
            {
                int realNumber = rng.Next(minRange, maxRange + 1); // minRange to maxRange inclusive

                int guess = readIntInRange("Please guess a number between " + minRange + " and " + maxRange + ": ");
                int amountGuesses = 1;

                while (guess != realNumber)
                {
                    amountGuesses++;
                    guess = readIntInRange("You guessed wrong, try something " + (guess < realNumber ? "higher" : "lower") + ": ");
                }

                Console.WriteLine(Environment.NewLine + "You guessed right, it took you {0} attempts.", amountGuesses);

                Console.Write(Environment.NewLine + "Do you want to play again? (y/n): ");
                string playOption = Console.ReadLine() ?? string.Empty;
                if (playOption.ToLower() == "n")
                    keepPlaying = false;

                Console.WriteLine();
            } while (keepPlaying);

            Console.WriteLine("Thank you for playing this game.");

            Console.ReadLine();
        }

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
