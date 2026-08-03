---
theme: default
title: Multi-Tenancy
info: Critter Stack Workshop — Section 6 of 8
class: text-center
highlighter: shiki
lineNumbers: false
transition: slide-left
mdc: true
drawings:
  persist: false
---

# Multi-Tenancy

<div class="pt-8 opacity-70">
Section 6 &mdash; one system, many customers
</div>

---
layout: two-cols
---

# Where we are

<Agenda :current="6" />

::right::

<div class="pl-6 pt-14 text-sm opacity-80">

A tenant id has to survive every hop we've built: HTTP, handler, outbox, broker,
projection daemon.

Miss one and you leak a customer's data into another customer's screen.

</div>

---

# Three models

| | Isolation | Cost | Noisy neighbour |
|---|---|---|---|
| **Conjoined** — one database, `tenant_id` column | Logical | Lowest | Yes |
| **Database per tenant** | Physical | Highest | No |
| **Hybrid** — grouped tenants per database | Physical, by group | Middle | Within a group |

<div class="pt-6 gotcha">

This is a business decision wearing a technical costume. "Can tenant A's data
ever physically sit next to tenant B's?" is a question for legal and sales, and
the answer determines everything downstream.

</div>

---

# Static vs. dynamic

- **Static** — tenants are known at startup, configured in `appsettings.json`.
  Fine for tens of tenants
- **Dynamic** — tenants come from a database table, and can be added while the
  system runs. Necessary the moment self-service signup exists

Dynamic tenancy means the schema migration story has to work for a database that
did not exist when the app started.

---

# Marten configuration

```csharp
builder.Services.AddMarten(opts =>
{
    opts.Connection(connectionString);

    // Mix and match, per document type.
    opts.Schema.For<Customer>().SingleTenanted();
    opts.Schema.For<Incident>().MultiTenanted();
});
```

- `MultiTenanted()` adds the tenant id and filters every query by it
- `SingleTenanted()` opts a document out — reference data shared by everyone
- The event store is tenanted the same way

---

# Database per tenant

```csharp
opts.MultiTenantedDatabases(x =>
{
    x.AddSingleTenantDatabase(tenant1ConnectionString, "tenant1");
    x.AddSingleTenantDatabase(tenant2ConnectionString, "tenant2");
});
```

Or point Marten at a **master table** that lists tenants and their connection
strings, and it will discover them at runtime.

<div class="pt-4 text-sm opacity-70">

The application code is identical either way. That's the design goal.

</div>

---

# Partitioning and Row Level Security

For conjoined tenancy at scale, two PostgreSQL features earn their keep:

- **Table partitioning by tenant** — the planner skips other tenants' data
  entirely. Real performance, not just tidiness
- **Row Level Security** — the database refuses to return another tenant's rows,
  even if the application asks

<div class="pt-6 gotcha">

RLS is defence in depth. The application should already be filtering correctly —
RLS is what saves you the day it doesn't.

</div>

---

# Migrations across N databases

Every tenant database needs the same schema. With a hundred tenants that is a
hundred migrations that must all succeed, or all be retryable.

```bash
dotnet run -- db-apply
dotnet run -- db-assert
```

- Weasel computes the delta per database
- A new tenant's database is created and migrated on first use
- **This is the operational cost of database-per-tenant.** Budget for it

---
layout: section
---

# Carrying the tenant id

---

# Detection at the edge

```csharp
app.MapWolverineEndpoints(opts =>
{
    opts.TenantId.IsRouteArgumentNamed("tenant");
    opts.TenantId.IsClaimTypeNamed("tenant.id");
    // also: IsQueryStringValue, IsRequestHeaderValue, custom strategies
});
```

Pick one and enforce it. Supporting several detection strategies at once is a
good way to have a request resolve to the wrong tenant.

---

# Propagation everywhere else

Once detected, the tenant id has to survive:

- The Marten session opened for the request
- The outbox row written in that transaction
- The message on the broker, and the handler on the other side
- A scheduled message delivered four hours later
- A saga's persisted state
- The projection daemon, running per tenant database

<div class="pt-4">

Wolverine propagates it as message metadata automatically. The failure mode to
watch for is code that creates its own session or bus without the tenant.

</div>

---

# The daemon across many databases

- One projection daemon per tenant database
- Checkpoints are per database, so tenants catch up independently
- A poison event in one tenant does not stop the others
- Resource usage scales with tenant count — this is the thing that surprises people

---

# Testing

- Run at least one test with **two tenants** and assert isolation explicitly
- A test that only ever uses `"tenant1"` proves nothing about filtering
- `ResetAllData` per tenant, or per database
- Seed baseline data per tenant with `IInitialData`

<div class="pt-4 gotcha">

The bug you're testing for is "query forgot the tenant filter and returned
everyone's data." That test only fails if a second tenant has data.

</div>

---

<Demo path="src/HelpDesk">

The same Help Desk running conjoined and database-per-tenant, switched by
configuration. Then onboard a new tenant while the application is running.

</Demo>

<div class="pt-4 text-sm opacity-60">
TODO — depends on the HelpDesk sample application.
</div>

---

# When this is premature

- One customer, no roadmap for a second
- An internal tool where "tenant" means "department" and they can all see
  each other anyway

<div class="pt-6">

That said: adding `MultiTenanted()` on day one costs almost nothing. Retrofitting
tenancy into a system with production data is a genuine project. This is one of
the few places where speculative design usually pays.

</div>

---

# Coming up next

**Section 7 — Observability, Metadata, and Metrics.** We now have a lot of moving
parts. Time to be able to see them.

---
layout: end
---

# Resources

<div class="text-left text-base pt-4">

- Marten multi-tenancy — <https://martendb.io/documents/multi-tenancy>
- Wolverine multi-tenancy — <https://wolverinefx.net/guide/handlers/multi-tenancy.html>

</div>
