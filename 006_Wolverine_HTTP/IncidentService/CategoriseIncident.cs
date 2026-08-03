using System.Text.Json.Serialization;
using FluentValidation;
using Marten;
using Marten.Schema;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

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
    // Uglier infrastructure above...
    
    [WolverinePost("/api/incidents/categorise")]
    [AggregateHandler] // works against one stream..
    // Any object in the OutgoingMessages collection will
    // be treated as a "cascading message" to be published by
    // Wolverine after the original CategoriseIncident command
    // is successfully completed
    public static (Events, OutgoingMessages) Post(
        CategoriseIncident command,
        Incident existing,
        User user
        )
    {
        var events = new Events();
        var messages = new OutgoingMessages();

        if (existing.Category != command.Category)
        {
            events += new IncidentCategorised
            {
                Category = command.Category,
                UserId = user.Id
            };

            // Send a command message to try to assign the priority
            messages.Add(new TryAssignPriority
            {
                IncidentId = existing.Id,
                UserId = user.Id
            });
        }

        return (events, messages);
    }

    public static string RouteToQueue(Guid id)
    {
        return "one";
    }
}

public static class CallMethod
{
    public static async Task DoIt(IHost host)
    {
        // Create a new IMessageBus
        IMessageBus bus = host.MessageBus();
        
        await bus.PublishAsync(new CategoriseIncident {  }).ConfigureAwait(false);
        await bus.InvokeAsync(new CategoriseIncident { Version = 1 }).ConfigureAwait(false);
    }
}

public static class CategoriseIncidentHandler
{
    // This is just faked up
    public static readonly Guid SystemId = Guid.NewGuid();

    [AggregateHandler]
    // The object? as return value will be interpreted
    // by Wolverine as appending one or zero events
    public static (Events, OutgoingMessages) Handle(
        CategoriseIncident command,
        Incident existing)
    {
        if (existing.Category != command.Category)
        {
            var incidentCategorised = new IncidentCategorised
            {
                Category = command.Category,
                UserId = SystemId
            };
            
            // Send the message to any and all subscribers to this message
            return ([incidentCategorised], [new TryAssignPriority { IncidentId = existing.Id }]);
        }

        // Wolverine will interpret this as "do no work"
        return ([], []);
    }
}