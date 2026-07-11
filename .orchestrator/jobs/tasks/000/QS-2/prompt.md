# QS-2: Core library scaffold + CI

## Context
Quality Studio (see README.md) is the engineer room of the Agent Orchestrator universe: agent-driven, layered code reviews with quality truth persisted next to the code. This card creates the technical foundation every other slice builds on. The core library working name is `coding-agent-quality`; the .NET root namespace is `AgentOrchestrator.CodeQuality` (final package id is decided at first publish - do NOT publish anything).

## Deliverables
1. .NET solution at repo root (slnx or sln): `src/AgentOrchestrator.CodeQuality/` (classlib, net10.0 if available else latest LTS) + `tests/AgentOrchestrator.CodeQuality.Tests/` (xunit).
2. Directory.Build.props: Apache-2.0 license metadata, RepositoryUrl https://github.com/agent-orc/quality-studio, nullable enable, treat warnings as errors.
3. One real starter type to prove the skeleton: `ReviewKind` enum (Code, Security, Performance) + `ReviewLevel` enum (Project, Module, Namespace, File, Function) with XML docs - these names come from the README concept.
4. GitHub Actions workflow `.github/workflows/build.yml`: build + test on push/PR to main.
5. README: add a short "Repository layout" section (keep the existing concept text untouched).

## Constraints
- English throughout (public repo).
- No package publishing, no version tags.
- Keep it minimal: no premature abstractions beyond the two enums.

## Verification
`dotnet build` and `dotnet test` green locally; workflow file is valid YAML. Commit + push.
