using JasperFx;
using Marten;
using Marten.Linq.SoftDeletes;
using Marten.Patching;
using Marten.Schema;
using Marten.Schema.Indexing.Unique;
using Marten.Storage;
using Newtonsoft.Json;
using Spectre.Console;

var connectionString = "Host=localhost;Port=5432;Database=marten_testing;Username=postgres;password=postgres";
await using var store = DocumentStore.For(m =>
{
    m.Connection(connectionString);
    m.DatabaseSchemaName = "customers";

    m.Schema.For<Customer>();
    
    
});

var customer = new Customer
{
    Duration = new ContractDuration(new DateOnly(2023, 12, 1), new DateOnly(2024, 12, 1)),
    Region = "West Coast",
    Priorities = new Dictionary<IncidentCategory, IncidentPriority>
    {
        { IncidentCategory.Database, IncidentPriority.High }
    }, FullName = "Wilt Chamberlain",
    Age = 20
};

// IDocumentSession is Marten's unit of work 
await using var session = store.LightweightSession("tenant1");
session.Store(customer);

await session.SaveChangesAsync();

// var customerTwo = new Customer
// {
//     Duration = new ContractDuration(new DateOnly(2023, 12, 1), new DateOnly(2024, 12, 1)),
//     Region = "East Coast",
//     Priorities = new Dictionary<IncidentCategory, IncidentPriority>
//     {
//         { IncidentCategory.Database, IncidentPriority.High }
//     }, FullName = "Wilt Chamberlain",
//     Age = 20
// };
//
// using (var session2 = store.LightweightSession("two"))
// {
//     session2.ForTenant("one").Store(customerTwo);
//     await session2.SaveChangesAsync();
// }

// Only gives you not deleted customers
var activeCustomers = await session
    .Query<Customer>()
    .Where(x => x.Age > 40)
    .ToListAsync();

//AnsiConsole.MarkupLine($"[green]Number of active customers for tenant one is {activeCustomers.Count}[/]");

// Marten assigned an identity for us on Store(), so 
// we'll use that to load another copy of what was 
// just saved
var customer2 = await session.LoadAsync<Customer>(customer.Id);

// Just making a pretty JSON printout
Console.WriteLine(JsonConvert.SerializeObject(customer2, Formatting.Indented));

public class Customer
{
    public Guid Id { get; set; }

    // We'll use this later for some "logic" about how incidents
    // can be automatically prioritized
    public Dictionary<IncidentCategory, IncidentPriority> Priorities { get; set; }
        = new();

    public string? Region { get; set; }

    public ContractDuration Duration { get; set; }
    public string FullName { get; set; }
    public int Age { get; set; }
}

public record ContractDuration(DateOnly Start, DateOnly End);

public enum IncidentCategory
{
    Software,
    Hardware,
    Network,
    Database
}

public enum IncidentPriority
{
    Critical,
    High,
    Medium,
    Low
}