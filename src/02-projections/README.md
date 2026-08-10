# Projection samples

Backing code for the projections deep-dive deck
([slides/09-projections.md](../../slides/09-projections.md)).

## `TeleHealth/` is vendored — do not "fix" it

Everything under `TeleHealth/` is copied from the Marten repository at
`src/DaemonTests/TeleHealth`. The only change is the namespace
(`DaemonTests.TeleHealth` → `Projections.TeleHealth`).

Keeping it byte-identical to upstream is the point: the slides import it by
region, so if the sample changes in Marten, re-copying the folder is the whole
update. Reformat it or rename something and that stops being true.

`AppointmentMetrics.cs` is the exception — it comes from
`src/DaemonTests/Composites/multi_stage_projections.cs` rather than the
TeleHealth folder, because the composite registration needs it.

`GlobalUsings.cs` mirrors Marten's own `src/Shared/DedupeAliases.cs` so the
vendored files compile unmodified. `IdentityAttribute` moved to the `JasperFx`
namespace, and the alias is how Marten itself papers over that.

## `TeleHealthStore.cs` is ours

Workshop-authored. It holds the `CompositeProjectionFor` registration, mirroring
the one in Marten's `multi_stage_projections.cs` test, minus the multi-tenancy
setup that test needs and this deck doesn't.

## Refreshing from upstream

```bash
cp ~/code/marten/src/DaemonTests/TeleHealth/*.cs src/02-projections/TeleHealth/
rm src/02-projections/TeleHealth/ConnectionSource.cs
sed -i '' 's/^namespace DaemonTests\.TeleHealth;/namespace Projections.TeleHealth;/' \
    src/02-projections/TeleHealth/*.cs
```

Then `npm run verify --workspace slides` to confirm every region the slides
reference still exists.

## What is *not* here

The simpler samples — `EventProjection`, `FlatTableProjection`,
`MultiStreamProjection`, side effects — are inlined directly in the deck. They
are short and self-contained enough that a copy on the slide is clearer than a
project full of stub types nobody runs.
