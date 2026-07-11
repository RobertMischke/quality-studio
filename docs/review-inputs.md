# Review inputs

Quality Studio reads project review guidance from `.quality/inputs/*.md` in the reviewed repository. A separate global directory can be configured with `QualityStudio:GlobalInputsDirectory`, the `QUALITY_GLOBAL_INPUTS` environment variable, or the CLI's `--global-inputs` option.

Each Markdown file starts with small frontmatter:

```markdown
---
id: dotnet-style
kinds: [code, performance]
levels: [file]
priority: 100
---
Prefer cancellation-aware asynchronous APIs on request paths.
```

`kinds` and `levels` accept comma-separated bracket lists; `all` applies everywhere. Singular `kind` and `level` are also accepted. Higher priority inputs are injected first. Applicable global inputs precede project inputs, while a project input with the same `id` replaces its global counterpart.

The default 12,000-character budget is configurable as `QualityStudio:InputBudgetCharacters` or with `--input-budget`. Partial and omitted content is reported by the resolver and persisted in `reviewInputs.omitted`; it is never silently dropped.

Use `quality review <file> --kind code --explain-inputs` to inspect the exact selection without running an agent review.
