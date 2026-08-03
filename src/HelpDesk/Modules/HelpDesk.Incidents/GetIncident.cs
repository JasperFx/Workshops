using Marten;
using Marten.AspNetCore;
using Microsoft.AspNetCore.Http;
using Wolverine.Marten;
using Wolverine.Http;
using Wolverine.Persistence;

namespace HelpDesk.Incidents;

#region sample_get_incident
public static class GetIncidentEndpoint
{
    // [ReadAggregate] hands the endpoint the current state with no concurrency
    // protection, because this is a read.
    [WolverineGet("/api/incidents/{id}")]
    public static Incident Get(
        [ReadAggregate("id", Required = true, OnMissing = OnMissing.ProblemDetailsWith404)]
        Incident incident) => incident;
}

public static class GetIncidentHistoryEndpoint
{
    // The raw events. In a CRUD system this endpoint could not exist.
    [WolverineGet("/api/incidents/{id}/history")]
    public static async Task<IResult> Get(Guid id, IQuerySession session)
    {
        var events = await session.Events.FetchStreamAsync(id);

        if (!events.Any()) return Results.NotFound();

        return Results.Ok(events.Select(e => new
        {
            e.Version,
            e.Timestamp,
            Type = e.EventTypeName,
            e.Data
        }));
    }
}
#endregion

public static class StreamIncidentEndpoint
{
    // Marten writes the stored JSON straight to the response body - no
    // deserialise-then-reserialise round trip.
    [WolverineGet("/api/incidents/{id}/fast")]
    public static Task Get(Guid id, IQuerySession session, HttpContext context)
        => session.Json.WriteById<Incident>(id, context);
}
