using ContactManager.Application;
using ContactManager.Domain;

namespace ContactManager.Presentation;

public static class ConsoleInput
{
    public static string ReadNonEmpty(string label)
    {
        while (true)
        {
            Console.Write(label);
            var s = Console.ReadLine() ?? "";
            s = s.Trim();
            if (s.Length > 0) return s;
            Console.WriteLine("Value cannot be empty.");
        }
    }

    public static string? ReadOptional(string label)
    {
        Console.Write(label);
        var s = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim();
    }

    public static int ReadInt(string label)
    {
        while (true)
        {
            Console.Write(label);
            var s = (Console.ReadLine() ?? "").Trim();
            if (int.TryParse(s, out var v)) return v;
            Console.WriteLine("Please enter a valid number.");
        }
    }

    public static bool ReadYesNo(string label)
    {
        while (true)
        {
            Console.Write($"{label} (y/n): ");
            var s = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
            if (s is "y" or "yes") return true;
            if (s is "n" or "no") return false;
            Console.WriteLine("Please enter y or n.");
        }
    }
}