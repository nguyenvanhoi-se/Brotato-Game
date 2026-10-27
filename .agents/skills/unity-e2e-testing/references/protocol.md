# Build and driver contract

E2E/run.py copies Assets, Packages and ProjectSettings into a generated clone
under .e2e/projects, validates source hashes, then copies E2E/Unity into the clone.
Two Editor invocations persist BROTATO_E2E before compiling the guarded builder,
then build Windows Development. Only the clone changes runInBackground. Runtime
bridge code cannot enter a normal production build without the test define.

The successful build manifest `<exe>.e2e.json` records driver `unity-e2e-tcp`,
protocolVersion `1`, sourceFingerprint, testBuildFingerprint, scenePaths and
harness=true. Rebuild if original source/assets or Unity hooks/build recipe change.

Each player receives a random `UNITY_E2E_TOKEN`, an ephemeral `UNITY_E2E_PORT`,
`UNITY_E2E_ARTIFACTS_ROOT`, and `BROTATO_E2E_SESSION_NONCE` through its environment.
No token/license service or network account exists. Do not print the token.

Transport: UTF-8 newline-delimited JSON over TCP on IPv4 loopback. Requests have
integer id, token, command and command-specific fields. Replies have the same id,
ok, result (JSON encoded as a string), and error. Python decodes result once.
Limits: 1MiB frame, 64 queued requests, 30-second server execution timeout. Socket
workers enqueue requests; Unity object access, physics/input/UI and screenshot
operations execute on the main thread. Timeout/disconnect must never count as PASS.

Commands:

| Command | Parameters and observable result |
|---|---|
| hello | PID, nonce, protocolVersion and source/build fingerprints |
| scene / load_scene | Active scene; async load only scenes included in build |
| find | Exact name or absolute hierarchy path; objects with id/name/path/active |
| property | objectId or unique selector, component and public property/field |
| snapshot | Actual project adapter state/errors from E2EHarness |
| call | Allowlisted project hook method, invariant-culture string args |
| screenshot | Absolute path confined to the case artifact root; fresh complete PNG |
| input | Input System key enum name, duration0–10s; virtual keyboard release/cleanup |
| click | Unique UI Button, EventSystem/raycast/interactable validation |

Authentication and PID/session/fingerprint checks prevent a stale player from
being mistaken for the launched executable. They are not a reason to expose the
bridge publicly: it is a local development-only control surface. No unrestricted
reflection execution or shell command is exposed.

E2EHarness is the Brotato adapter. Its serialized Enemy prefab reference and
project-specific hook APIs must be re-inspected before porting the generic bridge
to another Unity project. Keep Unity version/packages and normal game architecture
unchanged. Source build failures, unavailable Editor/graphics and process crashes
must be distinguished from gameplay assertion failures in reports.
