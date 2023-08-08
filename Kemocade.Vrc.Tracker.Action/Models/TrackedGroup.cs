namespace Kemocade.Vrc.Tracker.Action.Models;

internal readonly record struct TrackedGroup
{
    public required TrackedGroupRole[] Roles { get; init; }

    public required TrackedGroupMember[] Members { get; init; }
}