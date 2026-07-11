# Quality Studio frontend performance

The interaction budgets are contracts, not aspirations:

| Interaction | Budget | First measurement | Result |
| --- | ---: | ---: | --- |
| Tree expand | < 50 ms scripting-to-paint | 0.7 ms | Pass |
| Tree collapse | < 50 ms scripting-to-paint | 0.9 ms | Pass |
| File open / first visible content | < 150 ms | 22.4 ms | Pass |

Measured 2026-07-11 in Microsoft Edge 150 (Chromium), headless at 1600 × 1000. The file route returned 333,782 bytes, deliberately above the 200 KB acceptance boundary. The view split the response into lines but inserted only 80 overscanned line rows. Tree expand/collapse inserted only the visible fixed-height window. Network time is included in the file-open mark because it starts at selection and ends on the first animation frame after visible content renders.

## Repeat the automated measurement

1. Run `npm start` (the harness defaults to `http://127.0.0.1:4200`; set `QS_URL` to use another URL).
2. Run `npm run perf` in `frontend/`.
3. The Playwright harness intercepts the file API with a deterministic payload, expands and collapses the tree, opens a file, prints the `PerformanceMeasure` entries, and exits non-zero if either hard budget is exceeded.

The app also logs stable JSON events named `qs.tree.toggle` and `qs.file.first-content`, including `durationMs`, `budgetMs`, `withinBudget`, and the selected path. API fallback and tree load use `qs.data.demo-fallback` and `qs.data.tree-loaded`.

## Verify with Chrome tracing

1. Open the app in a production Chrome build and DevTools → Performance.
2. Enable Screenshots and Web Vitals. Use 4× CPU throttling for a conservative check, then record.
3. Expand and collapse a repository row, stop recording, and inspect the click task through the following paint. Search the Timings track for `qs.tree.toggle`; its measure must remain below 50 ms.
4. Record again, select a file of 200 KB or less, and stop after its first lines appear. Find `qs.file.first-content`; it must remain below 150 ms. Confirm the Main track does not contain a whole-file highlighting task.
5. Save the trace with the browser version, CPU setting, payload size, and result. Compare scripting separately from transport when diagnosing a regression, but use the end-to-end measure for acceptance.

## Design constraints that protect the budget

- The tree is flattened in memory and renders an overscanned 40-row window.
- The code view renders an overscanned 80-line window regardless of file length.
- The first-content path displays plain escaped text. Syntax coloring can be added only as idle, chunked work or in a worker; whole-file main-thread highlighting is prohibited.
- Production bundle budgets are enforced at 350 KB warning / 450 KB error initially and 10/12 KB per component stylesheet.
