namespace QuickMove;

public sealed record MoveOutcome(bool Succeeded, string? Error)
{
    public static MoveOutcome Ok() => new(true, null);
    public static MoveOutcome Fail(string error) => new(false, error);
}
