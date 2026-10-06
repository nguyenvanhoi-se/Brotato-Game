# Project guidance

Small university Unity 2D Brotato-like game; three student developers. Prefer readable, simple code and a complete gameplay loop. Explain code and Unity setup in Vietnamese.

## Read first

- [State and next steps](docs/STATE.md): current handoff, bugs and verification limits.
- [Architecture](docs/ARCHITECTURE.md): actual systems and scene dependencies.
- [Decisions](docs/DECISIONS.md): reasons and constraints to preserve.
- [Game design](docs/GAME_DESIGN.md): current versus planned rules; read before gameplay changes.
- [Original plan](ke_hoach_do_an_game_2d_brotato_like.md): full roadmap, not proof that features exist. Later explicit user decisions override its examples.

## Constraints and conventions

- Use configured Unity (currently 6000.3.22f1); do not upgrade Unity or install/update packages without an explicit request.
- Preserve unrelated working-tree changes. Inspect code AND serialized scene/prefab values; Inspector values override C# initializers.
- Prefer private fields, [SerializeField] tunables, small focused MonoBehaviours and meaningful names. Avoid third-party libraries, unnecessary frameworks/interfaces/inheritance/design patterns.
- Scripts: Assets/Scripts/{Core,Player,Weapon,Enemy,Wave,Item,UI}/; scenes: Assets/Scenes/; prefabs: Assets/Prefabs/.
- Main gameplay scene: Assets/Scenes/GameScene.unity. Keep existing scenes/assets; older Gameplay.unity is not equivalent.
- Player uses normalized WASD input, Rigidbody2D, zero gravity and locked physics rotation. Use the existing Input System.
- Preserve one-life gameplay (HP zero -> Game Over), nonnegative HP/timers, bounded living enemies, mouse aim and automatic fire without clicks.
- Current user-approved MVP: 3 waves lasting 30/30/45 seconds, living caps 10/15/30 and enemy HP 10/20/30. See docs/GAME_DESIGN.md for spawn/speed/transition policy. Final 9-wave scope is planned.
- Avoid manually rewriting large .unity, .prefab, .meta or ProjectSettings files. Prefer Unity Inspector or small Editor automation. Do not change Packages/manifest.json without necessity and authorization.
- Commit asset .meta files with assets; generated Library/, Temp/, Logs/, builds and IDE files are not source.
- Inspect and explain the smallest change before editing. Do not add the next feature automatically; stop at requested scope.
- Check C# compilation and relevant Unity behavior. Compilation is not gameplay verification; claim Play Mode passed only if actually executed.
- Explain Inspector wiring/manual tests when required; report files changed, checks and limitations.

## Session workflow

At the beginning of a new session:

1. Read AGENTS.md.
2. Read docs/STATE.md.
3. Read relevant parts of docs/ARCHITECTURE.md.
4. Read relevant decisions in docs/DECISIONS.md (and docs/GAME_DESIGN.md for gameplay).
5. Inspect current git status/diff and relevant code before changing anything.

At the end of a meaningful task:

1. Update docs/STATE.md.
2. Update docs/DECISIONS.md if a meaningful technical/design decision was made.
3. Update docs/ARCHITECTURE.md if architecture changed.
4. Update docs/GAME_DESIGN.md if stable game rules/design changed.
5. Record tests/checks performed and remaining issues; label uncertainty Needs verification or Unknown.
