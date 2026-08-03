---
theme: default
title: Observability, Metadata, and Metrics
info: Critter Stack Workshop — Section 7 of 8
class: text-center
highlighter: shiki
lineNumbers: false
transition: slide-left
mdc: true
drawings:
  persist: false
---

# Observability, Metadata, and Metrics

<div class="pt-8 opacity-70">
Section 7 &mdash; seeing what your system is doing
</div>

---
layout: two-cols
---

# Where we are

<Agenda :current="7" />

::right::

<div class="pl-6 pt-14 text-sm opacity-80">

A request now crosses HTTP, a handler, a transaction, an outbox, a broker,
another process, and a projection daemon.

When it goes wrong at 3am, you need to be able to follow it.

</div>

---
layout: section
---

# Metadata

---

# Event metadata

Every event Marten stores carries more than your payload:

| | |
|---|---|
| `Id` / `StreamId` | Identity |
| `Version` / `Sequence` | Position in the stream, and globally |
| `Timestamp` | When it was appended, by the database clock |
| `CorrelationId` | The whole operation this belongs to |
| `CausationId` | The specific message that caused it |
| `Headers` | Whatever else you need |
| `TenantId` | From section 6 |

---

# Why this matters more than it sounds

- **Audit** — "who changed this, when, and as part of what?" answered from data
  you already have
- **Debugging** — causation chains reconstruct a failure across services
- **Retrofitting metrics** — the log already contains the answer to a question
  nobody had asked yet when the code was written

<div class="pt-6 gotcha">

Metadata you didn't capture is gone forever. This is the one part of the design
that is genuinely hard to add later, because history won't have it.

</div>

---

# Turning it on

Metadata columns cost storage and a little write time, so several are opt-in.

```csharp
opts.Events.MetadataConfig.CausationIdEnabled = true;
opts.Events.MetadataConfig.CorrelationIdEnabled = true;
opts.Events.MetadataConfig.HeadersEnabled = true;
```

Wolverine populates correlation and causation ids automatically when it is
driving the transaction.

---

# Audited members

Mark the fields worth pulling out of the payload and into structured logs and
traces.

```csharp
public record CategoriseIncident([property: Audit] Guid IncidentId, ...);
```

- Shows up in log messages, activity tags, and diagnostics
- Turns "a CategoriseIncident failed" into "CategoriseIncident for incident
  1234 failed"

---
layout: section
---

# Tracing

---

# Open Telemetry

Both tools emit standard `ActivitySource` spans.

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource("Wolverine")
        .AddSource("Marten")
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
```

- Wolverine spans: message received, executed, sent, retried, dead-lettered
- Marten spans: session commits, projection work, daemon activity
- Context propagates **across transports** — the trace survives the broker hop

---

# What a good trace looks like

One trace, spanning:

1. `POST /api/incidents/categorise` — the HTTP request
2. The handler execution
3. The Marten transaction that appends the event and writes the outbox row
4. The outbox send to Rabbit MQ
5. The notification service receiving and handling it

<div class="pt-6">

If step 5 shows up under a *different* trace, propagation is misconfigured, and
you have just lost the ability to debug your own system.

</div>

---

# Metrics

Wolverine publishes meters out of the box:

- Messages sent, received, succeeded, failed, dead-lettered
- Execution duration, per message type
- **Queue depth and message age** — the two that actually predict an incident
- Circuit breaker state

Marten adds projection lag, daemon health, and connection usage.

<div class="pt-4 gotcha">

Projection lag is the single most important metric in an event-sourced system.
It is the number that tells you whether your read models are lying.

</div>

---

# CritterWatch

The commercial management and observability tool for the Critter Stack.

- Purpose-built for Marten and Wolverine internals — not generic APM
- Message flow, dead letter inspection and replay, projection status
- Operational control, not just dashboards

<div class="pt-6 text-sm opacity-60">

TODO — confirm the exact 1.0 feature list and licensing story before delivery,
and decide whether attendees get hands-on access or this stays a demo.

</div>

---

# Health checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<WolverineHealthCheck>("wolverine");
```

- Is the node processing messages?
- Is the projection daemon running and within an acceptable lag?
- Can we reach the database and the broker?

A health check that only pings the process is a health check that reports green
during an outage.

---

# The local stack

```bash
docker compose --profile observability up -d
```

| | |
|---|---|
| Jaeger | <http://localhost:16686> — traces |
| Prometheus | <http://localhost:9090> — metrics |
| Grafana | <http://localhost:3000> — dashboards |

<div class="pt-4 text-sm opacity-70">

Being able to see traces on a laptop changes how people design. It is worth the
setup cost on day one of a project.

</div>

---

# What to alert on

<div class="grid grid-cols-2 gap-x-8 pt-2">

<div>

**Yes**
- Projection lag over threshold
- Dead letter queue growth rate
- Message age at head of queue
- Circuit breaker open

</div>

<div>

**No**
- Individual message failures
- Retry counts
- CPU, in isolation

</div>

</div>

<div class="pt-8">

Retries are the system working. Alerting on them trains people to ignore alerts.

</div>

---

<Demo path="src/HelpDesk">

One HTTP request, traced through Wolverine, Marten, Rabbit MQ, and into the
notification service. Then the Grafana dashboard, then CritterWatch.

</Demo>

<div class="pt-4 text-sm opacity-60">
TODO — depends on the HelpDesk sample application and the observability compose profile.
</div>

---

# Coming up next

**Section 8 — Testing & DevOps.** How to know this all works before your users
find out, and how to get it into production.

---
layout: end
---

# Resources

<div class="text-left text-base pt-4">

- Wolverine OpenTelemetry — <https://wolverinefx.net/guide/logging.html>
- Marten metadata — <https://martendb.io/events/metadata>
- CritterWatch — <https://jasperfx.net>

</div>
