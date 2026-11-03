# Agent skill inventory

Checked 2026-10-17 (Asia/Bangkok) for TASK-001.

| Source | Observed state | TASK-001 relevance |
| --- | --- | --- |
| `C:\Users\Admin\.codex\skills` | `.system`, `archify`, `research-direction-discovery` | `skill-installer` available under `.system`; no Flutter/Dart task skill found |
| `C:\Users\Admin\.agents\skills` | Workflow and app skills, no Flutter/Dart specific skill found | No TASK-001 skill found |
| Project `.agents/skills` | `flutter-apply-architecture-best-practices`, `state-management-riverpod`, `incremental-implementation` installed from the runbook's named GitHub repositories using the built-in installer | Read for TASK-001 |
| `codex plugin list` | Flutter/Dart and Addy plugins not installed | TASK-001 matrix skills unavailable locally |

TASK-002 additions: `dart-add-unit-test` and `dart-run-static-analysis` from official
`dart-lang/skills`; `api-and-interface-design` and `test-driven-development` from
`addyosmani/agent-skills`. All were installed with the built-in installer into
this project's `.agents/skills`; the unit-test, interface-design, and TDD
`SKILL.md` files were read. No bundled third-party scripts were executed.

TASK-003 additions: `persistence-drift` and `codegen-and-toolchain` from
`zakariaf/Flutter-Skills`, installed into `.agents/skills` and read. Their shell
scripts have not been executed. The project uses feature-first Data folders as
required by `AGENTS.md`; the skill's generic `lib/data/` layout is advisory.

TASK-006 additions: `service-boundary-and-native` and `async-safety` from
`zakariaf/Flutter-Skills`, installed into `.agents/skills` and read. The camera
plugin is kept in one Data adapter and injected through an app-level gateway.

TASK-015 additions: `ci-pipeline-and-gates` and `release-and-store-shipping`
from `zakariaf/Flutter-Skills`, installed into `.agents/skills` and read.
Selected official GitHub/Flutter/Fastlane documentation was checked before
writing the workflows. No third-party skill scripts were run.

Sources named by the runbook to inspect: official `flutter/agent-plugins`, `dart-lang/skills`, and selected `zakariaf/Flutter-Skills` / `addyosmani/agent-skills`. Three TASK-001 skills above were installed; the rest have not been installed. The official Flutter architecture skill's default MVVM directory layout conflicts with this project's explicit Domain/Data/Presentation layout, so project rules take precedence.

No third-party skill scripts have been executed. The Riverpod skill's `scripts/ban-legacy-providers.sh` has not been run because this is a Windows PowerShell environment and no app source exists yet. Skill availability should be checked per task.
