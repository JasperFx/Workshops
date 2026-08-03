namespace HelpDesk.Contracts;

#region sample_integration_events
// Integration events are a *deliberate* contract between modules, and they are
// versioned like a public API. Compare these to the domain events inside the
// Incidents module, which change whenever the domain model changes.
//
// The rule: a module publishes integration events. It never publishes its own
// domain events across a boundary.

/// <summary>Published by the Customers module when a new customer is registered.</summary>
public record CustomerRegistered(Guid CustomerId, string Name, string? Region);

/// <summary>
/// Published by the Customers module whenever a customer's automatic
/// prioritisation rules change. The Incidents module keeps its own copy.
/// </summary>
public record CustomerPrioritiesChanged(
    Guid CustomerId,
    Dictionary<IncidentCategory, IncidentPriority> Priorities);

/// <summary>Published by the Incidents module when an incident is closed.</summary>
public record IncidentClosedForBilling(
    Guid IncidentId,
    Guid CustomerId,
    IncidentCategory? Category,
    IncidentPriority? Priority,
    DateTimeOffset ClosedAt);

/// <summary>A request to tell a human something. Handled out of process.</summary>
public record NotificationRequested(
    Guid CustomerId,
    NotificationChannel Channel,
    string Subject,
    string Body);
#endregion

public enum NotificationChannel
{
    Email,
    Sms,
    Pager
}

#region sample_shared_vocabulary
// Shared vocabulary. These live in Contracts because they appear on the wire,
// so changing one is a breaking change for every module at once. Keep this
// list as small as you can stand.
public enum IncidentCategory
{
    Software,
    Hardware,
    Network,
    Database
}

public enum IncidentPriority
{
    Critical,
    High,
    Medium,
    Low
}
#endregion
