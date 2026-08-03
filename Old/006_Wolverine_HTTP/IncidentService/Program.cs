using FluentValidation;
using IncidentService;
using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Core;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using JasperFx.Resources;
using Marten;
using Oakton;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Marten;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddAuthentication("Test");
builder.Services.AddAuthorization();

builder.Services.AddMarten(opts =>
    {
        // You always have to tell Marten what the connection string to the underlying
        // PostgreSQL database is, but this is the only mandatory piece of 
        // configuration
        var connectionString = builder.Configuration.GetConnectionString("postgres");
        opts.Connection(connectionString);

        // so that Marten will "know" how to project events to the Incident
        // projected view
        opts.Projections.Add<IncidentProjection>(ProjectionLifecycle.Async);

        opts.Events.MetadataConfig.CausationIdEnabled = true;
        opts.Events.MetadataConfig.CorrelationIdEnabled = true;
    })


    // This is a mild optimization
    .UseLightweightSessions()

    // This adds middleware support for Marten as well as the 
    // transactional middleware support we'll introduce in a little bit...
    .IntegrateWithWolverine(opts =>
    {
        // Send Marten events to any subscribers as fast as you can!
        //opts.UseFastEventForwarding = true;
        
        
        // Wolverine helps distribute work
        opts.UseWolverineManagedEventSubscriptionDistribution = true;
        
        // Setting up a little transformation of an event with event metadata to an internal command message
        opts.SubscribeToEvent<IncidentCategorised>().TransformedTo(e => new TryAssignPriority
        {
            IncidentId = e.StreamId,
            UserId = e.Data.UserId
        });
    });

    // Option #1: Publish the events to Wolverine in strict order
    // .PublishEventsToWolverine("PriorityAssignments", r =>
    // {
    //     r.PublishEvent<IncidentCategorised>();
    // });
    // ;


builder.Host.UseWolverine(opts =>
{
    // If you are running on *one* node only!
    opts.Durability.Mode = DurabilityMode.Solo;
    
    opts.Policies.AutoApplyTransactions();
    
    // Applies a transactional inbox/outbox on 
    // local queues
    opts.Policies.UseDurableLocalQueues();
    
    // Apply the validation middleware *and* discover and register
    // Fluent Validation validators
    opts.UseFluentValidation();
    
    opts.LocalQueueFor<TryAssignPriority>()
        // By default, local queues allow for parallel processing with a maximum
        // parallel count equal to the number of processors on the executing
        // machine, but you can override the queue to be sequential and single file
        .Sequential()

        // Or add more to the maximum parallel count!
        .MaximumParallelMessages(10)

        // Pause processing on this local queue for 1 minute if there's
        // more than 20% failures for a period of 2 minutes
        .CircuitBreaker(cb =>
        {
            cb.PauseTime = 1.Minutes();
            cb.SamplingPeriod = 2.Minutes();
            cb.FailurePercentageThreshold = 20;
            
            // Definitely worry about this type of exception
            cb.Include<TimeoutException>();
            
            // Don't worry about this type of exception
            cb.Exclude<InvalidInputThatCouldNeverBeProcessedException>();
        });

    opts.ListenToPostgresqlQueue("one").ListenWithStrictOrdering();
    opts.ListenToPostgresqlQueue("two").ListenWithStrictOrdering();
    opts.ListenToPostgresqlQueue("three").ListenWithStrictOrdering();
});

// Depending on your DevOps setup and policies,
// you may or may not actually want this enabled
// in production installations, but some folks do
if (builder.Environment.IsDevelopment())
{
    // This will direct our application to set up
    // all known "stateful resources" at application bootstrapping
    // time
    builder.Services.AddResourceSetupOnStartup();
}

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddWolverineHttp();

builder.Services.CritterStackDefaults(x =>
{
    x.Development.GeneratedCodeMode = TypeLoadMode.Dynamic;
    x.Production.GeneratedCodeMode = TypeLoadMode.Static;

    x.Production.ResourceAutoCreate = AutoCreate.None;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapWolverineEndpoints(opts =>
{
    // Direct Wolverine.HTTP to use Fluent Validation
    // middleware to validate any request bodies where
    // there's a known validator (or many validators)
    opts.UseFluentValidationProblemDetailMiddleware();
    
    // Creates a User object in HTTP requests based on
    // the "user-id" claim
    opts.AddMiddleware(typeof(UserDetectionMiddleware));
    
    opts.TenantId
        .IsQueryStringValue("tenantid") ;
});

// Opt into JasperFx command line usage for quite a few
// diagnostics and utilities around Marten & Wolverine
return await app.RunJasperFxCommands(args);


// This is necessary for the Alba specification we'll
// do shortly
public partial class Program
{
}

