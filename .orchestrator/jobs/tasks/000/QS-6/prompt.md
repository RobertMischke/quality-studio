# QS-6: Agentic review runner v1

## Context
The slice where reviews actually happen: run a coding-agent CLI against a file, get structured findings, persist them as QS-3 meta JSON. Reuse the published `CodingAgentRunner` NuGet package (agent-orc/runner) for CLI spawning/event parsing - do not reinvent process handling.

## Deliverables
1. `ReviewRunner`: input (file path, kind, level=File), builds the review prompt from a template, runs the CLI (codex first; the CodingAgentRunner adapter abstraction keeps claude/gemini open), parses a structured findings response (JSON block contract in the prompt), writes the meta document with fresh reviewedHash.
2. Prompt templates per kind (code/security/performance) under `prompts/` - versioned files, English, with a strict output-format section.
3. Input hooks: template placeholders for global + per-project review guidelines (wired fully in QS-13; here just the insertion points).
4. `quality review <file> --kind code` CLI command.
5. Integration-style test behind an env flag (skipped in CI) + unit tests for prompt assembly and response parsing.

## Verification
dotnet test green; run one real review of a small file in this repo, commit the resulting meta JSON as a sample. Commit + push.
