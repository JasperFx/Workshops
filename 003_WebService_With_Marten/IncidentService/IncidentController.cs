using JasperFx.Core;
using Marten;
using Marten.AspNetCore;
using Microsoft.AspNetCore.Mvc;

public record LogIncident(
    Guid CustomerId,
    Contact Contact,
    string Description
);

public class IncidentController : ControllerBase
{
    private readonly IDocumentSession _session;

    public IncidentController(IDocumentSession session)
    {
        _session = session;
    }

    [HttpPost("/api/incidents")]
    public async Task<IResult> Log(
        [FromBody] LogIncident command)
    {
        // Let's come back to this one in a bit...
        var userId = Guid.NewGuid();
        
        var logged = new IncidentLogged(command.CustomerId, command.Contact, command.Description, userId);

        var incidentId = _session.Events.StartStream(logged).Id;
        await _session.SaveChangesAsync(HttpContext.RequestAborted);

        return Results.Created("/incidents/" + incidentId, incidentId);
    }

    [HttpGet("/api/incidents/{incidentId}")]
    public Task Get(Guid incidentId)
    {
        return _session.Json.WriteById<Incident>(incidentId, HttpContext);
    }
    
    [HttpGet("/api/incidents/{incidentId}/{time}")]
    public Task<Incident?> Get(Guid incidentId, DateTimeOffset time)
    {
        // "Time Travel"
        return _session.Events
            .AggregateStreamAsync<Incident>(incidentId, timestamp:time);
    }
    
    [HttpGet("/api/incidents/pending")]
    public Task<IReadOnlyList<Incident>> GetOpenIncidents()
    {
        return _session
                // *Wait* for the projection to catch up
            .QueryForNonStaleData<Incident>(5.Seconds())
            .Where(x => x.Status == IncidentStatus.Pending)
            .ToListAsync();
    }

}