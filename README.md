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

You need the .NET 10 SDK and Docker Desktop.

```bash
docker compose up -d
```

Postgres lands on **5440** and Rabbit on **5682**, not the default ports. That
keeps the workshop from colliding with anything else you already have running —
including a local Postgres on 5432, which many people have. If you go looking
for the workshop database on the default port, that is why it isn't there.

Verify the environment:

```bash
cd src/01-quickstarts/DocumentQuickstart && dotnet run
```

That's everything you need to follow along. Node is only required if you also
want to run the slide decks yourself — see below.

## Sample code

| | |
|---|---|
| `src/01-quickstarts/` | Two console apps for section 1 — documents, then event sourcing |
| `src/HelpDesk/` | The modular monolith that sections 2–8 demonstrate against — see [its README](src/HelpDesk/README.md) |

```bash
dotnet test src/HelpDesk/HelpDesk.Tests
```

## Running the slides

The decks are [Slidev](https://sli.dev). Optional — you don't need them to work
through the samples — but they're the fastest way to review the material, and
the code on the slides is imported live from `src/`, so it can't drift.

Needs Node 20+. Once:

```bash
npm install
```

Then open a section, `s1` through `s8`:

```bash
npm run s1 --workspace slides
```

That serves the deck at <http://localhost:3030> and hot-reloads as you edit —
both the markdown and the C# it imports. Useful keys while presenting:

| | |
|---|---|
| `o` | overview grid of every slide |
| `f` | full screen |
| `d` | toggle dark mode |
| `p` | presenter view, with a second window for the audience |

Build all eight decks to static sites, or export them to PDF as a fallback for
a projector that won't cooperate:

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
