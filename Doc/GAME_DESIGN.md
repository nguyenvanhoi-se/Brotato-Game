# Game design: current and planned

Sources: [original plan](../ke_hoach_do_an_game_2d_brotato_like.md), scripts/prefabs/GameScene and explicit conversation decisions. Distinguish current behavior, defective intended behavior and planned scope.

## Concept and controls

Single-player 2D top-down survival shooter inspired by Brotato, for three student developers. Prioritize a complete explainable loop over content volume.

Current: WASD movement, mouse-directed aim, continuous automatic fire; no click-to-fire/reload/weapon switching. ESC Pause is planned, not implemented. Detailed mouse-aim design and explicit user work override inconsistent introductory automatic-target wording.

## Current loop/outcomes

Open GameScene -> move/dodge/shoot -> enemies spawn/pursue -> bullets damage/kill -> survive each timer -> 2-second Wave Complete -> next wave -> finish wave 3 -> ALL WAVES COMPLETE.

Player HP zero -> GAME OVER. Both terminal outcomes freeze combat and offer PLAY AGAIN (fresh scene reload). One life; temporary test respawn removed. Final completion now has a distinct Victory state; Main Menu is not implemented.

Three waves share one scene. Intermediate completion freezes combat for 2 real seconds, clears remaining enemies/bullets and protects Player from damage. Player position/HP persist without healing. Resume movement, shooting and spawning after the announcement.

## Current values

- Player max HP 100, movement speed 5 units/s; normalized WASD prevents faster diagonals.
- Contact damage 10; global Player immunity 0.75s after accepted hit, including multiple attackers.
- PlayerVisual faces movement; gun independently aims at mouse and flips left-facing sprite.
- One gun, interval 0.2s/shot (5 shots/s); bullet damage 10, speed 10 units/s, lifetime 3s.
- Bullet disappears after enemy hit/lifetime. Consumed-hit guard prevents repeated damage; redundant Box trigger disabled in prefab. Real Play Mode test with both triggers enabled confirmed one 10-damage hit.
- HP is nonnegative; HP zero is terminal for the current round.

## Enemies/difficulty

Only one Enemy prefab, spider visual; base HP 30, speed 2 units/s, contact damage 10. No enemy shooting/kill reward/drop.

Spawn occurs on four sides outside orthographic camera, margin 1, minimum distance 3 from Player. Cap counts active living EnemyHealth under Enemies, not total spawns. Kills free room for replacements until timer expires.

Current explicit user-approved tuning, saved on WaveManager:

- Wave 1: duration 30s, cap 10, enemy HP 10, spawn interval 1.5s, speed 2 units/s.
- Wave 2: duration 30s, cap 15, enemy HP 20, spawn interval 1s, speed 2.5 units/s.
- Wave 3: duration 45s, cap 30, enemy HP 30, spawn interval 0.6s, speed 3 units/s.

Absolute WaveSettings replace the defective multiplier switch. New spawns and the initially placed enemy receive current wave HP/speed; prefab base values are not multiplied. Settings are inline serialized data, not WaveData assets yet.

Latest user request changes only Wave 3 from 30 to 45 seconds. Balance numbers require a full-duration human playtest; the accelerated Unity regression confirms functionality, not difficulty/fun.

## Planned final scope (not implemented)

- 9 waves in one gameplay scene: Forest 1-3, Desert 4-6, Dungeon 7-9. Themes change visuals, not mechanics.
- Normal/Fast/Tank enemies; no ranged enemies in main version.
- Four gun levels tied to kills, Kill Count/level HUD. Threshold/damage/rate tables in plan are examples, not active configs.
- Health, temporary Speed and temporary Damage pickups; chance-based drops. Example 20% chance/weights/effects are proposed, not present.
- WaveData and selected ScriptableObject tunables; avoid elaborate data frameworks for every value.
- Main Menu/Pause/Wave Complete/Game Over/Retry/Menu/final Victory, finished HUD, audio/effects/basic animation and Windows executable/demo/report.
- No inventory/shop/economy/save-load in first scope. Boss/multiple guns/multiplayer/quests/leaderboards/advanced AI/pooling only optional after core completion.

## Milestones and open rules

Week 1-2 foundations/combat and Week 3 survival loop exist. Requested MVP fixes passed actual Play Mode regression, including transitions and win/loss retries. Week 4 transition is implemented; inline Inspector wave data exists but reusable ScriptableObject WaveData remains a future task.

Current camera policy: track Player XY, preserve Z, free movement without arbitrary arena walls. A future authored map may define boundaries/clamping. Leftovers are cleared at intermissions; Player HP persists. Windows build and full-duration balance remain unverified. Add only the requested feature; stabilize core before extra polish/content.
