# Brotato-Game — Instructions for Codex Agents

## Source of truth

- The current repository is the source of truth: inspect C# source, scenes, prefabs, `.meta` GUIDs, `ProjectSettings`, and `Packages/manifest.json` before making claims or changes.
- Read `.agents/skills/brotato-game/SKILL.md` and the relevant companion reference for project-specific architecture, gameplay, Unity, debugging, and Git context.
- Existing README/Doc/PRD content may describe plans or an older snapshot. Do not treat an unverified system as implemented. Mark uncertain facts `Unknown / Not verified`.

## Implementation rules

1. Inspect the current code and serialized Unity assets before every change. Trace the caller, owning component, dependencies, scene/prefab wiring, and configuration.
2. Prefer reusing the architecture and systems already present. Never create a duplicate Manager/System when an implementation exists.
3. Keep changes small, targeted, safe, and easy to inspect. Do not perform a broad refactor unless requested.
4. Before renaming or deleting a class, field, or method, search code callers and serialized references, including scene/prefab YAML, overrides, and `.meta` GUID links.
5. Protect Unity Scenes, Prefabs, Inspector values, serialized field names, asset GUIDs, and user changes. Confirm component presence and reference assignments rather than assuming them.
6. If a change needs Inspector setup, report the exact `GameObject`, `Component`, `Field`, and `Reference` the user must assign.
7. Find and support a root cause before proposing or applying a bug fix. Separate evidence from hypotheses.
8. For a large task (multiple systems, broad cross-file work, architecture changes, or an unclear multi-stage feature), make a short plan before implementation.
9. Do not change the Unity version, upgrade/add packages, or alter project-wide Unity settings unless the user explicitly requests it.
10. Do not overwrite uncommitted working-tree changes. Inspect `git status` first; preserve user edits and isolate changes. If safe progress is blocked, explain why.
11. Never run `git reset --hard`, `git clean -fd`, or `git push --force` unless the user explicitly requests that exact operation.
12. Do not automatically commit or push. Only do so when the user asks.
13. Do not delete code/assets or rewrite architecture without checking dependencies and references first.
14. Do not claim compilation, runtime behavior, tests, or playability unless actually verified. If not confirmed, write `Unknown / Not verified`.
15. Communicate with the user in Vietnamese.

## Subagent routing

Use project agents for bounded tasks when delegation is useful; provide the original request and concrete evidence/scope to each agent.

- `code-mapper`: read-only code path, ownership, entry point, and dependency mapping before implementation.
- `game-developer`: targeted Unity/gameplay implementation after inspecting the project Skill and current wiring.
- `debugger`: read-only root-cause analysis and evidence-backed fix recommendation.
- `architect-reviewer`: read-only architecture, coupling, duplication, and feature design review.

All agents must follow these instructions and `.agents/skills/brotato-game/SKILL.md`. Re-inspect current files; do not assume a system exists just because an older document mentions it.
