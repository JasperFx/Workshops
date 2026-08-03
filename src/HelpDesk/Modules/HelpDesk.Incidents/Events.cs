using HelpDesk.Contracts;

namespace HelpDesk.Incidents;

#region sample_incident_domain_events
// Domain events. These are internal to the Incidents module and are free to
// change as the domain model changes - no other module deserialises them.
//
// Contrast with HelpDesk.Contracts.IntegrationEvents, which cross the boundary
// and therefore cannot change freely.
public record IncidentLogged(
    Guid CustomerId,
    Contact Contact,
    string Description,
    Guid LoggedBy);

public record IncidentCategorised(IncidentCategory Category, Guid CategorisedBy);

public record IncidentPrioritised(IncidentPriority Priority, Guid PrioritisedBy);

public record AgentAssignedToIncident(Guid AgentId);

public record AgentRespondedToIncident(Guid AgentId, string Content, bool VisibleToCustomer);

public record CustomerRespondedToIncident(Guid CustomerId, string Content);

public record IncidentResolved(ResolutionType Resolution, Guid ResolvedBy);

public record ResolutionAcknowledgedByCustomer(Guid AcknowledgedBy);

public record IncidentClosed(Guid ClosedBy);
#endregion

public record Contact(
    ContactChannel Channel,
    string? FirstName = null,
    string? LastName = null,
    string? EmailAddress = null,
    string? PhoneNumber = null);

public enum ContactChannel
{
    Email,
    Phone,
    InPerson,
    GeneratedBySystem
}

public enum IncidentStatus
{
    Pending = 1,
    Resolved = 8,
    ResolutionAcknowledgedByCustomer = 16,
    Closed = 32
}

public enum ResolutionType
{
    Temporary,
    Permanent,
    NotAnIncident
}

public enum IncidentNoteType
{
    FromAgent,
    FromCustomer
}

public record IncidentNote(IncidentNoteType Type, Guid From, string Content, bool VisibleToCustomer);
