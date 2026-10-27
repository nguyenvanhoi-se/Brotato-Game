---
name: unity-e2e-testing
description: Build and drive this Unity project's real player for E2E journeys and regression tests, collect screenshots/logs, and audit feature coverage using the independent Python/pytest driver. Use for Unity E2E execution, test authoring, coverage, and automation failures.
---

# Unity E2E testing

Use the repository's independent development-build bridge and pytest runner. No
commercial SDK, Desktop service or license is needed. Read the current source and
serialized scene/prefab wiring; the catalog is an inventory of reviewed cases,
not automatic proof that every feature is covered.

## Inspect, then run

1. Check git status, enabled build scenes and affected source/GUID references.
   Read the project-specific audit in [E2E/PROJECT_AUDIT.md](../../../E2E/PROJECT_AUDIT.md).
   Distinguish scene-reachable player journeys from component scenarios built by hooks.
2. From the repository root, use a real Python interpreter:

   ```powershell
   python E2E/bootstrap.py
   python .agents/skills/unity-e2e-testing/scripts/run.py --audit-only
   python .agents/skills/unity-e2e-testing/scripts/run.py --build-game
   ```

   Reuse `--build <exe>` only when both source and Unity-hook/build fingerprints match.
   Use `--case <ID>` for diagnosis; unselected cases must remain Not Tested.
3. Inspect report.html/results.json/junit.xml, raw pytest events, build/player logs
   and actual PNGs before reporting results. A collected or skipped case is not an
   executed journey. Report build status, verified driver connection, runtime case
   count and PASS/FAIL/SKIP/BLOCKED, including uncovered/not-applicable features.

## Author and diagnose

- For new journeys and declarative JSON, read [authoring.md](references/authoring.md).
  Map Feature → accessible User Flow → stable Case ID → observed result. Prefer
  input/UI actions and state assertions through the real player. Label direct hook
  setup `simulated`; it must not inflate natural journey coverage.
- For transport/build failures or driver extensions, read [protocol.md](references/protocol.md).
  The bridge belongs only in the isolated test clone, guarded by BROTATO_E2E and
  Development build. Never copy it into production assets or change gameplay to
  make a failing test pass.
- Attribute failures to observed source behavior, test logic, or environment.
  Repair the narrow cause, rerun affected cases, then run full regression. Preserve
  failed artifacts; do not replace failing expectations with an automatic PASS.
- Run existing infrastructure checks after framework/report changes:
  `python -m unittest discover -s E2E/selftests -v`. These do not count as game E2E passes.

The current game may lack a complete play/win/lose loop. Record that absence and
coverage gaps instead of constructing a new game state machine for testing.
Original Assets/Packages/ProjectSettings and restored Player/Wave source must stay
unchanged unless a separately authorized, evidenced gameplay repair requires it.
