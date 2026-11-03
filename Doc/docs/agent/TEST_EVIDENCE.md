# Test evidence

## M3 / TASK-012 — 2026-11-03 (Asia/Bangkok)

- Scope: user-approved local SQLite demo sync server; keep existing AI, defer framework replacement. Existing recognition UX changes preserved.
- `dart run build_runner build`: PASS; schema v4 adds persistent next retry timestamp; `dart run drift_dev schema dump lib/data/local/app_database.dart drift_schemas/`: PASS, v4 snapshot added.
- `dart format lib test`: PASS. Final `flutter analyze --no-pub`: PASS, no issues (11.6s).
- Final `flutter test --no-pub --reporter compact`: PASS, **44 tests**. Includes durable queue/restart/backoff, lost ACK, serial sync, transaction rollback, tombstone, malformed-page rollback, v1/v2/v3 upgrades, settings UI and actual HTTP/Python SQLite two-client integration. Integration explicitly disables Flutter test's fake HTTP client only within that test, restores it afterward, uses temporary fixture data and stops server in finally.
- `python tool/sync_server/test_server.py`: PASS, **4 tests**. Pagination/restart, idempotency/delete-first, payload conflict, rejecting image fields and invalid timestamps. Fixed Python SQLite connection closure after initial ResourceWarning/locked temporary directory failure.
- First Flutter run found stale schema-version expectations and incorrect HTTP Content-Length. Both fixed; real HTTP integration and final full suite passed afterward. Drift's multiple-database warning is expected from isolated fixture databases; executors are distinct.
- `flutter build apk --debug --no-pub --target-platform android-x64 --dart-define=SYNC_URL=http://10.0.2.2:8765 --dart-define=SYNC_TOKEN=build-verification-token-only`: PASS (32.3s), build/app/outputs/flutter-apk/app-debug.apk. Token in this build is an explicit test fixture; run-app.ps1 rebuilds using the locally generated demo token.
- Both persistence skill check scripts PASS (confinement, no DB file copy/OFFSET, pragmas). Both PowerShell launcher files parse successfully. `git diff --check`: PASS (line-ending conversion notices only).
- `adb devices`: no connected devices. Android runtime UI/network/airplane-mode tests NOT_RUN. No new real-data AI accuracy/F1 measured; no production release, deployment or GitHub Actions run claimed. Commands/scripts and human demo steps: M3_DEMO.md.

## TASK-001 — 2026-10-17 (Asia/Bangkok)

- Environment: Windows; Flutter 3.47.5, Dart 3.13.4 installed under parent workspace `.tooling/flutter`; app package `lingosign_ai`.
- `flutter create --no-pub --platforms android --project-name lingosign_ai --org com.lingosign .`: PASS, generated Android scaffold.
- `flutter pub get`: PASS, resolved `flutter_riverpod 3.4.3` and `go_router 18.1.0`.
- `dart format lib test`: PASS, 12 files formatted, 4 changed.
- `flutter analyze`: PASS, `No issues found! (ran in 23.8s)`.
- `flutter test`: PASS, 2 widget tests (`navigation/status`, `dark mode`), `All tests passed!`.
- `flutter doctor -v`: Android SDK 35.0.1 found; `cmdline-tools` missing, Android licenses unknown; ADB daemon could not start in sandbox; no real-device validation.
- `flutter build apk --debug`: NOT_VERIFIED. The first build entered Gradle and started preparing Android NDK 28.2.13676358, but its download remained at 0 bytes; the command was stopped. No APK was produced. Android SDK setup/build still needs verification.

The first `flutter test` attempt failed because the sandbox blocked its temp file copy; moving `TEMP`/`TMP` into `.tooling/tmp` fixed that. The second attempt reached the runner but the sandbox blocked local loopback. The final pass ran with local test-runner network access. These environment failures were not app test failures.

## TASK-002 — 2026-10-17 (Asia/Bangkok)

- Focused `flutter test test/features` before implementation: RED, compilation failed because Domain types did not yet exist.
- Focused `flutter test test/features` after implementation: PASS, 6 unit tests.
- `dart format lib test`: PASS, 27 files processed, 3 changed.
- `flutter analyze`: PASS, `No issues found! (ran in 11.5s)`.
- Full `flutter test`: PASS, 8 tests total (6 Domain, 2 widget).
- `rg` scan for Flutter/Riverpod/Drift/Supabase/GoRouter/TFLite/MediaPipe package imports under `lib/**/domain/**`: no matches.
- `git diff --check`: PASS. Parent Git folder is still untracked; unrelated root `f.html` deletion is preserved.

## TASK-003 (partial) — 2026-10-17 (Asia/Bangkok)

- `dart run build_runner build --delete-conflicting-outputs`: PASS, 4 outputs. Current build_runner warned this flag is ignored by this version.
- `flutter analyze`: PASS after generation and local repository changes, `No issues found!`.
- `flutter test test/data/local/local_repositories_test.dart`: PASS, 4 tests covering schema v1/restart persistence, catalog provenance validation, foreign key rejection, and history tombstone.
- Schema v1 creation and reopen verified. No prior shipped schema exists to upgrade from, so a real upgrade migration test is pending a future schema version.
- Catalog seed: BLOCKED pending user-supplied verified catalog with source and license. No test fixture is bundled as production content.
- `dart run drift_dev schema dump lib/data/local/app_database.dart drift_schemas/`: PASS, exported baseline v1 JSON.
- Full `flutter test`: PASS, 12 tests total (6 Domain, 4 SQLite, 2 widget).

## TASK-006 (partial) — 2026-10-17 (Asia/Bangkok)

- `flutter pub get`: PASS, resolved official `camera 0.12.1` and endorsed CameraX implementation.
- `dart format lib test`: PASS, no changed files on final format.
- `flutter analyze`: PASS, `No issues found!`.
- `flutter test test/widget_test.dart`: PASS, 6 widget tests including camera start/stop, permission denial, background/resume and interrupted initialization with an injected fake gateway. This does not validate the native plugin.
- Android manifest declares camera permission; camera opens only on explicit tap and disables audio. No camera frames are persisted or uploaded.
- Real Android device, APK build, native permission prompt, preview orientation and repeated lifecycle transitions: NOT_VERIFIED. Existing Android NDK download issue remains.
- Final `flutter analyze`: PASS, `No issues found!`; final full `flutter test`: PASS, 16 tests total.
- Manual device checklist saved at `docs/agent/ANDROID_CAMERA_CHECKS.md`.

## TASK-015 (partial) — 2026-10-17 (Asia/Bangkok)

- Repository root is `D:\da_nen_tang\cuoi_ky`; the Flutter project is nested in `LingoSign_Agent_Skills_Bundle_Resume_Ready`. CI workflows were placed at the Git root.
- Python PyYAML `BaseLoader` parsed both workflows and found expected jobs/triggers: PASS. No GitHub Actions run has occurred.
- `dart format --output=none --set-exit-if-changed lib test`: PASS, 34 files, 0 changed.
- `flutter analyze --fatal-infos`: PASS, `No issues found!`.
- `dart run build_runner build --delete-conflicting-outputs`: PASS; SHA-256 of `app_database.g.dart` unchanged. Current build_runner warns that the flag is ignored.
- `dart run drift_dev schema dump lib/data/local/app_database.dart drift_schemas/`: PASS; SHA-256 of v1 snapshot unchanged.
- `flutter test --test-randomize-ordering-seed random --reporter expanded`: PASS, seed `2429203045`, 16 tests.
- `gradlew.bat :app:bundleRelease --offline --console=plain` with no secrets: EXPECTED FAILURE before build, stating four missing signing values. Confirms debug signing is no longer used for release.
- `gradlew.bat :app:tasks --offline --console=plain`: BLOCKED during Android project configuration because NDK 28.2.13676358 has no `source.properties` (incomplete download); it did not validate the full Android build.
- ADB run with elevated local-daemon access: PASS daemon startup, zero attached devices.
- Fastlane lane, CI execution, signed APK/AAB and real-device validation: NOT_RUN/NOT_VERIFIED. Ruby/Fastlane are not installed locally; signing secrets are not available.
- Android SDK repair: official NDK r28c ZIP reached 748,118,221 bytes; Python ZIP CRC check passed; installed `source.properties` reports `Pkg.Revision=28.2.13676358`. Gradle also installed Android Platform 36 and CMake 3.22.1.
- First post-NDK debug build failed in CameraX Kotlin compilation because the Pub cache is on C: while this project is on D:. Added `kotlin.incremental=false` to `android/gradle.properties`; subsequent `flutter build apk --debug` PASS.
- Final `flutter build apk --debug` after manifest permission cleanup: PASS (`assembleDebug` 4.6s); output `build/app/outputs/flutter-apk/app-debug.apk`, 173,935,344 bytes. `aapt dump badging` reports `com.lingosign.lingosign_ai`, version `1.0.0`/code `1`, minSdk 24, targetSdk 36.
- The first merged APK declared unused `RECORD_AUDIO`, `READ_EXTERNAL_STORAGE`, and `WRITE_EXTERNAL_STORAGE` via plugins. Added main manifest merger removal rules, rebuilt, and `aapt dump badging` now shows only CAMERA, INTERNET, ACCESS_NETWORK_STATE, and AndroidX dynamic receiver permission. This verifies removal in the packaged APK. No physical device was attached, so camera native behavior remains NOT_VERIFIED.
- `gradlew.bat :app:tasks --offline --console=plain`: PASS after NDK repair (`BUILD SUCCESSFUL in 36s`); Android tasks resolved. Gradle emitted template/plugin deprecation warnings, with no build failure.
- Local test signing (not a production upload key): generated a two-day RSA keystore under ignored `.tooling/tmp`. `flutter build apk --release --obfuscate --split-debug-info=build/symbols/1.0.0+1-test`: PASS, 53,620,072-byte APK. `apksigner verify --verbose --print-certs` confirms signer `CN=LingoSign CI Test` and certificate SHA-256 `26d80778909aa6f3d21c54432647e6910e498e02786047f02e1ba19aa572cf05`. Three Dart obfuscation symbol files were produced. The test keystore is not suitable for distribution.
- `flutter build appbundle --release --obfuscate --split-debug-info=...`: FAIL at Flutter post-build native-symbol check, although direct `gradlew.bat :app:bundleRelease --offline` returned `BUILD SUCCESSFUL`. `apkanalyzer files list` shows `libapp.so` but no `libapp.so.sym/.dbg` in the AAB. Flutter 3.47.5's `gradle.dart` requires that symbol entry; this is an independently observed packaging/check failure, not a completed release build. Installed official Android command-line tools 22.0 after checking ZIP SHA-256 `90ae805d20434428bffcb699c290860f19bb5f66a67e6b330067e3de801fb04a`; the error persisted. A trial `ndk.debugSymbolLevel = "FULL"` did not fix it and was reverted. AAB file produced by Gradle remains a rejected test artifact; do not distribute it.
- Isolated the AAB failure to obfuscation flags: `flutter build appbundle --release` with the same disposable test key PASS (`Built ...app-release.aab (53.6MB)`). `apkanalyzer files list` confirms `libapp.so.sym` and `libflutter.so.sym` for x86_64, armeabi-v7a and arm64-v8a. `jarsigner -verify` reports `jar verified`. Fastlane's AAB step was updated to use this validated command; the APK step retains obfuscation and Dart symbol output. Fastlane itself remains NOT_RUN because Ruby/Fastlane are unavailable locally; GitHub CI remains NOT_RUN.

## TASK-007 (partial) — 2026-10-17 (Asia/Bangkok)

- Inspected `model/DACN1.ipynb`; RF input is 42 interleaved x/y features, then Z-score, 16-component PCA and 100-tree RF. Notebook does not include camera feature extraction. Loaded the user-supplied pickles locally only for portable export; no training was performed.
- `python -W ignore tool/export_sklearn_model.py`: PASS; exports 29 labels and `forest.bin` of 25,777,664 bytes. SHA-256 of exported forest: `A41E3C9B9705E1C32CE546AA8C30819F5CDF08C093123A1B9C70769A76AC53A0`.
- `flutter analyze --no-pub --fatal-infos`: PASS, no issues.
- Full `flutter test --no-pub --reporter expanded`: PASS, 19 tests before the final binary threshold-precision change. Model-focused `flutter test --no-pub test/features/recognition/data/model`: PASS after final change, 3 tests including 35 synthetic parity vectors against Python probabilities (per-class tolerance 1e-5).
- `flutter build apk --debug --no-pub`: PASS after final change. APK ZIP audit confirms bundled `assets/flutter_assets/assets/model/forest.bin` (25,777,664 bytes) and `model.json` (16,440 bytes). `git diff --check`: PASS.
- Native camera→landmarks→42 features, on-device latency and real-device prediction: NOT_RUN/NOT_VERIFIED. No accuracy claim follows from the synthetic parity vectors.
- Android Studio emulator `Medium_Phone_API_36` (API 36): `adb install -r` PASS; MainActivity launched and rendered; camera permission dialog appeared only after Start Camera; allowing it displayed the emulator's virtual camera image; Stop Camera returned to the inactive state. App process remained alive. This is an emulator UI/native-camera smoke test, not model recognition or physical-device validation.
- Restarted the AVD with `-camera-front webcam0` after `emulator -webcam-list` found the laptop webcam. The app preview showed a live webcam scene with a person partly in frame. Camera stopped and temporary webcam screenshots deleted. This confirms webcam passthrough, not the person's identity, full-face visibility or model inference.

## TASK-007 live landmark pipeline (partial) — 2026-10-17 23:55 Asia/Bangkok

- Added `hand_landmarker 3.1.0` and `hand_feature_extractor.dart`; camera frames now feed on-device MediaPipe and its 21 landmarks are converted to 42 x/y values before the portable RF prediction. Original CSV extraction rules are unavailable; wrist/rotation/scale/handedness mapping is inferred and unvalidated.
- `flutter test --no-pub --reporter compact`: PASS, 23 tests, including extractor invariants and experimental prediction UI. `flutter analyze --no-pub --fatal-infos`: PASS, no issues. `flutter build apk --debug --no-pub --split-per-abi`: PASS; `app-x86_64-debug.apk` 108,083,669 bytes.
- On `Medium_Phone_API_36` with `-camera-front webcam0`, x86_64 APK installed and opened. Camera permission was granted; Android log shows `libmediapipe_tasks_jni.so` loaded; UI displayed active camera and `Chưa thấy bàn tay` from the no-hand stream. A temporary screenshot showed live webcam preview but the hand was partly outside the frame and obstructed; screenshot deleted. Hand-present prediction and accuracy still NOT_VERIFIED.
- The all-ABI debug APK (257,805,278 bytes) could not install because emulator storage was low. An old app install containing only Flutter cache was removed, then the smaller x86_64 APK installed successfully. No app database file was present in the old install.
- 2026-10-18 00:01 Asia/Bangkok: Emulator screen had slept during the first user-assisted hand test, closing the camera. Woke it, set screen timeout to 30 minutes for testing, and restarted camera. With a visible hand in the webcam preview, the live UI showed `Dự đoán thử nghiệm: C (66%)`. The user confirmed the intended sign was C. Temporary verification screenshot was deleted. This is one agreeing live example, not a measured accuracy rate.
