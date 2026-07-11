# QS-7: Minimal API host

## Context
The Quality Studio frontend (QS-8) needs a backend. Small ASP.NET Core minimal API over the core library. Builds on QS-4/QS-5; review triggering integrates QS-6 when present.

## Deliverables
1. `src/QualityStudio.Api`: endpoints
   - GET /api/tree?path= - hierarchy nodes with aggregated per-kind state (QS-5)
   - GET /api/file?path= - file content + its meta documents
   - GET /api/scan - staleness report (QS-4)
   - POST /api/review - trigger a review (QS-6) if available, else 501 with a clear message
2. Configurable repo root (appsettings + env override), CORS for the dev frontend.
3. Consistent JSON casing with the meta contract; problem-details errors.
4. Smoke tests (WebApplicationFactory) for tree + scan.

## Verification
dotnet test green; curl samples for each endpoint in the task log. Commit + push.
