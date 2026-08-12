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

<<< ../src/HelpDesk/HelpDesk.Api/Telemetry.cs#sample_otel_wiring cs {maxHeight:'350px'}

<div class="pt-2 gotcha">

`AddSource` takes `"Wolverine"` and `"Marten"`. `AddMeter` does **not** —
Wolverine's meter is named per service, so `AddMeter("Wolverine")` compiles,
runs, and exports nothing.

</div>

---

# Same instrumentation, different destination

Application Insights instead of a collector. The `AddSource` and `AddMeter`
names do not change — which is rather the point of standardising on OTEL.

```csharp
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(ServiceName))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddSource("Wolverine")
        .AddSource("Marten"))
    .WithMetrics(m => m
        .AddMeter($"Wolverine:{ServiceName}")
        .AddMeter("Marten"))

    // One line swaps where it all goes. Nothing above it changes.
    .UseAzureMonitor(o =>
    {
        o.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    });
```

<div class="pt-2 text-sm opacity-70">

Needs `Azure.Monitor.OpenTelemetry.AspNetCore`.

</div>

---

# What Wolverine spans cover

- Message received, executed, sent, retried, dead-lettered
- Context propagates **across transports** — the trace survives the broker hop
- Marten spans sit underneath: connections, batches, event appends

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

Wolverine publishes meters out of the box. Marten publishes far fewer than you
would expect, and the two are worth separating.

<div class="pt-4"></div>

- **Wolverine** — counts, durations, and queue depths, all tagged by message type
- **Marten** — mostly tracing; counters are yours to define

<div class="pt-6 gotcha">

Projection lag is the number that tells you whether your read models are lying —
and it is *not* an exported metric. Two slides from now on where it does live.

</div>

---

# What Wolverine actually exports

Meter name: **`Wolverine:{ServiceName}`**

| | |
|---|---|
| **Counters** | `-messages-sent` · `-messages-received` · `-messages-succeeded` |
| | `-execution-failure` · `-dead-letter-queue` |
| **Histograms** | `-execution-time` (ms) |
| | `-effective-time` — **sent until executed** |
| **Gauges** | `-inbox-count` · `-outbox-count` · `-scheduled-count` |
| | `-database-connection-count` · `-database-connection-budget` |

<div class="pt-3 text-sm opacity-70">

All prefixed `wolverine`. Tagged with `message.type`, `message.destination`,
`tenant.id`, and `exception.type` on failures.

</div>

---

# The two that predict an incident

<div class="pt-2"></div>

**`wolverine-effective-time`** — how long a message waited between being sent
and being executed. Rising means you are falling behind, and it rises *before*
anything actually fails.

<div class="pt-3"></div>

**`wolverine-inbox-count`** — the backlog. In section 3 this is the number that
climbed while the circuit breaker held the queue latched.

<div class="pt-4 gotcha">

Failure counters tell you what already went wrong. These two tell you what is
about to.

</div>

---

# What Marten exports

Meter name: **`Marten`** — and it is deliberately thin.

<div class="pt-3"></div>

Most of Marten's telemetry is **tracing**, not metrics — `marten.connection`,
`marten.batch.execution.started`, `marten.command.execution.started`,
`marten.event.append`.

<div class="pt-3"></div>

For counters you define what matters to you:

```csharp
opts.OpenTelemetry.ExportCounterOnChangeSets<int>(
    "marten.events.appended", "Events",
    (counter, commit) => counter.Add(commit.GetEvents().Count()));
```

<div class="pt-3 gotcha">

Worth knowing before you go looking for a projection-lag metric and cannot find
one. The daemon's progress lives in the database, and CritterWatch is what
reads it for you.

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
