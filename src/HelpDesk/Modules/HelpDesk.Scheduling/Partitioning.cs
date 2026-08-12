using Wolverine;
using Wolverine.Configuration;

namespace HelpDesk.Scheduling;

public static class Partitioning
{
    #region sample_global_partitioning
    public static void Configure(WolverineOptions opts)
    {
        // Step one: teach Wolverine how to derive a GroupId from a message.
        // Here, everything about one incident shares a group.
        opts.MessagePartitioning.ByMessage<ScheduleTechnician>(x => x.IncidentId.ToString());

        // Multi-tenanted systems often want exactly this, and nothing more.
        opts.MessagePartitioning.ByTenantId();

        // Step two: no two messages sharing a GroupId will ever execute
        // concurrently -- not just on this node, but across the whole cluster.
        // Different groups still run in parallel.
        opts.MessagePartitioning.GlobalPartitioned(topology =>
        {
            // Spread the groups over 8 local queues. A group always lands on
            // the same slot, so ordering within it is preserved.
            topology.LocalQueues("scheduling", 8);

            topology.Message<ScheduleTechnician>();
        });
    }
    #endregion

    #region sample_partition_by_group_id
    public static void PartitionOneEndpoint(WolverineOptions opts)
    {
        // The narrower version: partition a single listener. Ordering within a
        // GroupId, parallelism across GroupIds, and no cluster-wide
        // coordination to pay for.
        opts.LocalQueueFor<ScheduleTechnician>()
            .PartitionProcessingByGroupId(PartitionSlots.Five);
    }
    #endregion
}
