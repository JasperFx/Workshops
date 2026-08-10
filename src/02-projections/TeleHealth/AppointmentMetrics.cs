using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JasperFx.Events;
using Marten;
using Marten.Events.Projections;

namespace Projections.TeleHealth;

// Vendored from src/DaemonTests/Composites/multi_stage_projections.cs in the
// Marten repository. Lives here rather than in that test file so the composite
// registration below has something to point at.

#region sample_appointment_metrics_projection
public class AppointmentMetrics
{
    [Identity]
    public string SpecialtyCode { get; set; }
    public int Count { get; set; }
}

// The lowest-level option: raw IProjection. You get the batch of events and a
// session, and everything else is yours. Reach for this when none of the
// conventions fit -- and notice it is still just a class.
public class AppointmentMetricsProjection: IProjection
{
    public async Task ApplyAsync(IDocumentOperations operations, IReadOnlyList<IEvent> events,
        CancellationToken cancellation)
    {
        var groups = events
            .Where(e => e.Data is AppointmentRequested)
            .Select(e => (AppointmentRequested)e.Data)
            .GroupBy(r => r.SpecialtyCode);

        foreach (var group in groups)
        {
            var metrics = await operations.LoadAsync<AppointmentMetrics>(group.Key, cancellation)
                          ?? new AppointmentMetrics { SpecialtyCode = group.Key };
            metrics.Count += group.Count();
            operations.Store(metrics);
        }
    }
}
#endregion
