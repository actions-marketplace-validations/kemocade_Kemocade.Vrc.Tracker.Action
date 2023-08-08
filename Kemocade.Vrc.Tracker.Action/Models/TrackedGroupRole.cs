namespace Kemocade.Vrc.Tracker.Action.Models;

internal readonly record struct TrackedGroupRole
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int[] MemberIndexes { get; init; }
}
