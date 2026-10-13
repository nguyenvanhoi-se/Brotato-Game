# Brotato-Game agent instructions

## Source of truth

- Treat the checked-out C# source and serialized Unity assets as authoritative. `Doc/`, README, and the Product Requirements Document can describe plans or an older snapshot; verify claims before treating them as implemented.
- At task start, check `git status` and inspect only the files and Unity references relevant to the request. Mark absent or unverified systems `Not found in repository` or `Unknown / Not verified`.
- Use `.agents/skills/brotato-game/SKILL.md` for project workflow. It routes to focused references; read only the references relevant to the task.

## Change safety

- Prefer the smallest change that reuses an existing implementation. Do not introduce duplicate Managers/systems or impose an architecture not supported by the current code.
- Before renaming or deleting code, search C# callers and serialized Scene/Prefab references, including field names and `.meta` GUIDs. Preserve Unity serialization, Inspector assignments, and user changes.
- Do not overwrite dirty work, change Unity version/packages/settings, or edit unrelated assets. Never run `git reset --hard`, `git clean -fd`, or `git push --force` without an explicit request. Do not commit or push unless asked.
- For broad or multi-system work, first provide a short plan. If manual Inspector work is needed, name the `GameObject`, `Component`, `Field`, and `Reference`.
- Verify at the level requested and supported. Do not claim compilation, tests, or runtime behavior without evidence.

## Project Skill and agent roles

- Read `.agents/skills/brotato-game/SKILL.md`; then use its links to select `architecture.md`, `gameplay.md`, `coding-rules.md`, `debugging.md`, `unity-setup.md`, or `git-workflow.md` as needed.
- Use project agents only when delegation helps with an independent, bounded task. `code-mapper`, `architect-reviewer`, and `debugger` are read-only. `game-developer` implements scoped changes after inspecting the current source and references.
- Keep reports in Vietnamese. State evidence, files changed, verification performed, and remaining `Unknown / Not verified` items.
