using System.Text.Json.Serialization;
using FluentValidation;
using Marten.Schema;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Http;
using Wolverine.Marten;
using Wolverine.Persistence;

namespace IncidentService;

public class CategoriseIncident
{
    [JsonPropertyName("Id")] [Identity] public Guid Id { get; set; }

    [JsonPropertyName("Category")] public IncidentCategory Category { get; set; }

    [JsonPropertyName("Version")]
    // This is to communicate to the server that
    // this command was issued assuming that the 
    // incident is currently at this revision
    // number
    public int Version { get; set; }

    public class Validator : AbstractValidator<CategoriseIncident>
    {
        public Validator()
        {
            RuleFor(x => x.Version).GreaterThan(0);
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}

public static class CategoriseIncidentEndpoint
{
    [WolverineBefore]
    public static ProblemDetails AssertNotClosed(Incident incident)
    {
        if (incident.Status == IncidentStatus.Closed)
        {
            return new ProblemDetails { Status = 400, Detail = "Incident is already closed" };
        }

        // All good, keep going!
        return WolverineContinue.NoProblems;
    }
    
    [WolverinePost("/api/incidents/categorise")]
    // Any object in the OutgoingMessages collection will
    // be treated as a "cascading message" to be published by
    // Wolverine after the original CategoriseIncident command
    // is successfully completed
    public static (Events, OutgoingMessages) Post(
        CategoriseIncident command,
        
        [WriteAggregate(Required = true, OnMissing = OnMissing.ProblemDetailsWith400)]
        Incident existing,
        
        // This example uses some custom middleware off to the side
        // that loads the current User document from the logged
        // in ClaimsPrincipal
        User user)
    {
        if (existing.Category != command.Category)
        {
            var e = new IncidentCategorised
            {
                Category = command.Category,
                UserId = user.Id
            };

            // Send a command message to try to assign the priority
            var m = new TryAssignPriority
            {
                IncidentId = existing.Id,
                UserId = user.Id
            };

            return ([e], [m]);
        }

        // Do absolutely nothing
        return ([], []);
    }
}

public static class CategoriseIncidentHandler
{
    public static readonly Guid SystemId = Guid.NewGuid();
    
    public static (Events, OutgoingMessages) Handle(
        CategoriseIncident command,
        
        [WriteAggregate]
        Incident existing)
    {
        if (existing.Category != command.Category)
        {
            var incidentCategorised = new IncidentCategorised
            {
                Category = command.Category,
                UserId = SystemId
            };
            return ([incidentCategorised], [new TryAssignPriority { IncidentId = existing.Id }]);
        }

        // Wolverine will interpret this as "do no work"
        return ([], []);
    }
}