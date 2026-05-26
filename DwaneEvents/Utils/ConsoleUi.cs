using System;

namespace DwaneEvents.Utils
{
    /// <summary>Thin helpers so menu code stays readable.</summary>
    public static class ConsoleUi
    {
        public static void Header(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"  {title}");
            Console.WriteLine(new string('═', 60));
            Console.ResetColor();
        }

        public static void Success(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {msg}");
            Console.ResetColor();
        }

        public static void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ {msg}");
            Console.ResetColor();
        }

        public static void Warning(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ {msg}");
            Console.ResetColor();
        }

        public static void Info(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"    {msg}");
            Console.ResetColor();
        }

        public static string Prompt(string label)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  {label}: ");
            Console.ResetColor();
            return Console.ReadLine()?.Trim() ?? string.Empty;
        }

        public static string PromptOptional(string label, string current)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  {label} [{current}]: ");
            Console.ResetColor();
            var input = Console.ReadLine()?.Trim() ?? string.Empty;
            return string.IsNullOrEmpty(input) ? current : input;
        }

        public static bool Confirm(string question)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"  {question} (y/n): ");
            Console.ResetColor();
            var ans = Console.ReadLine()?.Trim().ToLower();
            return ans == "y" || ans == "yes";
        }

        public static void Pause()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("\n  Press ENTER to continue...");
            Console.ResetColor();
            Console.ReadLine();
        }

        public static void Divider() => Console.WriteLine("  " + new string('─', 56));
    }
}
