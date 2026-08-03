namespace DocumentQuickstart;

#region sample_document_customer
// No base class. No attributes. No mapping file. No migration script.
// Marten stores this as JSONB and infers everything it needs from the type.
public class Customer
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Region { get; set; }

    // A dictionary keyed by an enum. Try modelling *this* in a
    // relational schema without hating your life.
    public Dictionary<IncidentCategory, IncidentPriority> Priorities { get; set; } = new();

    public ContractDuration? Contract { get; set; }

    public List<ContactMethod> Contacts { get; set; } = [];
}
#endregion

public record ContractDuration(DateOnly Start, DateOnly End);

public record ContactMethod(ContactChannel Channel, string Value);

public enum ContactChannel
{
    Email,
    Phone,
    InPerson,
    GeneratedBySystem
}

public enum IncidentCategory
{
    Software,
    Hardware,
    Network,
    Database
}

public enum IncidentPriority
{
    Critical,
    High,
    Medium,
    Low
}
