namespace Engine;

public static class Time
{
    public static float DeltaTime;
    public static float FixedDeltaTime;
    public static float AccumulationTime;
    public static float InterpolationTime => Time.AccumulationTime / Time.FixedDeltaTime;
}