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

## Automated E2E regression (AltTester + Python + pytest)

- Read `E2E/README.md` and `E2E/PROJECT_AUDIT.md` before changing tests. Checked-in source/assets remain authoritative; refresh `python E2E/run.py --audit-only` after feature or scene changes.
- New reachable features require complete player journeys in `E2E/catalog.py` and assertions in `E2E/tests/test_project_flows.py` (or a focused new test module); include stable Test ID, objective, preconditions, steps and expected results. Update the Feature → Flow → Case mapping. Do not treat InputAction declarations or PRD plans as implemented features.
- Changes to gameplay, state transitions, UI names, prefab references, save data or enabled scenes require updating affected cases and regression expectations. Inspect GUIDs/callers and extend the audit; the current catalog does not automatically prove coverage of new source.
- Natural E2E uses the real Windows build. Hooks under `E2E/Unity/` are copied only into the isolated `.e2e/projects/` clone; label hook-created scenarios `simulated`. Never introduce replacement gameplay types to get tests compiling, never count simulated scenarios as complete player journeys, or add test hooks to production builds.
- Setup: `python E2E/bootstrap.py`; start an installed/licensed AltTester Desktop/Server compatible with SDK 2.3.2. Desktop/Server 2.4.0 is installed locally; its Start Server requires a valid license key. Full regression: `python E2E/run.py --build-game`. Existing matching build: `python E2E/run.py --build <exe>`. The source checkout's Assets/Packages/ProjectSettings are not instrumented in place.
- Run `python -m unittest discover -s E2E/selftests -v` for harness/report changes. These are infrastructure checks and must not be reported as game E2E passes.
- Before reporting completion, inspect the run's `report.html`, `results.json`, `junit.xml`, `pytest.xml`, `pytest.log`, Unity/player logs and screenshots. Report PASS/FAIL/SKIP/BLOCKED and Not Tested/Not Applicable separately; report the CLI exit code. Missing Editor/license/SDK/server/build is Blocked, never PASS. A compile failure blocks player tests; it is not an executed gameplay failure.
- Keep failures visible; do not suppress failed tests, add blanket xfail, change approved expectations just to pass, or claim 100% coverage without feature-level runtime evidence. List source/wiring gaps and all Unknown / Not verified items. Do not commit/push unless requested.
