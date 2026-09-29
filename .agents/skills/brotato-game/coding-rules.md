# C# coding and architecture rules

These rules preserve current Unity serialization and the small MonoBehaviour-based code style while avoiding assumptions about systems not found in source.

## Naming and fields

- Use PascalCase for classes, methods, properties and events; camelCase for locals/private fields. Use `const` only for compile-time constants.
- Keep Inspector tunables private with `[SerializeField]` when Unity needs to configure them. Prefer typed component/reference fields over unstructured lookups when a real owner can supply them.
- Do not expose or rename fields just for convenience. If a serialized field, class, or API must change, search C# callers and all scene/prefab YAML first, then preserve or deliberately migrate references.
- Add validation only for a real invariant. Avoid generic frameworks and duplicate managers/data layers.

## Components and dependencies

- Prefer explicit serialized references or a clear initialization call over global searches. `EnemyController` already has a legacy `FindGameObjectWithTag("Player")` fallback; preserve it carefully if compatibility needs it, but do not spread `Find`, `FindObjectOfType`, or `GameObject.Find` by default.
- Keep dependencies visible via `[RequireComponent]`, serialized fields, parameters, or documented lookups. Confirm the required component exists on the actual scene/prefab object, not only in source.
- Reuse existing implementations. Before creating a manager/system, search source declarations, prefab instances, scene components, asset GUIDs, and call sites.
- No project-wide singleton or event convention was found. Do not add one by assumption. When events are needed, define ownership and pair subscriptions with cleanup.
- No project-defined ScriptableObject data source was found. Do not describe `WaveSettings` as a ScriptableObject; its declaration is absent.

## Public and serialized API safety

- Preserve class names, public methods/properties, and serialized field names where possible.
- Before renaming/deleting, search Assets source and YAML for class/method names, `m_Script` GUIDs, prefab overrides, and serialized field keys. C# search alone does not catch Unity references.
- Do not delete a field/method/component merely because it appears unused in scripts; serialized references can be the only caller.

## Lifecycle responsibilities

Use callbacks for the timing they provide; avoid putting unrelated work in every frame.

- `Awake()`: cache same-object components and establish local invariants. `EnemyController` caches Rigidbody2D here.
- `OnEnable()`: subscribe to events when enabled if needed; pair subscriptions in `OnDisable()`.
- `Start()`: resolve dependencies requiring other scene objects' Awake only when explicit wiring is impractical. Current EnemyController performs its tag fallback here.
- `Update()`: frame input/lightweight non-physics timing. Avoid expensive hierarchy searches/repeated allocations.
- `FixedUpdate()`: Rigidbody/physics movement. Current enemy pursuit calls `MovePosition` here.
- `OnDisable()`: release subscriptions/temporary enabled state safely and idempotently.
- `OnDestroy()`: final resource/global-registration cleanup if required; do not rely on it as the only stop mechanism.

Do not add empty callbacks without a reason. Do not put physics motion in `Update()` merely because non-physics code nearby uses it.

## Unity asset and code safety

- Preserve `.meta` files and GUIDs. When moving/renaming Unity assets, check serialized GUID references and use an understood Unity import workflow.
- Avoid hand-editing large Unity YAML assets unless required and references can be validated. Never rewrite a scene/prefab to match stale prose.
- Use existing packages; do not upgrade/add packages unless requested and compatible with this Unity version.
- Keep gameplay changes scoped. Do not add a competing input/damage/data system or remove code with unchecked dependencies.

## Review checklist

- Does the change implement only the requested behavior?
- Are source types and actual serialized components present?
- Are public/serialized APIs preserved or deliberately migrated?
- Are null/missing references handled at a real boundary?
- Is Rigidbody2D work in the physics lifecycle?
- Are subscriptions paired and lifecycle-safe?
- Are relevant docs updated without promoting plans to facts?
