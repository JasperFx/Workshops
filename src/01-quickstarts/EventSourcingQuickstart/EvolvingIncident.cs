using JasperFx.Events;

namespace EventSourcingQuickstart;

#region sample_incident_aggregate_evolve
// A third option: one Evolve() method instead of Create()/Apply() overloads.
// All the folding logic lives in a single switch, which some teams much prefer
// to hunting through overloads -- and it gives you an obvious place to decide
// what happens for an event you don't recognise.
//
// Marten's source generator finds this by convention and builds the evolver at
// compile time. No runtime reflection, and it works under AOT.
public record EvolvingIncident(
    Guid Id,
    Guid CustomerId,
    IncidentStatus Status,
    IncidentCategory? Category = null,
    IncidentPriority? Priority = null,
    Guid? AgentId = null)
{
    public EvolvingIncident? Evolve(IEvent e) => e.Data switch
    {
        IncidentLogged logged =>
            new EvolvingIncident(e.StreamId, logged.CustomerId, IncidentStatus.Pending),

        IncidentCategorised categorised => this with { Category = categorised.Category },

        IncidentPrioritised prioritised => this with { Priority = prioritised.Priority },

        AgentAssignedToIncident assigned => this with { AgentId = assigned.AgentId },

        // Note the discards. A bare type pattern without one is *not* picked up
        // by the source generator, and the event silently never reaches Evolve.
        IncidentResolved _ => this with { Status = IncidentStatus.Resolved },

        ResolutionAcknowledgedByCustomer _ =>
            this with { Status = IncidentStatus.ResolutionAcknowledgedByCustomer },

        IncidentClosed _ => this with { Status = IncidentStatus.Closed },

        // Anything else leaves the snapshot alone.
        _ => this
    };
}
#endregion
