using EventSourcingDemo;
using JasperFx.Events;
using Marten.Events;
using Marten.Events.Aggregation;

public record Incident(
    Guid Id,
    Guid CustomerId,
    IncidentStatus Status,
    IncidentNote[] Notes,
    IncidentCategory? Category = null,
    IncidentPriority? Priority = null,
    Guid? AgentId = null,

    // This is meant to be the revision number
    // of the event stream for this incident
    int Version = 1
)
{
    public static Incident Create(IEvent<IncidentLogged> logged) =>
        new(logged.StreamId, logged.Data.CustomerId, IncidentStatus.Pending, []);

    public Incident Apply(IncidentCategorised categorised, Incident current) =>
        current with { Category = categorised.Category };

    public Incident Apply(IncidentPrioritised prioritised, Incident current) =>
        current with { Priority = prioritised.Priority };

    public Incident Apply(AgentAssignedToIncident prioritised, Incident current) =>
        current with { AgentId = prioritised.AgentId };

    public Incident Apply(IncidentResolved resolved, Incident current) =>
        current with { Status = IncidentStatus.Resolved };

    public Incident Apply(ResolutionAcknowledgedByCustomer acknowledged, Incident current) =>
        current with { Status = IncidentStatus.ResolutionAcknowledgedByCustomer };

    public Incident Apply(IncidentClosed closed, Incident current) =>
        current with { Status = IncidentStatus.Closed };
}

public record IncidentNote(
    IncidentNoteType Type,
    Guid From,
    string Content,
    bool VisibleToCustomer
);

public enum IncidentNoteType
{
    FromAgent,
    FromCustomer
}

// This class contains the directions for Marten about how to create the
// Incident view from the raw event data
public class IncidentProjection: SingleStreamProjection<Incident, Guid>
{
    public override Incident? Evolve(Incident? snapshot, Guid id, IEvent e)
    {
        return e.Data switch
        {
            IncidentLogged logged => new Incident(id, logged.CustomerId, IncidentStatus.Pending, []),
            IncidentResolved resolved => snapshot with { Status = IncidentStatus.Resolved },
            IncidentClosed closed => snapshot with { Status = IncidentStatus.Closed },
            IncidentPrioritised prioritised => snapshot with { Priority = prioritised.Priority },
            IncidentCategorised categorised => snapshot with { Category = categorised.Category },
            AgentAssignedToIncident assigned => snapshot with { AgentId = assigned.AgentId },
            ResolutionAcknowledgedByCustomer acknowledged => snapshot with
            {
                Status = IncidentStatus.ResolutionAcknowledgedByCustomer
            },
            _ => snapshot
        };
    }
}
