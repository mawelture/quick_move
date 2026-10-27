namespace QuickMove;

public enum MoveAction
{
    Move,
    Skip,
    Fail
}

public sealed record PlannedItem(
    string SourcePath,
    string? DestinationPath,
    MoveAction Action,
    string? Reason,
    bool Renamed);
