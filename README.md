# Critter Stack Workshops

Training material for CQRS and Event Sourcing with the Critter Stack —
[Marten](https://martendb.io), [Polecat](https://polecat.jasperfx.net), and
[Wolverine](https://wolverinefx.net).

Eight hours of material, delivered as four two-hour blocks. See
[OUTLINE.md](OUTLINE.md) for the full breakdown.

## Layout

| | |
|---|---|
| `OUTLINE.md` | Master outline — sections, timing, and what maps from the old decks |
| `slides/` | Eight [Slidev](https://sli.dev) decks, one per section |
| `src/` | Sample applications the decks import their code from |
| `Old/` | The previous workshop — original samples and `.pptx` decks, archived |

## Getting set up

```bash
docker compose up -d
npm install
```

Postgres lands on **5440** and Rabbit on **5682**, not the default ports, so this
workshop never collides with the containers the Marten and Wolverine
repositories run.

Verify the environment:

```bash
cd src/01-quickstarts/DocumentQuickstart && dotnet run
```

## Sample code

| | |
|---|---|
| `src/01-quickstarts/` | Two console apps for section 1 — documents, then event sourcing |
| `src/HelpDesk/` | The modular monolith that sections 2–8 demonstrate against — see [its README](src/HelpDesk/README.md) |

```bash
dotnet test src/HelpDesk/HelpDesk.Tests
```

## Running a deck

```bash
npm run dev --workspace slides
```

Or a specific section — `s1` through `s8`:

```bash
npm run s3 --workspace slides
```

Build all eight to static sites, or export them all to PDF:

```bash
npm run build --workspace slides
npm run export --workspace slides
```

## Code in the slides

Slides never contain copy-pasted code. Every sample is imported by name out of
a real, compiling project in `src/`, the same way the Marten and Wolverine docs
do it.

In the C#:

```csharp
#region sample_es_time_travel
var asOfVersion1 = await session.Events.AggregateStreamAsync<Incident>(incidentId, version: 1);
#endregion
```

In the deck:

```
<<< ../src/01-quickstarts/EventSourcingQuickstart/Program.cs#sample_es_time_travel cs
```

Regions follow Marten's `sample_*` naming convention. Slidev resolves them at
build time, so a slide can never drift from the code it claims to show.

`npm run build` runs `verify-snippets.mjs` first, which fails the build if any
deck references a file or region that no longer exists. Run it on its own with:

```bash
npm run verify --workspace slides
```

> The `workspaces` field in the root `package.json` is load-bearing — Slidev
> sandboxes snippet imports to the workspace root, and the decks import from
> `../src`.
