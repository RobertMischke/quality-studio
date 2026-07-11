# QS-11: Website seed for /quality

## Context
Quality Studio gets its page under agent-orchestrator.dev/quality. A designed ecosystem/concept page already exists as the seed (operator artifact); this card turns the repo into a deployable static site following the established pattern of the runner repo (agent-orc/runner, website/ folder deployed 1:1 via deploy branch).

## Deliverables
1. `website/index.html` (+ assets): self-contained static page - what Quality Studio is (engineer room, layered agent reviews, quality truth next to the code, augmented browsing), the three review kinds, the level hierarchy, the handover-to-Agent-Studio direction, and a "status: founded 2026-07-11, building in the open" note. Light/dark via prefers-color-scheme. English. No external CDNs.
2. Visual kinship with the family pages (look at agent-orc/runner website/ and agent-orc/token-economy website/ for tone and structure) while keeping an own accent.
3. `website/DEPLOY.md`: deployment steps mirroring the runner pattern (deploy branch + meta-repo sites.json entry for slug "quality", path /quality/ - the sites.json edit itself is an operator step in the private meta-repo, document it, do not attempt it).
4. GitHub Actions workflow publishing website/ to the deploy branch on push to main (copy the proven runner workflow).

## Verification
Page opens locally in both themes, passes a quick HTML validation, workflow YAML valid. Commit + push.
