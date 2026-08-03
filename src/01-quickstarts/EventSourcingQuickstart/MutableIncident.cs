using JasperFx.Events;

namespace EventSourcingQuickstart;

#region sample_incident_aggregate_mutable
// The exact same aggregate, written with mutable state and void Apply()
// methods. Marten supports both -- pick whichever your team argues about
// less. A parameterless constructor keeps serialization simple.
public class MutableIncident
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public IncidentStatus Status { get; set; }
    public List<IncidentNote> Notes { get; set; } = [];
    public IncidentCategory? Category { get; set; }
    public IncidentPriority? Priority { get; set; }
    public Guid? AgentId { get; set; }

    public MutableIncident()
    {
    }

    public MutableIncident(IEvent<IncidentLogged> logged)
    {
        Id = logged.StreamId;
        CustomerId = logged.Data.CustomerId;
        Status = IncidentStatus.Pending;
    }

    public void Apply(IncidentCategorised e) => Category = e.Category;

    public void Apply(IncidentPrioritised e) => Priority = e.Priority;

    public void Apply(AgentAssignedToIncident e) => AgentId = e.AgentId;

    public void Apply(IncidentResolved _) => Status = IncidentStatus.Resolved;

    public void Apply(ResolutionAcknowledgedByCustomer _) =>
        Status = IncidentStatus.ResolutionAcknowledgedByCustomer;

    public void Apply(IncidentClosed _) => Status = IncidentStatus.Closed;
}
#endregion
