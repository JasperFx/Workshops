using HelpDesk.Contracts;
using Marten;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;

namespace HelpDesk.Billing;

public class BillableItem
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid IncidentId { get; set; }
    public IncidentCategory? Category { get; set; }
    public IncidentPriority? Priority { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset BilledAt { get; set; }
}

#region sample_billing_handler
public static class IncidentClosedForBillingHandler
{
    // Note the parameter type. Asking for IDocumentSession would hand you the
    // *main* store's session and quietly write this row into the wrong
    // database -- the module boundary is only as real as the store you write
    // to. Billing takes its own store and opens its own session.
    public static async Task Handle(
        IncidentClosedForBilling e,
        IBillingStore store,
        CancellationToken token)
    {
        await using var session = store.LightweightSession();

        // Idempotency: closing an incident twice must not bill it twice. The
        // deterministic id makes a repeat delivery an overwrite instead of an
        // insert.
        var item = new BillableItem
        {
            Id = DeterministicId(e.IncidentId),
            CustomerId = e.CustomerId,
            IncidentId = e.IncidentId,
            Category = e.Category,
            Priority = e.Priority,
            Amount = RateFor(e.Priority),
            BilledAt = e.ClosedAt
        };

        session.Store(item);
        await session.SaveChangesAsync(token);
    }

    private static decimal RateFor(IncidentPriority? priority) => priority switch
    {
        IncidentPriority.Critical => 500m,
        IncidentPriority.High => 250m,
        IncidentPriority.Medium => 100m,
        _ => 50m
    };

    // Same incident id in, same billable item id out, forever.
    private static Guid DeterministicId(Guid incidentId)
    {
        var bytes = incidentId.ToByteArray();
        bytes[0] ^= 0xB1;
        return new Guid(bytes);
    }
}
#endregion

public static class GetBillableItemsEndpoint
{
    [WolverineGet("/api/billing/customers/{customerId}")]
    public static async Task<IReadOnlyList<BillableItem>> Get(Guid customerId, IBillingStore store)
    {
        await using var session = store.QuerySession();

        return await session.Query<BillableItem>()
            .Where(x => x.CustomerId == customerId)
            .ToListAsync();
    }
}
