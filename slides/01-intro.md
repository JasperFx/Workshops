---
theme: default
title: Introduction to CQRS and Event Sourcing
info: |
  Critter Stack Workshop — Section 1 of 8
  CQRS and Event Sourcing with Marten, Polecat, and Wolverine
class: text-center
highlighter: shiki
lineNumbers: false
transition: slide-left
mdc: true
drawings:
  persist: false
---

# CQRS and Event Sourcing

## with the "Critter Stack"

<div class="pt-8 opacity-70">
Section 1 &mdash; Introduction
</div>

---
layout: two-cols
---

# Where we're going

<Agenda :current="1" />

::right::

<div class="pl-6 pt-14 text-sm opacity-80">

Four two-hour blocks.

The first hour builds a working event-sourced service.

The other seven take that service and break it, one production concern at a time.

</div>

---

# Goals

- Teach event sourcing and CQRS as **architecture**, not as an API tour
- Show what it actually takes to build a **robust** event-driven system
- Convince you the Critter Stack is the lowest-friction way to get there
- Make you love low-ceremony code

<div class="pt-8 gotcha">

And one more that most vendor talks skip: **teaching you when *not* to do any
of this.** Every section ends with a "this is the wrong answer when…" slide.

</div>

---

# Environment

```bash
git clone https://github.com/jasperfx/Workshops
cd Workshops
docker compose up -d
```

- .NET 10 SDK
- Docker Desktop
- Postgres comes up on **5440**, Rabbit on **5682** — not the defaults, so this
  won't fight a PostgreSQL you already have on 5432. Don't go looking for the
  workshop database there.

<div class="pt-6 text-sm opacity-70">

Verify with `cd src/01-quickstarts/DocumentQuickstart && dotnet run`

</div>

---

# About me

- Owner and founder of **JasperFx Software LLC**
- Marten core team, Wolverine project lead
- Longtime OSS author — StructureMap, FubuMVC, and a trail of others
- <https://jeremydmiller.com> · <https://jasperfx.net>

---
layout: section
---

# The Critter Stack

---

# The Critter Stack, 2026 edition

| | |
|---|---|
| **Marten** | Document database + event store on PostgreSQL |
| **Polecat** | The same model on SQL Server 2025 |
| **Fisher** | The same model on SQLite |
| **Wolverine** | Command execution, messaging, background work, HTTP |
| **Weasel** | Database schema management for the stack |
| **CritterWatch** | Commercial management and observability |

<div class="pt-6">

Shared core in `JasperFx` / `JasperFx.Events`, so Marten, Polecat, and Fisher
are increasingly the same engine pointed at different databases.

</div>

---

# Just to level set

<div class="text-2xl pt-4 pb-8">

Using Marten or Polecat **does not** mean going all-in on event sourcing.

</div>

Mix event sourcing, plain documents, good ol' relational tables, and EF Core entities in one
system — and when they share a store, they share a transaction.

The database is a separate choice: **Marten** on PostgreSQL, **Polecat** on
SQL Server 2025. Same programming model either way; everything we write today
is Marten.

<div class="pt-4 gotcha">

This is what makes the modular monolith in section 5 possible. Pick the
persistence style per module.

</div>

---
layout: section
---

# Marten as a document database

---

# No ceremony

<<< ../src/01-quickstarts/DocumentQuickstart/Customer.cs#sample_document_customer cs

<div class="pt-2 text-sm opacity-70">

`ContractDuration` and `ContactMethod` are plain records, stored nested inside
the same JSONB document.

</div>

---

# Storing and loading

<<< ../src/01-quickstarts/DocumentQuickstart/Program.cs#sample_document_storing cs {all|1-11|13-16|18-19}

---

# Querying

<<< ../src/01-quickstarts/DocumentQuickstart/Program.cs#sample_document_querying cs

---

# Why a document database at all?

- Because you develop faster, full stop
- Handles deeply nested structures without a join in sight
- Genuinely good story for polymorphic entities
- Ideal when entities are self-contained

<div class="pt-6 gotcha">

**When it's the wrong answer:** heavy ad-hoc reporting across entity types,
schemas that a separate team owns, or a domain that really is a graph of
many-to-many relationships. Reach for a table.

</div>

---

<Demo path="src/01-quickstarts/DocumentQuickstart" run="dotnet run">

Document quickstart — store, load, query, and look at the JSONB that Marten wrote.

</Demo>

---
layout: section
---

# Event sourcing

---
layout: statement
---

## A style of persistence where the single source of truth is a read-only, append-only sequence of all the events that resulted in a change in system state

---

# What you get

- **Business language** — the storage model uses the domain's vocabulary
- **Audit log** — for free, and complete, because it *is* the data
- **Temporal querying** — what did this look like last Tuesday?
- **Retrofitting metrics** — answer questions you hadn't thought of yet
- **Complement to CQRS** — one write model, many read models
- **Concurrency** — an append-only log has an obvious version number

---

# Conceptual shifts

- Modeling state as a sequence of
  facts, rather than a row you overwrite, is genuinely different
- De-emphasizing "noun-centric" modeling
- Leveraging read models rather than querying the raw state

<div class="pt-5 text-sm opacity-70">

Marten and Polecat have models for strong *or* eventual consistency. It's important to be aware of both models and where either is
valuable

</div>

---

# Event storming the help desk

<div class="flex justify-center">
  <img src="/event-storming.png" class="max-h-[340px]" alt="Event storming board for the help desk domain" />
</div>

<div class="text-sm opacity-70 text-center">

Blue commands, orange events, green read models.

</div>

---

# The domain: a help desk

Incidents get logged, categorised, prioritised, assigned, responded to,
resolved, acknowledged, and closed.

<div class="pt-4"></div>

<<< ../src/01-quickstarts/EventSourcingQuickstart/Events.cs#sample_incident_events cs {maxHeight:'280px'}

<div class="pt-2 text-sm opacity-70">

Those orange stickies, now compiling.

</div>

---

# Marten terminology

| Term | Meaning |
|---|---|
| **Event** | A persisted business event — a change in state or a record of an action |
| **Stream** | A related sequence of events representing one workflow or concept |
| **Projection** | Any strategy for deriving a read-side view from events |
| **Aggregate** | A projection that folds one stream into a single view |

---

# Starting a stream

<<< ../src/01-quickstarts/EventSourcingQuickstart/Program.cs#sample_es_starting_a_stream cs

<div class="pt-4"></div>

## Appending to it

<<< ../src/01-quickstarts/EventSourcingQuickstart/Program.cs#sample_es_appending cs

---

# The aggregate

<<< ../src/01-quickstarts/EventSourcingQuickstart/Incident.cs#sample_incident_aggregate cs {maxHeight:'380px'}

---

# Or mutable, if your team prefers

<<< ../src/01-quickstarts/EventSourcingQuickstart/MutableIncident.cs#sample_incident_aggregate_mutable cs {maxHeight:'380px'}

---

# Or one Evolve() method for the whole thing

<<< ../src/01-quickstarts/EventSourcingQuickstart/EvolvingIncident.cs#sample_incident_aggregate_evolve cs {maxHeight:'400px'}

---

# Three shapes, one behaviour

| | |
|---|---|
| `Create` / `Apply` overloads | Conventional, and the most common |
| Mutable class, `void Apply` | Same thing for teams that dislike records |
| Single `Evolve(IEvent e)` | All the folding in one switch |

<div class="pt-5">

All three are found by the **source generator** at compile time — no runtime
reflection, and all three work under AOT.

</div>

<div class="pt-4 gotcha">

Pick whichever your team argues about less. `Evolve` earns its keep when you
want one obvious place to decide what happens to an event you don't recognise.

</div>

---

# Reading it back

<<< ../src/01-quickstarts/EventSourcingQuickstart/Program.cs#sample_es_reading_raw_events cs

<div class="pt-4"></div>

<<< ../src/01-quickstarts/EventSourcingQuickstart/Program.cs#sample_es_live_aggregation cs

---

# Time travel

<<< ../src/01-quickstarts/EventSourcingQuickstart/Program.cs#sample_es_time_travel cs

<div class="pt-4 gotcha">

This is the demo that sells event sourcing to non-developers. Support
engineers and auditors ask this question constantly, and in a CRUD system
the answer is "we don't know."

</div>

---

<Demo path="src/01-quickstarts/EventSourcingQuickstart" run="dotnet run">

Append events, read the raw stream, aggregate it live, and rewind it.

</Demo>

---

# When the boundary isn't a stream

Everything so far assumes one stream is one consistency boundary. Plenty of
invariants don't fit that shape:

- *"A student may not enroll in more than ten courses"* — spans student **and** course
- *"This email address must be unique"* — spans every user who ever registered

<div class="pt-4">

The usual bad options are to grow the aggregate until it swallows both, or to
give up and check optimistically and hope.

</div>

<div class="pt-4 gotcha">

**Dynamic Consistency Boundaries** let a command declare its own boundary at
read time, instead of inheriting one from how you happened to lay out streams.

</div>

---

# DCB in Marten and Polecat

```csharp
// Tag an event with every identity it touches
var enrolled = session.Events.BuildEvent(new StudentEnrolled(studentId, courseId));
enrolled.WithTag(studentId, courseId);
session.Events.Append(streamId, enrolled);

// Declare the consistency boundary for *this* decision
var query = new EventTagQuery()
    .Or<StudentId>(studentId)
    .Or<CourseId>(courseId);

var boundary = await session.Events
    .FetchForWritingByTags<StudentCourseEnrollment>(query);

boundary.AppendOne(new AssignmentSubmitted(/* ... */));

// Throws DcbConcurrencyException if anything matching that query
// was appended since we read
await session.SaveChangesAsync();
```

<div class="pt-2 text-sm opacity-70">

Tags are strongly typed. The boundary is the query, not the stream.

</div>

---

# Why we built it, and when to reach for it

<div class="pt-4">

So here it isn't a workaround — it's a **modeling option**. Use it when the
invariant genuinely spans entities and forcing one aggregate to own it would
distort the domain.

</div>

<div class="pt-6 gotcha">

Reach for a single stream first. It is simpler, faster, and right most of the
time. DCB is there for the cases where it isn't.

</div>

---

# Privacy, GDPR, and an append-only log

<div class="pt-2 pb-4">

The obvious tension: right-to-be-forgotten, meeting a log you promised never
to rewrite.

</div>

| | |
|---|---|
| **Data masking** | Rewrite the personal fields in place, keep the event and its shape |
| **Stream archiving** | Move a whole stream out of the active tables |
| **Crypto shredding** | Encrypt per subject, then destroy the key — the event survives, it just stops meaning anything |

<div class="pt-5 text-sm opacity-80">

Masking and archiving are built in today. **[Tayra](https://tayra.dev)** —
commercial, currently in preview — covers the third: field-level encryption,
blind indexes so encrypted fields stay queryable, and key destruction for
erasure.

</div>

<div class="pt-4 gotcha">

The catch is timing, not tooling. All three want you to have decided which
fields are personal **before** you have five years of history.

</div>

---

# When event sourcing is the wrong answer

- CRUD screens over reference data
- A reporting database
- Anything where nobody will ever ask *"how did we get here?"*

<div class="pt-8 text-base">

The test I'd apply: **is the history itself valuable to the business?**

If the answer is no, you are paying for an audit log nobody reads.

</div>

---
layout: section
---

# Projections

---

# Three lifecycles

| | Consistency | Write cost | Read cost |
|---|---|---|---|
| **Inline** | Strong — same transaction | Slower | Fast |
| **Live** | Strong — computed on demand | Free | Slower |
| **Async** | Eventual — background daemon | Fast | Fast |

<div class="pt-6">

Inline for small streams you read constantly. Live for streams you rarely read.
Async for everything read-heavy that can tolerate a few milliseconds of lag.

</div>

<div class="pt-4 text-sm opacity-70">

Section 4 comes back to this, because "a few milliseconds of lag" is where
most of the hard questions live.

</div>

---

# Registering one

<<< ../src/01-quickstarts/EventSourcingQuickstart/Program.cs#sample_es_bootstrapping cs

---

# Three roles for projections

- **Write models** — the state a command handler needs to make a decision.
  Small, and shaped by the invariants you enforce.
- **Read models** — what a screen or API response needs. Denormalised, often
  spanning several streams.
- **Query models** — flat, indexed, built for searching and reporting.

<div class="pt-6 gotcha">

Conflating these is the most common early mistake. One aggregate that serves
all three roles will be bad at all three.

</div>

---

# What changed in Marten 9

- **Runtime code generation is gone.** Conventional projections use source
  generators now — faster cold starts, AOT-friendly, no `codegen` step
- **Lightweight sessions are the default.** Identity-map sessions still exist,
  you just opt into them
- **Quick append with server timestamps** is the default append mode
- .NET 8 support dropped

<div class="pt-6 text-sm opacity-70">

If you're carrying a codebase from Marten 8, this is the list to read first.

</div>

---
layout: section
---

# CQRS

---
layout: statement
---

## Command Query Responsibility Segregation

### An architectural pattern where reads and writes are strictly segregated

---

# CQRS and event sourcing

<div class="text-xl pt-4">

Usable apart. Better together.

</div>

- CQRS without event sourcing is fine, and common
- Event sourcing without CQRS is possible, and usually miserable — because
  the write model is a terrible read model
- Together, the event log is the write side and projections are the read side.
  The pattern and the persistence strategy fit each other exactly

---

# Everything a command handler has to do

- Validate the command's inputs
- Fetch the existing write-model state
- Decide what new events to append
- Manage the transaction
- Handle concurrency
- Handle errors and retries
- Publish resulting events
- Stay observable

<div class="pt-6 gotcha">

Hold onto this list. It is the outline for the rest of the day — sections 2
through 8 are each one line of it, taken seriously.

</div>

---
layout: section
---

# Wolverine

---

# Wolverine

- Open source .NET framework
- Command executor — the "mediator" role, without the ceremony
- Background processing
- Asynchronous messaging across a dozen transports
- An alternative HTTP framework
- Deep integration with Marten, Polecat, and EF Core

---

# The aggregate handler workflow

<<< ../src/HelpDesk/Modules/HelpDesk.Incidents/CategoriseIncident.cs#sample_categorise_incident cs {maxHeight:'390px'}

---

# That has a name: the Decider pattern

Jérémie Chassaing's formulation of event sourcing as **two pure functions**:

| | |
|---|---|
| `decide(state, command) → events` | Given where we are, what *should* happen? |
| `evolve(state, event) → state` | Given that happened, where are we now? |

<div class="pt-4">

You have already written both:

</div>

- **decide** is the handler — `Post(CategoriseIncident, Incident) → Events`
- **evolve** is `Apply()` / `Evolve()` on the aggregate

<div class="pt-2"></div>

<div class="gotcha">

Neither one touches a database, a transaction, or a broker. That is not a
coincidence and it is not framework magic — it is the whole point of the
pattern, and it is why these handlers unit test with no mocks at all.

</div>

---

# What just happened

- The simplest event-sourcing write on the planet
- Business logic is a **pure, synchronous function** — no mocks needed to test it
- **A-Frame architecture**: infrastructure on the outside, decisions in the middle
- Optimistic concurrency, handled for you
- The write model's lifecycle, wallpapered over
- **Cascading messages** — return them, Wolverine publishes them

<div class="pt-4 text-sm opacity-70">

Nothing in that method knows about HTTP, Marten, transactions, or Rabbit MQ.

</div>

---

# A-Frame Architecture

<div class="flex justify-center">
  <img src="/a-frame-architecture.png" class="max-h-[270px]" alt="Wolverine at the apex, with Infrastructure and Domain Logic as the two legs" />
</div>

<div class="pt-3">

Wolverine is the **conductor** — it talks to the infrastructure, calls your
domain logic, and keeps those two from ever talking to each other. Neither leg
depends on the other, so the domain side stays a pure function you can test by
calling it.

</div>

---

# Compound Handlers

One message, several methods, each with one job. Wolverine wires them together
and orders them by what feeds what.

| | |
|---|---|
| `Load` / `LoadAsync` | Fetch what the decision needs |
| `Validate` / `Before` | Reject early, short-circuit the rest |
| `Handle` | The decision — pure, no I/O |
| `After` / `PostProcess` | Follow-up work |
| `Finally` | Cleanup, runs regardless |

<div class="pt-4 text-sm opacity-70">

Whatever `Load` returns is available as parameters to everything after it.

</div>

---

# All the I/O in one place

```csharp
public static class ShipOrderHandler
{
    // Runs first. Every database call the handler needs lives here.
    public static async Task<(Order, Customer)> LoadAsync(
        ShipOrder command, IDocumentSession session)
    {
        var order = await session.LoadAsync<Order>(command.OrderId);
        var customer = await session.LoadAsync<Customer>(command.CustomerId);
        return (order, customer);
    }

    // ...so this stays a pure function of its inputs.
    public static IEnumerable<object> Handle(
        ShipOrder command, Order order, Customer customer)
    {
        yield return new MailOvernight(order.Id);
    }
}
```

<div class="pt-2 gotcha">

The A-Frame in one class: `LoadAsync` is the infrastructure leg, `Handle` is
the domain leg, Wolverine is the apex.

</div>

---

# Or skip the loading method entirely

`[Entity]` does the same job declaratively — Wolverine works out the identity
and fetches it before your handler runs.

```csharp
[WolverinePost("/api/todo/update")]
public static Update<Todo> Handle(
    RenameTodo command,
    [Entity] Todo todo)          // loaded using command.Id
{
    todo.Name = command.Name;
    return Storage.Update(todo);
}
```

It finds the id by looking, in order, for an explicit `[Entity("name")]`, then
a `{EntityType}Id` member, then anything called `id` — on the message *or* the
route.

<div class="pt-3 text-sm opacity-70">

Works against Marten, EF Core, RavenDb, and CosmosDB. `[WriteAggregate]` from
earlier is the event-sourced member of the same family.

</div>

---

# Missing entities, and where the id comes from

Required by default. A miss logs and exits cleanly in a message handler, or
returns **404** from an HTTP endpoint — no null check needed.

```csharp
[WolverinePost("/api/todo/maybecomplete")]
public static IStorageAction<Todo> Handle(
    MaybeCompleteTodo command,
    [Entity(Required = false)] Todo? todo)
{
    if (todo == null) return Storage.Nothing<Todo>();

    todo.IsComplete = true;
    return Storage.Update(todo);
}
```

<div class="pt-2 gotcha">

`ValueSource` pins down where the id may come from — `InputMember`,
`RouteValue`, `FromQueryString`, `Header`, `Claim`. Use a `Load` method when
the fetch is interesting; `[Entity]` when it is just "get me this by id."

</div>

---

# Unwinding the magic

Wolverine writes the glue code for you — and you can read every line of it.

```bash
dotnet run codegen preview
```

- Middleware is **compiled in**, not resolved through a pipeline on every call
- `[WolverineBefore]` methods run first and can short-circuit with `ProblemDetails`
- Side effect types (`IStartStream`, `Events`, `OutgoingMessages`) let handlers
  stay pure and still cause things to happen
- Fluent Validation slots in as middleware

<div class="pt-4 gotcha">

Marten 9 and Wolverine 6 went **opposite directions** here, which trips people
up. Marten dropped runtime code generation entirely for source generators.
Wolverine kept Roslyn, generating at startup by default — `codegen write` moves
that to build time, and that is what you do for production.

</div>

---

# Coming up next

**Section 2 — Asynchronous Messaging.** Those cascading messages have to go
somewhere. We'll route them locally, then over a broker, and look at the three
different ways to get Marten events onto the bus.

---
layout: end
---

# Resources

<div class="text-left text-base pt-4">

- JasperFx Software — <https://jasperfx.net>
- Marten — <https://martendb.io>
- Wolverine — <https://wolverinefx.net>
- Polecat — <https://polecat.jasperfx.net>

</div>
