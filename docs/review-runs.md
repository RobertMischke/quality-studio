# UI review runs

Quality Studio starts Coding Agent Runner reviews through the API host. The browser never launches an agent process. `POST /api/review` (or the repository-scoped equivalent) validates the selected hierarchy node, records its descendant file plan, enqueues the work, and immediately returns `202 Accepted` with a run ID.

The API runs file reviews with bounded concurrency (`ReviewJobs:MaxConcurrency`, default `2`). Container sweeps continue after individual file failures and write the selected project, module, or namespace review after all file attempts finish. Run snapshots expose each file's state and error.

Run orchestration is durable under `<repository>/.quality/runs/<runId>/`:

- `manifest.json` is the immutable enqueue-time plan. It records the selected node and level, kind, model, CLI type, aggregate controls, and every target file with its subject hash.
- `progress.jsonl` is an append-only file-transition log. Each flushed line records the run and file path, state, timestamps, and any error. Recovery ignores an incomplete line left by a crash and continues from the other records.
- `status.json` is the current overall state and its counters, cursor, timestamps, errors, and usage. It is replaced atomically through a same-directory temporary file.

At startup the API scans the registered repositories for durable runs. `queued` and formerly `running` runs are enqueued again; a file recorded as `done` or `failed` is not reviewed again. A file that was `running` when the process stopped is returned to `queued`, because its sidecar write cannot be assumed to have completed. `paused` runs are restored but remain idle. Terminal `done`, `failed`, and `cancelled` runs are loaded into recent history without being resumed.

The UI polls `GET /api/review/runs` every 1.5 seconds only while a run is queued or running. A terminal transition refreshes the hierarchy and the open file, so sidecar grades and staleness decorations update without a page reload. `POST /api/review/runs/{id}/pause` stops active work at the cancellation boundary while preserving completed files, and `POST /api/review/runs/{id}/resume` requeues the remaining files. Repository-scoped forms of both routes are also available. `DELETE /api/review/runs/{id}` retains its existing behavior and permanently cancels queued, paused, or active work.

`.quality/runs/` is ignored by Git because it is disposable orchestration working data. The review sidecars remain the committed review truth.
