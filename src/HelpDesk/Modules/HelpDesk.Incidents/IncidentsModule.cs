using JasperFx.Events.Projections;
using Marten.Events.Projections;
using Marten;

namespace HelpDesk.Incidents;

#region sample_incidents_module_registration
public static class IncidentsModule
{
    public const string SchemaName = "incidents";

    public static void ConfigureMarten(StoreOptions opts)
    {
        opts.Events.DatabaseSchemaName = SchemaName;

        // Inline: the snapshot is written in the same transaction as the events,
        // so a read straight after a write is never stale. Incidents are small
        // and read constantly, which is exactly the case Inline is for.
        opts.Projections.Snapshot<Incident>(SnapshotLifecycle.Inline);

        opts.Schema.For<Incident>().DatabaseSchemaName(SchemaName);
        opts.Schema.For<CustomerPriorityRules>().DatabaseSchemaName(SchemaName);
        opts.Schema.For<CustomerIncidentSummary>().DatabaseSchemaName(SchemaName);

        // The one Async projection in the workshop. Everything else is Inline,
        // so this is what "eventually consistent" actually means here.
        opts.Projections.Add<CustomerIncidentSummaryProjection>(ProjectionLifecycle.Async);
    }
}
#endregion
