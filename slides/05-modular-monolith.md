---
theme: default
title: Modular Monolith
info: Critter Stack Workshop — Section 5 of 8
class: text-center
highlighter: shiki
lineNumbers: false
transition: slide-left
mdc: true
drawings:
  persist: false
---

# Modular Monolith

<div class="pt-8 opacity-70">
Section 5 &mdash; boundaries without the distributed systems tax
</div>

---
layout: two-cols
---

# Where we are

<Agenda :current="5" />

::right::

<div class="pl-6 pt-14 text-sm opacity-80">

Everything so far has been one module.

Real systems have several, and the seams between them are where the design
lives.

</div>

---

# The honest accounting

Microservices give you independent deployment and independent scaling. They
charge you for it:

- Every in-process call becomes a network call that can fail
- Every transaction becomes eventual consistency
- Every refactor across a boundary becomes a coordinated release
- Debugging needs distributed tracing before you can even start

<div class="pt-6">

A modular monolith gets you the **boundaries** without paying most of that bill,
and leaves the door open to pay it later — per module, when there's a reason.

</div>

---

# What a module boundary actually is

Three things, and you need all three or you have a namespace, not a module:

1. **Code** — a module owns its types, and other modules can't reach in
2. **Data** — a module owns its tables or schema. No cross-module joins
3. **Messaging** — modules talk through a published contract, not method calls

<div class="pt-6 gotcha">

Most "modular monoliths" get 1, skip 2, and end up with a shared database that
nothing can be extracted from. Data ownership is the load-bearing one.

</div>

---

# Vertical slice architecture

Organise by feature, not by layer.

```
/Incidents
  LogIncident.cs          command + validator + handler + endpoint
  CategoriseIncident.cs
  Incident.cs             the aggregate
  Events.cs
```

- Everything one feature needs, in one file, readable top to bottom
- Wolverine's handler and endpoint discovery makes this work without registration
- A feature is deleted by deleting a file

<div class="pt-4 text-sm opacity-70">

This is not a rule, it's a default. Shared domain concepts still deserve their
own home.

</div>

---

# Marten: a store per module

<<< ../src/HelpDesk/HelpDesk.Api/Program.cs#sample_helpdesk_billing_store cs

<div class="pt-2"></div>

<<< ../src/HelpDesk/Modules/HelpDesk.Billing/BillingModule.cs#sample_billing_ancillary_store cs {maxHeight:'190px'}

---

# Or just a schema, when that's enough

<<< ../src/HelpDesk/Modules/HelpDesk.Customers/CustomersModule.cs#sample_customers_module_registration cs

- Ancillary stores are fully independent — own schema, own projections, own daemon
- Same database, or a different one, decided by configuration
- The Incidents module physically cannot query Billing's tables

<div class="pt-4 text-sm opacity-70">

And per section 1, each module picks its own persistence style. Incidents is
event sourced; Customers is documents; Billing could be EF Core.

</div>

---

# Wolverine: local queues as the seam

A message sent from Incidents to Billing goes through a queue even though both
are in the same process.

- The call is already asynchronous, already durable, already retryable
- The handler on the other side has no idea the sender was in-process
- Extraction later changes routing configuration and nothing else

<div class="pt-6 gotcha">

This is the whole trick. If modules talk by direct method call, extraction is a
rewrite. If they talk by message, extraction is a config change.

</div>

---

# Domain events vs. integration events

| | Domain event | Integration event |
|---|---|---|
| Audience | Inside the module | Other modules |
| Shape | Whatever the domain needs | A deliberate, stable contract |
| Changes | Freely | Like a public API |

<<< ../src/HelpDesk/Modules/HelpDesk.Contracts/IntegrationEvents.cs#sample_integration_events cs {maxHeight:'250px'}

---

# The mistake this design exists to prevent

<<< ../src/HelpDesk/Modules/HelpDesk.Incidents/CustomerPriorityRules.cs#sample_customer_priority_rules cs {maxHeight:'400px'}

---

# The test that proves the seam holds

<<< ../src/HelpDesk/HelpDesk.Tests/EndToEndTests.cs#sample_cross_module_test cs

---

# HTTP endpoints per module

Wolverine.HTTP discovers endpoints by assembly scanning, so each module
contributes its own routes without a central registration file.

- No `Program.cs` that knows about every feature in the system
- Route prefixes per module if you want the URL structure to show the boundary
- Middleware applies by convention, per module or globally

---

# Testing a module in isolation

- Bootstrap the host with only that module's assembly registered
- `DisableAllExternalWolverineTransports` so no broker is needed
- Assert on the **integration events** the module publishes — that's its contract
- Cross-module tests exist, but there should be far fewer of them

---

# The extraction path

When a module genuinely needs to become a service:

1. It already owns its data → no schema untangling
2. It already talks by message → change the routing to point at a broker
3. It already publishes integration events → consumers don't change
4. Move the project, add a host, deploy

<div class="pt-6">

The work is in DevOps, not in the code. That is the point of the whole exercise.

</div>

---

<Demo path="src/HelpDesk">

The four-module Help Desk — Incidents, Customers, Billing, Notifications — in one
process. Then move Notifications out and watch how little changes.

</Demo>

<div class="pt-4 text-sm opacity-60">
TODO — depends on the HelpDesk sample application.
</div>

---

# When this is over-engineering

- A system one person maintains does not need enforced module boundaries
- Three tables and a CRUD screen is not a bounded context
- Splitting before you know where the seams are produces the wrong seams, and
  wrong seams are expensive to move

<div class="pt-6">

Modularise when you have evidence of a boundary, not in anticipation of one.

</div>

---

# Coming up next

**Section 6 — Multi-Tenancy.** Same modules, many customers, and a tenant id
that has to survive every hop we've built so far.

---
layout: end
---

# Resources

<div class="text-left text-base pt-4">

- Marten ancillary stores — <https://martendb.io/configuration/hostbuilder>
- Wolverine + modular monoliths — <https://wolverinefx.net/tutorials/modular-monolith.html>

</div>
