using HelpDesk.Contracts;
using Marten;
using Wolverine;
using Microsoft.AspNetCore.Http;
using Wolverine.Http;
using Wolverine.Marten;

namespace HelpDesk.Customers;

public record RegisterCustomer(string Name, string? Region);

public record CustomerRegisteredResponse(Guid CustomerId)
    : CreationResponse("/api/customers/" + CustomerId);

#region sample_register_customer
public static class RegisterCustomerEndpoint
{
    [WolverinePost("/api/customers")]
    public static (CustomerRegisteredResponse, IMartenOp, OutgoingMessages) Post(
        RegisterCustomer command)
    {
        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            Name = command.Name,
            Region = command.Region
        };

        // The document write and the integration event go out together, in one
        // transaction, through the outbox. No dual write.
        return (
            new CustomerRegisteredResponse(customer.Id),
            MartenOps.Store(customer),
            [new CustomerRegistered(customer.Id, customer.Name, customer.Region)]
        );
    }
}
#endregion

public record SetCustomerPriorities(
    Guid CustomerId,
    Dictionary<IncidentCategory, IncidentPriority> Priorities);

#region sample_set_customer_priorities
public static class SetCustomerPrioritiesEndpoint
{
    [WolverinePost("/api/customers/priorities")]
    public static async Task<(IResult, OutgoingMessages)> Post(
        SetCustomerPriorities command,
        IDocumentSession session)
    {
        var customer = await session.LoadAsync<Customer>(command.CustomerId);
        if (customer is null)
        {
            return (Results.NotFound(), []);
        }

        customer.Priorities = command.Priorities;
        session.Store(customer);

        // Tell the rest of the system. The Incidents module cares; the Billing
        // module doesn't. Neither of them is named here.
        return (
            Results.NoContent(),
            [new CustomerPrioritiesChanged(customer.Id, customer.Priorities)]
        );
    }
}
#endregion

public static class GetCustomerEndpoint
{
    [WolverineGet("/api/customers/{id}")]
    public static Task<Customer?> Get(Guid id, IQuerySession session)
        => session.LoadAsync<Customer>(id);
}
