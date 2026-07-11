FOUNDING TASK of coding-agent-code-quality (READ README.md first - it carries the operator-approved concept; this task elaborates, it does NOT re-decide).

1. REVIEW-META SCHEMA v1: precise JSON schema for the per-unit meta file (reviewedAt, kind, findings[] structure, grade scale, reviewedHash - hash algorithm + what exactly is hashed, multi-kind handling per unit, module/project aggregate files). File naming + placement convention (same feature folder). Worked examples for an Angular component and a .NET service.
2. HIERARCHY CONTRACT: how Project/Module/Namespace/File/Function levels are identified per technology (Angular workspace, .NET solution) - keep it derivable from repo structure, no manual registry.
3. STALENESS: rules for reviewedHash mismatch (stale marking, partial staleness for module aggregates).
4. EMBEDDING PATH recommendation: component package vs iframe vs API-only for the Agent Studio integration - one recommendation with reasons.
5. PACKAGE NAMING finalization proposal: AgentOrchestrator.CodeQuality direction (check nuget availability, propose final id + repo docs update).
6. HONEST SLICE PLAN: CQ-2..n (scaffold with TE-style release rails, schema lib + tests, first file-level code-review sweep runner, module aggregation, Studio embedding v1) with sizes.
Deliverable: docs/concept.md (English) + updated README status; NO production code beyond doc examples. Second-opinion pass before finishing.
