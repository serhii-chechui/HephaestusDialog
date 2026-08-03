# WTFGames Dialog

A branching dialog system split into an engine-agnostic core, a JSON serialization
layer, and (later) a GraphView authoring editor.

> **The Core is pure C#** — its assembly (`com.wtfgames.hephaestus.dialog.core`) is compiled
> with `noEngineReferences: true` and depends on nothing but the BCL. It has **no
> `UnityEngine` and no Hephaestus dependency**; the `hephaestus` in the package name reflects
> the WTFGames package family, not a runtime coupling. Consumers wire the system to their
> engine/UI/DI/content stack through the ports below.

## Assemblies

| Assembly | Platform | Depends on | Purpose |
|----------|----------|------------|---------|
| `com.wtfgames.hephaestus.dialog.core` | any (`noEngineReferences`) | BCL only | Graph model, runner, condition/action registry, ports. |
| `com.wtfgames.hephaestus.dialog.serialization` | any | Core + Newtonsoft.Json | Read/write the JSON dialog format, schema versioning + migrations. |
| `com.wtfgames.hephaestus.dialog.editor` | Editor | Core + Serialization + GraphView | *(Phase 3)* Node-graph authoring window. |

## Core concepts

- **Graph model** (`Model/`): a typed node graph — `EntryNode`, `LineNode`, `ChoiceNode`,
  `ConditionNode`, `ActionNode`, `ExitNode`. Edges are string node-id references. Text is a
  localization **key**, never inline text.
- **Conditions & actions** (`Registry/`): authored as data (`ConditionSpec`/`ActionSpec` =
  `{ type, params }`). The consumer registers handlers + editor descriptors by string id via
  `IDialogRegistry` — the core stays domain-agnostic (a stat check, a quest start, a flag set
  are all just registered types).
- **Runner** (`Runtime/`): `IDialogRunner` walks the graph synchronously and callback-driven.
  It emits `Line` / `Choices` / `Ended`; the host calls `Advance()` (continue a line) or
  `Choose(optionId)` (pick an option). Auto-nodes (entry/action/condition) are walked without
  pausing. No async, no UnityEngine.
- **Ports** (`Ports/`): `IDialogRepository` / `IStringTableRepository` (async load — plain
  `Task`), `ILocalizedTextProvider`, `IStringTable`. The game implements these (e.g. over
  Addressables) — the core only declares them.

See `docs/dialog-refactor-ddr.md` in the game repo for the full design decision record.
