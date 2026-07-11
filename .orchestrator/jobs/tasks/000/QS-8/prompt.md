# QS-8: Angular shell with Agent Studio visual kinship

## Context
Quality Studio's frontend should feel like a sibling of Agent Studio - same family, different room. DELIBERATELY borrow the visual language: study the Agent Studio frontend (https://github.com/agent-orc/agent-studio, frontend/ - dark/light theming approach, status dots, lane/card aesthetics, typography scale, spacing) and reuse its patterns and tokens. Cross-checking both directions ("looking left and right") is explicitly wanted: where Agent Studio has a good pattern, take it; where Quality Studio needs something new, design it so it could flow BACK into Agent Studio later.

## Deliverables
1. Angular workspace `frontend/` (standalone components, signals), three-pane layout: file tree (left), code view (center), review panel (right).
2. Theme tokens as CSS custom properties, light + dark, visibly kin to Agent Studio's palette and status-dot language (fresh=green, stale=amber, missing=grey dot conventions).
3. Data services against the QS-7 API (tree, file, scan).
4. HARD performance budgets from the concept, enforced from day one: tree expand/collapse and selection interactions < 50ms scripting; opening a file renders first content < 150ms for files up to 200KB (virtualized rendering; no full-file syntax highlight on the main thread - highlight lazily/chunked).
5. A PERF.md documenting how the budgets are measured (Chrome tracing steps) with first measurements.

## Constraints
No component library imports (match Agent Studio's hand-rolled aesthetic). English UI strings.

## Verification
ng build green; screenshots (tree+file open, light and dark) attached; PERF.md numbers within budget. Commit + push.
