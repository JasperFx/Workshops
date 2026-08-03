# Critter Stack Workshop — Master Outline

**CQRS and Event Sourcing with the "Critter Stack"**
8 hours of material, delivered as 4 × 2-hour blocks.

---

## 1. Framing

### Audience

Working .NET developers. Assume competence with ASP.NET Core, `async`/`await`, DI, and
relational databases. Do **not** assume any prior exposure to event sourcing, CQRS,
messaging, or the Critter Stack.

### Goals

Carried forward from the existing deck, lightly rewritten:

1. Teach event sourcing and CQRS as *architecture*, not as a library API tour.
2. Show what it actually takes to build a **robust** event-driven system — the stuff that
   only shows up in production: resiliency, consistency, tenancy, observability, deployment.
3. Convince the room the Critter Stack is the lowest-friction way to get there.
4. Make them love low-ceremony code.

### The honest addition

The old deck sells event sourcing hard and never says "don't." Every new section gets a
**"when this is the wrong answer"** slide. It buys credibility for everything else and it
is the single most common piece of feedback on workshops like this.

---

## 2. Version baseline

The old samples target Marten 8.18 / Wolverine 5.12 / .NET 10. The current releases are a
major version ahead and several defaults changed, so **all new sample code is written
against**:

| Package | Version | Notes |
|---|---|---|
| .NET | 10 | .NET 8 support was dropped in the 2026 wave |
| Marten | 9.22.x | PostgreSQL |
| Polecat | 5.9.x | SQL Server 2025 — Marten's model on SQL Server |
| Wolverine | 6.24.x | `WolverineFx.*` packages |
| JasperFx / JasperFx.Events | 2.x | shared core |
| Weasel | 9.x | .NET 8 dropped |

Changes since the old deck that need to be *taught*, not just absorbed:

- **Marten 9 eliminated runtime code generation.** Conventional projections use source
  generators now. This kills a whole category of "why is my cold start slow" and
  "why do I need a `codegen` step" conversation. Worth a slide.
- **Lightweight sessions are the default.** The old samples call `.UseLightweightSessions()`
  explicitly; new samples should not, and should explain why identity-map sessions still exist.
- **Quick append with server timestamps is the default** event append mode.
- **AOT compliance / cold start work** — relevant to the serverless discussion in section 8.
- **Wolverine 6 kept Roslyn runtime codegen**, but `JasperFx.RuntimeCompiler` is now a
  development-time-only dependency. Smaller deployed images.
- **Polecat exists.** The old deck predates it. The stack is no longer PostgreSQL-only, and
  that reframes the "Key Points about the Critter Stack" slide substantially.
- **CritterWatch** is the commercial observability product. It anchors section 7.

> Open item: confirm Polecat's exact positioning language and whether SQL Server 2025 is a
> hard floor before writing section 1's stack-overview slides.

---

## 3. Schedule

| Block | Hour | Section |
|---|---|---|
| **1** | 1 | 1 — Introduction to CQRS and Event Sourcing with the Critter Stack |
| | 2 | 2 — Asynchronous Messaging |
| **2** | 3 | 3 — Resiliency & Concurrency |
| | 4 | 4 — Data Consistency |
| **3** | 5 | 5 — Modular Monolith |
| | 6 | 6 — Multi-Tenancy |
| **4** | 7 | 7 — Observability, Metadata, and Metrics |
| | 8 | 8 — Testing & DevOps |

Per hour, budget roughly **35 minutes of slides, 20 minutes of live code, 5 minutes of
questions**. Every section ends with a "when not to" slide and a resources slide.

Sections 1–2 build the system. Sections 3–8 each take that system and stress it along one axis.

---

## 4. The running domain

Keep the **Help Desk / Incident Tracking** domain from the existing workshop. It is already
event-stormed, the room understands it without a domain lecture, and there is prior art.

It does need to grow to support sections 5 and 6, which have nothing to demonstrate against
a single-module app. Proposed module set:

- **Incidents** — the event-sourced core. `Incident` stream, categorise/prioritise/assign/resolve/close.
- **Customers** — a plain Marten document store. Deliberately *not* event sourced, to make
  the "Critter Stack ≠ 100% event sourcing" point structural rather than rhetorical.
- **Billing** — consumes incident events, owns its own store. The integration-event seam.
- **Notifications** — a separate process over Rabbit MQ. Already exists in the old samples.

---

## 5. Disposition of the existing decks

Three source decks were archived to `/Old`:

- `IntroductionAndAgenda.pptx` — 49 slides
- `SlideDeck.pptx` — 51 slides, a superset of the above
- `Resiliency.pptx` — 16 slides, a focused deep dive

`SlideDeck.pptx` is the canonical one. Mapping:

| Old slides | Topic | Disposition |
|---|---|---|
| 1–6 | Title, goals, key points, about me, the Critter Stack | → §1. **Rewrite** — must add Polecat and CritterWatch |
| 7–9 | Marten, document db, why a document db | → §1. Keep, refresh |
| 10–11 | Event sourcing definition + advantages | → §1. Keep, **add a tradeoffs slide** |
| 12–13 | Help Desk API, event storming | → §1. Keep, extend for the new module set |
| 14–15 | ES quickstart, Marten terminology | → §1. Keep, re-demo on Marten 9 |
| 16–18 | Projections, considerations, roles | → §1 (intro) + §4 (consistency lens). **Split** |
| 19–22 | Marten in AspNetCore, read side, time travel, fast web services | → §1. Keep |
| 23–25 | CQRS, better together, command handler concerns | → §1. Keep — slide 25 is the spine of the whole workshop |
| 26–28 | Wolverine, aggregate handler workflow, "what we just saw" | → §1. Keep, this is the punchline of hour 1 |
| 29–32 | Test harness, Alba, integration testing, CLI tooling | → §8. **Move** — it was oddly early in the old flow |
| 33–36 | Unwinding the magic, mediator, subscribing to events, background work | → §1 (mediator) + §2 (subscribing, background). **Split** |
| 37–39 | Wolverine HTTP, side effects, Fluent Validation middleware | → §1 |
| 40 | Integration testing for message handling | → §8. **Move** |
| 41 | Vertical slice architecture | → §5. **Move** — it belongs next to modularity |
| 42–44 | Rabbit MQ, transactional outbox, stateful resources | → §2 (transport, resources) + §3 (outbox). **Split** |
| 45–47 | Error handling, strategies, critical errors | → §3 |
| 48 | Designing for concurrency | → §3. **Expand heavily** — one slide is not enough |
| 49 | Multi-tenancy | → §6. **Expand heavily** — one slide becomes an hour |
| 50 | OpenTelemetry and metrics | → §7. **Expand heavily** — one slide becomes an hour |
| 51 | Resources | → every section gets its own |

`Resiliency.pptx` maps almost wholesale onto §3, with its outbox and consistency material
promoted into §4:

| Old slides | Topic | Disposition |
|---|---|---|
| 2–4 | Goals, things go wrong, message guarantees | → §3 opener. Excellent as-is |
| 5–7 | Durable local queues, outbox, idempotency | → §4 |
| 8, 10, 12–14 | Error handling, transient errors, poison pills, timeouts, DLQ | → §3 |
| 9 | External web service example | → §3 demo |
| 11 | Marten projection error handling | → §3 |
| 15 | Message ordering or parallelism | → §3 |

### Gaps — material that does not exist yet

Ranked by how much needs writing:

1. **§4 Data Consistency** — nearly all new. Only the outbox slides carry over.
2. **§5 Modular Monolith** — entirely new. One bullet on old slide 3 is the whole of it today.
3. **§7 Observability** — one old slide becomes an hour. CritterWatch did not exist.
4. **§6 Multi-Tenancy** — one old slide becomes an hour.
5. **§8 Testing & DevOps** — testing material exists and is good; DevOps is new.
6. **§3 Concurrency** — resiliency is covered well, concurrency is one slide.

---

## 6. Section detail

### §1 — Introduction to CQRS and Event Sourcing with the Critter Stack

**Objectives.** Attendee can define event sourcing and CQRS, explain the three projection
lifecycles and pick between them, and read a Wolverine aggregate handler.

**Beats.**
1. Goals, agenda, prerequisites, about me
2. The Critter Stack in 2026 — Marten, Polecat, Wolverine, Weasel, CritterWatch
3. Key point: the stack is not an event sourcing mandate. Mix ES, documents, and EF Core;
   PostgreSQL *or* SQL Server; one process or many
4. Marten as a document database → **demo**
5. Why a document database at all
6. Event sourcing: definition, advantages, **and the tradeoffs**
7. The Help Desk domain, event storming board
8. Marten terminology: event, stream, projection, aggregate/snapshot
9. Event sourcing quickstart → **demo**
10. Projection lifecycles — Inline / Live / Async tradeoff table
11. Projection roles — write model, read model, query model
12. What Marten 9 changed and why you care
13. Read-side web service, JSON streaming, time travel → **demo**
14. CQRS: definition; usable apart, better together
15. **Everything a command handler has to do** — the list that structures the rest of the day
16. Wolverine: mediator, background processing, messaging, HTTP
17. Aggregate handler workflow → **demo**, the payoff
18. What we just saw: A-Frame architecture, pure functions, optimistic concurrency, cascading messages
19. Unwinding the magic — middleware, side effects, Fluent Validation

**Demos.** `Sample.DocumentQuickstart`, `Sample.EventSourcingQuickstart`, `HelpDesk.Api` (read side + first command).

---

### §2 — Asynchronous Messaging

**Objectives.** Attendee can route a message three ways, choose between the event-forwarding
strategies, and explain why they'd add a broker.

**Beats.**
1. Why async messaging: temporal decoupling, load leveling, failure isolation
2. Wolverine as mediator vs. Wolverine as a message bus — same handlers either way
3. `InvokeAsync` / `SendAsync` / `PublishAsync` / request-reply
4. Message routing: local queues, conventional routing, explicit rules
5. Transports: Rabbit MQ, Azure Service Bus, SQS/SNS, Kafka, Pub/Sub, MQTT, SignalR, gRPC,
   and the database-backed queues
6. Cascading messages; a first look at sagas
7. **Getting events onto the bus — three strategies and when each is right**
   - `EventForwardingToWolverine` — inline, fast, no ordering guarantee
   - `PublishEventsToWolverine` subscriptions — async daemon, strictly ordered
   - `ISubscription` — custom batch processing
8. Scheduled and delayed delivery
9. The stateful resource model, `AutoProvision`
10. Message versioning and interop with non-Wolverine systems
11. When *not* to go async

**Demos.** `HelpDesk.Api` + `HelpDesk.Notifications` over Rabbit MQ; the three
event-subscription strategies side by side.

---

### §3 — Resiliency & Concurrency

**Objectives.** Attendee can write an error-handling policy, explain at-least-once delivery,
and handle a concurrency conflict correctly.

**Beats.**
1. Goals: no work is lost, data stays consistent, humans get called at 3am as rarely as possible
2. Things go wrong — the taxonomy from the old deck. Keep it verbatim, it lands
3. Delivery guarantees: fire and forget, at least once, at most once, expiration
4. Exception matching and the policy vocabulary: retry with cooldown, requeue,
   scheduled retry, pause-then-requeue, discard, dead letter, custom actions
5. Where policies live: global, per-handler, per-endpoint
6. Circuit breakers
7. Execution timeouts
8. Transient errors and exponential backoff — the distressed-subsystem story
9. Marten projection error handling: serialization failures, poison pills, `SkipApplyErrors`,
   pausing projections and subscriptions
10. Dead letter queues and replay
11. **Concurrency** — the section the old deck under-served
    - Optimistic concurrency via stream revision
    - `FetchForWriting`, `FetchLatest`, `[WriteAggregate]`
    - Exclusive locking, and its cost
    - Retrying on `ConcurrencyException` as an error policy
12. Ordering vs. parallelism: sequential local queues, ordered listeners, partitioned consumers
13. Node distribution and leadership election
14. When *not* to add resiliency machinery

**Demos.** Fault-injecting `ITechnicianService`; watch retries, circuit breaker, and DLQ fire.
A deliberate concurrency collision resolved two different ways.

---

### §4 — Data Consistency

**Objectives.** Attendee can articulate the dual-write problem and pick a consistency
strategy per read.

**Beats.**
1. Strong vs. eventual consistency — where each is actually acceptable
2. **The dual-write problem**, drawn on a whiteboard slide
3. The transactional outbox as the answer; store-and-forward; ordering
4. Wolverine transactional middleware, `AutoApplyTransactions`, durable local queues
5. Idempotency: durable endpoints, transactional middleware, non-transactional handlers
6. Inline vs. async projections revisited — now under a consistency lens
7. **Read-your-own-writes** — the problem every event-sourcing team hits in week two
   - `QueryForNonStaleData`
   - Returning the updated aggregate from the command via `FetchLatest`
   - UI-side strategies
8. Sagas and process managers for multi-step workflows
9. Compensating actions instead of distributed transactions
10. Projection rebuilds; rebuilding without downtime
11. **Event schema evolution** — upcasting, event type aliases, JSON transforms.
    This is the question that gets asked in every single workshop
12. When eventual consistency is the wrong answer

**Demos.** Turn off the outbox and produce a real inconsistency, then turn it back on.
Rebuild a projection against a live system. Upcast an event schema.

---

### §5 — Modular Monolith

**Objectives.** Attendee can lay out module boundaries and describe the extraction path to
a separate service.

**Beats.**
1. Why modular monolith — the honest cost accounting vs. microservices-first
2. What a module boundary actually is: data, code, and messaging
3. Vertical slice architecture the Wolverine way *(moved from old slide 41)*
4. Marten: separate stores per module via `AddMartenStore<T>`, ancillary stores, schema separation
5. Wolverine: assembly discovery, per-module handlers, local queues as the seam between modules
6. **Domain events vs. integration events** — the published language
7. Wolverine.HTTP endpoint discovery per module
8. Testing a module in isolation
9. **The extraction path** — what changes when a module becomes a service. Ideally: routing
   config and nothing else
10. When a modular monolith is over-engineering

**Demos.** The four-module Help Desk. Then move Notifications out of process and show how
little code changes.

---

### §6 — Multi-Tenancy

**Objectives.** Attendee can choose a tenancy model and trace a tenant id from HTTP request
through to the right database.

**Beats.**
1. Tenancy models and their tradeoffs: conjoined, database-per-tenant, hybrid
2. Static vs. dynamic tenant registries; onboarding a tenant at runtime
3. Marten: `MultiTenanted()`, `SingleTenanted()`, `MultiTenantedDatabases`, master table tenancy
4. **Mixing tenanted and non-tenanted documents in one store** — the old sample already does
   this and it's a good teaching moment
5. Table partitioning by tenant
6. PostgreSQL Row Level Security *(new in the 2026 wave)*
7. Schema migrations across N tenant databases
8. Wolverine tenant id **detection**: route argument, claim, header, query string
9. Wolverine tenant id **propagation**: through messages, sagas, and scheduled work
10. The async daemon across many tenant databases
11. Testing multi-tenanted code
12. When multi-tenancy is premature

**Demos.** The same Help Desk running conjoined and database-per-tenant, switched by config.
Onboard a new tenant while the app is running.

---

### §7 — Observability, Metadata, and Metrics

**Objectives.** Attendee can trace a request across process boundaries and knows what
metadata to capture at append time.

**Beats.**
1. **Event metadata** — correlation id, causation id, headers, user, timestamps, custom fields
2. Why metadata is not optional: audit, debugging, retrofitting metrics onto history
3. Configuring Marten metadata; the cost of each field
4. `[Audit]` members and structured logging
5. Open Telemetry: Wolverine activities, Marten activities, propagation across transports
6. Metrics: Wolverine's built-in meters, queue depth, execution time, Marten's metrics
7. **CritterWatch** — what the commercial tooling gives you over raw OTEL
8. Health checks
9. A dashboard stack: OTEL collector, Jaeger, Prometheus, Grafana — all in `docker-compose`
10. What to alert on, and what to ignore

**Demos.** One HTTP request traced through Wolverine, Marten, Rabbit MQ, and into the
notification service. Grafana dashboard. CritterWatch.

> Open item: does the workshop venue get a CritterWatch license for hands-on, or is this
> demo-only? Affects how much of the section is exercisable by attendees.

---

### §8 — Testing & DevOps

**Objectives.** Attendee can write all three test flavors and knows what has to happen at
deployment time.

**Beats.**
1. The testing pyramid for this stack, and why it's differently shaped
2. **Pure function handlers → plain unit tests, no mock objects.** The A-Frame payoff
3. Given/When/Then specification testing of aggregates
4. Alba for HTTP integration testing *(moved from old slides 29–31)*
5. Wolverine message tracking: `ExecuteAndWaitAsync`, `TrackActivity`, `InvokeMessageAndWaitAsync`
6. Marten test helpers: `ResetAllData`, `IInitialData`, `WaitForNonStaleProjectionDataAsync`
7. `DisableAllExternalWolverineTransports` and why integration tests should not need a broker
8. Self-contained tests; test containers; CI setup
9. **Diagnostics** — the JasperFx command line: `describe`, `codegen`, `db-apply`, `projections`, `resources`
10. Schema migration strategy; the `IResource` model; `AddResourceSetupOnStartup`
11. Environment checks and `--check` as a CI gate
12. Zero-downtime deployment; projection rebuild strategy; blue/green
13. AOT, cold start, and serverless *(new in the 2026 wave)*
14. **A production readiness checklist** — the takeaway artifact

**Demos.** Full test suite run. `dotnet run -- describe`. A schema migration applied from
the command line.

---

## 7. Repository layout

```
/Old                      archived — original samples and .pptx decks
/slides                   one Slidev project, eight decks
  package.json
  /decks
    01-intro.md ... 08-testing-devops.md
  /components             shared Vue components
  /snippets               code that exists only to be shown
/src                      sample applications
  /01-quickstarts
  /HelpDesk               the main modular monolith
  /HelpDesk.Notifications
docker-compose.yml        postgres, sqlserver, rabbitmq, otel, jaeger, grafana
OUTLINE.md
README.md
```

Slidev pulls code out of `/src` with `<<< @/../src/...#region`, so **the slides compile
against real, building, tested code**. No copy-pasted snippets that rot.

---

## 8. Open questions

1. **Polecat coverage.** Is SQL Server a first-class path through the whole workshop, or a
   30-second "this also exists" in §1? Doing it properly means either dual samples or an
   abstraction layer over the samples, and both are expensive.
2. **CritterWatch hands-on** vs. demo-only in §7.
3. **Hands-on labs.** Is this "watch Jeremy code" or do attendees have exercises? Exercises
   need a `/exercises` tree with starting points and solutions, and roughly doubles the
   sample-code work.
4. **Where the 2-hour breaks fall.** The pairing above is thematically clean, but §4 is the
   densest hour and might want to lead a block rather than close one.
