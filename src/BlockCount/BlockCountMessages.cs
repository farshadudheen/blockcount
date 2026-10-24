namespace BlockCount;

public static class BlockCountMessages
{
    public static string BelowMinimum(string name, int minimum, int count) =>
        $"{name} requires at least {minimum} {Entry(minimum)}. The current count is {count}.";

    public static string AboveMaximum(string name, int maximum, int count) =>
        $"{name} allows at most {maximum} {Entry(maximum)}. The current count is {count}.";

    public static string Exact(string name, int expected, int count) =>
        $"{name} must contain exactly {expected} {Entry(expected)}. The current count is {count}.";

    public static string MinimumAboveMaximum(string name) =>
        $"The count configuration for {name} is invalid. The minimum must be less than or equal to the maximum.";

    public static string Negative(string name) =>
        $"The count configuration for {name} is invalid. Minimum and maximum must be zero or greater.";

    public static string Entry(int count) => count == 1 ? "entry" : "entries";
}
