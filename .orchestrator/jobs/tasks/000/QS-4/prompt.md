# QS-4: Staleness engine + scan

## Context
A review is only truth while the code has not moved. Using the QS-3 contract, compute per-file staleness: FRESH (hash matches), STALE (hash differs), MISSING (no meta). Builds on QS-2/QS-3.

## Deliverables
1. `StalenessEvaluator` in the core library: given a repo root, enumerate source files (respect .gitignore; configurable include globs), pair them with meta files, produce a typed report.
2. Console entry point (`src/quality-cli` or dotnet tool project): `quality scan [path]` printing a compact summary (counts per state, list of stale/missing with relative paths) and exiting non-zero when stale reviews exist (CI-friendly).
3. Unit tests with a fixture tree (fresh/stale/missing cases).

## Constraints
Performance matters (concept has hard perf goals): scanning a 5k-file tree must stream, not load all contents at once; hash lazily only where a meta file exists.

## Verification
dotnet test green; run the CLI against this very repository and paste the output into the task log. Commit + push.
