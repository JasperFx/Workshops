---
theme: default
title: Data Consistency
info: Critter Stack Workshop — Section 4 of 8
class: text-center
highlighter: shiki
lineNumbers: false
transition: slide-left
mdc: true
drawings:
  persist: false
---

# Data Consistency

<div class="pt-8 opacity-70">
Section 4 &mdash; the hard one
</div>

---
layout: two-cols
---

# Where we are

<Agenda :current="4" />

::right::

<div class="pl-6 pt-14 text-sm opacity-80">

Section 3 kept work from being lost.

This section keeps it correct.

</div>

---
layout: statement
---

## Most of the pain teams attribute to event sourcing is actually eventual consistency

### They are separable, and the choice is yours

---

# Strong vs. eventual

| | Strong | Eventual |
|---|---|---|
| Read after write | Always current | Might be stale |
| Write cost | Higher | Lower |
| Read scale | Limited | Excellent |
| Failure mode | Slow or unavailable | Quietly wrong |

<div class="pt-6">

Choose per read, not per system. The same event store can serve an inline
projection to a command handler and an async projection to a dashboard.

</div>

---
layout: section
---

# The dual-write problem

---

# Two writes, one story

```csharp
// Save the incident
await session.SaveChangesAsync();

// Tell everyone about it
await bus.PublishAsync(new IncidentLogged(...));
```

<div class="pt-6">

What if the process dies between line 2 and line 5?

</div>

<div class="pt-4">

You have an incident nobody knows about. Swap the order and you have a
notification about an incident that doesn't exist. There is no ordering of two
independent writes that is safe.

</div>

---

# The transactional outbox

Write the message to the **same database, in the same transaction** as the data.
A background process forwards it afterwards.

- If the transaction commits, the message will be sent
- If it rolls back, the message was never there
- One atomic write, no distributed transaction, no two-phase commit

<div class="pt-6">

This is not a Critter Stack idea. It is the standard answer. What the Critter
Stack does is make it a configuration line instead of a project.

</div>

---

# Turning it on

<<< ../src/HelpDesk/HelpDesk.Api/Program.cs#sample_helpdesk_main_store cs

<div class="pt-2 text-sm opacity-70">

…plus three policy lines in the Wolverine setup:

</div>

```csharp
opts.Policies.AutoApplyTransactions();
opts.Policies.UseDurableLocalQueues();
opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
```

---

# What that buys you

- `AutoApplyTransactions` wraps handlers that touch persistence in a transaction
- Durable queues put local messages through the same machinery
- Marten's `IntegrateWithWolverine()` shares one session, and therefore one
  transaction, between the event append and the outgoing messages

---

# Idempotency is still your job

At-least-once delivery means a handler will occasionally run twice.

- **Durable endpoints** deduplicate on the inbox side by message id
- **Transactional middleware** makes a repeat run a no-op if it writes the same events
- **Non-transactional handlers** — calling an external API, sending an email —
  have no such protection

<div class="pt-4 gotcha">

The pattern that works: make the handler's effect a function of the message,
then re-running it is harmless. The pattern that doesn't: `balance += amount`.

</div>

---

# Idempotency, concretely

<<< ../src/HelpDesk/Modules/HelpDesk.Billing/BillableItem.cs#sample_billing_handler cs {maxHeight:'390px'}

---
layout: section
---

# Read your own writes

---

# The week-two problem

<div class="pt-4">

A user creates an incident. The UI redirects to the incident page. The page is
served by an async projection that hasn't caught up yet.

</div>

<div class="pt-4">

The user sees a 404 on the thing they just made.

</div>

<div class="pt-8">

This is the single most common complaint about CQRS, and there are four good
answers.

</div>

---

# Four answers

1. **Make that one projection inline.** Strong consistency where it matters,
   eventual everywhere else
2. **Return the new state from the command.** `FetchLatest` gives the handler
   the post-append aggregate — hand it straight back in the response
3. **Wait, briefly.** `QueryForNonStaleData(15.Seconds())` blocks until the
   daemon catches up. Use with care and a short timeout
4. **Fix it in the UI.** The client already has the data it just submitted;
   render optimistically

<div class="pt-4 gotcha">

Option 2 is usually the best one and the least used. The command already
computed the new state — don't throw it away.

</div>

---

# Sagas and process managers

Multi-step workflows that span time, messages, and failure.

- State is persisted, so a saga survives restarts
- Timeouts are first-class — "if no response in 4 hours, escalate"
- Each step is individually retryable
- The saga's state *is* the consistency boundary

<div class="pt-6 text-sm opacity-70">

Use a saga when the workflow has memory. Use cascading messages when it doesn't.

</div>

---

# Compensating actions

You cannot have a distributed transaction across services. You can have an
apology.

- Book the technician. If billing later fails, cancel the booking
- Model the compensation as a first-class event — `TechnicianBookingCancelled`
- The log tells the true story: it happened, then it was undone

<div class="pt-6 gotcha">

This is a *domain* decision, not a technical one. "What do we do if step three
fails?" is a question for the business, and they usually have an answer already.

</div>

---
layout: section
---

# Projections over time

---

# Rebuilds

Projections are derived data, so they can always be thrown away and rebuilt.
That is a superpower and an operational hazard.

- Fixing a projection bug is a rebuild, not a migration script
- Rebuilding a large event store takes real time
- The projection is unavailable, or stale, while it rebuilds

Strategies: rebuild into a new table and swap; rebuild a single tenant;
rebuild from a checkpoint rather than from zero.

---

# Event schema evolution

Events are permanent. The code that reads them is not.

- **Additive changes** are free — add a nullable property
- **Renames** need a type alias so old JSON still resolves
- **Structural changes** need an upcaster: old JSON in, new event out
- **Deletions** are the hard case — the data is still there forever

<div class="pt-4 gotcha">

Decide your versioning policy in week one, not week fifty. The cheapest policy
is "never change an event, only add new ones" and it is worth the ugliness.

</div>

---

<Demo path="src/HelpDesk">

Break the outbox and produce a real inconsistency. Turn it back on. Then rebuild
a projection against a live system, and upcast an event schema.

</Demo>

<div class="pt-4 text-sm opacity-60">
TODO — depends on the HelpDesk sample application.
</div>

---

# When eventual consistency is the wrong answer

- Anything a human will immediately re-read and be confused by
- Invariants that must hold across a single aggregate — use inline, or don't
  split it
- Regulatory reads where "briefly wrong" is a compliance event

<div class="pt-6">

Async projections are the default for a reason, but the default is not a law.

</div>

---

# Coming up next

**Section 5 — Modular Monolith.** One database, one process, several bounded
contexts. Where do the seams go, and what crosses them?

---
layout: end
---

# Resources

<div class="text-left text-base pt-4">

- Wolverine durable messaging — <https://wolverinefx.net/guide/durability/>
- Marten projections — <https://martendb.io/events/projections/>
- Event versioning — <https://martendb.io/events/versioning>

</div>
