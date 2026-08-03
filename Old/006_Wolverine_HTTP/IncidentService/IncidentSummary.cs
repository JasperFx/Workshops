using Marten.Events.Projections;
using Marten.Schema;

namespace IncidentService;

public class IncidentSummary
{
    public string Id { get; set; } 
    
    public int OpenCount { get; set; }
    public int ResolvedCount { get; set; }
}

public class IncidentSummaryProjection : MultiStreamProjection<IncidentSummary, string>
{
    public IncidentSummaryProjection()
    {
        RollUpByTenant();
    }

    public void Apply(IncidentSummary summary, IncidentLogged _) 
        => summary.OpenCount++;
    
    public void Apply(IncidentSummary summary, IncidentResolved _) 
        => summary.ResolvedCount++;
}