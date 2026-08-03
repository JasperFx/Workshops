using Alba;
using Alba.Security;
using HelpDesk.Billing;
using JasperFx.CommandLine;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.Tracking;

namespace HelpDesk.Tests;

#region sample_app_fixture
public class AppFixture : IAsyncLifetime
{
    public IAlbaHost Host { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        // Without this the host never actually starts. Complain to the
        // ASP.NET Core team, not to me.
        JasperFxEnvironment.AutoStartHost = true;

        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // No broker required. Messages that would have gone to Rabbit
                // stay in memory, and Wolverine's tracking can still see them.
                services.DisableAllExternalWolverineTransports();
            });
        }, new AuthenticationStub());
    }

    public async ValueTask DisposeAsync()
    {
        if (Host is not null) await Host.DisposeAsync();
    }
}
#endregion

[CollectionDefinition("integration")]
public class IntegrationCollection : ICollectionFixture<AppFixture>;

#region sample_integration_context
[Collection("integration")]
public abstract class IntegrationContext(AppFixture fixture) : IAsyncLifetime
{
    public IAlbaHost Host => fixture.Host;

    public IDocumentStore Store => Host.Services.GetRequiredService<IDocumentStore>();

    public IBillingStore BillingStore => Host.Services.GetRequiredService<IBillingStore>();

    public async ValueTask InitializeAsync()
    {
        // Wipe every module's data, including the ancillary store, so each test
        // starts from a known state. Fast enough to do per test.
        await Store.Advanced.ResetAllData();
        await BillingStore.Advanced.ResetAllData();

        await Setup();
    }

    protected virtual Task Setup() => Task.CompletedTask;

    // Deliberately no teardown - leaving the data behind makes a failing test
    // possible to investigate afterwards.
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected Task<IScenarioResult> Scenario(Action<Scenario> configure)
        => Host.Scenario(configure);

    /// <summary>
    /// Makes an HTTP call and waits for every cascading message it triggers to
    /// finish before returning. No sleeps, no polling, no flaky async tests.
    /// </summary>
    protected async Task<(ITrackedSession, IScenarioResult)> TrackedHttpCall(
        Action<Scenario> configure)
    {
        IScenarioResult result = null!;

        var tracked = await Host.ExecuteAndWaitAsync(async () =>
        {
            result = await Host.Scenario(configure);
        });

        return (tracked, result);
    }
}
#endregion
