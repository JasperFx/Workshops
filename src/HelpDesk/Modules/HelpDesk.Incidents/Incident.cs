using HelpDesk.Contracts;
using JasperFx.Events;

namespace HelpDesk.Incidents;

#region sample_helpdesk_incident_aggregate
/// <summary>
/// The write model. Everything a command handler needs to decide what happens
/// next, and nothing else.
/// </summary>
public record Incident(
    Guid Id,
    Guid CustomerId,
    IncidentStatus Status,
    IncidentNote[] Notes,
    IncidentCategory? Category = null,
    IncidentPriority? Priority = null,
    Guid? AgentId = null)
{
    public static Incident Create(IEvent<IncidentLogged> logged) =>
        new(logged.StreamId, logged.Data.CustomerId, IncidentStatus.Pending, []);

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
