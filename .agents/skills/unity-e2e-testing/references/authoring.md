# Authoring journeys against verified game flows

The active implementation lives in E2E/; this skill routes to it without duplicating
runtime source. Start with enabled scenes and follow serialized references/callers
to discover what a player can actually do. Names in an InputAction asset or a PRD
are not evidence that a user flow exists.

Keep stable IDs/specifications in E2E/catalog.py: feature, goal, preconditions,
steps, expected result, and natural/simulated kind. Runtime actual/status are
produced by pytest hooks. Keep known unimplemented or untested branches explicit.

E2E/flows/startup.json is an executed lifecycle example: launch → verify scene and
camera/light → real screenshot → check errors → close player. Its ID and kind must
match the catalog. E2E/flows.py supports launch, wait_visible, assert_scene,
assert_property, click, press_key, load_scene, wait_state, screenshot,
expect_no_errors, hook and close. `hook` is rejected in natural flows. Screenshot
names are basenames inside that case's artifact directory.

Each pytest case starts a separate real process and validates PID/session and two
fingerprints. The fixture cleans up connection and owned process on setup, call,
or teardown failures. For save/load flows added later, isolate only the actual save
implementation's data; do not delete a user's real profile or all PlayerPrefs.

Use name/path selectors that are unique and backed by real hierarchy. Ambiguous,
missing, disabled or covered controls should fail visibly rather than selecting
an arbitrary object. Use bounded waits for observed state changes instead of
pretending a state change happened after a fixed delay. The UI/input primitives
can be extended when a game has those accessible flows; their presence in the
driver does not establish feature coverage.

Existing Enemy/Spawner/Contact fixtures run production components in the player
but are simulated setup, because the actual SampleScene has no gameplay objects.
Do not silently repair prefab composition or add gameplay systems to turn those
scenarios into natural journeys. An assertion failure can be a valid regression
finding and must remain in the final report.
