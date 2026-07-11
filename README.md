# Code Quality

**Agent-driven, layered code reviews with quality truth persisted next to the code.**

Part of the [Agent Orchestrator](https://agent-orchestrator.dev) universe — alongside
Agent Studio (the cockpit), Coding Agent Runner (executes), Coding Agent Chat
(converses), and Token Economy (accounts). Code Quality is the one that **reviews**.

> Working state, 2026-07-11: repository founded from the operator-approved concept
> below. Product URL will be `agent-orchestrator.dev/code-quality`; the core package
> direction is `AgentOrchestrator.CodeQuality` (final package naming decided at first
> publish). This README carries the founding concept until `docs/` grows.

## What this is — and what it is not

This is **not static code analysis**. Coding agents read, judge, and grade the code —
orchestrated across review kinds and abstraction levels — and their findings become
versioned, repo-owned facts. You work *with* agents on quality; the tool orchestrates
them and keeps the ledger honest.

## The concept

### 1. Levels, not one blob

Quality statements exist per level of a hierarchy and are never aggregated away:

```
Project → Module → Namespace → File → Function
```

A file review, a module review, and a project review are *different statements*.
Sweeps run over a whole project per **review kind**: `code`, `architecture`,
`security` (security is designed as a detachable module — it can grow into its own
thing).

### 2. Review metadata lives next to the code (the heart)

Every reviewed unit gets a small structured JSON meta file **in the same feature
folder** as the code it describes:

- `reviewedAt` — when the last review ran
- `kind` — code / architecture / security
- `findings[]` — structured findings
- `grade` — the level's grade
- `reviewedHash` — hash of the exact content that was reviewed

The hash makes staleness self-evident: if the code has moved on, the review visibly
no longer applies. History comes for free via Git. The repository owns its quality
truth — diffable, portable, reviewable like any other artifact.

Relationship to task-time reviews in Agent Studio: a task review is a **snapshot of a
diff**; Code Quality is the **standing truth of the codebase**.

### 3. Product shape

- **Core as a package** (`AgentOrchestrator.CodeQuality` direction): hierarchy model,
  meta-file schema, staleness logic, sweep planning — pure and testable.
- **API**: trigger sweeps, read the quality state, manage review runs.
- **Frontend**: its own surface in the Studio style, reusing the shared component
  family (tabs, panels, conversation components) — primarily the companion's own
  development and inspection tool.
- **Embedded in Agent Studio**: the place where quality is actually *used* —
  triggering, management, grades shown at files/modules/projects. No second daily
  tool; the operator stays in the Studio.

### 4. Neighbors in the universe

- Project graph (component/module structure) supplies the hierarchy's upper levels.
- Style-guide layer supplies the per-technology rules that reviews check against.
- Retro-grading and the remote review pipeline of Agent Studio are execution paths.

## Status

- [x] Repository founded, concept anchored (this README)
- [ ] CQ-1: concept elaboration — meta-file schema, naming finalization, embedding
      path, honest slice plan (runs as a task in this repository)
- [ ] Scaffold (package, CI, release rails — Token Economy pattern)

## License

Apache-2.0 — see [LICENSE](LICENSE).
