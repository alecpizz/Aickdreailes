namespace Engine;

public static class Logging
{
    private const ConsoleColor DefaultColor = ConsoleColor.White;
    private const ConsoleColor WarnColor = ConsoleColor.DarkYellow;
    private const ConsoleColor ErrorColor = ConsoleColor.Red;
    private const ConsoleColor SuccessColor = ConsoleColor.Green;
    public static void Log(string msg)
    {
        Console.WriteLine(msg);
    }

    public static void LogWarning(string msg)
    {
        Console.ForegroundColor = WarnColor;
        Console.WriteLine(msg);
        Console.ForegroundColor = DefaultColor;
    }

    public static void LogError(string msg)
    {
        Console.ForegroundColor = ErrorColor;
        Console.WriteLine(msg);
        Console.ForegroundColor = DefaultColor;
    }

    public static void LogSuccess(string msg)
    {
        Console.ForegroundColor = SuccessColor;
        Console.WriteLine(msg);
        Console.ForegroundColor = DefaultColor;
    }
}