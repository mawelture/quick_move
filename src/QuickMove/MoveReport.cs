namespace QuickMove;

public static class MoveReport
{
    public static string Build(IReadOnlyList<PlannedItem> plan, IReadOnlyList<MoveOutcome> outcomes)
    {
        var moved = 0;
        var renamed = 0;
        var skipped = 0;
        var failed = 0;
        string? first = null;
        var outcomeIndex = 0;
        foreach (var item in plan)
        {
            if (item.Action == MoveAction.Skip)
            {
                skipped++;
                continue;
            }

            if (item.Action == MoveAction.Fail)
            {
                failed++;
                first ??= item.Reason;
                continue;
            }

            var outcome = outcomes[outcomeIndex++];
            if (!outcome.Succeeded)
            {
                failed++;
                first ??= outcome.Error;
                continue;
            }

            moved++;
            if (item.Renamed)
                renamed++;
        }

        return MoveSummary.Format(moved, renamed, skipped, failed, first);
    }
}
