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

And one more, which is not on the old version of this slide: **teach you when
not to do any of this.** Every section ends with a "this is the wrong answer
when…" slide.

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
- Postgres lands on **5440**, Rabbit on **5682** — deliberately off the default
  ports so this workshop never collides with anything else you have running

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
| **Wolverine** | Command execution, messaging, background work, HTTP |
| **Weasel** | Schema management underneath both |
| **CritterWatch** | Commercial management and observability |

<div class="pt-6">

Shared core in `JasperFx` / `JasperFx.Events`, so Marten and Polecat are
increasingly the same engine pointed at different databases.

</div>

---

# The single most misunderstood thing

<div class="text-2xl pt-4 pb-8">

Using Marten **does not** mean going all-in on event sourcing.

</div>

Mix event-sourced aggregates, plain documents, and EF Core entities in one
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

# What it costs

- Every read model is **eventually consistent** unless you pay for it
- **Schema evolution** is a real, permanent job — events live forever
- Your team has to learn a genuinely different way to model state
- "Just look at the table" debugging stops working
- **Right-to-be-forgotten** takes design work against an append-only log

<div class="pt-4 text-sm opacity-70">

On that last one — Marten has stream archiving and event data masking
(`AddMaskingRuleForProtectedInformation`) precisely for this. It is a solved
problem, but only if you decide *which* fields are personal before you have
five years of history.

</div>

<div class="pt-4 gotcha">

**When it's the wrong answer:** CRUD screens over reference data. A reporting
database. Anything where nobody will ever ask "how did we get here?"

</div>

---

# The domain: a help desk

Incidents get logged, categorised, prioritised, assigned, responded to,
resolved, acknowledged, and closed.

<div class="pt-4"></div>

<<< ../src/01-quickstarts/EventSourcingQuickstart/Events.cs#sample_incident_events cs {maxHeight:'280px'}

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

# Appending to it

<<< ../src/01-quickstarts/EventSourcingQuickstart/Program.cs#sample_es_appending cs

---

# The aggregate

<<< ../src/01-quickstarts/EventSourcingQuickstart/Incident.cs#sample_incident_aggregate cs {maxHeight:'380px'}

---

# Or mutable, if your team prefers

<<< ../src/01-quickstarts/EventSourcingQuickstart/MutableIncident.cs#sample_incident_aggregate_mutable cs {maxHeight:'380px'}

<div class="pt-2 text-sm opacity-70">

Marten supports both. Pick whichever your team argues about less.

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

# Unwinding the magic

Wolverine writes the glue code for you — and you can read every line of it.

```bash
dotnet run -- codegen preview
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
