---
theme: default
title: Testing & DevOps
info: Critter Stack Workshop — Section 8 of 8
class: text-center
highlighter: shiki
lineNumbers: false
transition: slide-left
mdc: true
drawings:
  persist: false
---

# Testing & DevOps

<div class="pt-8 opacity-70">
Section 8 &mdash; knowing it works, and getting it shipped
</div>

---
layout: two-cols
---

# Where we are

<Agenda :current="8" />

::right::

<div class="pl-6 pt-14 text-sm opacity-80">

Last hour.

Everything we've built, now proven and deployed.

</div>

---

# A differently shaped pyramid

The usual advice — mostly unit tests, few integration tests — assumes
integration tests are slow and flaky.

<div class="pt-4">

With this stack they are neither. Marten resets state in milliseconds, Alba runs
the real app in-process, and Wolverine's tracking makes async assertions
deterministic.

</div>

<div class="pt-4 gotcha">

So the pyramid gets fatter in the middle. Write the integration test. It tests
something real, and it costs about a second.

</div>

---
layout: section
---

# Unit tests

---

# The A-Frame payoff

<<< ../src/HelpDesk/HelpDesk.Tests/UnitTests.cs#sample_pure_function_unit_tests cs {maxHeight:'390px'}

---

# Given / When / Then

Event-sourced aggregates want a specification style, and event sourcing gives it
to you for free.

- **Given** a list of events
- **When** a command is handled against the aggregate they produce
- **Then** these events are appended

<div class="pt-6">

The test reads like the requirement, and the "given" is literally the domain's
own vocabulary.

</div>

---
layout: section
---

# Integration tests

---

# Alba

Runs the real application in-process — real routing, real middleware, real DI,
real serialization.

<<< ../src/HelpDesk/HelpDesk.Tests/IntegrationContext.cs#sample_app_fixture cs {maxHeight:'330px'}

---

# The shared test context

<<< ../src/HelpDesk/HelpDesk.Tests/IntegrationContext.cs#sample_integration_context cs {maxHeight:'380px'}

---

# The async problem

The command returned. Did the cascading message get handled yet?

<<< ../src/HelpDesk/HelpDesk.Tests/EndToEndTests.cs#sample_full_lifecycle_test cs {maxHeight:'340px'}

- Wolverine's tracking waits until **all** cascading activity is finished
- No `Thread.Sleep`, no polling, no flakiness
- And you can assert on messages that were sent, not just on final state

<div class="pt-4 gotcha">

This is the feature that makes testing event-driven systems tolerable. Without
it, every async test is a race condition with a timeout.

</div>

---

# What a tracked session holds

The session is sliced by **what happened to a message**, not by its type:

<div class="grid grid-cols-2 gap-x-8 pt-2 text-sm">

<div>

- `Sent`
- `Received`
- `Executed`
- `ExecutionStarted` / `ExecutionFinished`
- `MessageSucceeded`

</div>

<div>

- `MessageFailed`
- `Requeued`
- `MovedToErrorQueue`
- `NoHandlers` / `NoRoutes`
- `Scheduled` / `Discarded`

</div>

</div>

<div class="pt-4 text-sm opacity-70">

Plus `AllRecordsInOrder()` and `AllExceptions()` across the whole cascade.

</div>

---

# Reading what happened

<<< ../src/HelpDesk/HelpDesk.Tests/TrackedSessionTests.cs#sample_reading_the_messages_sent cs {maxHeight:'370px'}

<div class="pt-2 text-sm opacity-70">

`AllRecordsInOrder()` is the one to print when a test fails and you have no
idea why.

</div>

---

# Asserting on what was published

<<< ../src/HelpDesk/HelpDesk.Tests/TrackedSessionTests.cs#sample_asserting_on_messages_sent cs {maxHeight:'380px'}

---

# Why that last test needs no broker

`NotificationRequested` is routed to Rabbit MQ in production. The fixture calls
`DisableAllExternalWolverineTransports()`, so instead of leaving the process it
lands in `Sent` — where a test can assert on it.

<div class="pt-4"></div>

| | |
|---|---|
| `SingleMessage<T>()` | exactly one, or a useful failure |
| `MessagesOf<T>()` | all of them |
| `SingleEnvelope<T>()` | the metadata — destination, headers, tenant |

---

# Tracking waits for messages, not for Marten

<div class="pt-2">

Wolverine's tracking knows nothing about Marten's async daemon. When
`TrackedHttpCall` returns, every **message** is done — but an **Async
projection** can still be behind.

</div>

<<< ../src/HelpDesk/HelpDesk.Tests/TrackedSessionTests.cs#sample_waiting_for_marten_async_projections cs {maxHeight:'270px'}

<div class="pt-2 gotcha">

Two different kinds of "async", two different waits. Using only the first is
the most common cause of a projection test that passes locally and fails in CI.

</div>

---

# Controlling state

```csharp
await theStore.Advanced.ResetAllData();
```

- Wipes everything and re-runs `IInitialData` seeding
- Fast enough to run before every test
- **Self-contained tests**: each test sets up what it needs and asserts on it,
  with no dependence on execution order

For async projections, `WaitForNonStaleProjectionDataAsync` gives a deterministic
assertion point instead of a sleep.

---

# In CI

- Postgres and Rabbit as service containers, or Testcontainers
- The same `docker compose` file developers use locally
- Schema is created by the app on startup — no migration step to keep in sync

---
layout: section
---

# DevOps

---

# The command line

Every Critter Stack app gets a diagnostic CLI for free.

```bash
dotnet run -- describe            # what is this app configured to do?
dotnet run -- resources list      # queues, exchanges, tables
dotnet run -- resources setup     # create them
dotnet run -- db-apply            # apply schema changes
dotnet run -- db-assert           # is the schema what the app expects?
dotnet run -- projections         # projection status, rebuilds
```

<div class="pt-4 text-sm opacity-70">

`describe` is the fastest way to answer "why isn't my handler firing?"

</div>

---

# Schema management

Three strategies, and you should pick deliberately:

1. **Auto-create at startup** — wonderful in development, and some teams run it
   in production quite happily
2. **`db-apply` as a deployment step** — the app never has DDL rights at runtime
3. **Export SQL and hand it to a DBA** — Weasel will generate the script

<div class="pt-4 gotcha">

Whichever you pick, put **`db-assert` in your startup or health check**. Finding
out at 3am that the schema drifted is avoidable.

</div>

---

# Environment checks

```bash
dotnet run -- check-env
```

- Can we reach the database? The broker?
- Is the schema current?
- Are the required queues present?

Run it as a smoke test after deployment, before routing traffic to the new
instance.

---

# Zero-downtime deployment

The event store makes most of this easy, and one part hard.

- **Events are append-only** — old and new code can both read them, if you
  versioned properly (section 4)
- **Projections are the hard part** — a projection whose shape changed needs a
  rebuild, and the old code is still reading the old shape

Strategy: build the new projection into a new table under a new name, let it
catch up, then switch readers over.

---

# Cold start and serverless

New in the 2026 releases, and relevant if you deploy to Lambda or Container Apps:

- Marten 9 removed runtime code generation entirely — source generators instead
- AOT compliance across the stack
- Wolverine's runtime compiler is a development-time dependency now, so deployed
  images are smaller
- Polecat was built with cold start as a first-class concern

---

# Production checklist

<div class="grid grid-cols-2 gap-x-8 text-sm pt-2">

<div>

**Correctness**
- Outbox enabled on every sending endpoint
- Handlers idempotent
- Concurrency policy chosen per aggregate
- Event versioning policy written down

</div>

<div>

**Operations**
- Projection lag alerting
- Dead letter queue monitored and drainable
- `db-assert` in health checks
- Traces propagating across transports
- Tenant isolation covered by a test

</div>

</div>

<div class="pt-8 text-center opacity-70">

This is the takeaway artifact. Steal it.

</div>

---

<Demo path="src/HelpDesk" run="dotnet test">

The full test suite. Then `describe`, then a schema migration applied from the
command line.

</Demo>

<div class="pt-4 text-sm opacity-60">
TODO — depends on the HelpDesk sample application.
</div>

---
layout: end
---

# Thank you

<div class="text-left text-base pt-4">

- JasperFx Software — <https://jasperfx.net>
- Marten — <https://martendb.io>
- Wolverine — <https://wolverinefx.net>
- Polecat — <https://polecat.jasperfx.net>
- Alba — <https://jasperfx.github.io/alba>

</div>
