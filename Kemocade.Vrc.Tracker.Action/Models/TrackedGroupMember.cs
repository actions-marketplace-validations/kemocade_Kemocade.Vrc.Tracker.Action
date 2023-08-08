namespace Kemocade.Vrc.Tracker.Action.Models;

internal readonly record struct TrackedGroupMember
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int[] RoleIndexes { get; init; }
}
