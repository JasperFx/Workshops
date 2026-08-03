using Marten;

namespace HelpDesk.Billing;

#region sample_billing_ancillary_store
/// <summary>
/// Billing goes further than Incidents and Customers: it gets its own
/// <see cref="IDocumentStore"/> rather than a schema inside the shared one.
///
/// An ancillary store has its own configuration, its own projections, and its
/// own async daemon. It can point at a different database entirely by changing
/// one connection string, which makes this the module that is already most of
/// the way to being a separate service.
/// </summary>
public interface IBillingStore : IDocumentStore;
#endregion

public static class BillingModule
{
    public const string SchemaName = "billing";
}
