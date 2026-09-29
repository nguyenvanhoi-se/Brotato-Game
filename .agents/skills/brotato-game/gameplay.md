# Gameplay systems: implementation status

This describes source and serialized assets in this repository as inspected 2026-09-29. The root Product Requirements Document is a product plan and `Doc/` has claims from a different/larger snapshot; neither upgrades an absent system to implemented status.

## Current scene flow

```text
Launch enabled build scene: SampleScene
    ↓
Main Camera + Global Light 2D only
    ↓
No Player instance, gameplay manager, spawner, UI, or combat object in scene
    ↓
No complete playable gameplay flow is wired in this repository snapshot
```

Player and Enemy prefabs exist as separate assets, but SampleScene contains no instances. Enemy scripts contain partial behavior in isolation; source presence does not establish runtime wiring.

## Systems

| System | Status and evidence |
|---|---|
| Player movement | **Not implemented / not found in source.** Player prefab serializes a component labelled `PlayerController`, but its script GUID has no matching source/meta in this repository. Implementation/runtime behavior unknown. |
| Player health | **Not implemented / not found.** `EnemyContactDamage` refers to `PlayerHealth.TakeDamage(int)`, but no declaration/source was found. |
| Weapon / attack | **Not implemented / not found.** No Weapon script or prefab; Player prefab child is only named `PlayerVisual`. |
| Projectile / bullet | **Not implemented / not found.** No projectile script or prefab. |
| Enemy movement | **Source implemented, not scene-wired.** `EnemyController` pursues a Transform target in `FixedUpdate` using Rigidbody2D. Null target returns. `Start` attempts Player-tag fallback; current settings have no custom Player tag. |
| Enemy health / damage | **Partial source.** `EnemyHealth` has integer HP, ignores nonpositive damage or damage after death, clamps at zero, destroys its GameObject at zero. No reward/event. It is absent from saved Enemy prefab. |
| Enemy contact attack | **Partial source, not wired.** `EnemyContactDamage` calls PlayerHealth on trigger enter/stay, but that class is absent and this component is absent from Enemy prefab. |
| Enemy spawn | **Partial source, not scene-wired.** `EnemySpawner` has prefab/player/parent/camera fields, scheduling, cap scan, four camera-edge positions and cleanup. It requires missing `WaveSettings`; no component exists in build scene. The saved Enemy prefab has no `EnemyHealth`, which is the component its alive-cap scan counts. Runtime unverified. |
| Wave progression | **Not implemented / not found.** No `WaveManager` or `WaveSettings` declaration. `ConfigureForWave` alone is not a wave system. |
| XP / level | **Not implemented / not found.** |
| Item / pickup / drop | **Not implemented / not found.** |
| Shop / currency / economy | **Not implemented / not found.** |
| Game state / manager | **Not implemented / not found** in source or scene. |
| UI / HUD / menus | **Not implemented / not found** in scene/assets. No Canvas, EventSystem, UI prefab or UI manager found in scanned Assets. |
| Audio | **No gameplay audio system found.** Audio folder has no non-meta files. Scene camera has an AudioListener, which alone is not an audio system. |
| Camera follow | **Not implemented / not found.** Camera exists; no follow script found. |
| Data/configuration | **Partial.** Enemy defaults are serialized C# fields. InputSystem actions asset exists. No gameplay ScriptableObject class/asset or verified wave data source. Spawner references missing `WaveSettings`. |

## Source-level combat path and breaks

```text
EnemySpawner.ConfigureForWave(WaveSettings)   [declaration not found]
  → ConfigureEnemy(instantiated Enemy)
      → EnemyController.SetTarget / SetMoveSpeed (on Enemy prefab)
      → EnemyHealth.InitializeHealth (not on saved Enemy prefab)

EnemyController.FixedUpdate
  → move Rigidbody2D toward target Transform
  → null target means return

EnemyContactDamage trigger callbacks
  → GetComponentInParent<PlayerHealth>()       [declaration not found]
  → TakeDamage(contactDamage)

EnemyHealth.TakeDamage
  → clamp HP to zero
  → Destroy(gameObject) on death
  → no kill count, XP, item, currency, or wave notification
```

This is a static source trace. Missing declarations and absent component/scene wiring mean a complete Player → Weapon → Projectile → Enemy → reward → progression loop is **not implemented / not verified** in this checkout.

## Source defaults (not scene tuning)

- `EnemyController.moveSpeed` defaults to 2; `SetMoveSpeed` clamps to at least 0.1.
- `EnemyHealth.maxHealth` defaults to 30; `InitializeHealth` clamps input to at least 1.
- `EnemyContactDamage.contactDamage` defaults to 10.
- `EnemySpawner` spawn margin defaults to 1; minimum player distance 3; each scheduled spawn tries at most 16 positions. Interval, max living count, speed and health are read from the missing `WaveSettings` members.
- These are code defaults, not verified gameplay balance values.

## Product-plan distinction

The root PRD states goals including nine waves, weapon upgrades, kill count, items, and victory. They are **planned requirements**, not implementation evidence. The intended next milestone and current human playability are **Unknown / Not verified** from this snapshot.

