using JasperFx.Events;
using Marten;

namespace IncidentService;

public static class IncidentResolvedHandler
{
    public static void Handle(IEvent<IncidentResolved> e, IDocumentSession session)
    {
        // Mark the event stream for the incident being resolved
        // as "archived"
        session.Events.ArchiveStream(e.StreamId);
    }
}