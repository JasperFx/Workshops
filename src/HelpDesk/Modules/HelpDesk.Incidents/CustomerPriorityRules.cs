using HelpDesk.Contracts;
using Marten;
using Wolverine.Marten;

namespace HelpDesk.Incidents;

#region sample_customer_priority_rules
/// <summary>
/// The Incidents module's own replica of the prioritisation rules it needs.
///
/// The old version of this workshop had the Incidents code load a Customer
/// document directly. That is a cross-module database read, and it is exactly
/// what makes a "modular monolith" impossible to pull apart later: the moment
/// Customers moves to its own service, that query stops compiling.
///
/// Instead this module owns a small replica, fed by an integration event. The
/// data is eventually consistent, which is fine - a prioritisation rule that
/// takes a few milliseconds to propagate hurts nobody.
/// </summary>
public class CustomerPriorityRules
{
    public Guid Id { get; set; }

    public Dictionary<IncidentCategory, IncidentPriority> Priorities { get; set; } = new();

    public IncidentPriority? PriorityFor(IncidentCategory? category) =>
        category.HasValue && Priorities.TryGetValue(category.Value, out var priority)
            ? priority
            : null;
}

public static class CustomerPrioritiesChangedHandler
{
    public static IMartenOp Handle(CustomerPrioritiesChanged e) =>
        MartenOps.Store(new CustomerPriorityRules
        {
            Id = e.CustomerId,
            Priorities = e.Priorities
        });
}

public static class CustomerRegisteredHandler
{
    // A customer with no rules yet still needs a row, so that "unknown customer"
    // and "customer with no rules" are distinguishable downstream.
    public static IMartenOp Handle(CustomerRegistered e) =>
        MartenOps.Store(new CustomerPriorityRules { Id = e.CustomerId });
}
#endregion
