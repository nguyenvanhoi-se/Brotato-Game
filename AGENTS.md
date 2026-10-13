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

## Pull Request Review Rules

When reviewing pull requests:

- Prioritize demonstrable bugs, gameplay regressions, broken references, serialization issues, and violations of approved project requirements.
- Verify claims against the current C# source, serialized Unity assets, and relevant references. Do not assume documentation describes the current implementation.
- Follow `.agents/skills/brotato-game/SKILL.md` and consult only the focused references relevant to the changes under review.
- Check affected callers and serialized Scene/Prefab references before flagging renames, deletions, or API changes.
- Look for null-reference risks, missing components, incorrect Inspector assignments, invalid state transitions, event subscription issues, duplicate systems, and resource-lifetime problems when relevant.
- Check gameplay behavior only against requirements supported by the current repository and approved design decisions. Do not treat planned features as implemented requirements.
- Flag unnecessary architecture changes, duplicate Managers, unrelated refactoring, unauthorized Unity version or package changes, and changes that overwrite existing user work.
- Report only actionable findings. For each finding, include severity, file and line, triggering conditions, impact, and a concise suggested fix.
- Distinguish confirmed defects from potential issues. Label anything requiring Unity Editor, Inspector, compilation, or Play Mode verification as `Unknown / Not verified` until verified.
- Do not claim that code compiles, tests pass, or gameplay works unless evidence supports the claim.
- Ignore minor style preferences unless they materially affect correctness, maintainability, or consistency with project conventions.
- Write review summaries in Vietnamese. If no actionable findings are identified, say so and state important verification limitations.
