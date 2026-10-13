# Unity project setup snapshot

Values were read from this checkout on 2026-09-29. Re-check files/Inspector for each setup task. Static YAML does not prove what a live Editor session currently loads.

## Unity and packages

- `ProjectSettings/ProjectVersion.txt`: Unity Editor `6000.3.22f1` (revision `1c726e1fb402`).
- `Packages/manifest.json` declares Input System `1.20.0`, URP `17.3.0`, UGUI `2.0.0`, Unity Test Framework `1.6.0`, Visual Scripting `1.9.12`, Timeline `1.8.12`, and the listed 2D animation/PSD/Aseprite/sprite/shape/tilemap/tooling packages. Unity modules and Editor integrations are also declared. These are package dependencies, not proof each is used by gameplay.
- `Packages/packages-lock.json` exists. Do not update package files as routine work.
- `Assets/Settings/InputSystem_Actions.inputactions` has `Player` and `UI` maps. Player actions include Move, Look, Attack, Interact, Crouch, Jump, Previous, Next, Sprint; UI includes Navigate, Submit, Cancel, Point and pointer/scroll/tracked-device actions. EditorBuildSettings references it as the Input System actions config object. `ProjectSettings/ProjectSettings.asset` stores `activeInputHandler: 1`. No Player/Input script source was found, so runtime consumption is **Unknown / Not verified**.

## Render pipeline and camera

- URP package is declared. `Assets/Settings/UniversalRP.asset` references `Assets/Settings/Renderer2D.asset` by GUID.
- `ProjectSettings/GraphicsSettings.asset` has `m_CustomRenderPipeline: {fileID: 0}`. `ProjectSettings/QualitySettings.asset` assigns the UniversalRP GUID to all six quality tiers; `m_CurrentQuality: 5` selects the sixth tier (`Ultra`). The tier override references URP although the global GraphicsSettings field is empty.
- Enabled build scene is `Assets/Scenes/SampleScene.unity`: Main Camera tagged MainCamera, orthographic size 5 at (0,0,-10), AudioListener and URP camera data; root `Global Light 2D` has Global-type URP 2D Light. No camera-follow script exists.
- `Assets/Settings/Scenes/URP2DSceneTemplate.unity` is a template and not an enabled build scene.

## Physics, tags, layers, sorting

- `ProjectSettings/Physics2DSettings.asset`: gravity `{x: 0, y: -9.81}`, velocity iterations 8, position iterations 3; serialized layer collision matrix is all `f` bits. Player and Enemy prefab Rigidbody2D components each have gravity scale 0.
- `ProjectSettings/TagManager.asset`: custom tags array empty. Built-in layers include Default, TransparentFX, Ignore Raycast, Water, UI; remaining user slots blank. Only Default sorting layer exists.
- Both prefab roots are Untagged. `EnemyController` fallback searches tag `Player`, not declared in current settings. No runtime error behavior was tested.

## Scenes, prefabs, and other assets

- Build Settings enables only SampleScene. Exact hierarchy/component inventory is in [architecture.md](architecture.md).
- Player and Enemy prefabs are standalone and absent from SampleScene. Player prefab serializes PlayerController with no matching source/meta in repository. Enemy prefab has EnemyController but no EnemyHealth or EnemyContactDamage.
- No Weapon, Bullet, Pickup or UI prefab; no gameplay ScriptableObject type/asset, Animator Controller/clip, AudioClip, Canvas, or EventSystem was found in scanned Assets.
- `Assets/Settings/DefaultVolumeProfile.asset` is a render/post-processing volume profile, not an audio asset or gameplay data system.

## UI, audio, and limits

No Canvas, EventSystem, UI manager, menu/HUD prefab, AudioSource, AudioClip, or audio manager was found in scanned Assets. Camera's AudioListener alone does not form an audio system; Audio folder has no non-meta file.

No Unity Editor session was opened. Active platform, live Inspector overrides, import/compile state, runtime input behavior, and rendered output are **Unknown / Not verified**. Check current settings/assets and Unity Console before asserting or changing them.
