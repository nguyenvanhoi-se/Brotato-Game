# Debugging workflow

Use evidence from the current branch. Do not diagnose from the PRD or older `Doc/` descriptions alone. Distinguish a static source finding from a runtime-confirmed cause.

## Investigation sequence

1. **Capture the symptom:** exact exception/compiler message, expected vs observed result, timing, affected scene/prefab, and recent relevant change. Check working-tree changes before editing.
2. **Find the code path:** search exception text, method, component, and callers. Read the full relevant script and inspect Awake/OnEnable/Start/Update/FixedUpdate/collision ordering.
3. **Trace data and ownership:** follow values from Inspector/assets/config into calls. Identify who creates, initializes, disables, destroys, or subscribes to the object.
4. **Check serialized wiring:** scene/prefab component lists, hierarchy, enabled states, object GUIDs, field names/values, tags/layers, and build scene. Confirm required components are attached, not merely declared.
5. **Check runtime evidence when available:** Unity Console/stack trace, Play Mode Inspector values, Rigidbody2D/collider settings, action-map state, and actual trigger/collision callbacks. Do not claim runtime verification without it.
6. **State a cause only when supported:** point to code/config evidence. If several causes remain, label them as hypotheses and say what would distinguish them.
7. **Make a narrow fix and verify the regression path** at the level requested. Report what ran and what remains unverified.

## Symptom-specific checks

- **NullReferenceException / MissingReferenceException:** locate dereference; determine if the reference is serialized, discovered, destroyed, or assigned only on one path. Check active scene instance and prefab overrides; do not blindly add a global Find fallback.
- **UnassignedReferenceException:** inspect the exact field on the active object and prefab override, its YAML key and asset GUID. An asset existing elsewhere does not prove the field points to it.
- **MissingComponentException:** inspect serialized component IDs/script GUID, `[RequireComponent]`, and prefab variants. A missing script reference may be the issue; do not add a duplicate component prematurely.
- **CS compiler errors:** inspect first diagnostic, declarations/namespaces, assembly definitions, package sources, and spelling. Snapshot source refers to `PlayerHealth` and `WaveSettings` with no declarations found; this is a static dependency gap, not a claim about compiler output on every branch.
- **Prefab/scene references:** compare `m_Script`/object GUIDs with `.meta` files; inspect prefab instances and overrides. Preserve GUIDs and user changes.
- **Input System:** inspect `activeInputHandler`, package manifest, action maps/bindings, and code that enables/reads actions. An action asset existing does not prove a component consumes it.
- **Collision/trigger/health:** inspect Rigidbody2D body type/simulation/constraints, collider enabled/`isTrigger`, layers/matrix, callback signature, and `GetComponent` vs `GetComponentInParent`. Check expected components on both active objects. PlayerHealth is absent in this snapshot; Enemy prefab has neither EnemyHealth nor EnemyContactDamage.
- **UI reference:** first confirm a Canvas/EventSystem or UI component exists. None appears in the snapshot SampleScene; do not debug an assumed HUD.
- **Time/update:** inspect scaled/unscaled clocks, timeScale writers and component enabled states. No game-state manager/timeScale owner was found in current source.

## Verification language

Use `static inspection found...`, `Unity Editor compilation confirmed...`, or `Play Mode reproduced/fixed...` only when each is true. If Editor verification is unavailable or not requested, label runtime behavior `Unknown / Not verified`. Do not run tests unless requested.
