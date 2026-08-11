---
theme: default
title: Resiliency & Concurrency
info: Critter Stack Workshop — Section 3 of 8
class: text-center
highlighter: shiki
lineNumbers: false
transition: slide-left
mdc: true
drawings:
  persist: false
---

# Resiliency & Concurrency

<div class="pt-8 opacity-70">
Section 3 &mdash; what happens when things go wrong
</div>

---
layout: two-cols
---

# Where we are

<Agenda :current="3" />

::right::

<div class="pl-6 pt-14 text-sm opacity-80">

We have messages crossing process boundaries.

Now everything that can break, breaks.

</div>

---

# Goals

<div class="text-xl pt-6 pb-6">

1. No work is lost
2. Data stays consistent
3. Humans get paged as rarely as possible

</div>

Everything in this section is in service of one of those three.

---

# Things go wrong

<div class="grid grid-cols-2 gap-x-8">

<div>

- Transient errors
- Network hiccups
- Database timeouts
- Infrastructure is down
- External services unavailable

</div>

<div>

- Unexpected slowness
- Concurrency conflicts
- System crashes
- Other people's code has bugs

</div>

</div>

<div class="pt-8">

These fail differently and want different responses. A single global "retry 3
times" policy treats them all the same, which is why it doesn't work.

</div>

---

# Delivery guarantees

| | Meaning | Cost |
|---|---|---|
| **Fire and forget** | Best effort, no durability | Fastest, loses work |
| **At least once** | Delivered, possibly more than once | Needs idempotent handlers |
| **At most once** | Never duplicated, may be lost | Rarely what you want |
| **Expiration** | Discard if too old to matter | Needs a meaningful deadline |

<div class="pt-6 gotcha">

At-least-once is the practical default. That makes **idempotency your problem**,
not the framework's — section 4.

</div>

---
layout: section
---

# Error handling policies

---

# The vocabulary

| Policy | Use when |
|---|---|
| `RetryWithCooldown` | Transient, will probably work in 50ms |
| `Requeue` | Transient, but let other messages go first |
| `ScheduleRetry` | Will work later, but not soon |
| `PauseThenRequeue` | The whole downstream subsystem is down |
| `Discard` | The message can never succeed |
| `MoveToErrorQueue` | A human needs to look at this |

---

# Matched by exception type

<<< ../src/HelpDesk/Modules/HelpDesk.Scheduling/ScheduleTechnician.cs#sample_scheduling_error_policies cs {maxHeight:'350px'}

<div class="pt-2 text-sm opacity-70">

Policies apply globally, per handler, or per endpoint. This one is per handler.

</div>

---

# Circuit breakers

<<< ../src/HelpDesk/Modules/HelpDesk.Scheduling/SchedulingModule.cs#sample_scheduling_circuit_breaker cs {maxHeight:'360px'}

---

# Timeouts

- Message execution timeouts stop a wedged handler from holding a slot forever
- `opts.DefaultExecutionTimeout` sets the fleet default
- `[MessageTimeout(seconds)]` overrides it per handler
- A hung handler with no timeout is indistinguishable from a deadlock

---

# The token is not decoration

<<< ../src/HelpDesk/Modules/HelpDesk.Scheduling/ScheduleTechnician.cs#sample_cancellation_token_handler cs

<div class="pt-3 gotcha">

Wolverine cancels that token when the timeout fires, but the handler is only
actually interruptible if it **passes the token all the way down** to whatever
is blocking. A handler that accepts a token and ignores it times out on paper
and hangs in production.

</div>

---

# Transient errors and backoff

- Polly sits underneath Marten for database-level transients
- Wolverine's retry policies handle the message level
- **Exponential backoff** on anything that talks to a network
- The "distressed subsystem" pattern: pause the endpoint, not the process

---

# Projection error handling

The async daemon is a long-running process reading your entire event history.
It will eventually meet an event it cannot handle.

- **Serialization errors** — an event type that no longer deserializes
- **Application errors** — a bug in an `Apply` method
- **Poison pills** — one event that kills the daemon on every restart

Options: skip, log and continue, or stop the projection and alert.

<div class="pt-4 gotcha">

The default should be *stop and alert* in production. Silently skipping events
means your read model is quietly wrong, which is worse than being down.

</div>

---

# Dead letter queues

- Where messages go when the policies are exhausted
- Store the exception, the message, and enough context to diagnose
- **Replay** matters as much as capture — a DLQ you can't drain is a graveyard
- Wolverine can persist dead letters to the database, not just the broker

---
layout: section
---

# Concurrency

---

# The problem

Two agents open the same incident. Both categorise it. Both commands read
version 4 and decide to append.

<div class="pt-6">

Without protection: two events appended, both "valid" against a state that no
longer existed by the time they were written.

</div>

<div class="pt-6">

An append-only log has an obvious defence — the stream revision number.

</div>

---

# Optimistic concurrency

```csharp
// Reads the stream, remembers the revision, and fails the append
// if anything else got there first.
var stream = await session.Events.FetchForWriting<Incident>(incidentId, expectedVersion);
```

- `FetchForWriting` — read for a write, with concurrency protection
- `FetchLatest` — read the current state, no write intended
- `[WriteAggregate]` — the Wolverine middleware that does this for you

The command carries the version the user was looking at. If it's stale, reject.

---

# Somebody has to turn that into a status code

<<< ../src/HelpDesk/HelpDesk.Api/ConcurrencyExceptionHandler.cs#sample_concurrency_exception_handler cs {maxHeight:'330px'}

---

# And a test that proves it

<<< ../src/HelpDesk/HelpDesk.Tests/EndToEndTests.cs#sample_optimistic_concurrency_test cs {maxHeight:'370px'}

---

# Handling the conflict

Three honest options, in increasing order of effort:

1. **Reject** — 409 back to the caller, let them re-read and retry. Correct, and
   often the right user experience
2. **Retry** — treat `ConcurrencyException` as a retryable error policy and
   re-run the handler against fresh state
3. **Merge** — domain-specific reconciliation. Expensive, occasionally worth it

<div class="pt-4 gotcha">

Option 2 is only safe if the handler is a pure function of the aggregate. Which
is exactly what the A-Frame architecture from section 1 gives you.

</div>

---

# Exclusive locking

When optimistic concurrency isn't enough, Marten can take a real lock.

- Correct, and simple to reason about
- Serialises all writes to that stream
- A throughput ceiling and a deadlock risk

<div class="pt-6 gotcha">

Reach for this when conflicts are frequent *and* retrying is expensive. If
conflicts are rare, optimistic concurrency is strictly better.

</div>

---

# Ordering vs. parallelism

```csharp
opts.LocalQueueFor<TryAssignPriority>()
    .Sequential();                    // strict order, one at a time

opts.LocalQueueFor<SendNotification>()
    .MaximumParallelMessages(10);     // throughput, no order
```

- Global ordering is expensive and usually unnecessary
- **Per-stream** ordering is usually what people actually mean
- Partitioned listeners give you ordering within a key and parallelism across keys

---

# Across many nodes

- Wolverine distributes durable work across the running nodes
- Leadership election for singleton work — the daemon, scheduled jobs
- A node that dies has its work reassigned
- This is why the durable inbox is in the database and not in memory

---

<Demo path="src/HelpDesk/HelpDesk.Api/Resiliency.http" run="dotnet run">

A technician-scheduling service that fails on demand. Healthy, then flaky,
then hard down — watch the breaker latch the queue and the backlog build.
Then bring it back and watch it drain.

</Demo>

---

# When this is over-engineering

- A read-only endpoint does not need a dead letter queue
- A message with one consumer that can always be recomputed does not need durability
- If a conflict has never happened and the write rate is one per minute, an
  optimistic version check is enough forever

<div class="pt-6">

Resiliency machinery has an operational cost. Add it where you have evidence.

</div>

---

# Coming up next

**Section 4 — Data Consistency.** We've kept work from being lost. Next: keeping
it *correct*, which turns out to be a different problem.

---
layout: end
---

# Resources

<div class="text-left text-base pt-4">

- Wolverine error handling — <https://wolverinefx.net/guide/handlers/error-handling.html>
- Marten optimistic concurrency — <https://martendb.io/events/appending>
- Projection error handling — <https://martendb.io/events/projections/>

</div>
