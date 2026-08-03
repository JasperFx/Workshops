using JasperFx.Events;

namespace EventSourcingQuickstart;

#region sample_incident_aggregate
// The "write model": everything a command handler needs to decide what
// happens next, and nothing else. It is derived from events, never stored
// by the application directly.
public record Incident(
    Guid Id,
    Guid CustomerId,
    IncidentStatus Status,
    IncidentNote[] Notes,
    IncidentCategory? Category = null,
    IncidentPriority? Priority = null,
    Guid? AgentId = null)
{
    // Create() builds the aggregate from the first event of the stream.
    // Taking IEvent<T> instead of the bare event gives us the stream id
    // and all of Marten's event metadata.
    public static Incident Create(IEvent<IncidentLogged> logged) =>
        new(logged.StreamId, logged.Data.CustomerId, IncidentStatus.Pending, []);

    // Apply() folds each subsequent event into the current state.
    public Incident Apply(IncidentCategorised e) => this with { Category = e.Category };

    public Incident Apply(IncidentPrioritised e) => this with { Priority = e.Priority };

    public Incident Apply(AgentAssignedToIncident e) => this with { AgentId = e.AgentId };

    public Incident Apply(AgentRespondedToIncident e) => this with
    {
        Notes = [.. Notes, new IncidentNote(IncidentNoteType.FromAgent, e.AgentId, e.Content, e.VisibleToCustomer)]
    };

    public Incident Apply(CustomerRespondedToIncident e) => this with
    {
        Notes = [.. Notes, new IncidentNote(IncidentNoteType.FromCustomer, e.CustomerId, e.Content, true)]
    };

    public Incident Apply(IncidentResolved _) => this with { Status = IncidentStatus.Resolved };

    public Incident Apply(ResolutionAcknowledgedByCustomer _) =>
        this with { Status = IncidentStatus.ResolutionAcknowledgedByCustomer };

    public Incident Apply(IncidentClosed _) => this with { Status = IncidentStatus.Closed };
}
#endregion
