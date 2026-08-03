using JasperFx.Core;
using Wolverine.ErrorHandling;
using Wolverine.Runtime.Handlers;

namespace IncidentService;

public record ScheduleTechnician(Guid IncidentId);

public record TechnicianScheduledResult(Guid? TechnicianId);

public record TechnicianScheduled(Guid IncidentId, Guid TechnicianId);

public interface ITechnicianService
{
    Task<TechnicianScheduledResult> TryScheduleAsync(ScheduleTechnician request, CancellationToken token);
}

public class TooBusyException : Exception;

public class TechnicianOfflineException : Exception;

public class ServiceOfflineException : Exception;

public class InvalidRequestException : Exception;

public class NetworkHiccupException : Exception;


public static class ScheduleTechnicianHandler
{
    // This *only* applies to this message handler
    public static void Configure(HandlerChain chain)
    {
        chain.OnException<NetworkHiccupException>()
            .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds())
            .Then.MoveToErrorQueue();

        chain.OnException<TechnicianOfflineException>() .Requeue();

        chain.OnException<ServiceOfflineException>()
            .PauseThenRequeue(30.Minutes());

        chain.OnException<InvalidRequestException>()
            .Discard();
        
        chain.OnAnyException().MoveToErrorQueue();
    }
    
    public static async Task<TechnicianScheduled?> HandleAsync(
        ScheduleTechnician message, 
        ITechnicianService service,
        CancellationToken token)
    {
        var result = await service.TryScheduleAsync(message, token);
        if (result.TechnicianId.HasValue)
        {
            return new TechnicianScheduled(message.IncidentId, result.TechnicianId.Value);
        }

        return null;
    }
}