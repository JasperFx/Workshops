using Microsoft.Extensions.Logging;

namespace HelpDesk.Scheduling;

public interface ITechnicianService
{
    Task<Guid> AssignAsync(Guid incidentId, CancellationToken token);
}

#region sample_fault_injecting_service
/// <summary>
/// Stands in for the third-party scheduling API every help desk eventually has
/// to integrate with, and which is never as reliable as its sales team claimed.
/// </summary>
public class FaultInjectingTechnicianService(FaultSwitch faults, ILogger<FaultInjectingTechnicianService> logger)
    : ITechnicianService
{
    public async Task<Guid> AssignAsync(Guid incidentId, CancellationToken token)
    {
        Interlocked.Increment(ref faults.Attempts);

        switch (faults.Mode)
        {
            case FaultMode.Down:
                Interlocked.Increment(ref faults.Failures);
                throw new SchedulingServiceDownException();

            case FaultMode.Invalid:
                Interlocked.Increment(ref faults.Failures);
                throw new InvalidSchedulingRequestException($"Incident {incidentId} is not schedulable");

            case FaultMode.Hang:
                // Note the token. Without honouring it this handler would hold
                // its slot until the process dies.
                await Task.Delay(TimeSpan.FromMinutes(5), token);
                break;

            case FaultMode.Flaky when Random.Shared.Next(100) < faults.FlakyPercentage:
                Interlocked.Increment(ref faults.Failures);
                throw new TechnicianOfflineException(Guid.CreateVersion7());
        }

        // The happy path still costs something.
        await Task.Delay(25, token);

        Interlocked.Increment(ref faults.Successes);
        var technicianId = Guid.CreateVersion7();

        logger.LogInformation("Assigned technician {TechnicianId} to incident {IncidentId}",
            technicianId, incidentId);

        return technicianId;
    }
}
#endregion
