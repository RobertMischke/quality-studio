# QS-13: Review inputs (global + project)

## Context
Concept point "input management": reviews are only as good as their briefing. Style guides, architecture rules and review rubrics exist globally (the operator's standards) and per project. They must flow into the QS-6 prompt templates through the insertion points QS-6 left open.

## Deliverables
1. Input store convention: `.quality/inputs/` in the target repo (project level) + a configurable global inputs directory; markdown files with a small frontmatter (id, kind-applicability, priority).
2. `InputResolver`: collect applicable inputs for (kind, level), deterministic order (global first, project overrides by id), size budget with explicit truncation report (never silently drop).
3. Wire into QS-6's ReviewRunner; `quality review --explain-inputs` prints what was injected and why.
4. API surface: GET /api/inputs (QS-7) listing resolved inputs per kind; simple read-only inputs page or panel section in the frontend (QS-8 family).
5. Tests: resolution order, override behavior, truncation reporting.

## Verification
dotnet test green; --explain-inputs sample in the task log. Commit + push.
