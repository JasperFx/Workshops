using System.Text.Json;
using DocumentQuickstart;
using Marten;
using Spectre.Console;

#region sample_document_bootstrapping
// The only mandatory piece of configuration is the connection string.
await using var store = DocumentStore.For(opts =>
{
    opts.Connection(ConnectionSource.ConnectionString);

    // Keep this demo's tables out of the way of the other samples.
    opts.DatabaseSchemaName = "quickstart";
});
#endregion

#region sample_document_storing
var customer = new Customer
{
    Name = "Wilt Chamberlain",
    Region = "West Coast",
    Contract = new ContractDuration(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1)),
    Priorities =
    {
        [IncidentCategory.Database] = IncidentPriority.Critical,
        [IncidentCategory.Network] = IncidentPriority.High
    },
    Contacts = [new ContactMethod(ContactChannel.Email, "wilt@example.com")]
};

// IDocumentSession is Marten's unit of work. Nothing has hit the
// database yet -- Store() only queues the change.
await using var session = store.LightweightSession();
session.Store(customer);

// *This* is the round trip, and it is one transaction.
await session.SaveChangesAsync();
#endregion

#region sample_document_loading
// Store() assigned the identity for us, so we can load it straight back.
var loaded = await session.LoadAsync<Customer>(customer.Id);
#endregion

#region sample_document_querying
// LINQ, translated to SQL against the JSONB body. No SQL written by hand,
// and no schema migration was required to make this queryable.
var westCoast = await session
    .Query<Customer>()
    .Where(x => x.Region == "West Coast")
    .OrderBy(x => x.Name)
    .ToListAsync();

// Query into a nested collection just as easily.
var reachableByEmail = await session
    .Query<Customer>()
    .Where(x => x.Contacts.Any(c => c.Channel == ContactChannel.Email))
    .ToListAsync();
#endregion

AnsiConsole.MarkupLine($"[green]Stored and reloaded[/] {loaded!.Name}");
AnsiConsole.MarkupLine($"[green]West coast customers:[/] {westCoast.Count}");
AnsiConsole.MarkupLine($"[green]Reachable by email:[/] {reachableByEmail.Count}");
AnsiConsole.WriteLine();

AnsiConsole.MarkupLine("[grey]The document as Marten stored it:[/]");
Console.WriteLine(JsonSerializer.Serialize(loaded, new JsonSerializerOptions { WriteIndented = true }));
