using EventSourcingDemo;
using JasperFx.Core;
using Marten;
using Marten.Events.Projections;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Spectre.Console;

// From the docker compose file
var connectionString = "Host=localhost;Port=5432;Database=marten_testing;Username=postgres;password=postgres";
await using var store = DocumentStore.For(opts =>
{
    opts.Connection(connectionString);

    opts.Projections.Snapshot<Incident>(SnapshotLifecycle.Async);
});

await using var session = store.LightweightSession();

var contact = new Contact(ContactChannel.Email, "Han", "Solo");
var userId = Guid.NewGuid();

// Sequential Guid
var incidentId = session.Events.StartStream<Incident>(
    new IncidentLogged(Guid.NewGuid(), contact, "Software is crashing",userId),
    new IncidentCategorised
    {
        Category = IncidentCategory.Database,
        UserId = userId
    }
    
).Id;

using var daemon = await store.BuildProjectionDaemonAsync();
await daemon.StartAllAsync();

await session.SaveChangesAsync();

session.Events.Append(incidentId, new IncidentPrioritised(IncidentPriority.High, userId));
await session.SaveChangesAsync();


// JSON junk
var settings = new JsonSerializerSettings
{
    Formatting = Formatting.Indented
};
settings.Converters.Add(new StringEnumConverter());


AnsiConsole.WriteLine();
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[green]The persisted event data:[/]");

var events = await session.Events.FetchStreamAsync(incidentId);
foreach (var e in events)
{
    Console.WriteLine(JsonConvert.SerializeObject(e, settings));
}


AnsiConsole.WriteLine();
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[green]The current state of the new incident:[/]");

// Skipping ahead here, but let's load the current state of the Incident
// by using a Marten "Live" aggregation

var stream = await session.Events.FetchForWriting<Incident>(incidentId);
var incident = stream.Aggregate;

//var incident = await session.LoadAsync<Incident>(incidentId);


//var incident = await session.Events.AggregateStreamAsync<Incident>(incidentId);

var incidentVersion = (await session.Events.FetchStreamStateAsync(incidentId)).Version;

// In your application
var openIssues = await session
        
        // USe with caution!
    .QueryForNonStaleData<Incident>(15.Seconds())
    .Where(x => x.Status == IncidentStatus.Pending)
    .ToListAsync();


// reliable "Assert"
await daemon.WaitForNonStaleData(10.Seconds());

// Do your "Act"

// force the daemon to completely catch up
await daemon.CatchUpAsync(CancellationToken.None);

Console.WriteLine(JsonConvert.SerializeObject(incident, settings));

