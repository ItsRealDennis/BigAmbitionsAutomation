namespace BAA.Core.Abstractions;

/// <summary>
/// Push notifications raised by the adapter's Harmony hooks (always on Unity's main thread).
/// The engine subscribes to these; they are the primary, cheap trigger for automation, with the
/// throttled heartbeat as a fallback.
/// </summary>
public interface IGameEvents
{
    event Action<DailyTickArgs>? DailyTick;
    event Action<HourTickArgs>? HourTick;
    event Action<ShelfDepletionArgs>? ShelfDepleted;
    event Action<DeliveryArgs>? DeliveryCompleted;
    event Action<EmployeeEventArgs>? EmployeeResigned;

    /// <summary>A save finished loading — reset engine state and load that save's config.</summary>
    event Action<SaveArgs>? SaveLoaded;

    /// <summary>A save is being unloaded/quit — stop automation and flush.</summary>
    event Action<SaveArgs>? SaveUnloading;
}
