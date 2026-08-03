using HelpDesk.Contracts;

namespace HelpDesk.Customers;

#region sample_customers_document
// The Customers module is deliberately NOT event sourced.
//
// Nothing about a customer record benefits from an append-only history here,
// and pretending otherwise would be event sourcing as a cargo cult. One system,
// two persistence styles, one transaction when they need to agree.
public class Customer
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Region { get; set; }

    /// <summary>
    /// Drives automatic prioritisation of that customer's incidents. The
    /// Incidents module keeps its own replica of this, fed by an integration
    /// event - it never reads this table.
    /// </summary>
    public Dictionary<IncidentCategory, IncidentPriority> Priorities { get; set; } = new();
}
#endregion
