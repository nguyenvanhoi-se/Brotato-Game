---
name: brotato-game
description: Project-specific Unity Brotato-Game development skill for architecture, gameplay, debugging, coding conventions, Unity setup, and Git workflow.
metadata:
  short-description: Work safely in the Brotato Unity project
---

# Role and goal

Act as the engineering agent for this Unity project. Help implement requested features, debug defects, refactor narrowly, and explain the code while preserving the architecture that actually exists in the checkout. The accompanying files are a static repository snapshot, not a promise that the current branch still matches it.

## When this skill applies

Use it for changes, debugging, refactoring, architectural questions, or Unity setup work in this repository. It is not an instruction to add gameplay features automatically or to invent systems from the product plan.

## Source-of-truth rule

At the start of each task, inspect `git status` and relevant files. For implementation facts, prefer current C# source, scene/prefab YAML, `.meta` references, `ProjectSettings`, and `Packages/manifest.json` over older prose. `Doc/` and the root `Product Requirements Document` contain claims/plans that do not match the checked-in `Assets/` snapshot in several places. Record differences as drift; do not silently turn a documented goal into an implemented feature. If a required type or asset is not present, say `Not found in repository` or `Unknown / Not verified`.

Read [architecture.md](architecture.md) before architecture-sensitive work. Read [gameplay.md](gameplay.md) for gameplay questions, [coding-rules.md](coding-rules.md) before code edits, [debugging.md](debugging.md) for defects, [unity-setup.md](unity-setup.md) for editor/asset settings, and [git-workflow.md](git-workflow.md) before Git operations.

## Task workflow

1. Restate the requested outcome in concrete terms and identify the files/systems that current code actually uses.
2. Trace references in both directions: callers, component lookups, serialized scene/prefab links, script GUIDs, events, and data types. Search for every reference before changing a public API, class name, serialized field, or asset.
3. Reuse existing implementations when present. Make the smallest coherent change; do not add a second manager, health model, input path, data source, or duplicate system.
4. Preserve unrelated user changes and serialized tuning. Do not edit gameplay, scenes, prefabs, or ProjectSettings for a documentation-only request.
5. Verify only what the user requested and the environment supports. Report checks actually run; distinguish static inspection from Unity compilation, Play Mode, and human playtesting.
6. Update relevant project documentation when actual architecture or behavior changes. Keep these skill files consistent and label unknowns.

## Safe change rules

- Preserve existing class names, public members, serialized field names, and prefab/scene references unless required and all references have been checked.
- Do not delete code/assets based on a single search result. Inspect GUIDs, YAML references, build settings, and code callers first.
- Avoid broad refactors, package changes, or project-wide setup changes unless requested.
- Do not assume a script is wired because its `.cs` file exists; inspect scene/prefab components. Do not assume a prefab is used because it exists; search for instances/references.
- Do not treat an Input Actions asset or package as proof that gameplay consumes it.
- Follow [git-workflow.md](git-workflow.md). Never run destructive history/worktree commands listed there without an explicit request.

## Debug and Unity workflow

Use the evidence-led checklist in [debugging.md](debugging.md). Confirm the symptom, identify the exact component and lifecycle path, trace data/object references, inspect serialized wiring, then make a scoped fix. Use [unity-setup.md](unity-setup.md) as an inspected baseline and re-check current files. Preserve `.meta` GUID links and Unity serialization. Do not invoke setup tools or edit scenes merely to validate source. If Editor/runtime verification is unavailable or not requested, state that limit.

## Git workflow

Check `git status` before and after work. If there are existing user changes, do not overwrite, reset, clean, stage, commit, pull over, or otherwise absorb them without authorization; preserve them and tell the user if they affect the task. The documented branch workflow is not blanket authorization to push unrelated work.

## Reporting

Respond in the user's language. Summarize files changed, verified implementation findings, checks actually performed, and remaining unknowns/risks. Separate `Implemented`, `Not implemented / Not found in repository`, and `Unknown / Not verified` where useful. Never present planned PRD behavior or stale documentation as repository fact.

