using Marten;

namespace Projections;

public static class ProjectionErrorHandling
{
    #region sample_projection_error_handling
    public static void Configure(StoreOptions opts)
    {
        // Continuous processing -- the daemon running behind your live system.
        // These are the DEFAULTS. Marten skips by default here, on the theory
        // that one bad event should not stop every projection in the system.
        opts.Projections.Errors.SkipApplyErrors = true;
        opts.Projections.Errors.SkipSerializationErrors = true;
        opts.Projections.Errors.SkipUnknownEvents = true;

        // Rebuilds -- these are also the defaults, and deliberately the
        // opposite. A rebuild is your chance to get the read model exactly
        // right, so anything unexpected stops it immediately.
        opts.Projections.RebuildErrors.SkipApplyErrors = false;
        opts.Projections.RebuildErrors.SkipSerializationErrors = false;
        opts.Projections.RebuildErrors.SkipUnknownEvents = false;
    }
    #endregion

    #region sample_projection_errors_strict
    public static void StrictInProduction(StoreOptions opts)
    {
        // The other side of the argument: a skipped event means a read model
        // that is quietly, permanently wrong -- and nothing told you. Turning
        // these off pauses the projection instead, which is louder and easier
        // to notice than data that is subtly incorrect.
        //
        // The cost is real: one poison event now stops that projection until
        // somebody intervenes. Pick deliberately.
        opts.Projections.Errors.SkipApplyErrors = false;
        opts.Projections.Errors.SkipSerializationErrors = false;

        // Keep this one on even when strict. During a blue/green deploy the
        // old nodes will legitimately meet event types they have never heard
        // of, and stopping for that is a self-inflicted outage.
        opts.Projections.Errors.SkipUnknownEvents = true;
    }
    #endregion
}
