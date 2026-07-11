# QS-10: Handover - Quality Studio -> Agent Studio tasks

## Context
DECIDED direction (see README "Handover"): Quality Studio does not embed into Agent Studio; it CREATES TASKS in Agent Studio from review findings. The engineer triages findings in Quality Studio and hands selected ones over as actionable cards.

## Deliverables
1. `AgentStudioTaskClient` in the core library: create a task via the Agent Studio API (POST /api/tasks - discover the exact contract from https://github.com/agent-orc/agent-studio backend; note ALL mutations need the X-Client-Id header). Configurable base URL + client id; NEVER hardcode either.
2. Task template: finding -> card (title "Fix: <finding summary> in <file>", prompt with file path, finding text, review kind, meta reference, acceptance criteria "review re-run comes back fresh+clean").
3. Dry-run mode that prints the would-be card instead of POSTing (default ON until explicitly configured).
4. API endpoint POST /api/handover (QS-7 host) + a "Create task" action on findings in the review panel (QS-9), visible only when a target is configured.
5. docs/concepts/handover.md: the contract, the flow, and why handover beats embedding (from the decision).

## Verification
dotnet test green (client against a mock); dry-run output sample in the task log; if a local Agent Studio is reachable, one real card in a scratch project as proof (then archive it). Commit + push.
