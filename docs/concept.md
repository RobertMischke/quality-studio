# Quality Studio concept and contracts v1

Status: proposed founding contract, 2026-07-11. This document elaborates the
operator-approved product concept in the repository README; it does not replace
or reopen that concept. Normative words such as **MUST**, **SHOULD**, and **MAY**
describe the intended v1 implementation.

Quality Studio stores standing, agent-produced review statements beside the
code. A statement belongs to exactly one derived unit and one review kind. A
Project, Module, Namespace, File, or Function statement is independently
authored: an aggregate view never turns child grades into a substitute for a
review at another level.

## Decisions at a glance

- The v1 built-in review kinds are `code`, `security`, and `performance`.
  Architecture can be an aspect of a project/module `code` review; adding it as
  a fourth kind would require a later schema version. Security remains
  detachable because it has separate files, prompts, runs, grades, and UI state.
- There is one review-meta document per `(unit, kind)`. Reviewing performance
  never refreshes code or security metadata.
- Source subjects are normalized text and hashed with SHA-256. Aggregate hashes
  are deterministic manifests of derived child subject hashes. Review standards
  and prompt versions have a separate input hash, so code drift and policy drift
  remain distinguishable.
- The hierarchy is derived from checked-in workspace/solution files, compiler
  structure, and repository paths. There is no Quality Studio hierarchy registry.
- Quality Studio is a separate engineer room. It hands selected finding
  snapshots to Agent Studio through Agent Studio's normal task mutation path; it
  is not embedded in Agent Studio.
- The final proposed library and NuGet ID is
  `AgentOrchestrator.CodeQuality`; the product remains **Quality Studio**, the
  formal long name remains **Agent Quality Studio**, and the repository remains
  `agent-orc/quality-studio`.

## Review-meta JSON Schema v1

The logical schema URL below becomes the published `$id` when the website ships.
Until then, the copy embedded here is authoritative. Producers MUST validate
against JSON Schema draft 2020-12 and write UTF-8 JSON. Core properties are
closed; vendor experiments use `x-` properties. Runtime readers SHOULD ignore
unknown properties so a newer document can be inspected, but MUST NOT silently
treat an unsupported `schemaVersion` as current.

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://agent-orchestrator.dev/quality/schemas/review-meta.v1.schema.json",
  "title": "Quality Studio review metadata v1",
  "type": "object",
  "additionalProperties": false,
  "patternProperties": {
    "^x-[a-z0-9][a-z0-9.-]*$": true
  },
  "required": [
    "$schema",
    "schemaVersion",
    "unit",
    "reviewedAt",
    "kind",
    "reviewer",
    "reviewedHash",
    "subjectInputs",
    "reviewInputs",
    "grade",
    "summary",
    "aspects",
    "findings"
  ],
  "properties": {
    "$schema": {
      "const": "https://agent-orchestrator.dev/quality/schemas/review-meta.v1.schema.json"
    },
    "schemaVersion": {
      "const": 1
    },
    "unit": {
      "$ref": "#/$defs/unit"
    },
    "reviewedAt": {
      "type": "string",
      "format": "date-time",
      "pattern": "Z$"
    },
    "kind": {
      "enum": ["code", "security", "performance"]
    },
    "reviewer": {
      "$ref": "#/$defs/reviewer"
    },
    "reviewedHash": {
      "$ref": "#/$defs/subjectManifestHash"
    },
    "subjectInputs": {
      "type": "array",
      "minItems": 1,
      "items": {
        "$ref": "#/$defs/subjectInput"
      }
    },
    "reviewInputs": {
      "$ref": "#/$defs/reviewInputs"
    },
    "grade": {
      "$ref": "#/$defs/grade"
    },
    "summary": {
      "type": "string",
      "minLength": 1,
      "maxLength": 2000
    },
    "aspects": {
      "type": "array",
      "minItems": 1,
      "items": {
        "$ref": "#/$defs/aspect"
      }
    },
    "findings": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/finding"
      }
    },
    "aggregate": {
      "$ref": "#/$defs/aggregate"
    }
  },
  "allOf": [
    {
      "if": {
        "type": "object",
        "properties": {
          "unit": {
            "type": "object",
            "properties": {
              "level": {
                "enum": ["project", "module", "namespace"]
              }
            },
            "required": ["level"]
          }
        }
      },
      "then": {
        "type": "object",
        "properties": {
          "aggregate": {
            "$ref": "#/$defs/aggregate"
          }
        },
        "required": ["aggregate"]
      },
      "else": {
        "type": "object",
        "not": {
          "type": "object",
          "properties": {
            "aggregate": true
          },
          "required": ["aggregate"]
        }
      }
    }
  ],
  "$defs": {
    "repoPath": {
      "type": "string",
      "minLength": 1,
      "pattern": "^(?!/)(?!.*(?:^|/)\\.\\.(?:/|$))[^\\\\]+$"
    },
    "sha256String": {
      "type": "string",
      "pattern": "^sha256:[a-f0-9]{64}$"
    },
    "subjectManifestHash": {
      "type": "object",
      "additionalProperties": false,
      "required": ["algorithm", "canonicalization", "value"],
      "properties": {
        "algorithm": {
          "const": "sha256"
        },
        "canonicalization": {
          "const": "quality-studio-subject-manifest-v1"
        },
        "value": {
          "type": "string",
          "pattern": "^[a-f0-9]{64}$"
        }
      }
    },
    "inputManifestHash": {
      "type": "object",
      "additionalProperties": false,
      "required": ["algorithm", "canonicalization", "value"],
      "properties": {
        "algorithm": {
          "const": "sha256"
        },
        "canonicalization": {
          "const": "quality-studio-review-inputs-v1"
        },
        "value": {
          "type": "string",
          "pattern": "^[a-f0-9]{64}$"
        }
      }
    },
    "unit": {
      "type": "object",
      "additionalProperties": false,
      "required": ["id", "adapter", "level", "path", "displayName"],
      "properties": {
        "id": {
          "type": "string",
          "pattern": "^qs-v1/(angular|dotnet)/(project|module|namespace|file|function)/[^\\s]+$"
        },
        "adapter": {
          "enum": ["angular", "dotnet"]
        },
        "level": {
          "enum": ["project", "module", "namespace", "file", "function"]
        },
        "path": {
          "$ref": "#/$defs/repoPath"
        },
        "displayName": {
          "type": "string",
          "minLength": 1,
          "maxLength": 500
        },
        "symbolId": {
          "type": "string",
          "minLength": 1,
          "maxLength": 2000
        }
      }
    },
    "reviewer": {
      "type": "object",
      "additionalProperties": false,
      "required": ["agent", "model"],
      "properties": {
        "agent": {
          "type": "string",
          "minLength": 1,
          "maxLength": 200
        },
        "model": {
          "type": "string",
          "minLength": 1,
          "maxLength": 200
        },
        "agentVersion": {
          "type": "string",
          "minLength": 1,
          "maxLength": 100
        },
        "runId": {
          "type": "string",
          "minLength": 1,
          "maxLength": 200
        }
      }
    },
    "subjectInput": {
      "type": "object",
      "additionalProperties": false,
      "required": ["path", "selector", "contentHash"],
      "properties": {
        "path": {
          "$ref": "#/$defs/repoPath"
        },
        "selector": {
          "type": "string",
          "minLength": 1,
          "maxLength": 2000
        },
        "contentHash": {
          "$ref": "#/$defs/sha256String"
        }
      }
    },
    "standardReference": {
      "type": "object",
      "additionalProperties": false,
      "required": ["id", "scope", "version", "contentHash"],
      "properties": {
        "id": {
          "type": "string",
          "pattern": "^[a-z0-9][a-z0-9._-]{1,127}$"
        },
        "scope": {
          "enum": ["built-in", "global", "project"]
        },
        "version": {
          "type": "string",
          "minLength": 1,
          "maxLength": 100
        },
        "contentHash": {
          "$ref": "#/$defs/sha256String"
        }
      }
    },
    "promptReference": {
      "type": "object",
      "additionalProperties": false,
      "required": ["id", "version", "contentHash"],
      "properties": {
        "id": {
          "type": "string",
          "minLength": 1,
          "maxLength": 200
        },
        "version": {
          "type": "string",
          "minLength": 1,
          "maxLength": 100
        },
        "contentHash": {
          "$ref": "#/$defs/sha256String"
        }
      }
    },
    "reviewInputs": {
      "type": "object",
      "additionalProperties": false,
      "required": ["effectiveHash", "complete", "standards", "omitted", "prompt"],
      "properties": {
        "effectiveHash": {
          "$ref": "#/$defs/inputManifestHash"
        },
        "complete": {
          "type": "boolean"
        },
        "standards": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/standardReference"
          }
        },
        "omitted": {
          "type": "array",
          "items": {
            "type": "string",
            "pattern": "^[a-z0-9][a-z0-9._-]{1,127}$"
          }
        },
        "prompt": {
          "$ref": "#/$defs/promptReference"
        }
      }
    },
    "grade": {
      "type": "object",
      "additionalProperties": false,
      "required": ["score", "band", "rationale"],
      "properties": {
        "score": {
          "type": "integer",
          "minimum": 0,
          "maximum": 100
        },
        "band": {
          "enum": ["A", "B", "C", "D", "F"]
        },
        "rationale": {
          "type": "string",
          "minLength": 1,
          "maxLength": 2000
        }
      },
      "oneOf": [
        {
          "type": "object",
          "properties": {
            "score": { "type": "integer", "minimum": 90 },
            "band": { "const": "A" }
          }
        },
        {
          "type": "object",
          "properties": {
            "score": { "type": "integer", "minimum": 80, "maximum": 89 },
            "band": { "const": "B" }
          }
        },
        {
          "type": "object",
          "properties": {
            "score": { "type": "integer", "minimum": 70, "maximum": 79 },
            "band": { "const": "C" }
          }
        },
        {
          "type": "object",
          "properties": {
            "score": { "type": "integer", "minimum": 60, "maximum": 69 },
            "band": { "const": "D" }
          }
        },
        {
          "type": "object",
          "properties": {
            "score": { "type": "integer", "maximum": 59 },
            "band": { "const": "F" }
          }
        }
      ]
    },
    "aspect": {
      "type": "object",
      "additionalProperties": false,
      "required": ["id", "title", "grade"],
      "properties": {
        "id": {
          "type": "string",
          "pattern": "^[a-z][a-z0-9.-]{1,63}$"
        },
        "title": {
          "type": "string",
          "minLength": 1,
          "maxLength": 200
        },
        "grade": {
          "$ref": "#/$defs/grade"
        }
      }
    },
    "position": {
      "type": "object",
      "additionalProperties": false,
      "required": ["line", "column"],
      "properties": {
        "line": {
          "type": "integer",
          "minimum": 1
        },
        "column": {
          "type": "integer",
          "minimum": 1
        }
      }
    },
    "range": {
      "type": "object",
      "additionalProperties": false,
      "required": ["start", "end"],
      "properties": {
        "start": {
          "$ref": "#/$defs/position"
        },
        "end": {
          "$ref": "#/$defs/position"
        }
      }
    },
    "location": {
      "type": "object",
      "additionalProperties": false,
      "required": ["path"],
      "properties": {
        "path": {
          "$ref": "#/$defs/repoPath"
        },
        "range": {
          "$ref": "#/$defs/range"
        },
        "symbolId": {
          "type": "string",
          "minLength": 1,
          "maxLength": 2000
        }
      }
    },
    "finding": {
      "type": "object",
      "additionalProperties": false,
      "required": ["id", "aspect", "severity", "title", "description", "recommendation", "locations"],
      "properties": {
        "id": {
          "type": "string",
          "pattern": "^[a-z][a-z0-9._-]{2,127}$"
        },
        "fingerprint": {
          "$ref": "#/$defs/sha256String"
        },
        "aspect": {
          "type": "string",
          "pattern": "^[a-z][a-z0-9.-]{1,63}$"
        },
        "severity": {
          "enum": ["critical", "high", "medium", "low", "info"]
        },
        "ruleId": {
          "type": "string",
          "minLength": 1,
          "maxLength": 200
        },
        "title": {
          "type": "string",
          "minLength": 1,
          "maxLength": 300
        },
        "description": {
          "type": "string",
          "minLength": 1,
          "maxLength": 10000
        },
        "evidence": {
          "type": "string",
          "minLength": 1,
          "maxLength": 10000
        },
        "recommendation": {
          "type": "string",
          "minLength": 1,
          "maxLength": 10000
        },
        "locations": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/location"
          }
        }
      }
    },
    "aggregateMember": {
      "type": "object",
      "additionalProperties": false,
      "required": ["unitId", "path", "subjectHash"],
      "properties": {
        "unitId": {
          "type": "string",
          "pattern": "^qs-v1/"
        },
        "path": {
          "$ref": "#/$defs/repoPath"
        },
        "subjectHash": {
          "$ref": "#/$defs/sha256String"
        }
      }
    },
    "exclusion": {
      "type": "object",
      "additionalProperties": false,
      "required": ["path", "reason"],
      "properties": {
        "path": {
          "$ref": "#/$defs/repoPath"
        },
        "reason": {
          "type": "string",
          "minLength": 1,
          "maxLength": 500
        }
      }
    },
    "aggregate": {
      "type": "object",
      "additionalProperties": false,
      "required": ["members", "excluded"],
      "properties": {
        "members": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/aggregateMember"
          }
        },
        "excluded": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/exclusion"
          }
        }
      }
    }
  }
}
```

The schema's field invariants, filenames, canonical hashing steps, and complete
examples are collected in [Review-meta operational rules and examples](#review-meta-operational-rules-and-examples)
after the cross-cutting hierarchy and product contracts.

## Derivable hierarchy contract

The repository is a browser container, not an extra review level. Adapters emit
canonical units and parent/child edges for the five review levels. Physical
folders may also appear in the UI, but folder badges are projections of
descendants; a folder does not silently become a sixth review level.

### Rules shared by both adapters

- Repository root is the Git worktree root containing the selected entry file.
  An explicit CLI/API entry path wins. Without one, adapters discover every
  root-level workspace/solution they support rather than choosing an arbitrary
  “first” file.
- Canonical paths use the spelling in the Git index when tracked, `/` separators,
  and ordinal comparison. On case-insensitive filesystems, case aliases resolve
  to that one canonical spelling. Symlinks resolving outside the worktree are
  not followed; in-repository aliases are de-duplicated by real path. The repo
  root is represented as `.` when a unit anchor has no non-empty relative path.
- Unit IDs have the form
  `qs-v1/<adapter>/<level>/<identity-component>/...`. Each component is UTF-8
  RFC 3986 percent-encoded with uppercase hex; even `/` inside a component is
  encoded as `%2F`. Identity tuples below are ordered and therefore reproducible.
- `.gitignore` is respected. Meta sidecars, `.quality`, package caches, compiler
  outputs, generated sources identified by the compiler/framework, and configured
  test/fixture exclusions do not become review subjects. Exclusion reasons are
  surfaced, and aggregate metadata records exclusions used during that review.
- Derivation is re-run from source truth. A rename, workspace membership change,
  namespace change, or symbol identity change creates/removes IDs without a
  manual migration registry. An unmatched old sidecar is reported as orphaned.

### Angular workspace adapter

| Level | Derivation and identity tuple |
| --- | --- |
| Project | Each named `projects` entry in the nearest `angular.json`; `(angular.json path, project name)`. The workspace is a UI container when it has multiple entries. The project anchor is the entry's `root`, falling back to the workspace root when empty. |
| Module | The project root module plus compiler-discovered `@NgModule` declarations and lazy route boundaries (`loadChildren`/`loadComponent`); `(project ID, boundary kind, declaring repo path, exported symbol)`. The root sentinel tuple is `(project ID, root, project root, root)`. An NgModule owns its declarations. A lazy standalone boundary owns files below its entry directory unless an explicit NgModule owns them. Everything else falls back to the project root module. Ties are an adapter diagnostic, resolved ordinally by module ID, never by a registry. |
| Namespace | A logical directory under the owning module anchor; `(module ID, normalized relative directory)`. `.` represents files at the anchor. This is deliberately not the TypeScript `namespace` keyword. It makes the feature-folder structure the stable browsing contract for NgModule and standalone applications alike. |
| File | Reviewable project source selected by Angular/TypeScript configuration; `(module ID, repo path)`. `.ts`, component templates, and styles may be individual file units. A TypeScript component review may list its `templateUrl`/`styleUrl` files as additional subject inputs, as in the worked example. Tests and generated files are excluded by default but can be included explicitly by review-run options. |
| Function | Named TypeScript AST declarations: functions, methods, constructors, accessors, and functions/arrows assigned to a named binding; `(file ID, TypeScript symbol name, normalized signature)`. Overload signatures share the implementation unit. Anonymous callbacks and lambdas are locations within the nearest named function/file in v1, not unstable ordinal-based function units. Decorators and attached documentation are part of the hashed syntax span. |

Standalone components require no synthetic registry entry: they belong to a lazy
route module when that boundary exists and otherwise to the root module, with
feature directories supplying Namespace nodes. Changes to `angular.json`, route
configuration, or compiler ownership invalidate affected aggregate manifests.

### .NET solution adapter

| Level | Derivation and identity tuple |
| --- | --- |
| Project | Each discovered or explicitly selected `.sln`/`.slnx`; `(solution repo path)`. If none exists, one deterministic synthetic repository project contains all discovered MSBuild projects and uses `(repo root, literal synthetic-dotnet-project)` as its stable identity; the sorted project paths belong in its aggregate manifest, not its ID. Multiple solution files are separate Project roots, not an arbitrary winner. |
| Module | Each solution member `.csproj`, evaluated through MSBuild; `(Project ID, project repo path)`. Target frameworks are build variants of one module. A linked compile file receives a distinct File identity in every owning module. Project references are graph edges, not parent/child levels. |
| Namespace | Roslyn's fully qualified semantic namespace, including `<global>`; `(Module ID, namespace name)`. File-scoped and block namespaces normalize to the same identity. If a file contributes to multiple namespaces, the browser shows an alias beneath each while every alias points to one canonical File unit and one sidecar. |
| File | Evaluated `Compile` items after standard generated/output exclusions; `(Module ID, repo path)`. Partial types do not merge files: each physical source remains independently reviewable. |
| Function | Roslyn documentation-comment ID for methods, constructors, operators, conversions, and accessors; `(File ID, documentation ID)`. A local function uses `(containing documentation ID, local name, normalized signature, syntax span start)` because Roslyn has no public documentation ID for it. Lambdas/anonymous methods remain locations on their containing function in v1. |

MSBuild evaluation failures, missing projects, ambiguous case aliases, and files
outside the repository are diagnostics, not guessed hierarchy. A file projected
under multiple namespaces keeps the same `unit.id`; aggregate member lists
de-duplicate that ID.

## Staleness contract

Staleness is a computed view of an immutable review statement. Scanning or
browsing MUST NOT rewrite a meta file merely to mark it stale. Evaluation happens
independently for every unit and kind against the current working-tree content,
so an unstaged edit is visible immediately; HEAD/index/worktree state is shown
separately as Git decoration.

| State | Leaf File/Function | Project/Module/Namespace aggregate |
| --- | --- | --- |
| `fresh` | Current canonical subject manifest equals `reviewedHash`. | Current member IDs/hashes and aggregate control inputs exactly match the stored manifests. Empty-to-empty is fresh when its controls also match. |
| `partially-stale` | Not applicable. | At least one stored member still exists with the same hash **and** a member or aggregate control input is changed, added, or deleted. |
| `stale` | Unit still derives, but its manifest differs; symbol extraction failure also makes a Function stale. | Manifest differs and no stored member remains valid, or the aggregate anchor/identity can no longer be derived reliably. |
| `missing` | Unit derives but the requested kind has no meta file. | Unit derives but the requested kind has no aggregate meta file. This is displayed as “not reviewed,” not as F. |
| `unsupported` | Bytes/syntax cannot be canonicalized by v1. | A required member cannot be derived or hashed. |
| `orphaned` | Meta exists but its unit no longer derives. | Aggregate meta exists but its unit no longer derives. It is reported outside the current hierarchy. |

For aggregates the evaluator compares by `unitId`, then reports `changed`,
`added`, and `deleted` member and control-input lists. A rename is one deleted and
one added member.
The historical grade remains visible with “last reviewed” wording, but a partial
or stale grade MUST NOT use fresh colors or be included in a current-grade
average. No parent grade is recalculated from child grades.

`reviewInputs.effectiveHash` is compared separately with today's resolved
standards and prompt. A mismatch adds `inputs-stale` (with changed/added/deleted
standard IDs) to any of the content states above. `complete: false` adds
`inputs-incomplete`. This separation answers whether code moved, the briefing
moved, or both. It also prevents a policy edit from masquerading as a source edit.

When a File or Function is not fresh, stored ranges are historical anchors. The
editor may show the text and finding in a stale panel, but it MUST disable exact
inline/gutter placement until a best-effort remap is explicitly labeled and the
review is rerun. Hash or schema parse errors fail closed as stale/invalid, never
fresh. Partial staleness is per kind: a stale performance aggregate says nothing
about the code or security sibling.

## Review standards and input management

Standards are Markdown so human guidance stays reviewable. Project inputs live at
`.quality/inputs/**/*.md`. The global directory is configurable with
`QUALITY_STUDIO_GLOBAL_INPUTS`; defaults are
`%APPDATA%/AgentOrchestrator/Quality/inputs` on Windows and
`${XDG_CONFIG_HOME:-~/.config}/agent-orchestrator/quality/inputs` elsewhere.
Built-in prompt guidance is the lowest scope. A project can operate without a
global directory.

Every file has YAML front matter followed by the exact Markdown body injected
into the review. After YAML parsing, the following JSON Schema is normative (the
`content` property represents the body after the closing delimiter):

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://agent-orchestrator.dev/quality/schemas/review-input.v1.schema.json",
  "title": "Quality Studio review input v1",
  "type": "object",
  "additionalProperties": false,
  "required": ["schemaVersion", "id", "version", "title", "enabled", "priority", "appliesTo", "content"],
  "properties": {
    "schemaVersion": { "const": 1 },
    "id": {
      "type": "string",
      "pattern": "^[a-z0-9][a-z0-9._-]{1,127}$"
    },
    "version": {
      "type": "string",
      "minLength": 1,
      "maxLength": 100
    },
    "title": {
      "type": "string",
      "minLength": 1,
      "maxLength": 200
    },
    "enabled": { "type": "boolean" },
    "priority": {
      "type": "integer",
      "minimum": 0,
      "maximum": 1000
    },
    "appliesTo": {
      "type": "object",
      "additionalProperties": false,
      "required": ["kinds", "levels", "technologies", "paths"],
      "properties": {
        "kinds": {
          "type": "array",
          "minItems": 1,
          "uniqueItems": true,
          "items": { "enum": ["code", "security", "performance"] }
        },
        "levels": {
          "type": "array",
          "minItems": 1,
          "uniqueItems": true,
          "items": { "enum": ["project", "module", "namespace", "file", "function"] }
        },
        "technologies": {
          "type": "array",
          "minItems": 1,
          "uniqueItems": true,
          "items": { "enum": ["angular", "dotnet", "any"] }
        },
        "paths": {
          "type": "array",
          "minItems": 1,
          "uniqueItems": true,
          "items": {
            "type": "string",
            "minLength": 1,
            "maxLength": 500
          }
        }
      }
    },
    "content": {
      "type": "string",
      "maxLength": 100000
    }
  },
  "allOf": [
    {
      "if": {
        "type": "object",
        "properties": { "enabled": { "const": true } },
        "required": ["enabled"]
      },
      "then": {
        "type": "object",
        "properties": { "content": { "type": "string", "minLength": 1 } }
      }
    }
  ]
}
```

Example project input:

```markdown
---
schemaVersion: 1
id: frontend.interaction-budget
version: "2026-07-11"
title: Frontend interaction performance budget
enabled: true
priority: 800
appliesTo:
  kinds: [performance]
  levels: [file, module]
  technologies: [angular]
  paths: ["frontend/**"]
---
Tree interactions must remain below the repository's measured p95 budget.
Report synchronous main-thread work and avoid unbounded DOM growth.
```

Resolution is deterministic:

1. Parse and validate all files. Duplicate IDs inside one scope are an error; no
   filesystem-order winner exists. Glob syntax is Git wildmatch over canonical
   repo paths. Unknown kinds/levels and malformed documents are errors surfaced
   before a review starts.
2. Merge by ID using whole-document replacement:
   built-in &lt; global &lt; project. A higher scope with the same ID replaces the
   entire lower document; fields and arrays do not merge. A project document with
   `enabled: false` is an explicit tombstone for the lower-scope ID and may have
   an empty body.
3. Filter the winners by kind, level, technology, and path. Sort matches by
   `priority` descending, then scope (`project`, `global`, `built-in`), then ID
   ordinal. The resolver prints this order and every replacement/filter reason in
   `quality review --explain-inputs`.
4. Apply a configurable prompt-character budget. The default is fail-closed: if
   all matching documents do not fit, do not run the review. An explicit
   allow-omission option may include complete documents in order until the budget
   is reached, but MUST print and persist every omitted ID, set `complete: false`,
   and never silently truncate a document body.
5. Normalize each complete Markdown body with the source-text rules, hash it, and
   persist ID, winning scope, version, and hash in `reviewInputs.standards`.
   Effective-hash inputs also include the versioned prompt template. This makes a
   run auditable even when a global standard is not committed to the target repo.

## Product core: augmented code browsing

The primary surface is a three-pane engineer workspace, not a dashboard:

- The left pane opens at repository/workspace roots and incrementally expands
  projects, modules, logical namespaces/folders, files, and functions. Every
  reviewable node shows separate code/security/performance chips with grade and
  `fresh`, `partial`, `stale`, or `missing` state. Presentation-only folder nodes
  show explicitly derived descendant state. Git HEAD/index/worktree decorations
  coexist with, but never replace, quality state.
- The center is a real viewport-rendered editor (CodeMirror 6 is the v1 target)
  with line numbers, syntax, selection, keyboard search, and read-only source.
  Findings are aspect-colored gutter/range decorations at the code. A kind/aspect
  switcher changes overlays without reloading the blob; it never collapses a file
  to one blanket good/bad badge.
- The right pane explains the selected aspect/finding, evidence, recommendation,
  grade rationale, reviewer and timestamp, and offers re-review or handover.
  Freshness is inline in the header, tree, gutter, and detail. A stale banner keeps
  historical content visible but disables authoritative line anchors.
- The tree has roving focus and standard keyboard semantics: arrows navigate and
  expand/collapse, Home/End jump, type-ahead selects, Enter opens, Shift/Ctrl
  extend selection where supported, and the context-menu key/Shift+F10 opens the
  same actions as right click. Focus, selection, expanded state, and loading state
  are distinct and screen-reader named.
- Context actions are scoped: open, review this unit/kind, scan subtree, explain
  inputs, copy canonical path/ID, and hand over selected fresh findings. Errors
  remain attached to the node/action and can be retried.

### Performance requirements and technical path

Performance is an acceptance constraint from the first UI slice. On the agreed
reference desktop, against a warm local API and a representative repository with
at least 100,000 presentation nodes, the targets are:

- p95 expand/collapse, keyboard move, and selection scripting time below 50 ms;
- p95 first visible file text below 150 ms from selection for files up to 200 KB,
  excluding application cold start and an agent review run;
- quality badges for the visible viewport within 100 ms after text, without
  blocking typing/navigation; and
- bounded rendered tree/editor DOM proportional to the viewport, not repository
  or file size.

QS-8 records hardware, corpus, trace method, cold/warm cache state, p50/p95, and
regressions in `PERF.md`; a claim without those measurements is not accepted.
Files above 200 KB remain usable via plain/chunked text and receive an explicit
large-file mode rather than freezing for full syntax highlighting.

The implementation path is a virtualized flat tree with stable node IDs and
incremental, paged child loading; an Angular signal store updates only affected
rows. The editor renders only visible lines. Syntax and review decorations are
parsed lazily in a Web Worker or backend task with cancellation, bounded
concurrency, and chunked delivery. Content and meta responses use ETags keyed by
canonical subject/meta hashes. Hashing streams off the UI thread, and caches are
keyed by repo, path, Git blob/index identity, working-tree fingerprint, and kind.

A filesystem watcher plus incremental `git status --porcelain=v2 -z` refreshes
only affected paths and ancestors. The model distinguishes HEAD, index, and
working tree, including untracked, ignored, modified, staged, renamed, and deleted
files. Rename pairs preserve UI selection while the derived unit becomes stale.
Watcher overflow, branch switch, or Git index replacement triggers a visible
background rescan; cancellation/debounce prevents refresh storms. File loads do
not wait for a repository-wide scan.

## Quality Studio to Agent Studio handover

The former component-package/iframe/API-only embedding evaluation is dropped.
Quality Studio calls Agent Studio through its configured normal task mutation
endpoint. No Quality Studio panel or special task type is required in Agent
Studio. The integration adapter maps this versioned logical envelope into the
current task create fields and sends the normal mutation client identity/header;
it does not invent a second mutation path.

```json
{
  "contractVersion": 1,
  "idempotencyKey": "sha256:eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
  "targetProjectId": "PROJ-016",
  "source": {
    "repositoryId": "agent-orc/quality-studio",
    "repositoryUrl": "https://github.com/agent-orc/quality-studio",
    "revision": "4f3c2b1",
    "workingTreeDirty": false,
    "file": "src/Orders/Services/OrderPricingService.cs",
    "reviewedContentHash": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
    "currentContentHash": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
    "reviewedHash": "sha256:89abcdef0123456789abcdef0123456789abcdef0123456789abcdef01234567",
    "metaPath": "src/Orders/Services/.quality/reviews/files/file.bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.review-meta.performance.json",
    "schemaVersion": 1,
    "reviewedAt": "2026-07-11T15:04:18.023Z",
    "kind": "performance",
    "sourceState": "fresh",
    "backlink": "https://quality.local/quality/repos/agent-orc%2Fquality-studio/file?path=src%2FOrders%2FServices%2FOrderPricingService.cs&kind=performance&finding=serialized-price-lookups"
  },
  "findings": [
    {
      "id": "serialized-price-lookups",
      "fingerprint": "sha256:d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0",
      "aspect": "latency",
      "severity": "high",
      "title": "Independent lookups run sequentially",
      "description": "Each item waits for the previous remote lookup.",
      "recommendation": "Use bounded concurrency while preserving cancellation.",
      "locations": [
        {
          "path": "src/Orders/Services/OrderPricingService.cs",
          "startLine": 41,
          "endLineExclusive": 42
        }
      ]
    }
  ],
  "task": {
    "title": "Fix: Independent lookups run sequentially in OrderPricingService.cs",
    "prompt": "Address the attached Quality Studio finding against the recorded source snapshot. Re-check current code before editing and preserve cancellation and service limits.",
    "acceptanceCriteria": [
      "Relevant automated tests pass.",
      "A performance review rerun is fresh.",
      "The selected finding fingerprint is absent or its disposition is explicitly justified."
    ]
  }
}
```

The selected findings are immutable snapshots, not live pointers. One handover
may batch findings only from the same meta document and primary file; cross-file
selections create separate tasks. The idempotency key is lowercase SHA-256 over
UTF-8 `quality-studio/handover/v1\0<metaPath>\0<reviewedHash>\0<sorted finding
IDs joined by comma>`. Retries reuse it; Agent Studio or the client rejects a
duplicate rather than creating another card.

Fresh findings hand over directly. A stale review is allowed only after an
explicit confirmation, and carries both hashes plus `sourceState: stale`; unknown
current content blocks handover. The task prompt always tells the agent to inspect
current code. The backlink uses the configured Quality Studio origin and stable
repo/path/kind/finding query, never a hard-coded development host.

Configuration supplies Agent Studio base URL, target project, authentication, and
client ID. Dry-run is the default until configured. Mutation failure leaves the
selection intact and supports idempotent retry. A successful response returns the
normal task ID/URL for UI navigation; the review-meta document is not rewritten
with task state, preserving repo-owned review history.

## Package naming finalization

The proposal is to publish the core as `AgentOrchestrator.CodeQuality`, with the
same .NET root namespace and assembly name. It describes the reusable schema,
hierarchy, hashing, staleness, and sweep-planning core without coupling the
package to the Quality Studio UI. `AgentOrchestrator.Quality` is too broad, while
`AgentOrchestrator.QualityStudio` would make a standalone core sound UI-specific.

At 2026-07-11 16:55 UTC, NuGet's exact-ID flat-container endpoint for
[`AgentOrchestrator.CodeQuality`](https://api.nuget.org/v3-flatcontainer/agentorchestrator.codequality/index.json)
returned HTTP 404, and autocomplete returned no ID match. NuGet documents that
the [package base-address resource](https://learn.microsoft.com/en-us/nuget/api/package-base-address-resource)
enumerates listed and unlisted versions, so no public version was observed at
that instant. This is an availability observation, not a reservation or a
guarantee that first publish will succeed.

An adjacent package,
[`AgentOrchestrator.Runtime`](https://www.nuget.org/packages/AgentOrchestrator.Runtime),
already exists under an owner that must be confirmed as part of this ecosystem.
Before release, the operator MUST confirm namespace ownership, enable the
appropriate NuGet organization/owners, seek an
[ID-prefix reservation](https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation)
where eligible, and repeat the exact-ID check. Documentation should use the final
ID now; QS-2 may build/pack it but MUST NOT publish it.

## Research box: graphical code graph

No graph UI or graph-backbone decision is made here. QS-12 is a time-boxed,
three-day spike that asks whether a graphical meta layer materially improves
module/namespace navigation and impact reasoning without compromising instant
browsing.

The spike compares Roslyn workspace, .NET language-server data, and
compiled-assembly reflection on one representative .NET solution, and measures
initial load, incremental one-file update, peak memory, edge count, and navigation
usefulness on a corpus including a 100-project solution. It records what an
analogous TypeScript/Angular investigation would need, but does not dilute the
three-day spike with a second prototype. Its throwaway prototype may render or
print ownership/reference edges, but is excluded from production builds.

The artifact is `docs/research/code-graph.md` with measurements, screenshots or
output, accessibility considerations, risks, and a recommendation of “adopt,”
“research further,” or “do not adopt.” Exit requires evidence that the graph
answers a concrete engineer question faster than the tree and an incremental path
that stays within UI budgets. Impact-analysis staleness, a production graph
database, cross-repository graphs, and replacing the derivable hierarchy are
explicit non-goals.

## Website seed for `agent-orchestrator.dev/quality`

The operator's ecosystem page is the visual/content seed. The page must describe
what exists and label planned screens as such; it must not advertise unshipped
capability. Its English outline is:

1. Hero: “the engineer room” in the Agent Orchestrator universe, with a clear
   founded/building-in-the-open status and link to the repository.
2. Augmented browser visual: tree, code, aspect overlays, grades, and inline
   staleness—the product core rather than a generic analytics dashboard.
3. Standing truth: sidecar JSON, Git history, exact hashes, and the distinction
   from task-time diff reviews.
4. Five levels and three kinds: independent statements, detachable security, and
   aspect-split findings.
5. Review inputs: global standards plus project overrides with auditable hashes.
6. Handover story: triage a finding in Quality Studio, create a normal Agent
   Studio task, return through the backlink; no embedding claim.
7. Standalone path: package, CLI sweep runner, API, and repository ownership of
   output.
8. Ecosystem neighbors: Agent Studio, Coding Agent Runner, Coding Agent Chat,
   Token Economy, and project graph, without implying hard dependencies.
9. Transparent roadmap/research box, documentation links, license, and a “follow
   development” call to action.

## Honest implementation slices

The founding prompt used legacy `CQ` numbers; the repository and already-created
cards use `QS`, so this plan preserves QS-2 through QS-13. Estimates are focused
engineering days after dependencies are available: S=1-2, M=3-5, L=6-10. They
include tests and documentation but not queue time, external review, or release
approval. Separate API, frontend, augmented-browser, and handover slices are
intentional; calling them one “Studio embedding” slice would hide the work and
contradict the decided direction.

| Slice | Size / dependencies | Shippable outcome and proof |
| --- | --- | --- |
| QS-2 Core scaffold + release rails | S (2 days); none | .NET core/test solution, taxonomy enums, deterministic/package metadata and SourceLink, warning-clean build/test/pack CI, Apache metadata, and a retained package artifact. Mirrors the Token Economy release-rail shape but has no publish credential, tag, or automatic NuGet release. Proof: clean build/test/pack on Windows and Linux. |
| QS-3 Review-meta contract v1 | M (5 days); QS-1, QS-2 | Records/serializer, strict schema artifact, canonical text/manifest hashing, sample files, and compatibility policy matching this document. Proof: schema-valid examples, round trips, line-ending/encoding/hash vectors, malformed/invariant tests. |
| QS-4 Staleness engine + scan | M (4 days); QS-3 | Streaming discovery and per-kind fresh/stale/missing/unsupported/orphaned evaluation, aggregate-ready diagnostics, and CI-friendly `quality scan`. Proof: fixtures plus a 5,000-file scan showing lazy bounded hashing. |
| QS-5 Hierarchy + aggregation | L (8 days); QS-3, QS-4 | Angular and .NET adapters, canonical IDs, all five levels, sibling kinds, aggregate manifests, partial-staleness truth table, and `scan --by-level`. Proof: multi-project Angular and multi-project .NET fixtures including standalone components, namespaces, linked files, and member add/change/delete cases. |
| QS-6 File review sweep runner v1 | L (7 days); QS-3, Coding Agent Runner | One-file `code`/`security`/`performance` runs, strict structured response parsing, versioned prompt hooks, atomic sidecar writes, cancellation and dry-run. Proof: parsing/prompt tests, opt-in live-agent test, and one committed sample; no module review yet. |
| QS-7 Minimal API | M (4 days); QS-4, QS-5; QS-6 optional | Paged tree, file/meta, scan, and optional review endpoints with repo-root confinement, ETags, cancellation, problem details, and structured timing logs. Proof: host smoke tests and curl contract examples. |
| QS-8 Angular shell + performance rails | L (8 days); QS-7 | Three-pane, accessible, light/dark shell with virtualized incremental tree and viewport editor, Agent Studio visual kinship, Git decorations, trace harness, and `PERF.md`. Proof: production build, both-theme screenshots, 100k-node and 200KB-file p95 budgets. |
| QS-9 Augmented browser v1 | L (7 days); QS-8, QS-5 | Per-node kind/grade/staleness meta layer, aspect switcher, inline findings, historical stale mode, and no-refetch overlay switching. Proof: interaction tests/screenshots plus unchanged QS-8 p95 budgets. This is the first product-core UI milestone. |
| QS-10 Agent Studio handover | M (5 days); QS-7, QS-9, current Agent Studio mutation contract | Configured task client, versioned snapshot mapping, X-Client-Id/auth path, idempotency, dry-run, stale confirmation, UI action, and backlinks. Proof: mock mutation tests, duplicate retry test, dry-run artifact, and optional scratch-project card. |
| QS-11 Website `/quality` seed | M (3 days); product wording from QS-1 | Self-contained family-styled static page and documented deploy-branch/operator meta-repo step. Proof: local light/dark/accessibility/HTML checks and deployment workflow validation; status copy distinguishes shipped/planned. |
| QS-12 Code-graph research | S (3-day hard stop); QS-5 fixtures useful | Research report and excluded throwaway spike with initial/incremental/memory measurements; no production dependency. Proof: reproducible output and one evidence-based next-step decision, not a speculative architecture commitment. |
| QS-13 Global + project review inputs | M (5 days); QS-3, QS-6; QS-7/8 for exposure | Markdown/frontmatter resolver, validation, precedence/tombstones, explicit budget report, effective input hashing, CLI explanation, read-only API/UI view. Proof: resolution/hash/omission tests and an explain-inputs transcript. |

Suggested delivery order is QS-2 → QS-3 → QS-4/QS-6 → QS-5/QS-13 → QS-7 →
QS-8 → QS-9 → QS-10. QS-11 and the strictly time-boxed QS-12 can proceed in
parallel once their inputs are stable. Module/project agent review execution is a
later slice after QS-5; QS-5 only makes hierarchy and aggregate truth honest.

## Review-meta operational rules and examples

### Semantic invariants and grade scale

JSON Schema cannot express every invariant. A writer and loader MUST additionally
enforce all of the following:

- `unit.adapter`, `unit.level`, `unit.path`, and `unit.symbolId` agree with the
  derived `unit.id`; finding IDs and aspect IDs are unique within the document;
  every `finding.aspect` names an entry in `aspects`.
- Paths use the Git-index spelling, `/` separators, no leading `./`, no `..`, and
  are relative to the repository root. Arrays used in hashes are in the canonical
  order defined below, and contain no duplicate identity keys.
- Locations are 1-based Unicode-scalar line/column positions. `end` is exclusive
  and MUST be after `start`. A location without a range points to the whole file.
- `reviewedAt` is an RFC 3339 UTC instant ending in `Z`; writers use millisecond
  precision. `reviewer` is provenance, not an endorsement.
- Scores are integers from 0 through 100. Bands are A=90-100, B=80-89,
  C=70-79, D=60-69, F=0-59; there are no plus/minus grades in v1. The reviewing
  agent assigns aspect and overall grades and explains them in `rationale`.
  V1 deliberately defines no hidden severity-to-score arithmetic. An absent meta
  document means **not reviewed**, never an implicit F.
- A finding ID is stable for the same issue across reruns when reconciliation can
  establish identity. `fingerprint`, when present, is the runner's deterministic
  matching aid; it is not a security signature. Re-review replaces the statement
  as a whole rather than mutating findings to “resolved.”
- Severity means: `critical` is an imminent exploit, data-loss, or unusable-system
  risk; `high` is a likely serious defect or budget breach; `medium` is a material
  maintainability/correctness risk with a viable workaround; `low` is bounded
  improvement; `info` is non-actionable context. File/Function findings MUST have
  at least one location; higher-level findings MAY use an empty `locations` array
  when no honest source anchor exists.
- Only Project, Module, and Namespace documents contain `aggregate`. Its grade
  and findings are that level's own review statement. `members` records coverage;
  it is not a list from which the grade is mechanically calculated.

### Granularity, names, and placement

`<kind>` is the lowercase enum value. `<unit-key>` is the full, lowercase
SHA-256 hex digest of the UTF-8 bytes of `unit.id`. Full hashes avoid escaping,
case, and collision rules in cross-platform filenames.

| Level | Anchor and filename |
| --- | --- |
| Project | `<project-root>/.quality/reviews/project.<unit-key>.review-meta.<kind>.json` |
| Module | `<module-root>/.quality/reviews/module.<unit-key>.review-meta.<kind>.json` |
| Namespace | `<module-root>/.quality/reviews/namespaces/namespace.<unit-key>.review-meta.<kind>.json` |
| File | `<source-directory>/.quality/reviews/files/file.<unit-key>.review-meta.<kind>.json` |
| Function | `<source-directory>/.quality/reviews/functions/function.<unit-key>.review-meta.<kind>.json` |

File IDs include their compiler/module context, so the unit key prevents a linked
.NET file compiled by two modules from colliding with itself. The `.quality`
directory is a child of the source's feature folder and therefore remains beside
the code rather than in a central service database. Review discovery MUST exclude
`*.review-meta.*.json`, `.quality/`, `bin/`, `obj/`, Angular output directories,
and adapter-identified generated files from source inputs.

Renames derive a new unit ID and filename. Git may carry the old artifact through
the rename, but it remains orphaned until rewritten with the new identity; tools
MUST NOT guess identity from similar content. The namespace filename is anchored
at its module because a semantic namespace can span several folders. Project and
module files are the requested aggregate files; namespace uses the same aggregate
contract.

Multiple kinds are siblings, for example:

```text
order-card.component.ts
.quality/reviews/files/
  file.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.review-meta.code.json
  file.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.review-meta.performance.json
  file.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.review-meta.security.json
```

Each sibling has its own timestamp, reviewer, grade, findings, subject hash, and
input hash. A security package can therefore own or remove only the security
sibling without teaching the core about security internals.

### Exact hashing contract

V1 hashes the exact normalized text supplied to the reviewer, not the JSON meta
file, filesystem timestamps, Git commit, or platform-native line endings.

1. Read a subject as UTF-8 (with or without BOM), UTF-16LE with BOM, or UTF-16BE
   with BOM. Invalid text and binary files are `unsupported` in v1 and MUST NOT
   receive fabricated review metadata.
2. Decode to Unicode, remove only the encoding BOM, replace CRLF and lone CR with
   LF, and make no other change: no trimming, Unicode normalization, tab
   expansion, or final-newline insertion. Encode the result as UTF-8 without BOM.
3. For a whole-file selector (`file`), `contentHash` is `sha256:` plus SHA-256 of
   those bytes. For a compiler-derived symbol selector, hash the exact syntax
   span supplied to the reviewer after the same transform, including attached
   attributes/decorators and documentation comments. Companion template/style
   files are separate whole-file entries, not concatenated invisibly.
4. Sort `subjectInputs` ordinally by `(path, selector)`. Construct the object
   `{"domain":"quality-studio/reviewed-subject/v1","unitId":<unit.id>,"inputs":<subjectInputs>}`,
   serialize it with RFC 8785 JSON Canonicalization Scheme, hash its UTF-8 bytes,
   and store the lowercase hex digest in `reviewedHash.value`.
5. For an aggregate, derive immediate child units, recursively calculate each
   child's current subject hash, sort `aggregate.members` ordinally by `unitId`,
   and use entries shaped as `{unitId,path,subjectHash}`. SHA-256 the RFC 8785
   object `{"domain":"quality-studio/aggregate-members/v1","members":<members>}`
   and add that digest to `subjectInputs` as an `aggregate-members` selector at
   the aggregate anchor. Also add normalized whole-file inputs with selector
   `aggregate-control` for structural files actually reviewed at that level—for
   example `angular.json`, route/NgModule declarations, a solution, or a
   `.csproj`. Apply step 4 to this sorted combined input list. Thus project or
   dependency configuration changes stale an aggregate even when its source-file
   membership is unchanged, while `aggregate.members` still enables a precise
   partial-staleness explanation.

Including paths and selectors means a rename or symbol-identity change is stale
even when text is identical. Hashing normalized reviewer bytes makes the result
portable across Git line-ending settings. `reviewInputs.effectiveHash` is SHA-256
of the RFC 8785 object
`{"domain":"quality-studio/review-inputs/v1","complete":<complete>,"standards":<ordered standards>,"omitted":<resolver-ordered omitted IDs>,"prompt":<prompt>}`.
It detects briefing changes without misreporting them as code changes.

### Worked example: Angular component code review

Location:
`frontend/src/app/orders/order-card/.quality/reviews/files/file.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.review-meta.code.json`.
Digest values are illustrative but structurally valid.

```json
{
  "$schema": "https://agent-orchestrator.dev/quality/schemas/review-meta.v1.schema.json",
  "schemaVersion": 1,
  "unit": {
    "id": "qs-v1/angular/file/frontend%2Fangular.json/storefront/frontend%2Fsrc%2Fapp%2Forders%2Forder-card%2Forder-card.component.ts",
    "adapter": "angular",
    "level": "file",
    "path": "frontend/src/app/orders/order-card/order-card.component.ts",
    "displayName": "OrderCardComponent"
  },
  "reviewedAt": "2026-07-11T14:32:09.417Z",
  "kind": "code",
  "reviewer": {
    "agent": "codex",
    "model": "gpt-5",
    "agentVersion": "1.0.0",
    "runId": "run-01jzq3a0k8d2"
  },
  "reviewedHash": {
    "algorithm": "sha256",
    "canonicalization": "quality-studio-subject-manifest-v1",
    "value": "2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a2a"
  },
  "subjectInputs": [
    {
      "path": "frontend/src/app/orders/order-card/order-card.component.html",
      "selector": "file",
      "contentHash": "sha256:1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b1b"
    },
    {
      "path": "frontend/src/app/orders/order-card/order-card.component.ts",
      "selector": "file",
      "contentHash": "sha256:3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c"
    }
  ],
  "reviewInputs": {
    "effectiveHash": {
      "algorithm": "sha256",
      "canonicalization": "quality-studio-review-inputs-v1",
      "value": "4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d"
    },
    "complete": true,
    "standards": [
      {
        "id": "angular.component-contract",
        "scope": "project",
        "version": "2026-07-01",
        "contentHash": "sha256:5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e5e"
      }
    ],
    "omitted": [],
    "prompt": {
      "id": "file-code-review",
      "version": "1.0.0",
      "contentHash": "sha256:6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f"
    }
  },
  "grade": {
    "score": 82,
    "band": "B",
    "rationale": "Clear component boundaries, with an avoidable duplicated state branch."
  },
  "summary": "The component is readable and focused; its empty-state logic needs one consolidation.",
  "aspects": [
    {
      "id": "correctness",
      "title": "Correctness",
      "grade": {
        "score": 91,
        "band": "A",
        "rationale": "Inputs and emitted events are handled consistently."
      }
    },
    {
      "id": "maintainability",
      "title": "Maintainability",
      "grade": {
        "score": 76,
        "band": "C",
        "rationale": "Two branches encode the same empty-state decision."
      }
    }
  ],
  "findings": [
    {
      "id": "duplicate-empty-state",
      "fingerprint": "sha256:7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a7a",
      "aspect": "maintainability",
      "severity": "medium",
      "ruleId": "angular.single-state-source",
      "title": "Empty state has two sources of truth",
      "description": "The computed state and template condition can diverge when the input changes during loading.",
      "evidence": "Both the computed branch and the template test independently check whether orders is empty.",
      "recommendation": "Expose one computed view state and render only from that value.",
      "locations": [
        {
          "path": "frontend/src/app/orders/order-card/order-card.component.ts",
          "range": {
            "start": { "line": 28, "column": 3 },
            "end": { "line": 34, "column": 4 }
          },
          "symbolId": "OrderCardComponent#viewState"
        }
      ]
    }
  ]
}
```

### Worked example: .NET service performance review

Location:
`src/Orders/Services/.quality/reviews/files/file.bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.review-meta.performance.json`.

```json
{
  "$schema": "https://agent-orchestrator.dev/quality/schemas/review-meta.v1.schema.json",
  "schemaVersion": 1,
  "unit": {
    "id": "qs-v1/dotnet/file/QualityStudio.slnx/src%2FOrders%2FOrders.csproj/src%2FOrders%2FServices%2FOrderPricingService.cs",
    "adapter": "dotnet",
    "level": "file",
    "path": "src/Orders/Services/OrderPricingService.cs",
    "displayName": "OrderPricingService"
  },
  "reviewedAt": "2026-07-11T15:04:18.023Z",
  "kind": "performance",
  "reviewer": {
    "agent": "codex",
    "model": "gpt-5",
    "runId": "run-01jzq5t5yf4m"
  },
  "reviewedHash": {
    "algorithm": "sha256",
    "canonicalization": "quality-studio-subject-manifest-v1",
    "value": "89abcdef0123456789abcdef0123456789abcdef0123456789abcdef01234567"
  },
  "subjectInputs": [
    {
      "path": "src/Orders/Services/OrderPricingService.cs",
      "selector": "file",
      "contentHash": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
    }
  ],
  "reviewInputs": {
    "effectiveHash": {
      "algorithm": "sha256",
      "canonicalization": "quality-studio-review-inputs-v1",
      "value": "adadadadadadadadadadadadadadadadadadadadadadadadadadadadadadadad"
    },
    "complete": true,
    "standards": [
      {
        "id": "dotnet.hot-paths",
        "scope": "global",
        "version": "3",
        "contentHash": "sha256:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789"
      }
    ],
    "omitted": [],
    "prompt": {
      "id": "file-performance-review",
      "version": "1.0.0",
      "contentHash": "sha256:cfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcfcf"
    }
  },
  "grade": {
    "score": 68,
    "band": "D",
    "rationale": "The hot path performs sequential remote price lookups."
  },
  "summary": "Correct output, but request latency grows linearly with item count.",
  "aspects": [
    {
      "id": "latency",
      "title": "Latency",
      "grade": {
        "score": 58,
        "band": "F",
        "rationale": "Await inside the loop serializes independent calls."
      }
    },
    {
      "id": "allocation",
      "title": "Allocation",
      "grade": {
        "score": 87,
        "band": "B",
        "rationale": "The method allocates only its result collection."
      }
    }
  ],
  "findings": [
    {
      "id": "serialized-price-lookups",
      "fingerprint": "sha256:d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0",
      "aspect": "latency",
      "severity": "high",
      "title": "Independent lookups run sequentially",
      "description": "Each item waits for the previous remote lookup, so latency is approximately N times one lookup.",
      "evidence": "GetPriceAsync is awaited within the foreach body.",
      "recommendation": "Issue bounded concurrent lookups and await them together, preserving cancellation and service limits.",
      "locations": [
        {
          "path": "src/Orders/Services/OrderPricingService.cs",
          "range": {
            "start": { "line": 41, "column": 13 },
            "end": { "line": 41, "column": 77 }
          },
          "symbolId": "M:Orders.Services.OrderPricingService.CalculateAsync(System.Collections.Generic.IReadOnlyList{Orders.Order},System.Threading.CancellationToken)"
        }
      ]
    }
  ]
}
```

### Aggregate example: .NET module code review

Location (with an illustrative 64-hex unit key):
`src/Orders/.quality/reviews/module.6666666666666666666666666666666666666666666666666666666666666666.review-meta.code.json`.
Project aggregates use the same shape at the project anchor.

```json
{
  "$schema": "https://agent-orchestrator.dev/quality/schemas/review-meta.v1.schema.json",
  "schemaVersion": 1,
  "unit": {
    "id": "qs-v1/dotnet/module/QualityStudio.slnx/src%2FOrders%2FOrders.csproj",
    "adapter": "dotnet",
    "level": "module",
    "path": "src/Orders/Orders.csproj",
    "displayName": "Orders"
  },
  "reviewedAt": "2026-07-11T15:20:02.100Z",
  "kind": "code",
  "reviewer": {
    "agent": "codex",
    "model": "gpt-5",
    "runId": "run-01jzq6v6d2qs"
  },
  "reviewedHash": {
    "algorithm": "sha256",
    "canonicalization": "quality-studio-subject-manifest-v1",
    "value": "1111111111111111111111111111111111111111111111111111111111111111"
  },
  "subjectInputs": [
    {
      "path": "src/Orders/Orders.csproj",
      "selector": "aggregate-control",
      "contentHash": "sha256:6666666666666666666666666666666666666666666666666666666666666666"
    },
    {
      "path": "src/Orders/Orders.csproj",
      "selector": "aggregate-members",
      "contentHash": "sha256:7777777777777777777777777777777777777777777777777777777777777777"
    }
  ],
  "reviewInputs": {
    "effectiveHash": {
      "algorithm": "sha256",
      "canonicalization": "quality-studio-review-inputs-v1",
      "value": "2222222222222222222222222222222222222222222222222222222222222222"
    },
    "complete": true,
    "standards": [],
    "omitted": [],
    "prompt": {
      "id": "module-code-review",
      "version": "1.0.0",
      "contentHash": "sha256:3333333333333333333333333333333333333333333333333333333333333333"
    }
  },
  "grade": {
    "score": 88,
    "band": "B",
    "rationale": "Clear boundaries, with one dependency direction to simplify."
  },
  "summary": "The module is cohesive and its current aggregate coverage is complete.",
  "aspects": [
    {
      "id": "boundaries",
      "title": "Boundaries",
      "grade": {
        "score": 88,
        "band": "B",
        "rationale": "Public entry points are narrow and internally consistent."
      }
    }
  ],
  "findings": [],
  "aggregate": {
    "members": [
      {
        "unitId": "qs-v1/dotnet/namespace/QualityStudio.slnx/src%2FOrders%2FOrders.csproj/Orders",
        "path": "src/Orders",
        "subjectHash": "sha256:4444444444444444444444444444444444444444444444444444444444444444"
      },
      {
        "unitId": "qs-v1/dotnet/namespace/QualityStudio.slnx/src%2FOrders%2FOrders.csproj/Orders.Services",
        "path": "src/Orders/Services",
        "subjectHash": "sha256:5555555555555555555555555555555555555555555555555555555555555555"
      }
    ],
    "excluded": [
      {
        "path": "src/Orders/obj",
        "reason": "MSBuild generated output"
      }
    ]
  }
}
```

## V1 acceptance boundary

V1 is credible when a developer can derive an Angular or .NET hierarchy, run a
file-level review of any built-in kind, commit independently stale-aware sidecars,
browse them at the code within measured budgets, resolve the exact standards that
informed them, and hand a finding snapshot to Agent Studio. It does not claim
automatic remediation, dependency-impact staleness, a production code graph,
real-time multi-user collaboration, or a computed universal quality score.
