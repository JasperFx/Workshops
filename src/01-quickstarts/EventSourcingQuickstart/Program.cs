using System.Text.Json;
using System.Text.Json.Serialization;
using EventSourcingQuickstart;
using JasperFx.Events.Projections;
using Marten;
using Spectre.Console;

#region sample_es_bootstrapping
await using var store = DocumentStore.For(opts =>
{
    opts.Connection(ConnectionSource.ConnectionString);
    opts.DatabaseSchemaName = "es_quickstart";

    // Keep a snapshot of Incident up to date on every append, inside the
    // same transaction as the events. Strong consistency, slower writes.
    opts.Projections.Snapshot<Incident>(SnapshotLifecycle.Inline);
});
#endregion

await using var session = store.LightweightSession();

var customerId = Guid.NewGuid();
var agentId = Guid.NewGuid();
var contact = new Contact(ContactChannel.Email, "Han", "Solo", "han@example.com");

#region sample_es_starting_a_stream
// StartStream assigns a new stream id and queues the first events.
// Nothing is written until SaveChangesAsync.
var incidentId = session.Events.StartStream<Incident>(
    new IncidentLogged(customerId, contact, "The hyperdrive is making a noise", customerId),
    new IncidentCategorised(IncidentCategory.Hardware, customerId)
).Id;

await session.SaveChangesAsync();
#endregion

#region sample_es_appending
// Later, in some other request, append to the existing stream.
session.Events.Append(incidentId,
    new IncidentPrioritised(IncidentPriority.Critical, agentId),
    new AgentAssignedToIncident(agentId));

await session.SaveChangesAsync();
#endregion

#region sample_es_reading_raw_events
// The events are the source of truth, and you can always read them back.
var events = await session.Events.FetchStreamAsync(incidentId);

foreach (var e in events)
{
    AnsiConsole.MarkupLine(
        $"[grey]v{e.Version}[/] [yellow]{e.EventTypeName}[/] at {e.Timestamp:HH:mm:ss}");
}
#endregion

#region sample_es_live_aggregation
// "Live" aggregation: replay the stream on demand. Nothing extra is
// stored, so writes stay fast and reads pay the cost.
var live = await session.Events.AggregateStreamAsync<Incident>(incidentId);
#endregion

#region sample_es_inline_snapshot
// Because we registered an Inline snapshot above, the same state is also
// sitting in a table and can be loaded as a plain document.
var snapshot = await session.LoadAsync<Incident>(incidentId);
#endregion

#region sample_es_time_travel
// Rewind. What did this incident look like immediately after it was
// logged, before anyone categorised or prioritised it?
var asOfVersion1 = await session.Events.AggregateStreamAsync<Incident>(incidentId, version: 1);

// Or by wall clock, which is what auditors and support engineers ask for.
var asOfAnHourAgo = await session.Events
    .AggregateStreamAsync<Incident>(incidentId, timestamp: DateTimeOffset.UtcNow.AddHours(-1));
#endregion

var json = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() }
};

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[green]Live aggregation:[/]");
Console.WriteLine(JsonSerializer.Serialize(live, json));

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[green]Inline snapshot (identical state, different cost profile):[/]");
Console.WriteLine(JsonSerializer.Serialize(snapshot, json));

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[green]Rewound to version 1:[/]");
Console.WriteLine(JsonSerializer.Serialize(asOfVersion1, json));

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine(
    asOfAnHourAgo is null
        ? "[grey]An hour ago this incident did not exist yet.[/]"
        : "[grey]An hour ago this incident already existed.[/]");
