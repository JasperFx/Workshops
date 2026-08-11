using JasperFx.Core;
using Microsoft.Extensions.Logging;
using Wolverine.Attributes;
using Wolverine.Configuration;
using Wolverine.ErrorHandling;
using Wolverine.Runtime.Handlers;

namespace HelpDesk.Scheduling;

public record ScheduleTechnician(Guid IncidentId);

public record TechnicianAssigned(Guid IncidentId, Guid TechnicianId);

#region sample_scheduling_error_policies
public static class ScheduleTechnicianHandler
{
    /// <summary>
    /// Wolverine finds this by convention and applies the policies to this
    /// handler only. Order matters -- the first rule that matches an exception
    /// wins, so the specific cases come before OnAnyException.
    /// </summary>
    public static void Configure(HandlerChain chain)
    {
        // A blip. Try again almost immediately, then a little slower, then
        // give up and let a human see it.
        chain.OnException<TechnicianOfflineException>()
            .RetryWithCooldown(50.Milliseconds(), 250.Milliseconds(), 1.Seconds())
            .Then.MoveToErrorQueue();

        // The whole downstream service is down. Retrying now just burns the
        // database and fills the logs -- stop the queue for a while instead.
        chain.OnException<SchedulingServiceDownException>()
            .PauseThenRequeue(30.Seconds());

        // This can never succeed. Retrying is not resilience, it is denial.
        chain.OnException<InvalidSchedulingRequestException>()
            .Discard();

        // Anything we did not anticipate is, by definition, worth a human.
        chain.OnAnyException()
            .MoveToErrorQueue();
    }

    #region sample_cancellation_token_handler
    // Five seconds for this handler specifically, overriding the fleet default.
    [MessageTimeout(5)]

    // The CancellationToken is not decoration. Wolverine cancels it when the
    // message times out, and the handler is only actually interruptible if it
    // passes the token all the way down to whatever is blocking.
    public static async Task<TechnicianAssigned> Handle(
        ScheduleTechnician command,
        ITechnicianService technicians,
        ILogger<ScheduleTechnician> logger,
        CancellationToken token)
    {
        logger.LogInformation("Scheduling a technician for incident {IncidentId}", command.IncidentId);

        var technicianId = await technicians.AssignAsync(command.IncidentId, token);

        return new TechnicianAssigned(command.IncidentId, technicianId);
    }
    #endregion
}
#endregion

public static class TechnicianAssignedHandler
{
    public static void Handle(TechnicianAssigned message, ILogger<TechnicianAssigned> logger)
        => logger.LogInformation("Technician {TechnicianId} confirmed for incident {IncidentId}",
            message.TechnicianId, message.IncidentId);
}
