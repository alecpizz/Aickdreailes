namespace Engine;
public static class Program
{
    public static void Main(string[] args)
    {
        Engine engine = new Engine();
        engine.Run();
        engine.Cleanup();
    }
}