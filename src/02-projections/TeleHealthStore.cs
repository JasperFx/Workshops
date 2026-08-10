using Marten;
using Projections.TeleHealth;

namespace Projections;

/// <summary>
/// Workshop-authored bootstrapping for the vendored TeleHealth sample. The
/// registration below mirrors the one in the Marten repository's
/// DaemonTests/Composites/multi_stage_projections.cs.
/// </summary>
public static class TeleHealthStore
{
    public static IDocumentStore Build(string connectionString) =>
        DocumentStore.For(opts =>
        {
            opts.Connection(connectionString);
            opts.DatabaseSchemaName = "telehealth";

            ConfigureComposite(opts);
        });

    #region sample_defining_a_composite_projection
    public static void ConfigureComposite(StoreOptions opts)
    {
        // A composite projection is a *group* of projections that share one
        // daemon subscription and one checkpoint. They advance together, and
        // stages run in order.
        opts.Projections.CompositeProjectionFor("TeleHealth", projection =>
        {
            // Stage 1 -- these only read the raw events
            projection.Add<ProviderShiftProjection>();
            projection.Add<AppointmentProjection>();
            projection.Snapshot<Board>();
            projection.Add(new AppointmentMetricsProjection());

            // Stage 2 -- these run after stage 1 has been applied, and are
            // allowed to depend on what stage 1 just wrote
            projection.Add<AppointmentDetailsProjection>(2);
            projection.Add<BoardSummaryProjection>(2);
            projection.Add<AppointmentByExternalIdentifierProjection>(2);
        });
    }
    #endregion
}
