using HelpDesk.Contracts;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Http;
using Wolverine.Marten;
using Wolverine.Persistence;

namespace HelpDesk.Incidents;

public record ResolveIncident(ResolutionType Resolution);

public static class ResolveIncidentEndpoint
{
    [WolverinePost("/api/incidents/{id}/resolve")]
    public static (Events, OutgoingMessages) Post(
        ResolveIncident command,
        [WriteAggregate("id", Required = true, OnMissing = OnMissing.ProblemDetailsWith404)]
        Incident incident,
        User user)
    {
        if (incident.Status is IncidentStatus.Resolved or IncidentStatus.Closed) return ([], []);

        return ([new IncidentResolved(command.Resolution, user.Id)], []);
    }
}

/// <summary>
/// This carries no data the handler needs, but an endpoint still wants an
/// explicit request body type: without one, Wolverine would treat the
/// <see cref="Incident"/> parameter below as the request body and refuse to
/// compile the chain, because the middleware also takes an Incident.
/// </summary>
public record CloseIncident(string? Comment = null);

#region sample_close_incident
public static class CloseIncidentEndpoint
{
    [WolverineBefore]
    public static ProblemDetails AssertResolved([ReadAggregate("id")] Incident incident)
    {
        return incident.Status is IncidentStatus.Pending
            ? new ProblemDetails { Status = 400, Detail = "Resolve the incident before closing it" }
            : WolverineContinue.NoProblems;
    }

    [WolverinePost("/api/incidents/{id}/close")]
    public static (Events, OutgoingMessages) Post(
        CloseIncident command,
        [WriteAggregate("id", Required = true, OnMissing = OnMissing.ProblemDetailsWith404)]
        Incident incident,
        User user)
    {
        if (incident.Status == IncidentStatus.Closed) return ([], []);

        // A domain event stays inside the module. An integration event goes out
        // to whoever is listening - here, Billing. Publishing IncidentClosed
        // itself would couple Billing to this module's internal model.
        return (
            [new IncidentClosed(user.Id)],
            [new IncidentClosedForBilling(
                incident.Id,
                incident.CustomerId,
                incident.Category,
                incident.Priority,
                DateTimeOffset.UtcNow)]
        );
    }
}
#endregion
