using JasperFx;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Patching;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarten(opts =>
{
    // You always have to tell Marten what the connection string to the underlying
    // PostgreSQL database is, but this is the only mandatory piece of 
    // configuration
    var connectionString = builder.Configuration.GetConnectionString("postgres");
    opts.Connection(connectionString);

    opts.DisableNpgsqlLogging = true;
    
    opts.UseSystemTextJsonForSerialization(EnumStorage.AsString);

    // We have to tell Marten about the projection we built in the previous post
    // so that Marten will "know" how to project events to the IncidentDetails
    // projected view
    opts.Projections.Add<IncidentProjection>(ProjectionLifecycle.Async);

    // In production, let's turn off all the automatic database
    // migration stuff
    if (builder.Environment.IsProduction())
    {
        opts.AutoCreateSchemaObjects = AutoCreate.None;
    }
})
// Add background projection processing
.AddAsyncDaemon(DaemonMode.HotCold)
// This is a mild optimization
.UseLightweightSessions();


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();


// This is necessary for the Alba specification we'll
// do shortly
public partial class Program
{
    public static async Task manipulate_customer_data(IDocumentSession session)
    {
        var customer = new Customer
        {
            Name = "Acme",
            Region = "North America",
            Class = "first"
        };

        // Marten has "upsert", insert, and update semantics
        session.Insert(customer);

        // Partial updates to a range of Customer documents
        // by a LINQ filter
        session.Patch<Customer>(x => x.Region == "EMEA")
            .Set(x => x.Class, "First");

        // Both the above operations happen in one 
        // ACID transaction
        await session.SaveChangesAsync();

        // Because Marten is ACID compliant, this query would
        // immediately work as expected even though we made that 
        // broad patch up above and inserted a new document.
        var customers = await session.Query<Customer>()
            .Where(x => x.Class == "First")
            .Take(100)
            .ToListAsync();
    }
}