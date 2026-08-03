using IncidentService;
using JasperFx;
using JasperFx.Core;
using JasperFx.Events;
using Marten;
using Marten.Events;
using Marten.Exceptions;
using Marten.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.Marten;
using Wolverine.Runtime.Handlers;

public record RingAllTheAlarms(Guid IncidentId);

public class TryAssignPriority
{
    [Identity]
    public Guid IncidentId { get; set; }
    
    public Guid UserId { get; set; }
}

public class TryAssignPriorityController : ControllerBase
{
    [HttpPost("/api/incidents/prioritise")]
    public async Task<OkResult> Post(
        TryAssignPriority command,
        IMessageBus messageBus)
    {
        // Using Wolverine as a Mediator
        await messageBus.InvokeAsync(command);
        return Ok();
    }
}

public static class IncidentCategorisedHandler
{
    // Just cascading the TryAssignPriority message
    public static TryAssignPriority Handle(IEvent<IncidentCategorised> e)
    {
        return new TryAssignPriority { IncidentId = e.StreamId, UserId = e.Data.UserId };
    }
}

public static class TryAssignPriorityHandler
{
public static async Task delegate_to_wolverine(IMessageBus bus, TryAssignPriority command)
{
    await bus.InvokeAsync(command);
}
    
    public static void Configure(HandlerChain chain)
    {
        // It's a fall through, so you would only do *one*
        // of these options!

        // It can never succeed, so just discard it instead of wasting
        // time on retries or dead letter queues
        chain.OnException<ConcurrencyException>().Discard();

        // Do some selective retries with a progressive wait
        // in between tries, and if that fails, move it to the dead
        // letter storage
        chain.OnException<ConcurrencyException>()
            .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds())
            .Then
            .MoveToErrorQueue();
        
        // Or throw it away after a few tries...
        chain.OnException<ConcurrencyException>()
            .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds())
            .Then
            .Discard();
    }
    
    // rest of the handler code...
    
    // Wolverine will call this method before the "real" Handler method,
    // and it can "magically" connect that the Customer object should be delivered
    // to the Handle() method at runtime
    public static Task<Customer?> LoadAsync(Incident details, IDocumentSession session)
    {
        return session.LoadAsync<Customer>(details.CustomerId);
    }

    // There's some database lookup at runtime, but I've isolated that above, so the
    // behavioral logic that "decides" what to do is a pure function below. 
    [AggregateHandler]
    public static (Events, OutgoingMessages) Handle(
        TryAssignPriority command, 
        Incident details,
        Customer customer)
    {
        var events = new Events();
        var messages = new OutgoingMessages();

        if (details.Category.HasValue && customer.Priorities.TryGetValue(details.Category.Value, out var priority))
        {
            if (details.Priority != priority)
            {
                events.Add(new IncidentPrioritised(priority, command.UserId));

                if (priority == IncidentPriority.Critical)
                {
                    messages.Add(new RingAllTheAlarms(command.IncidentId));
                }
            }
        }

        return (events, messages);
    }
}