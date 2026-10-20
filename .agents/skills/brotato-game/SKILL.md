---
name: brotato-game
description: Project guidance for changing, debugging, or reviewing Brotato-Game Unity code and serialized assets.
metadata:
  short-description: Work safely in the Brotato Unity project
---

# Brotato-Game project workflow

Use this skill to make scoped changes to Brotato-Game and explain the current implementation. Its architecture and gameplay documents are static snapshots; the checked-out source and Unity serialization remain authoritative.

## When to use it

Use for repository-specific implementation, debugging, architecture, gameplay, or Unity setup questions. Do not infer a request to add features from the PRD or assume planned systems already exist.

## Source-of-truth rule

At task start, inspect `git status` and the relevant C# source, Scene/Prefab YAML, `.meta` references, and settings. These outrank `Doc/`, README, and the Product Requirements Document, which may describe a different or planned state. Record drift rather than promoting planned behavior to fact. For absent evidence, say `Not found in repository` or `Unknown / Not verified`.

Read only the relevant reference: [architecture.md](architecture.md) for design boundaries; [gameplay.md](gameplay.md) for implemented gameplay; [coding-rules.md](coding-rules.md) before C# edits; [debugging.md](debugging.md) for defects; [unity-setup.md](unity-setup.md) for Unity configuration; and [git-workflow.md](git-workflow.md) before Git mutations.

## Task workflow

1. Identify the requested behavior and inspect its current owner, callers, dependencies, and serialized wiring.
2. Reuse the current design and make the smallest coherent change. Do not create duplicate systems or add abstractions without a concrete need.
3. Before public API or asset changes, search code references, Scene/Prefab YAML, overrides, and `.meta` GUIDs.
4. Preserve unrelated working-tree changes and Unity tuning. Keep documentation-only tasks out of gameplay assets/settings.
5. Run only the relevant checks requested and supported; distinguish static inspection, Editor compilation, Play Mode, and playtesting.
6. Update the relevant reference when verified architecture or behavior changes; keep snapshots and unknowns explicit.

## Delegation

Delegate only independent, bounded work and include the concrete request, relevant evidence, and expected output. Avoid parallel agents editing the same files.

- `code-mapper`: read-only ownership, call-path, and serialized-reference mapping before multi-file implementation.
- `architect-reviewer`: read-only design, coupling, and duplication review when a feature changes system boundaries.
- `debugger`: read-only evidence-led root-cause analysis for a reported defect.
- `game-developer`: scoped implementation after mapping the actual code and Unity wiring; preserve unrelated work and serialized references.

The parent agent integrates findings, owns overlapping decisions, and reports runtime verification that remains `Unknown / Not verified`.

## Reporting

Report changed files, evidence, checks actually run, and remaining `Not found in repository` / `Unknown / Not verified` items. Keep the report in Vietnamese and do not present PRD plans as implementation facts.

