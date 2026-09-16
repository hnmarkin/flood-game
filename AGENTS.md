# AGENTS.md

## Project guidance

Before making changes, read and follow:

- `Assets\Dev\AgentRef\guidelines.md` for the general rules.

## Working rules

- NEVER write to `Assets\Game`. EVER. Do all work in `Assets\Dev`!
- Do NOT deviate from user instructions without notifying the user. If the user gives bad/incomplete instructions, stop before doing the task and clarify with the user.
- For large tasks, especially involving creation of new files or editing of mostly empty files, default to planning mode with the user before attempting implementation.

## Task Routing

- Drafting / mockups: use a lightweight prompt only.
- Development / coding: read `Assets\README.md` and `Assets\Game\Core\Architecture\architecture.md`.
  - UI development: also read `Assets\Dev\AgentRef\UIArchitectureReference.md` and `Assets\Dev\AgentRef\UIArchitectureReference.md`.
- Water system work: also read `Assets\Game\Features\WaterSystem\README.md`.

## Agent skills

### Issue tracker

Issues and specs live in GitHub Issues, managed with the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical triage labels use the defaults; `waiting-on-other-system` is an auxiliary dependency label. See `docs/agents/triage-labels.md`.

### Domain docs

This is a single-context repo using root `CONTEXT.md` and `docs/adr/`. See `docs/agents/domain.md`.
