# Actual architecture

Updated 2026-09-18. Unity 6000.3.22f1, URP 2D, existing Input System. Main enabled build scene: Assets/Scenes/GameScene.unity.

## Scene and components

GameScene contains Main Camera + CameraFollow, Player + PlayerHealth, scene-added WeaponHolder/GunVisual/FirePoint, Enemies with one placed enemy, EnemySpawner, WaveManager and GameManager. Manager references and camera target are explicitly saved. Weapon setup is not fully part of the standalone Player prefab.

- PlayerController: normalized keyboard WASD in Update; Rigidbody2D movement in FixedUpdate; rotates visual.
- PlayerHealth: initializes HP; positive-damage/dead/cooldown checks; HP clamp; single Died event. SetDamageEnabled prevents hits during intermission/results.
- WeaponAim: mouse world-position aim and automatic shots on a Time.time interval. Requires Camera.main, Bullet and FirePoint.
- Bullet: Rigidbody2D movement/lifetime; GetComponentInParent<EnemyHealth> on trigger; consumed flag before damage; deactivate immediately and Destroy. Saved prefab only enables the Circle trigger.
- EnemyController: explicit Player target (tag fallback for standalone use), pursuit, absolute SetMoveSpeed.
- EnemyHealth: InitializeHealth sets absolute max/current HP; TakeDamage ignores nonpositive damage/dead enemies and clamps at zero; death destroys.
- EnemyContactDamage: enter/stay trigger calls PlayerHealth.TakeDamage(10); Player owns shared 0.75-second immunity.
- EnemySpawner: accepts WaveSettings, schedules spawn with Time.time, counts active living EnemyHealth under Enemies, respects cap, assigns target and absolute stats. Initializes placed enemies too. Four-side camera-exterior positions, margin and minimum Player distance. ClearEnemies deactivates before delayed destruction.
- WaveSettings: serializable inline data (duration, maxEnemiesAlive, enemyHealth, spawnInterval, enemySpeed), owned by WaveManager; no ScriptableObject asset.
- WaveManager: configures wave, decrements scaled timer while Playing, decrements unscaled 2-second intermission while WaveComplete, starts next wave once.
- GameManager: Playing/WaveComplete/GameOver/Victory, health death subscription, cleanup, combat enable/disable, prototype OnGUI and retry.
- CameraFollow: Start/LateUpdate copies Player XY and keeps original camera Z; no map clamp or smoothing.

## State transitions

Playing + timer zero:
- Wave 1/2 -> WaveComplete -> freeze/cleanup -> wait 2 unscaled seconds -> configure next wave -> Playing.
- Wave 3 -> Victory -> freeze/cleanup -> PLAY AGAIN reload.
Playing + HP zero -> GameOver -> freeze/cleanup -> PLAY AGAIN reload.

Freeze disables spawner, PlayerController and WeaponAim, blocks Player damage and sets timeScale=0. Resume restores previous movement/weapon enable states, enables spawner/damage and restores timeScale=1. OnDestroy and retry also restore timeScale. Player HP/position persist through intermediate waves. Old bullets/enemies do not persist.

## Current configuration

Wave 1: 30s / cap10 / HP10 / spawn1.5s / speed2.
Wave 2: 30s / cap15 / HP20 / spawn1s / speed2.5.
Wave 3: 45s / cap30 / HP30 / spawn0.6s / speed3.
Player HP100, speed5, contact10, cooldown0.75s. Bullet damage10, speed10, lifetime3s; gun interval0.2s.

## Editor utilities and tests

- CombatSetup: Tools > Brotato > Apply Combat Setup, only in edit-mode GameScene. Uses SerializedObject/PrefabUtility and saves scene/prefab. Resets wave settings to defaults; avoid invoking after later custom tuning unless intended.
- CombatRegression: Tools > Brotato > Run Combat Regression, edit mode. Enters real Play Mode, tests physics/camera/stats/caps/transitions/retries, writes Temp/CombatRegression.txt and exits Play Mode. Test changes never save to scene; shortened timers and forced spawn clock are deliberate test-only acceleration.
- Helpers are under Assets/Editor and excluded from player builds.

## Constraints and remaining coupling

GameManager and WaveManager require valid saved references and a nonempty settings array. No dependency-injection framework. Enemies should remain parented under Enemies; ClearEnemies owns that subtree. Camera must be orthographic; no authored arena bounds yet. Global timeScale is owned by GameManager; a future pause system must coordinate with it. MainCamera tag is required for aiming. Player root prefab is Untagged, scene overrides Player tag; spawner explicitly assigns target.

No save/load, inventory, pickups, kill count, gun upgrades, enemy variants, finished UI/menu/pause, or Windows-build verification yet. See STATE for actual tests and limits.
