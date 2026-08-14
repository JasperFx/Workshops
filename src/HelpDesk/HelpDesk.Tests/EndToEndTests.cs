using System.Security.Claims;
using Alba;
using HelpDesk.Billing;
using HelpDesk.Contracts;
using HelpDesk.Customers;
using HelpDesk.Incidents;
using Marten;
using Shouldly;
using Wolverine.Tracking;

namespace HelpDesk.Tests;

public class end_to_end_across_modules(AppFixture fixture) : IntegrationContext(fixture)
{
    private readonly Guid _user = Guid.CreateVersion7();

    private void AsUser(Scenario x) => x.WithClaim(new Claim("user-id", _user.ToString()));

    private async Task<Guid> RegisterCustomerWithCriticalDatabaseRule()
    {
        // Registering a customer publishes CustomerRegistered, which the
        // Incidents module handles to seed its own replica. Wait for that
        // cascade to finish before continuing.
        var (_, created) = await TrackedHttpCall(x =>
        {
            x.Post.Json(new RegisterCustomer("Rebel Alliance", "Outer Rim")).ToUrl("/api/customers");
            x.StatusCodeShouldBe(201);
        });

        var customerId = created.ReadAsJson<CustomerRegisteredResponse>()!.CustomerId;

        await TrackedHttpCall(x =>
        {
            x.Post.Json(new SetCustomerPriorities(customerId, new()
            {
                [IncidentCategory.Database] = IncidentPriority.Critical
            })).ToUrl("/api/customers/priorities");
            x.StatusCodeShouldBe(204);
        });

        return customerId;
    }

    #region sample_cross_module_test
    [Fact]
    public async Task customer_rules_propagate_to_the_incidents_module()
    {
        var customerId = await RegisterCustomerWithCriticalDatabaseRule();

        // The Incidents module now holds its own replica, built entirely from
        // integration events. It never read the Customers table.
        await using var session = Store.LightweightSession();
        var rules = await session.LoadAsync<CustomerPriorityRules>(customerId);

        rules.ShouldNotBeNull();
        rules.PriorityFor(IncidentCategory.Database).ShouldBe(IncidentPriority.Critical);
    }
    #endregion

    [Fact]
    public async Task rejects_an_incident_for_an_unknown_customer()
    {
        await Scenario(x =>
        {
            AsUser(x);

            x.Post.Json(new LogIncident(
                    Guid.CreateVersion7(),
                    new Contact(ContactChannel.Email, "Han", "Solo"),
                    "Hyperdrive is making a noise"))
                .ToUrl("/api/incidents");

            x.StatusCodeShouldBe(400);
            x.ContentTypeShouldBe("application/problem+json");
        });
    }

    #region sample_full_lifecycle_test
    [Fact]
    public async Task full_incident_lifecycle_reaches_billing()
    {
        var customerId = await RegisterCustomerWithCriticalDatabaseRule();

        // Log
        var logged = await Scenario(x =>
        {
            AsUser(x);
            x.Post.Json(new LogIncident(
                    customerId,
                    new Contact(ContactChannel.Email, "Han", "Solo"),
                    "Hyperdrive is making a noise"))
                .ToUrl("/api/incidents");
            x.StatusCodeShouldBe(201);
        });

        var incidentId = logged.ReadAsJson<NewIncidentResponse>()!.IncidentId;

        // Categorise. This cascades TryAssignPriority, which finds the Critical
        // rule and asks for somebody to be paged.
        var (tracked, _) = await TrackedHttpCall(x =>
        {
            AsUser(x);
            x.Post.Json(new CategoriseIncident { Category = IncidentCategory.Database, Version = 1 })
                .ToUrl($"/api/incidents/{incidentId}/categorise");
            x.StatusCodeShouldBe(204);
        });

        tracked.Executed.SingleMessage<TryAssignPriority>().ShouldNotBeNull();

        // Would have gone to Rabbit MQ in production; the external transports
        // are stubbed, so we can assert on it here instead.
        tracked.Sent.SingleMessage<NotificationRequested>()
            .Channel.ShouldBe(NotificationChannel.Pager);

        // Resolve, then close. Closing publishes to Billing.
        await TrackedHttpCall(x =>
        {
            AsUser(x);
            x.Post.Json(new ResolveIncident(ResolutionType.Permanent))
                .ToUrl($"/api/incidents/{incidentId}/resolve");
            x.StatusCodeShouldBe(204);
        });

        await TrackedHttpCall(x =>
        {
            AsUser(x);
            x.Post.Json(new CloseIncident()).ToUrl($"/api/incidents/{incidentId}/close");
            x.StatusCodeShouldBe(204);
        });

        // Billing lives in its own store and learned about all of this from a
        // single integration event.
        await using var billing = BillingStore.LightweightSession();
        var items = await billing.Query<BillableItem>()
            .Where(x => x.CustomerId == customerId)
            .ToListAsync();

        var item = items.ShouldHaveSingleItem();
        item.IncidentId.ShouldBe(incidentId);
        item.Priority.ShouldBe(IncidentPriority.Critical);
        item.Amount.ShouldBe(500m);
    }
    #endregion

    #region sample_optimistic_concurrency_test
    [Fact]
    public async Task rejects_a_categorise_against_a_stale_version()
    {
        var customerId = await RegisterCustomerWithCriticalDatabaseRule();

        var logged = await Scenario(x =>
        {
            AsUser(x);
            x.Post.Json(new LogIncident(
                    customerId, new Contact(ContactChannel.Phone), "Printer is on fire"))
                .ToUrl("/api/incidents");
            x.StatusCodeShouldBe(201);
        });

        var incidentId = logged.ReadAsJson<NewIncidentResponse>()!.IncidentId;

        await TrackedHttpCall(x =>
        {
            AsUser(x);
            x.Post.Json(new CategoriseIncident { Category = IncidentCategory.Hardware, Version = 1 })
                .ToUrl($"/api/incidents/{incidentId}/categorise");
            x.StatusCodeShouldBe(204);
        });

        // A second caller still holding version 1 tries to categorise. The
        // stream has moved on, so this must not silently overwrite.
        await Scenario(x =>
        {
            AsUser(x);
            x.Post.Json(new CategoriseIncident { Category = IncidentCategory.Network, Version = 1 })
                .ToUrl($"/api/incidents/{incidentId}/categorise");
            x.StatusCodeShouldBe(409);
        });
    }
    #endregion

    [Fact]
    public async Task cannot_close_an_incident_that_was_never_resolved()
    {
        var customerId = await RegisterCustomerWithCriticalDatabaseRule();

        var logged = await Scenario(x =>
        {
            AsUser(x);
            x.Post.Json(new LogIncident(customerId, new Contact(ContactChannel.Phone), "Broken"))
                .ToUrl("/api/incidents");
            x.StatusCodeShouldBe(201);
        });

        var incidentId = logged.ReadAsJson<NewIncidentResponse>()!.IncidentId;

        await Scenario(x =>
        {
            AsUser(x);
            x.Post.Json(new CloseIncident()).ToUrl($"/api/incidents/{incidentId}/close");
            x.StatusCodeShouldBe(400);
        });
    }

    [Fact]
    public async Task exposes_the_full_event_history()
    {
        var customerId = await RegisterCustomerWithCriticalDatabaseRule();

        var logged = await Scenario(x =>
        {
            AsUser(x);
            x.Post.Json(new LogIncident(customerId, new Contact(ContactChannel.Phone), "Broken"))
                .ToUrl("/api/incidents");
            x.StatusCodeShouldBe(201);
        });

        var incidentId = logged.ReadAsJson<NewIncidentResponse>()!.IncidentId;

        await TrackedHttpCall(x =>
        {
            AsUser(x);
            x.Post.Json(new CategoriseIncident { Category = IncidentCategory.Database, Version = 1 })
                .ToUrl($"/api/incidents/{incidentId}/categorise");
            x.StatusCodeShouldBe(204);
        });

        await Scenario(x =>
        {
            AsUser(x);
            x.Get.Url($"/api/incidents/{incidentId}/history");
            x.StatusCodeShouldBe(200);
        });
    }
}
