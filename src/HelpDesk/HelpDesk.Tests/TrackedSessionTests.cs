using JasperFx.Core;
using System.Security.Claims;
using Alba;
using HelpDesk.Contracts;
using HelpDesk.Customers;
using HelpDesk.Incidents;
using Marten;
using Marten.Events;
using Shouldly;
using Wolverine.Tracking;

namespace HelpDesk.Tests;

public class tracked_session_usage(AppFixture fixture) : IntegrationContext(fixture)
{
    private readonly Guid _user = Guid.CreateVersion7();

    private void AsUser(Scenario x) => x.WithClaim(new Claim("user-id", _user.ToString()));

    private async Task<Guid> ACustomerWithCriticalDatabaseRule()
    {
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

    private async Task<Guid> AnIncidentFor(Guid customerId)
    {
        var logged = await Scenario(x =>
        {
            AsUser(x);
            x.Post.Json(new LogIncident(customerId, new Contact(ContactChannel.Phone), "Broken"))
                .ToUrl("/api/incidents");
            x.StatusCodeShouldBe(201);
        });

        return logged.ReadAsJson<NewIncidentResponse>()!.IncidentId;
    }

    #region sample_reading_the_messages_sent
    [Fact]
    public async Task read_everything_that_happened()
    {
        var customerId = await ACustomerWithCriticalDatabaseRule();
        var incidentId = await AnIncidentFor(customerId);

        var (tracked, _) = await TrackedHttpCall(x =>
        {
            AsUser(x);
            x.Post.Json(new CategoriseIncident { Category = IncidentCategory.Database, Version = 1 })
                .ToUrl($"/api/incidents/{incidentId}/categorise");
            x.StatusCodeShouldBe(204);
        });

        // Every record, in the order it happened. This is the one to reach for
        // when a test fails and you have no idea why -- print it and read it.
        var story = tracked.AllRecordsInOrder();
        story.ShouldNotBeEmpty();

        // The collections are sliced by what happened to the message, not by
        // message type: Sent, Received, Executed, MessageSucceeded,
        // MessageFailed, Requeued, MovedToErrorQueue, NoHandlers, NoRoutes...
        tracked.Executed.MessagesOf<TryAssignPriority>().ShouldNotBeEmpty();

        // Nothing blew up anywhere in the cascade, including in handlers the
        // HTTP call never touched directly.
        tracked.AllExceptions().ShouldBeEmpty();
    }
    #endregion

    #region sample_asserting_on_messages_sent
    [Fact]
    public async Task assert_on_what_was_published()
    {
        var customerId = await ACustomerWithCriticalDatabaseRule();
        var incidentId = await AnIncidentFor(customerId);

        var (tracked, _) = await TrackedHttpCall(x =>
        {
            AsUser(x);
            x.Post.Json(new CategoriseIncident { Category = IncidentCategory.Database, Version = 1 })
                .ToUrl($"/api/incidents/{incidentId}/categorise");
            x.StatusCodeShouldBe(204);
        });

        // Exactly one, or the assertion fails with a useful message rather
        // than a NullReferenceException three lines later.
        var assign = tracked.Executed.SingleMessage<TryAssignPriority>();
        assign.IncidentId.ShouldBe(incidentId);

        // NotificationRequested is routed to Rabbit MQ in production. The
        // fixture disables external transports, so it lands in Sent and can be
        // asserted on without a broker anywhere in sight.
        var notification = tracked.Sent.SingleMessage<NotificationRequested>();
        notification.Channel.ShouldBe(NotificationChannel.Pager);
        notification.CustomerId.ShouldBe(customerId);

        // The envelope, when you care about metadata rather than the body.
        var envelope = tracked.Sent.SingleEnvelope<NotificationRequested>();
        envelope.Destination.ShouldNotBeNull();
    }
    #endregion

    #region sample_waiting_for_marten_async_projections
    [Fact]
    public async Task wait_for_an_async_projection_to_catch_up()
    {
        var customerId = await ACustomerWithCriticalDatabaseRule();

        await AnIncidentFor(customerId);
        await AnIncidentFor(customerId);

        // Wolverine's tracking waits for *messages*. It knows nothing about
        // Marten's async daemon, so a projection can still be behind at the
        // moment TrackedHttpCall returns.
        //
        // This is the other half. Drive the daemon explicitly rather than
        // waiting on the host's: a suite that calls ResetAllData between tests
        // leaves the hosted daemon stopped, and waiting on it just times out
        // on shards that never restarted.
        using var daemon = await Store.BuildProjectionDaemonAsync();
        await daemon.StartAllAsync();
        await daemon.WaitForNonStaleData(30.Seconds());

        await using var session = Store.LightweightSession();
        var summary = await session.LoadAsync<CustomerIncidentSummary>(customerId);

        summary.ShouldNotBeNull();
        summary.Logged.ShouldBe(2);
    }
    #endregion
}
