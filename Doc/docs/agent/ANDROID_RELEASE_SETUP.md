# Android signed build setup

Status: **LOCAL TEST-KEY APK/AAB PASS, CI SIGNED BUILD NOT RUN**.

The repository-root workflows are `.github/workflows/ci.yml` and
`.github/workflows/release_android.yml`. The release workflow runs only when
manually dispatched on `main`; it builds an APK and AAB, then uploads those files
and versioned obfuscation symbols as private GitHub Actions artifacts. It does
not publish to Google Play.

Configure these GitHub Actions repository secrets before dispatching the release workflow:

- `ANDROID_KEYSTORE_BASE64`: base64 of the upload keystore file.
- `ANDROID_KEYSTORE_PASSWORD`: upload keystore password.
- `ANDROID_KEY_ALIAS`: upload key alias.
- `ANDROID_KEY_PASSWORD`: upload key password.

The workflow writes the decoded keystore under the runner's temporary directory.
Gradle reads the secret values from environment variables. Do not commit a
keystore or credentials. For a local signed build, create the ignored
`android/key.properties` with `storeFile`, `storePassword`, `keyAlias`, and
`keyPassword`, or set the same four environment variables. Release builds fail
when signing configuration is absent; debug builds need no signing setup.

The generated artifact still requires verification: inspect the certificate,
install the exact signed APK on a physical Android device, and repeat the camera,
SQLite restart, offline and AI checks before marking TASK-015 done. Do not claim
the release has passed based only on the workflow source.

Local release APK and AAB builds passed with a disposable test certificate.
Flutter 3.47.5 rejected an AAB built with `--obfuscate --split-debug-info`
because `libapp.so.sym/.dbg` was absent. Fastlane therefore builds an obfuscated
APK with Dart symbol files and a non-obfuscated AAB with native symbols. The
AAB passed Flutter's check; `apkanalyzer` found `libapp.so.sym` and
`libflutter.so.sym` for all three ABIs, and `jarsigner` verified its signature.
The disposable certificate is not the production upload key.
