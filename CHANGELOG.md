## <small>0.1.0 (2026-08-03)</small>

* feat(core): engine-pure Core (noEngineReferences) — typed node graph (entry/line/choice/condition/action/exit), synchronous callback-driven runner, open condition/action registry keyed by string id, and ports (repository, localized text)
* feat(serialization): versioned JSON (graph + per-locale string table) with a schema-migration hook
* feat(editor): GraphView authoring window — create/wire nodes, inline text, descriptor-driven condition/action editing, validator, three-file export, and an in-editor preview
* test: EditMode tests for the core and serialization
