using Microsoft.AspNetCore.Authentication;
using System.Text.Json.Serialization;
using Marten.Services;
using Weasel.Core;
using HelpDesk.Api;
using HelpDesk.Billing;
using HelpDesk.Contracts;
using HelpDesk.Customers;
using HelpDesk.Incidents;
using JasperFx;
using JasperFx.Resources;
using Marten;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
        DevelopmentAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("postgres")!;

#region sample_helpdesk_main_store
// The main store. Two modules share it, each owning its own schema, and each
// configuring itself -- the host never reaches into a module's storage.
builder.Services.AddMarten(opts =>
    {
        opts.Connection(connectionString);

        // Store enums as strings, not ints. This matters more than it looks:
        // events are permanent, and an int-valued enum means inserting a new
        // member in the middle silently reinterprets every historical event.
        opts.UseSystemTextJsonForSerialization(enumStorage: EnumStorage.AsString);

        IncidentsModule.ConfigureMarten(opts);
        CustomersModule.ConfigureMarten(opts);
    })
    // Shares one session, and therefore one transaction, between event appends
    // and outgoing messages. This is what makes the outbox work.
    .IntegrateWithWolverine();
#endregion

#region sample_helpdesk_billing_store
// Billing gets a store of its own. Point this at a different connection string
// and Billing is on a different database, with no code changes anywhere.
builder.Services.AddMartenStore<IBillingStore>(opts =>
    {
        opts.Connection(connectionString);
        opts.DatabaseSchemaName = BillingModule.SchemaName;
    })
    .IntegrateWithWolverine();
#endregion

#region sample_helpdesk_wolverine
builder.UseWolverine(opts =>
{
    // Handlers and endpoints live in the module assemblies, not here.
    opts.Discovery.IncludeAssembly(typeof(IncidentsModule).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CustomersModule).Assembly);
    opts.Discovery.IncludeAssembly(typeof(BillingModule).Assembly);

    opts.Policies.AutoApplyTransactions();

    // Local queues are the seam between modules in-process. They are durable,
    // so a message from Customers to Incidents survives a crash.
    opts.Policies.UseDurableLocalQueues();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    opts.UseFluentValidation();

    // Prioritisation must not race with itself for a single incident.
    opts.LocalQueueFor<TryAssignPriority>().Sequential();

    var rabbit = builder.Configuration.GetSection("RabbitMq");
    opts.UseRabbitMq(factory =>
        {
            factory.HostName = rabbit["HostName"] ?? "localhost";
            factory.Port = int.Parse(rabbit["Port"] ?? "5672");
        })
        .AutoProvision();

    // The only message that leaves this process. Everything else is a local
    // queue -- and moving any of them out is a change to *this* file only.
    opts.PublishMessage<NotificationRequested>()
        .ToRabbitExchange("notifications");
});
#endregion

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddResourceSetupOnStartup();
}

// Accept and emit enums as strings over HTTP too, so the API is legible from
// curl and the request bodies match what ends up in the event store.
builder.Services.ConfigureHttpJsonOptions(json =>
{
    json.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddWolverineHttp();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Turns Marten's ConcurrencyException into a 409 instead of a 500.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConcurrencyExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapWolverineEndpoints(opts =>
{
    opts.UseFluentValidationProblemDetailMiddleware();

    // Builds a User from the "user-id" claim, or 400s the request -- but only
    // for endpoints that actually ask for a User. Applying it globally would
    // force a user claim onto customer registration, which does not need one.
    opts.AddMiddleware(
        typeof(UserDetectionMiddleware),
        chain => chain.Method.Method.GetParameters().Any(p => p.ParameterType == typeof(User)));
});

return await app.RunJasperFxCommands(args);

// Needed by the Alba test harness.
public partial class Program;
