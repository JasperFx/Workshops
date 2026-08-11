using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace HelpDesk.Scheduling;

public record FaultConfiguration(FaultMode Mode, int FlakyPercentage = 40);

public record SchedulingStatus(
    FaultMode Mode,
    int Attempts,
    int Successes,
    int Failures);

#region sample_scheduling_demo_endpoints
public static class SchedulingEndpoints
{
    /// <summary>Change how the downstream service misbehaves, while running.</summary>
    [WolverinePost("/api/scheduling/faults")]
    public static IResult SetFaults(FaultConfiguration config, FaultSwitch faults)
    {
        faults.Mode = config.Mode;
        faults.FlakyPercentage = config.FlakyPercentage;
        return Results.Accepted();
    }

    /// <summary>Attempts, successes and failures since the last reset.</summary>
    [WolverineGet("/api/scheduling/status")]
    public static SchedulingStatus Status(FaultSwitch faults)
        => new(faults.Mode, faults.Attempts, faults.Successes, faults.Failures);

    [WolverinePost("/api/scheduling/reset")]
    public static IResult Reset(FaultSwitch faults)
    {
        faults.Attempts = faults.Successes = faults.Failures = 0;
        return Results.Accepted();
    }

    /// <summary>
    /// Fire a burst of scheduling requests. This is what trips the breaker --
    /// the failure percentage is measured over a window, so you need enough
    /// traffic in that window for it to mean anything.
    /// </summary>
    [WolverinePost("/api/scheduling/burst/{count}")]
    public static async Task<IResult> Burst(int count, IMessageBus bus)
    {
        for (var i = 0; i < count; i++)
        {
            await bus.PublishAsync(new ScheduleTechnician(Guid.CreateVersion7()));
        }

        return Results.Accepted();
    }
}
#endregion
