using Marten;

namespace HelpDesk.Customers;

#region sample_customers_module_registration
/// <summary>
/// Every module exposes its own configuration and owns its own schema. The host
/// calls this; it does not reach in and configure the module's storage itself.
/// </summary>
public static class CustomersModule
{
    public const string SchemaName = "customers";

    public static void ConfigureMarten(StoreOptions opts)
    {
        // Same physical database as Incidents, different schema. The boundary
        // is real enough that you can see it in pgAdmin, and cheap enough that
        // a transaction can still span both when it genuinely needs to.
        opts.Schema.For<Customer>().DatabaseSchemaName(SchemaName);
    }
}
#endregion
