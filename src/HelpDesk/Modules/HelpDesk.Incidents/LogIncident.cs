using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Attributes;
using Wolverine.Http;
using Wolverine.Marten;

namespace HelpDesk.Incidents;

public record LogIncident(Guid CustomerId, Contact Contact, string Description);

public record NewIncidentResponse(Guid IncidentId)
    : CreationResponse("/api/incidents/" + IncidentId);

public class LogIncidentValidator : AbstractValidator<LogIncident>
{
    public LogIncidentValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
    }
}

#region sample_log_incident
public static class LogIncidentEndpoint
{
    // Runs before the endpoint below and can short-circuit the request. Note
    // that it checks the module's *own* replica, not the Customers table.
    [WolverineBefore]
    public static async Task<ProblemDetails> ValidateCustomer(
        LogIncident command,
        IDocumentSession session)
    {
        var known = await session
            .Query<CustomerPriorityRules>()
            .AnyAsync(x => x.Id == command.CustomerId);

        return known
            ? WolverineContinue.NoProblems
            : new ProblemDetails { Detail = $"Unknown customer {command.CustomerId}", Status = 400 };
    }

    [WolverinePost("/api/incidents")]
    public static (NewIncidentResponse, IStartStream) Post(LogIncident command, User user)
    {
        var logged = new IncidentLogged(
            command.CustomerId,
            command.Contact,
            command.Description,
            user.Id);

        var op = MartenOps.StartStream<Incident>(logged);

        return (new NewIncidentResponse(op.StreamId), op);
    }
}
#endregion
