---
theme: default
title: Asynchronous Messaging
info: Critter Stack Workshop — Section 2 of 8
class: text-center
highlighter: shiki
lineNumbers: false
transition: slide-left
mdc: true
drawings:
  persist: false
---

# Asynchronous Messaging

## with Wolverine

<div class="pt-8 opacity-70">
Section 2 &mdash; the cascading messages have to go somewhere
</div>

---
layout: two-cols
---

# Where we are

<Agenda :current="2" />

::right::

<div class="pl-6 pt-14 text-sm opacity-80">

Section 1 ended with a handler returning `OutgoingMessages`.

This hour is about what happens to them.

</div>

---

# Why go asynchronous at all

- **Temporal decoupling** — the caller doesn't wait for work it doesn't need
- **Load levelling** — a queue absorbs a spike that would otherwise topple you
- **Failure isolation** — the notification service being down is not an outage
- **Fan-out** — one event, many independent consumers

<div class="pt-6 gotcha">

**When it's the wrong answer:** the caller needs the result to render the next
screen. Async messaging trades latency and simplicity for throughput and
resilience. If you're not spending that budget on something, don't.

</div>

---

# One handler, several delivery modes

The same handler method runs whether the message arrived from a local queue,
Rabbit MQ, or a direct in-process call.

| Call | Semantics |
|---|---|
| `InvokeAsync(msg)` | Execute now, in this thread, and wait. The "mediator" role |
| `InvokeAsync<T>(msg)` | Same, but returns a response |
| `SendAsync(msg)` | Route to exactly one destination |
| `PublishAsync(msg)` | Route to every interested subscriber |
| `ScheduleAsync(msg, delay)` | Deliver later |

<div class="pt-4 text-sm opacity-70">

This is why "we'll add a broker later" is not a rewrite with Wolverine.

</div>

---

# Routing

- **Local queues** — in-process, in-memory or durable. The default destination
  for a message with a handler and no explicit routing
- **Conventional routing** — derive the queue or topic name from the message type
- **Explicit rules** — `PublishMessage<T>().ToRabbitExchange("...")`

<div class="pt-6">

Start local. Move a message to a broker by changing configuration, not handlers.

</div>

---

# Transports

<div class="grid grid-cols-2 gap-x-8 pt-2">

<div>

**Brokers**
- Rabbit MQ
- Azure Service Bus
- Amazon SQS / SNS
- Google Pub/Sub
- Kafka
- MQTT

</div>

<div>

**Other**
- PostgreSQL-backed queues
- SQL Server-backed queues
- TCP
- SignalR
- gRPC

</div>

</div>

<div class="pt-6 gotcha">

The database-backed queues deserve more attention than they get. If you already
have Postgres and you need durable async work — not cross-team integration —
you may not need a broker at all.

</div>

---
layout: section
---

# Getting events onto the bus

---

# Three strategies, three trade-offs

This is the decision people get wrong most often, so let's be precise about it.

| Strategy | Ordering | Latency | Runs where |
|---|---|---|---|
| `EventForwardingToWolverine` | **None** | Lowest | Inline with the append |
| `PublishEventsToWolverine` | **Strict** | Daemon lag | Async daemon |
| `ISubscription` | Strict, batched | Daemon lag | Async daemon |

---

# Event forwarding

```csharp
.EventForwardingToWolverine(opts =>
{
    opts.SubscribeToEvent<IncidentCategorised>()
        .TransformedTo(e => new TryAssignPriority(e.StreamId, e.Data.CategorisedBy));
});
```

<div class="text-sm opacity-70 pt-2">

The Help Desk sample takes the other route — the endpoint returns the message
directly as a cascading message, which is simpler when the command already
knows what should happen next.

</div>

- Fires as part of the same transaction that appended the event
- Fast, and it uses the outbox, so nothing is lost
- **No ordering guarantee** between events

Use it when each message is independently meaningful.

---

# Ordered subscriptions

```csharp
.PublishEventsToWolverine("PriorityAssignments", r =>
{
    r.PublishEvent<IncidentCategorised>();
});
```

- The async daemon walks the event store in order and publishes as it goes
- Strictly ordered, and resumable from a known position
- Pays the daemon's latency

Use it when a downstream consumer's correctness depends on sequence.

---

# Custom subscriptions

`ISubscription` gives you a batch of events and a session, and lets you do
whatever you want — call an external API, write to a search index, emit a file.

- Ordered, checkpointed, and restartable
- Batch size and error handling are yours to configure
- The escape hatch when neither of the above fits

<div class="pt-6 text-sm opacity-70">

Section 3 covers what happens when the thing you're calling is down.

</div>

---

# Cascading messages

A handler returns messages instead of sending them.

<<< ../src/HelpDesk/Modules/HelpDesk.Incidents/TryAssignPriority.cs#sample_try_assign_priority cs {maxHeight:'250px'}

- The handler stays a pure function — testable with no mocks
- Wolverine publishes the outgoing messages **after** the transaction commits
- Combined with the outbox, this is exactly-once-ish delivery without ceremony

<div class="pt-4 gotcha">

The alternative — injecting `IMessageBus` and calling `PublishAsync` mid-handler —
works, but you've now got a handler that needs a mock to test and can publish a
message for a transaction that then rolls back.

</div>

---

# Scheduled and delayed delivery

```csharp
await bus.ScheduleAsync(new EscalateIncident(id), 4.Hours());
```

- Backed by the durable inbox, so it survives a restart
- The building block for timeouts, reminders, and SLA escalation
- Sagas turn this into stateful workflows — a first look here, more in section 4

---

# Stateful resources

Queues, exchanges, bindings, and database tables are all `IResource`s that
Wolverine and Marten know how to create.

<<< ../src/HelpDesk/HelpDesk.Notifications/Program.cs#sample_notifications_host cs

```bash
dotnet run -- resources list
dotnet run -- resources setup
```

<div class="pt-4 gotcha">

`AutoProvision` at startup is wonderful in development. Whether you want it in
production depends entirely on your DevOps policy — section 8 revisits this.

</div>

---

# Talking to systems that aren't Wolverine

- Message type aliases so the wire name isn't a .NET type name
- Custom serialization per endpoint
- Interop with MassTransit, NServiceBus, and CloudEvents
- Raw JSON in and out for systems that just post to a queue

<div class="pt-6 text-sm opacity-70">

Worth planning early. Renaming a message type after it's on the wire is the
same problem as renaming an event — section 4.

</div>

---

# The handler on the other side

<<< ../src/HelpDesk/HelpDesk.Notifications/Program.cs#sample_notification_handler cs

---

# And the one line that sends it there

<<< ../src/HelpDesk/HelpDesk.Api/Program.cs#sample_helpdesk_wolverine cs {maxHeight:'340px'}

---

<Demo path="src/HelpDesk" run="dotnet run">

The Help Desk API publishing to a separate notification service over Rabbit MQ,
then the same message moved back to a local queue by deleting one routing rule.

</Demo>

---

# Coming up next

**Section 3 — Resiliency & Concurrency.** We now have messages crossing process
boundaries. Next: what happens when the other side is broken, slow, or two
requests arrive for the same incident at once.

---
layout: end
---

# Resources

<div class="text-left text-base pt-4">

- Wolverine messaging — <https://wolverinefx.net/guide/messaging/>
- Rabbit MQ transport — <https://wolverinefx.net/guide/messaging/transports/rabbitmq/>
- Marten event subscriptions — <https://martendb.io/events/subscriptions>

</div>
