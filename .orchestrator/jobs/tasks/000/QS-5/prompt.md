# QS-5: Levels & kinds - hierarchy aggregation

## Context
Reviews exist per level (Project, Module, Namespace, File, Function) and per kind (Code, Security, Performance). The UI and the scan need an aggregated picture: "module X is green for code, stale for security". Builds on QS-3/QS-4.

## Deliverables
1. Hierarchy model: a node tree (project -> modules -> namespaces -> files -> functions) where each node carries the meta documents that exist at its level plus rolled-up state from below.
2. Aggregation rules (document them in code + a short docs/ page): worst-of for staleness, explicit "not reviewed at this level" as its own state (absence is information, not failure).
3. Aspect-split support per file: multiple meta documents of different kinds attach to the same node; aggregation is per kind.
4. Extend `quality scan` with `--by-level` output (tree summary, one line per module).
5. Tests for the aggregation rules.

## Verification
dotnet test green; scan --by-level output for this repo in the task log. Commit + push.
