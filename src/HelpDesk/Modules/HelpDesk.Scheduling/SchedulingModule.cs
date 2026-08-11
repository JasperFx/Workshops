using JasperFx.Core;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace HelpDesk.Scheduling;

public static class SchedulingModule
{
    public static void AddScheduling(this IServiceCollection services)
    {
        // Singleton so the demo can flip failure modes at runtime and every
        // handler sees it immediately.
        services.AddSingleton<FaultSwitch>();
        services.AddScoped<ITechnicianService, FaultInjectingTechnicianService>();
    }

    #region sample_scheduling_circuit_breaker
    public static void ConfigureWolverine(WolverineOptions opts)
    {
        opts.LocalQueueFor<ScheduleTechnician>()

            // Keep the demo legible -- one at a time, so the failure count
            // climbs in an order a room can follow.
            .Sequential()

            .CircuitBreaker(cb =>
            {
                // How long to stop processing once the breaker trips
                cb.PauseTime = 30.Seconds();

                // The window the failure percentage is measured over
                cb.SamplingPeriod = 10.Seconds();

                // Don't trip on the first unlucky message
                cb.MinimumThreshold = 5;

                // Trip when this share of messages in the window fail
                cb.FailurePercentageThreshold = 20;

                // A bad request is not evidence that the service is unhealthy.
                // Without this exclusion, FaultMode.Invalid would trip the
                // breaker and take down processing for messages that are fine.
                cb.Exclude<InvalidSchedulingRequestException>();
            });

        // A handler that hangs should lose its slot rather than hold it
        // forever. This is the fleet-wide default; [MessageTimeout] overrides
        // it per handler.
        opts.DefaultExecutionTimeout = 10.Seconds();
    }
    #endregion
}
