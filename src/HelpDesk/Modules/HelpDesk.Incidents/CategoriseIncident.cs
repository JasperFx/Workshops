using FluentValidation;
using HelpDesk.Contracts;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Http;
using Wolverine.Marten;
using Wolverine.Persistence;

namespace HelpDesk.Incidents;

public class CategoriseIncident
{
    public IncidentCategory Category { get; set; }

    /// <summary>
    /// The revision the caller believed the incident was at. If someone else
    /// got there first, the append fails rather than silently overwriting a
    /// decision made against state that no longer exists.
    /// </summary>
    public int Version { get; set; }

    public class Validator : AbstractValidator<CategoriseIncident>
    {
        public Validator() => RuleFor(x => x.Version).GreaterThan(0);
    }
}

#region sample_categorise_incident
public static class CategoriseIncidentEndpoint
{
    // Runs before the endpoint below and can short-circuit the whole request.
    [WolverineBefore]
    public static ProblemDetails AssertNotClosed([ReadAggregate("id")] Incident incident)
    {
        return incident.Status == IncidentStatus.Closed
            ? new ProblemDetails { Status = 400, Detail = "Incident is already closed" }
            : WolverineContinue.NoProblems;
    }

    [WolverinePost("/api/incidents/{id}/categorise")]
    public static (Events, OutgoingMessages) Post(
        CategoriseIncident command,

        // Loads the aggregate from the {id} route argument, enforces optimistic
        // concurrency against the Version on the request body, and 404s with a
        // ProblemDetails if the stream doesn't exist.
        [WriteAggregate("id", VersionSource = nameof(CategoriseIncident.Version),
            Required = true, OnMissing = OnMissing.ProblemDetailsWith404)]
        Incident incident,

        User user)
    {
        // Wolverine reads this as "do no work" - no events, no messages,
        // no transaction.
        if (incident.Category == command.Category) return ([], []);

        return (
            [new IncidentCategorised(command.Category, user.Id)],
            [new TryAssignPriority(incident.Id, user.Id)]
        );
    }
}
#endregion
