using HelpDesk.Contracts;
using JasperFx.Events;
using Marten.Events.Projections;

namespace HelpDesk.Incidents;

#region sample_customer_incident_summary
/// <summary>
/// A read model spanning every incident stream for one customer. This is the
/// workshop's only Async projection -- everything else is Inline -- which makes
/// it the thing to point at when talking about eventual consistency, and the
/// thing tests have to wait for.
/// </summary>
public class CustomerIncidentSummary
{
    public Guid Id { get; set; }
    public int Logged { get; set; }
    public int Resolved { get; set; }
    public int Closed { get; set; }
    public int OpenRightNow => Logged - Closed;
}

public partial class CustomerIncidentSummaryProjection
    : MultiStreamProjection<CustomerIncidentSummary, Guid>
{
    public CustomerIncidentSummaryProjection()
    {
        // The document is keyed by customer, but the events live on incident
        // streams -- so the identity has to come out of the event body.
        Identity<IncidentLogged>(x => x.CustomerId);
    }

    public void Apply(IncidentLogged _, CustomerIncidentSummary summary)
        => summary.Logged++;
}
#endregion
