# QS-9: Augmented code browsing v1

## Context
The concept's core UX: browsing code WITH its quality truth. Extends the QS-8 code view with the meta overlay.

## Deliverables
1. Per-file review header: kind chips (code/security/performance) with grade + staleness state + review timestamp + reviewer (agent/model); stale reviews get an unmissable but non-blocking banner.
2. Findings inline: severity-colored gutter markers on the finding's line range, hover/click opens the finding text in the review panel.
3. Aspect switcher: when multiple kinds have meta for the file, switch overlays without re-fetching content.
4. Tree decoration: per-node rolled-up state dots (from QS-5 aggregation) so problem areas are visible from the root.
5. Keep the QS-8 perf budgets: overlays must not regress tree/file-open interactions (re-measure, update PERF.md).

## Verification
ng build green; screenshots: file with findings overlay, stale banner, tree with state dots. Commit + push.
