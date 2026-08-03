# HelpDesk — the workshop's modular monolith

The sample system that sections 2 through 8 all demonstrate against. Four
modules in one process, plus one module deliberately running in another so the
extraction story is real rather than hypothetical.

## Modules

| Project | Persistence | Why it's shaped that way |
|---|---|---|
| `HelpDesk.Contracts` | none | Integration events and shared vocabulary. Every module may reference it; no module may reference another module. |
| `HelpDesk.Incidents` | event sourced, `incidents` schema | The core domain. Inline snapshot projection. |
| `HelpDesk.Customers` | plain documents, `customers` schema | Deliberately *not* event sourced — a customer record gains nothing from a history. |
| `HelpDesk.Billing` | its own `IBillingStore`, `billing` schema | Shows the harder boundary: an ancillary Marten store that could point at a different database tomorrow. |
| `HelpDesk.Notifications` | none | A separate process, over Rabbit MQ. |

`HelpDesk.Api` is the only project that knows all of them exist.

## The rule the code enforces

Modules communicate by **integration event**, never by reaching into each
other's tables.

The old version of this workshop had the Incidents code load a `Customer`
document directly to work out an incident's priority. That is a cross-module
database read, and it is exactly what makes a modular monolith impossible to
pull apart later — the moment Customers becomes its own service, that query
stops compiling.

Instead, `HelpDesk.Incidents` owns `CustomerPriorityRules`, a small replica fed
by `CustomerPrioritiesChanged`. Eventually consistent, and nobody cares that a
prioritisation rule takes a few milliseconds to propagate.

## Running it

```bash
docker compose up -d
```

Then in two terminals:

```bash
dotnet run --project src/HelpDesk/HelpDesk.Api
```

```bash
dotnet run --project src/HelpDesk/HelpDesk.Notifications
```

Walk through [HelpDesk.http](HelpDesk.Api/HelpDesk.http) top to bottom. It
covers the happy path, a 409 from optimistic concurrency, a rejected state
transition, the cross-process pager notification, and the billing record that
appears in a different store.

## Tests

```bash
dotnet test src/HelpDesk/HelpDesk.Tests
```

13 tests: pure-function unit tests with no mocks, and Alba integration tests
that run the real application in-process with the external transports stubbed
out, so no broker is required.

## Two things that will surprise you coming from Marten 8 / Wolverine 5

**Wolverine 6 moved Roslyn out of the core package.** A host that compiles
handler code at runtime needs `WolverineFx.RuntimeCompilation` referenced, or
it fails at startup. Production builds pre-generate instead, which is what
shrinks the deployed image.

**`[Identity]` is gone.** Aggregate ids come from a route argument or a
convention-named message property now: `[WriteAggregate("id")]`, or a
`TryAssignPriority.IncidentId` that Wolverine matches to `Incident` by name.
