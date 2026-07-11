# QS-3: Review-meta contract v1

## Context
The heart of Quality Studio: review truth lives NEXT TO the code as JSON metadata, with a content hash that detects staleness. QS-1 produces the concept document with the exact schema decisions (file placement, naming, hash normalization). This card implements that contract in AgentOrchestrator.CodeQuality (scaffold from QS-2).

## Wait condition
Requires QS-1's concept document (docs/concepts/) and QS-2's scaffold to be merged. If the QS-1 doc is missing, STOP and report instead of inventing a divergent schema.

## Deliverables
1. C# records for the meta document: review kind, level, timestamp (UTC ISO), reviewer identity (agent model + version), reviewedHash, verdict/grade, findings list (severity, message, optional line range), schema version field.
2. Serializer (System.Text.Json, camelCase, deterministic property order) + loader with forward-compatible unknown-field tolerance.
3. reviewedHash: SHA-256 over normalized content (exact normalization rules from the QS-1 doc - line endings at minimum).
4. JSON Schema file under `schemas/review-meta.v1.schema.json` kept in sync with the records.
5. Round-trip + hash-normalization unit tests.

## Verification
dotnet test green; a hand-written sample meta file under `samples/` validates against the schema. Commit + push.
