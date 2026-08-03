using HelpDesk.Contracts;
using Marten;
using Wolverine;
using Wolverine.Marten;

namespace HelpDesk.Incidents;

/// <summary>
/// The <c>IncidentId</c> name is not incidental: Wolverine matches it to the
/// <see cref="Incident"/> aggregate by convention, so the handler below does
/// not have to say where the id comes from.
/// </summary>
public record TryAssignPriority(Guid IncidentId, Guid UserId);

#region sample_try_assign_priority
public static class TryAssignPriorityHandler
{
    // Wolverine calls this before the real handler and works out that the
    // result should be passed in below. All the database access lives here,
    // isolated, so the decision below stays a pure function.
    public static Task<CustomerPriorityRules?> LoadAsync(Incident incident, IDocumentSession session)
        => session.LoadAsync<CustomerPriorityRules>(incident.CustomerId);

    public static (Events, OutgoingMessages) Handle(
        TryAssignPriority command,
        [WriteAggregate] Incident incident,
        CustomerPriorityRules? rules)
    {
        var priority = rules?.PriorityFor(incident.Category);

        if (priority is null || incident.Priority == priority) return ([], []);

        var messages = new OutgoingMessages();

        // Critical incidents wake somebody up. The Incidents module does not
        // know or care how - that is the Notifications module's problem, and
        // it is running in another process.
        if (priority == IncidentPriority.Critical)
        {
            messages.Add(new NotificationRequested(
                incident.CustomerId,
                NotificationChannel.Pager,
                $"Critical incident {incident.Id}",
                "A critical incident has been raised and needs an owner now."));
        }

        return ([new IncidentPrioritised(priority.Value, command.UserId)], messages);
    }
}
#endregion
