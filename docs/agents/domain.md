# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`CONTEXT.md`** at the repo root, or
- **`CONTEXT-MAP.md`** at the repo root if it exists: it points at one `CONTEXT.md` per context. Read each one relevant to the topic.
- **`docs/adr/`**: read ADRs that touch the area you're about to work in. In multi-context repos, also check `src/<context>/docs/adr/` for context-scoped decisions.

The root `CONTEXT.md` and `docs/adr/README.md` are committed routing placeholders in this repository, so these lookups are never ambiguous. Their contents remain intentionally empty of glossary terms and decisions until the `/domain-modeling` workflow resolves them. Do not fabricate terms or ADRs merely to populate the structure.

## File structure

This is a single-context repo:

```
/
├── CONTEXT.md
├── docs/adr/
│   ├── README.md
│   └── 0001-meaningful-decision.md
└── Assets/
```

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, or a test name), use the term as defined in `CONTEXT.md`. Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal: either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007 (event-sourced orders), but worth reopening because…_
