# Decisions

Accepted intent unless marked temporary. Actual defects/checks belong in [STATE](STATE.md). Full roadmap remains in [original plan](../ke_hoach_do_an_game_2d_brotato_like.md).

## D01 - Simple, scoped implementation

- Decision: small readable MonoBehaviours, private fields/Inspector tunables and existing packages; defer optional features/frameworks/pooling.
- Context: three students learning Unity; project must be finishable and explainable.
- Reason: reliable core loop takes priority over complexity/content volume.
- Alternatives: advanced patterns/AI/pooling are optional in plan, not initial requirements.
- Tradeoff: direct dependencies and temporary hardcoding need documentation.
- Preserve: no Unity/package upgrades, new third-party dependencies or automatic next-feature work without user direction.

## D02 - GameScene is the gameplay entry

- Decision: continue in Assets/Scenes/GameScene.unity; enabled build scene.
- Context: older Gameplay.unity did not contain full weapon setup.
- Reason: consolidate progress in existing combat scene.
- Alternatives: migrating older scene was not selected; deleting it was unnecessary.
- Tradeoff: legacy scenes remain and may confuse sessions.
- Preserve: keep scenes/assets; do not silently change main scene or create one scene per wave.

## D03 - Mouse aim and continuous automatic fire

- Decision: mouse world-position aim, fire without holding left button.
- Context: detailed weapon/control design and conversation specify this; introductory plan text mentions auto-targeting inconsistently.
- Reason: user moves/dodges while directing aim.
- Alternatives: held-button firing existed initially; nearest-enemy targeting is not current design.
- Tradeoff: depends on Mouse and Camera.main; fireRate means interval in seconds, unlike planned shots/sec examples.
- Preserve: no click-to-fire or enemy auto-aim without a new design request; retain units when migrating data.

## D04 - Three configurable waves for MVP (updated 2026-09-18)

- Decision: 3 waves in one scene: duration 30/30/45 seconds, living caps 10/15/30, enemy HP 10/20/30, spawn intervals 1.5/1/0.6 seconds and speed 2/2.5/3 units/s.
- Context: explicit user request changed 60 seconds to 30 during Milestone 2 work.
- Reason: current approved scope; 9 waves follow later.
- Alternatives: original 45-60-second plan range is superseded for duration; final nine-wave scope remains future.
- Latest user request supersedes the earlier all-30-second setting and multiplier examples.
- Tradeoff: inline serializable WaveSettings are editable in Inspector; reusable WaveData ScriptableObjects remain planned.
- Preserve: absolute per-wave stats, not compounded multipliers; counts mean concurrent living caps, not a total spawn budget.

## D05 - One life replaces test respawn

- Decision: HP zero emits PlayerHealth.Died and ends round; PLAY AGAIN reloads a fresh scene.
- Context: automatic full-HP respawn was briefly added for testing, removed for survival-loop implementation.
- Reason: original one-life rule and meaningful fail condition.
- Alternatives: multiple lives/permanent automatic respawn not selected.
- Tradeoff: death testing requires restart; dead Player remains visible while combat freezes.
- Preserve: nonnegative HP, shared cooldown across attackers, one death notification, no unsolicited respawn.

## D06 - Minimal UI and global round freeze (temporary)

- Decision: GameManager.OnGUI displays HUD/outcomes; end-round disables spawner/movement/weapon and sets Time.timeScale=0.
- Context: testable win/lose loop before UI polish.
- Reason: little scene/UI setup needed.
- Alternatives: Canvas/UIManager is planned, not yet implemented.
- Tradeoff: UI/game logic coupled; timeScale pauses physics/scaled clocks, NOT Update callbacks. Retry/cleanup must restore timeScale.
- Preserve: explicit shutdown/reset behavior; do not assume timeScale alone stops shooting.

## D07 - Prefab spawning and living-enemy cap (updated 2026-09-18)

- Decision: instantiate Enemy outside orthographic camera, parent under Enemies and count active EnemyHealth with HP > 0.
- Context: beginner-friendly bounded spawning.
- Reason: minimal bookkeeping; destroying a child frees cap space.
- Alternatives: registry/events/pooling not selected for this phase.
- Tradeoff: scan the small bounded enemy subtree instead of maintaining a registry; minimum distance from Player is 3 units.
- Preserve: absolute stat initialization for new/placed enemies; clear old enemies before the next wave, never heal survivors by reconfiguring mid-wave.

## D08 - Repository-backed memory

- Decision: concise AGENTS plus STATE/DECISIONS/ARCHITECTURE/GAME_DESIGN under docs; keep original plan.
- Context: explicit user request to continue future sessions without this long chat.
- Reason: inspectable Git-shareable facts, intent and uncertainty.
- Alternatives: chat-only memory rejected by request; duplicate .cursor rules unnecessary.
- Tradeoff: refresh docs after meaningful tasks; current code/serialized values and later instructions override stale summaries.
- Preserve: no invented verification; session workflow refreshes relevant memory files.

## D09 - Intermediate cleanup and one-life HP

- Decision: show WAVE COMPLETE for 2 unscaled seconds, freeze combat, remove old enemies/bullets, preserve Player HP/position, then resume.
- Reason: no old weaker enemies or projectiles leaking into a new difficulty level; no damage during the announcement.
- Final wave uses Victory; intermediate WaveComplete is not a terminal win.
- Tradeoff: survivors disappear without kill rewards (no reward/drop system exists yet). Revisit with that future feature.

## D10 - Follow camera, no arbitrary arena wall

- Decision: Main Camera follows Player XY in LateUpdate and preserves Z; Player remains unbounded until a map rectangle is designed.
- Context: user explicitly requested camera-follow verification; no authored map bounds were provided.
- Reason: Player remains visible and spawn region travels with gameplay.
- Tradeoff: flat prototype background makes movement less visually obvious; no camera smoothing/map edge clamp yet.

## D11 - One accepted hit per bullet

- Decision: consumed-hit flag before damage, immediate deactivation, delayed Destroy; disable redundant BoxCollider2D while retaining CircleCollider2D.
- Reason: multiple collider callbacks must not multiply damage. Real Play Mode physics regression passed with both colliders temporarily enabled.
