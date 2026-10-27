# Independent Unity E2E testing

Python/pytest drives a real Windows player through a development-only TCP bridge.
No external SDK, Desktop application, server subscription or test license is used.
Only pytest is an external Python dependency; Unity/Input System/UI packages already
belong to this project. Original gameplay, assets, packages and settings are preserved.

Use the Codex skill at `.agents/skills/unity-e2e-testing/SKILL.md`; AGENTS.md routes
E2E work there automatically. The generic bridge is separate from the Brotato
adapter in E2EHarness. Porting to another Unity game requires inspecting that game's
scene wiring and adapting its fixture/state contract, not inventing missing features.

## CLI

From the repository root with a real Python 3.10+ interpreter, Git and Unity
6000.3.22f1 plus Windows Build Support:

```powershell
python E2E/bootstrap.py
python E2E/run.py --audit-only
python E2E/run.py --build-game
python E2E/run.py --build 'D:/test-build/BrotatoE2E.exe'
python E2E/run.py --build 'D:/test-build/BrotatoE2E.exe' --case E2E-001
python .agents/skills/unity-e2e-testing/scripts/run.py --audit-only
python -m unittest discover -s E2E/selftests -v
```

Override Editor location with `--unity <path>`. Output is a new directory beneath
`.e2e/reports/independent-<UTC timestamp>/`, or `--out <empty-directory>`. Build and
suite timeouts default to900s (`--build-timeout`, `--suite-timeout`). Windows needs a
desktop/graphics session for the real player; the build Editor itself runs batchmode.
The supplied runtime Python can be used if `python` on PATH is a Store alias.

## Execution and isolation

1. Inventory/hash all original Assets, Packages, ProjectSettings; record source,
   enabled scenes, APIs, object hierarchy, script GUID references and input maps.
2. Copy only those project folders into a generated clone under `.e2e/projects/`.
   Add E2E/Unity there. Persist BROTATO_E2E in a configure Editor invocation, then
   build Windows Development in a second invocation. No extra Unity package is added.
3. Builder adds a fresh project adapter to enabled clone scenes and writes a success
   manifest only after BuildPipeline reports Succeeded. Only the clone enables
   runInBackground, so inspection doesn't pause frame-driven tests.
4. Each case launches a fresh player with a random local port, token and session
   nonce. The built-in bridge listens on loopback. Python verifies PID, nonce,
   protocol and source/build fingerprints; it cannot silently drive a stale player.
5. Commands query actual objects/state, drive Input System/UI, load scenes and save
   actual PNGs. Unity APIs run on its main thread; socket workers only queue commands.
6. Record step observations, assertion failures, runtime errors, player logs and
   failure screenshots. Always close the driver and only the process tree created
   for that case. A flow may explicitly close the player after checking its errors.
7. Aggregate setup/call/teardown outcomes without dropping failures. Cases not selected
   remain Not Tested. A timeout, missing dependency or build cannot become PASS.

Exit0 means selected cases actually passed; exit1 indicates pytest failure; exit2
or another nonzero code indicates blocked/incomplete/infrastructure execution.
`--audit-only` exit0 means inventory generation only, not runtime success. A subset
can exit0 while other cases are Not Tested. Preserve both failed and final runs.

## Journeys and coverage

The32 reviewed IDs were preserved during migration. E2E-001 executes the declarative
`flows/startup.json` lifecycle: launch → scene/camera/light assertions → screenshot →
error check → close. E2E-002/004 test idle/relaunch; E2E-003 uses driver scene reload.
Enemy/health/spawner/contact scenarios execute production components after explicit
simulated fixture setup. They are never counted as natural player journeys.

The actual SampleScene currently contains only camera/light. There is no wired
play/combat/win/lose/menu/save loop. The framework doesn't create those systems to
manufacture coverage. Source/API inventory is a discovery aid, not automatic proof
of semantic coverage. See PROJECT_AUDIT.md, catalog.COVERAGE_GAPS and the run's report.

Declarative steps: launch, assert_scene, wait_visible, assert_property, click,
press_key, load_scene, wait_state, screenshot, expect_no_errors, hook, close.
Flow ID/kind must match its catalog case; hook setup is rejected for natural flows.
Input/UI primitives require accessible game objects and are not evidence that a
gameplay feature exists. For authoring/protocol details read the skill references.

## Evidence

| Artifact | Meaning |
|---|---|
| report.html | Specs, actual, Feature→Flow→Case→Execution→Coverage |
| results.json / junit.xml | Counts, build/connection/runtime execution and case status |
| pytest.xml / pytest.log / events.jsonl | Raw pytest results |
| inventory.json | Original source/config hashes and inspected APIs/assets |
| unity-configure.log / unity-build.log | Real Editor compilation/build |
| build-command.json / `<exe>.e2e.json` | Exact commands and successful-build provenance |
| `<ID>/observations.json`, player-N.log | Real player actions/state/logs |
| `<ID>/*.png`, screenshot-error.txt | Actual captures or explicit capture failure |

JUnit represents Blocked with skipped type=BLOCKED and status properties. HTML/JSON
separate ordinary SKIP from BLOCKED; CI must also respect the nonzero CLI exit code.
Infrastructure selftests do not contribute to game PASS counts. Never suppress a
failure by changing gameplay/expectations without evidence and authorization.

## Legacy preservation

Before migration, committed E2E source/specs and all historical reports outside
binary build payloads were ZIP-backed up and verified under
`.e2e/backups/pre-independent-e2e-20261027/`. Original report directories remain
readable. Historical results describe the old tool and must not be mistaken for a
current run. SDK/Desktop/Server, their dedicated dependencies, generated clones and
old instrumented executables were removed; see that backup's cleanup.json.
RESTORED_SOURCES.md remains source provenance, not an installation instruction.

The framework uses Python standard-library sockets/JSON and built-in Unity APIs.
Relevant primary references: [ScreenCapture](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ScreenCapture.CaptureScreenshot.html),
[scripting symbols](https://docs.unity3d.com/6000.3/Documentation/Manual/custom-scripting-symbols.html).
