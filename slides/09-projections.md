---
theme: default
title: Projections in Depth
info: |
  Critter Stack Workshop — Projections deep dive
  Every projection type Marten offers, and what to do with them in production
class: text-center
highlighter: shiki
lineNumbers: false
transition: slide-left
mdc: true
drawings:
  persist: false
---

# Projections in Depth

<div class="pt-8 opacity-70">
Every shape Marten offers &mdash; and how to change one in production
</div>

---

# Where we're going

<div class="grid grid-cols-2 gap-x-10 pt-2">

<div>

**The shapes**
- Single stream — one stream in, one document out
- **Multi stream** — many streams, one document
- **Event projection** — arbitrary document operations
- **Flat table** — real relational columns
- **Composite** — a group that advances together
- Raw `IProjection` — the escape hatch

</div>

<div>

**Living with them**
- Event enrichment
- Side effects
- Blue/green with projection versioning
- Rebuilds

</div>

</div>

<div class="pt-6 text-sm opacity-70">

Section 1 covered single stream aggregates. Everything here is what you reach
for when one stream in, one document out isn't the shape you need.

</div>

---
layout: section
---

# Multi-stream projections

---

# One document, many streams

`SingleStreamProjection` folds one stream. `MultiStreamProjection` lets you
decide the grouping yourself.

<div class="pt-4"></div>

```csharp
public partial class MonthlyAccountActivityProjection
    : MultiStreamProjection<MonthlyAccountActivity, string>
{
    public MonthlyAccountActivityProjection()
    {
        // Route each event to a document keyed by "{accountId}:{yyyy-MM}"
        // using the stream ID (account) + event timestamp (month)
        Identity<IEvent<DepositRecorded>>(e => $"{e.StreamId}:{e.Timestamp:yyyy-MM}");
        Identity<IEvent<WithdrawalRecorded>>(e => $"{e.StreamId}:{e.Timestamp:yyyy-MM}");
        Identity<IEvent<FeeCharged>>(e => $"{e.StreamId}:{e.Timestamp:yyyy-MM}");
    }

    public MonthlyAccountActivity Create(IEvent<DepositRecorded> e) { /* ... */ }

    public void Apply(IEvent<DepositRecorded> e, MonthlyAccountActivity activity)
    {
        activity.TransactionCount++;
        activity.TotalDeposits += e.Data.Amount;
    }
}
```

---

# The identity is the whole design

The document key doesn't have to be a stream id. It can be anything you can
compute from the event:

| Grouping | Key |
|---|---|
| Per account, per month | `$"{e.StreamId}:{e.Timestamp:yyyy-MM}"` |
| Per tenant rollup | `e.TenantId` |
| Fan out to many documents | one event → several identities |
| Lookup against other data | a **custom grouper** with an `IQuerySession` |

<div class="pt-5 gotcha">

Fan-out is the one that surprises people. A single event can update *many*
documents — `Identity()` can return a collection, and each one gets its own
slice.

</div>

---

# When multi-stream costs you

- **Ordering across streams** is only guaranteed within the daemon's page
- **Rebuilds are more expensive** — there is no single stream to replay from
- Grouping by anything but the stream id means the daemon can't use the
  single-stream optimisations

<div class="pt-6">

Which is not an argument against it. It is an argument for knowing that a
multi-stream projection over a large event store is a different operational
animal to a snapshot.

</div>

---
layout: section
---

# Event projections

---

# When you want document operations, not an aggregate

An `EventProjection` doesn't build one document from a fold. It reacts to
events and does whatever document work you want.

```csharp
public partial class SampleEventProjection : EventProjection
{
    // Create a document from an event
    public Document1 Create(Event1 e) => new Document1 { Id = e.Id };

    // Or with event metadata
    public Document2 Create(IEvent<Event2> e)
        => new Document2 { Id = e.Data.Id, Timestamp = e.Timestamp };

    public void Project(StopEvent1 e, IDocumentOperations ops)
        => ops.Delete<Document1>(e.Id);

    public async Task Project(Event3 e, IDocumentOperations ops)
    {
        var lookup = await ops.LoadAsync<Lookup>(e.LookupId);
        // ...then carry out other operations against ops
    }

    // Applies to *any* event implementing ISpecialEvent -- interfaces and
    // common base classes both work
    public void Project(ISpecialEvent e, IDocumentOperations ops) { }
}
```

---

# Note the `partial`

<div class="pt-2">

`JasperFx.Events` 2.0 removed the old `Project<TEvent>(action)` lambda
registration. Method conventions on a **`partial`** class are the replacement —
the source generator finds each method at compile time and emits the dispatch.

</div>

<div class="pt-5 gotcha">

If you are porting from an older Marten and your projection suddenly does
nothing, check for a missing `partial`. There is no runtime reflection left to
save you.

</div>

---
layout: section
---

# Flat table projections

---

# Sometimes you just want columns

`FlatTableProjection` writes to a real relational table — for reporting tools,
BI, or anything that is going to be handed a connection string and left alone.

```csharp
public class FlatImportProjection: FlatTableProjection
{
    public FlatImportProjection() : base("import_history", SchemaNameSource.EventSchema)
    {
        Table.AddColumn<Guid>("id").AsPrimaryKey();
        Options.TeardownDataOnRebuild = true;

        Project<ImportStarted>(map =>
        {
            map.Map(x => x.ActivityType);
            map.Map(x => x.CustomerId);
            map.Map(x => x.PlannedSteps, "total_steps").DefaultValue(0);
            map.Map(x => x.Started);

            map.SetValue("status", "started");
            map.SetValue("step_number", 0);
        });

        Project<ImportProgress>(map =>
        {
            map.Increment("step_number");        // += 1 on this event
            map.Increment(x => x.Records);       // += the event's value
            map.SetValue("status", "working");
        });

        Delete<ImportFailed>();
    }
}
```

---

# Why this earns its place

- The output is a **table**, not JSONB. Your DBA can index it, your BI tool can
  read it, nobody needs to know it came from events
- `Increment` means running totals without a read-modify-write
- Weasel manages the DDL, so the table is part of your normal migration story

<div class="pt-6 gotcha">

This is often the easiest sell for event sourcing inside a sceptical
organisation. The reporting team never has to hear the word "projection".

</div>

---
layout: section
---

# Composite projections

---

# Projections that depend on other projections

Sometimes a read model needs what *another* projection just produced. Running
them as independent subscriptions makes that a race.

<div class="pt-4">

A **composite projection** is a group that shares one subscription and one
checkpoint, and runs in ordered stages.

</div>

<<< ../src/02-projections/TeleHealthStore.cs#sample_defining_a_composite_projection cs {maxHeight:'300px'}

---

# The second stage sees the first stage's output

<<< ../src/02-projections/TeleHealth/AppointmentDetailsProjection.cs#sample_appointmentdetailsprojection cs {maxHeight:'400px'}

---

# What the stages buy you

- **One checkpoint** for the whole group — they advance together or not at all
- Stage 2 may read what stage 1 wrote **in the same batch**
- `ProjectionDeleted<T, TId>` is published between stages, so a richer model can
  mirror deletions from a simpler one

<div class="pt-5 gotcha">

Without this, "projection B reads projection A" is only *eventually* correct,
and the window is however far apart their two daemons happen to be.

</div>

---
layout: section
---

# Event enrichment

---

# The N+1 problem, in a projection

A projection often needs reference data the event doesn't carry. The naive fix
is a lookup per event, which is a round trip per event.

<div class="pt-4">

`EnrichEventsAsync` batches those lookups for the whole slice group.

</div>

<<< ../src/02-projections/TeleHealth/ProviderShift.cs#sample_providershift_enricheventsasync cs {maxHeight:'320px'}

---

# Two ways to use what you looked up

| | |
|---|---|
| `.AddReferences()` | Attach the looked-up data to the slice for `Apply` to use |
| `.EnrichAsync(...)` | Take full control — including **replacing** the event |

<div class="pt-4"></div>

```csharp
// Swap the persisted event for a richer one before the fold sees it
.EnrichAsync((slice, e, provider) =>
{
    slice.ReplaceEvent(e, new EnhancedProviderJoined(e.Data.BoardId, provider));
});
```

<div class="pt-4 gotcha">

`ReplaceEvent` is the interesting one. The aggregate's `Apply` method gets an
event carrying the joined data, so the fold stays a pure function and the
lookup stays out of it.

</div>

---
layout: section
---

# Side effects

---

# A projection that causes things to happen

Override `RaiseSideEffects()` on a single- or multi-stream projection to emit
events or messages at the moment you know the new state.

```csharp
public override ValueTask RaiseSideEffects(IDocumentOperations operations, IEventSlice<Trip> slice)
{
    var currentTrip = slice.Snapshot;

    if (currentTrip.TotalMiles > 1000)
    {
        // Append to this stream
        slice.AppendEvent(new PassedThousandMiles());

        // Or to a different one
        slice.AppendEvent(currentTrip.InsuranceCompanyId, new IncrementThousandMileTrips());

        // Publish a message once the page commits
        slice.PublishMessage(new SendCongratulationsOnLongTrip(currentTrip.Id));

        // And ordinary document work
        operations.Store(new CompletelyDifferentDocument { OriginalTripId = currentTrip.Id });
    }

    return new ValueTask();
}
```

---

# Where side effects belong

Designed for **async** projections, and that is still where they fit best —
the daemon owns the batch, so "when the page commits" is a real moment.

<div class="pt-4"></div>

```csharp
builder.Services.AddMarten(opts =>
{
    opts.Connection(connectionString);

    // Opt in to side effects from Inline projections too
    opts.Events.EnableSideEffectsOnInlineProjections = true;
});
```

<div class="pt-4 gotcha">

Think hard before turning that on. Inline means the side effect happens inside
the caller's transaction, so a slow message send is now the user's latency.

</div>

---
layout: section
---

# Blue/green deployments

---

# The problem with changing a projection

You changed the shape of a read model. The old documents are wrong. Rebuilding
takes an hour. Your users are online right now.

<div class="pt-6">

You cannot rebuild in place without either downtime or serving wrong data.

</div>

---

# Projection versioning

<div class="pt-2">

Increment `ProjectionVersion` and the new version writes to **separate tables**.
Both versions can run at once.

</div>

<div class="pt-4"></div>

| | |
|---|---|
| 1 | Bump `ProjectionVersion` on the projection class |
| 2 | Run the new version **Async** so it can catch up |
| 3 | Deploy "green" nodes alongside "blue" — green builds, blue serves |
| 4 | Switch traffic once the new version has caught up |

<div class="pt-5 text-sm opacity-70">

`FetchForWriting()` gives strong consistency regardless of the underlying
lifecycle, so command handlers keep working through the transition with no code
changes.

</div>

---

# Spreading the work

```csharp
opts.Events.UseOptimizedProjectionRebuilds = true;
```

Rebuilds single stream projections **stream by stream**, in reverse order of
last modification, rather than a left fold from zero. Conceived exactly for this
— zero downtime deployment with less database load.

<div class="pt-5 text-sm opacity-70">

With Wolverine's `UseWolverineManagedEventSubscriptionDistribution`, projection
shards distribute across the cluster automatically, so old and new versions run
in parallel across nodes.

</div>

<div class="pt-4 text-sm opacity-60">

TODO — "side effect gating" for the catch-up phase. I could not find a feature
by that name in the Marten repo or docs; needs Jeremy's input on what it is
called and whether it has shipped.

</div>

---
layout: section
---

# Rebuilds

---

# Rebuilding

```csharp
using var daemon = await store.BuildProjectionDaemonAsync();

await daemon.RebuildProjectionAsync("Shop", CancellationToken.None);
```

<div class="pt-4"></div>

Or just one stream, when you know exactly what went wrong:

```csharp
await store.Advanced.RebuildSingleStreamAsync<SimpleAggregate>(streamId);
```

---

# Rebuilds are a superpower and a hazard

- A projection bug is a **rebuild**, not a migration script
- Derived data can always be thrown away and recomputed
- …but rebuild time scales with your entire event history
- …and the projection is unavailable, or stale, while it runs

<div class="pt-5 gotcha">

The question that decides your architecture: **how long does a full rebuild
take, and can you afford that?** Ask it before you have five years of events,
not after.

</div>

---

<Demo>

**CritterWatch** — kick off a projection rebuild against a running production
system, and watch the daemon work through it.

</Demo>

<div class="pt-4 text-sm opacity-60">

TODO — slide held open for the live demo.

</div>

---

# Choosing

| Need | Reach for |
|---|---|
| One stream → one document | `SingleStreamProjection` |
| Many streams → one document | `MultiStreamProjection` |
| Arbitrary document work per event | `EventProjection` |
| Relational columns for reporting | `FlatTableProjection` |
| Projections that depend on each other | `CompositeProjectionFor` |
| None of the above | `IProjection` |

<div class="pt-5 gotcha">

Start with the simplest that fits. Every step down this table costs you
something at rebuild time.

</div>

---
layout: end
---

# Resources

<div class="text-left text-base pt-4">

- Projections — <https://martendb.io/events/projections/>
- Composite projections — <https://martendb.io/events/projections/composite>
- Enrichment — <https://martendb.io/events/projections/enrichment>
- Side effects — <https://martendb.io/events/projections/side-effects>
- Rebuilding — <https://martendb.io/events/projections/rebuilding>
- [Projections, Consistency Models, and Zero Downtime Deployments](https://jeremydmiller.com/2025/03/26/projections-consistency-models-and-zero-downtime-deployments-with-the-critter-stack/)

</div>
