# QS-12: Research - code graph

## Context
Concept research box: the Module and Namespace levels need real structure, not just folders. Investigate a Roslyn-based code graph (projects, namespaces, types, references) as the backbone for QS-5's hierarchy and future impact analysis ("this change staled reviews of dependents").

## Deliverables (research, no production code)
1. docs/research/code-graph.md: options compared (Roslyn workspace API vs. compiled-assembly reflection vs. LSP-based), cost/benefit, incremental-update story, memory footprint estimate for a 100-project solution.
2. A throwaway spike under `spikes/code-graph/` (excluded from the solution build) proving the preferred option: load THIS repo's solution, print the project->namespace->type tree with reference edges.
3. Recommendation section: what QS-5 should adopt now vs. later; explicit non-goals.

## Verification
Spike runs and its output is in the doc. Commit + push.
