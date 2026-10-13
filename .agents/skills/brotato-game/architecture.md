# Repository architecture snapshot

Inspected 2026-09-29 from the checked-out repository. This is static file inspection; no Unity Editor, Play Mode, or player build was run for this report. Re-inspect before relying on it later.

## Repository layout

Observed top-level folders: `Assets/`, `Doc/`, `Packages/`, `ProjectSettings/`, plus `Library/`, `Logs/`, `Temp/`, and `UserSettings/`. `.git/` is present. `.gitignore` excludes Library, Temp, Obj, Builds, Logs, and UserSettings. `Assembly-CSharp.csproj` and `Brotato_CD4.slnx` are present. The root also contains `README.md` and a file named `Product Requirements Document` (no extension).

```text
Assets/
├── Animations/                         (no non-meta asset found)
├── Audio/                              (no non-meta asset found)
├── Materials/                          (no non-meta asset found)
├── Prefabs/
│   ├── Enemy/Enemy.prefab, spider.png
│   ├── Player/Player.prefab, PlayerUnlit.mat, survivor1_stand.png
│   └── Weapon/                         (no non-meta asset found)
├── Scenes/SampleScene.unity
├── Scripts/
│   ├── Enemy/EnemyController.cs, EnemyHealth.cs,
│   │         EnemyContactDamage.cs, EnemySpawner.cs
│   ├── Player/                          (no C# source found)
│   └── Weapon/                          (no C# source found)
├── Settings/
│   ├── DefaultVolumeProfile.asset
│   ├── InputSystem_Actions.inputactions
│   ├── Lit2DSceneTemplate.scenetemplate
│   ├── Renderer2D.asset
│   ├── UniversalRP.asset
│   ├── UniversalRenderPipelineGlobalSettings.asset
│   └── Scenes/URP2DSceneTemplate.unity
└── Sprites/                            (no non-meta asset found)
```

`Assets/Code/` is not present. Searches found no C# source outside the four Enemy scripts under `Assets/Scripts/`.

## Scenes and hierarchy

`ProjectSettings/EditorBuildSettings.asset` enables exactly `Assets/Scenes/SampleScene.unity`. `Assets/Settings/Scenes/URP2DSceneTemplate.unity` is a template asset and is not listed as an enabled build scene.

Serialized `SampleScene` hierarchy:

```text
SampleScene
├── Main Camera (tag MainCamera)
│   ├── Camera (orthographic, size 5; position (0, 0, -10))
│   ├── AudioListener
│   └── URP additional camera data
└── Global Light 2D (tag Untagged; URP 2D Light, Global type)
```

Both objects are scene roots. No Player, Enemy, spawner, gameplay manager, Canvas, EventSystem, or UI object appears in this scene YAML. The enabled scene does not serialize a gameplay hierarchy in this checkout.

## Prefabs

These are standalone assets; neither is instanced in `SampleScene`.

- `Assets/Prefabs/Player/Player.prefab`: root `Player` is Untagged; Transform, Rigidbody2D (gravity scale 0; rotation constrained), non-trigger CapsuleCollider2D, and a MonoBehaviour record labelled `Assembly-CSharp::PlayerController`. It references GUID `62d4b7f376264d24f9fe7489edfe02e0`; no matching script/meta was found in the repository. Child `PlayerVisual` has Transform and SpriteRenderer. Do not assume the missing PlayerController implementation or whether Unity resolves it outside this checkout.
- `Assets/Prefabs/Enemy/Enemy.prefab`: root `Enemy` is Untagged; Transform, SpriteRenderer, Rigidbody2D (gravity scale 0; rotation constrained), trigger CapsuleCollider2D, and `EnemyController`. It does not serialize `EnemyHealth` or `EnemyContactDamage`. Target is null in the prefab; speed is 2.
- No Weapon, Bullet/Projectile, Pickup, or UI prefab was found. `Assets/Prefabs/Weapon/` contains no non-meta asset.

## Script inventory and APIs

All four classes inherit `MonoBehaviour`; no project-defined base class or public fields were found. Their public surface is the properties/methods listed below. No C# singleton, event/delegate declaration, or custom `ScriptableObject` class was found in Assets source.

| Class | Fields / properties | Methods and lifecycle | Direct references / notes |
|---|---|---|---|
| `EnemyController` | Serialized private `Transform target`, `float moveSpeed = 2`; public read-only `MoveSpeed` | `Awake` caches Rigidbody2D; `Start` fallback via `GameObject.FindGameObjectWithTag("Player")`; public `SetTarget(Transform)`, `SetMoveSpeed(float)`; `FixedUpdate` pursues with `Rigidbody2D.MovePosition` | `[RequireComponent(typeof(Rigidbody2D))]`; target is serialized or looked up by tag. TagManager currently has no custom tags; Enemy prefab is Untagged. |
| `EnemyHealth` | Serialized private `int maxHealth = 30`; private current HP; public read-only `CurrentHealth`, `MaxHealth` | `Awake`; public `TakeDamage(int)`, `InitializeHealth(int)`; private `Die()` destroys GameObject | No events/reward callbacks. Not attached to Enemy prefab. |
| `EnemyContactDamage` | Serialized private `int contactDamage = 10` | `OnTriggerEnter2D`, `OnTriggerStay2D`; private `DamagePlayer(Collider2D)` | Calls `GetComponentInParent<PlayerHealth>()` then `TakeDamage(int)`. PlayerHealth declaration/source not found. Not on Enemy prefab. |
| `EnemySpawner` | Serialized enemy prefab, player Transform, enemies parent, Camera, spawn margin 1, minimum distance 3; private `WaveSettings settings`, `nextSpawnTime`; public `AliveCount` | Public `ConfigureForWave(WaveSettings)`, `ClearEnemies()`; private `Update`, `SpawnEnemy`, `ConfigureEnemy`, `GetSpawnPosition` | `WaveSettings` is not declared in repository source. Code reads `spawnInterval`, `maxEnemiesAlive`, `enemySpeed`, `enemyHealth`; observed accesses, not a verified type definition. Uses Instantiate/component searches. No scene has this component/reference. |

No `PlayerController`, `PlayerHealth`, `WeaponAim`, `Bullet`, `CameraFollow`, `WaveManager`, `WaveSettings`, `GameManager`, `UIManager`, or `AudioManager` source declaration was found in the repository. The Player prefab label does not provide implementation source.

## Observed dependency graph

```text
EnemySpawner
 ├── serialized Enemy prefab / Player / Enemies parent / Camera
 ├── WaveSettings (type not found; required member accesses listed above)
 └── Instantiate Enemy
      ├── EnemyController → Rigidbody2D; target Transform; optional Player-tag lookup
      └── EnemyHealth → expected by spawner initialization, absent on saved prefab

EnemyContactDamage → parent lookup for PlayerHealth (type not found) → TakeDamage(int)
EnemyHealth.TakeDamage(int) → Die() → Destroy(Enemy GameObject)
```

This describes source calls and saved prefab composition, not a functioning scene combat loop. No serialized scene links connect these scripts.

## Managers, system boundaries, and coupling

No manager class or manager GameObject is present in scanned Assets/source. `EnemySpawner` is the only orchestration-like gameplay component found; it is not a manager singleton and is not scene-wired. System statuses are in [gameplay.md](gameplay.md).

Concrete coupling/risk findings:

- Enemy target discovery falls back to hard-coded `Player` tag, but no custom tag is declared and both prefab roots are Untagged. A caller could avoid fallback by using `SetTarget`, but none is wired in the scene.
- `EnemySpawner` directly owns scheduling, cap counting, instantiation, initialization, and cleanup; it also references missing `WaveSettings`. Since its cap scan counts only `EnemyHealth` components and the saved Enemy prefab has none, the configured prefab would not be represented in that count if this path were wired as saved.
- `EnemyContactDamage` directly looks up missing `PlayerHealth`, and is not on the Enemy prefab.
- Player prefab points to a `PlayerController` script GUID with no matching script/meta in this repository.
- Project prose describes a larger architecture than current checked-in Assets, risking accidental integration with nonexistent managers/scenes.

## Documentation drift

`Doc/ARCHITECTURE.md`, `Doc/STATE.md`, and `Doc/GAME_DESIGN.md` describe `GameScene`, Player/Weapon/Core/Wave scripts, managers, and a regression utility not found in the current Assets scan; paths/values cannot be verified here. The root PRD is a plan, not implementation proof. Treat these as historical/planning material until reconciled against a later checkout. No assumption is made about why the drift exists.

