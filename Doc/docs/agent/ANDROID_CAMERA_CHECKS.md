# TASK-006 Android device checks

Status: **NOT_RUN**. Widget tests verify UI state and lifecycle orchestration with a fake gateway, not the native camera plugin.

Use a physical Android device with API 24+ and the debug app installed. Record device model, Android version, APK/build command, test date, and pass/fail evidence in `TEST_EVIDENCE.md`.

1. Launch the app as guest. Confirm the camera is off and no permission prompt appears before tapping **Bắt đầu camera**.
2. Tap **Bắt đầu camera** and deny the permission. Confirm a clear error and retry button; no crash or preview.
3. Grant camera permission in Android Settings, return, and retry. Confirm a live front camera preview (or fallback if no front camera), with expected rotation and mirroring.
4. Tap **Dừng camera**. Confirm preview disappears and Android's camera privacy indicator turns off.
5. Start again; switch apps, lock/unlock, and return. Confirm camera closes in background and preview reopens without a second concurrent session.
6. Repeat start/stop and background/resume several times. Check camera release, memory, and exceptions in `adb logcat`.
7. Confirm no microphone permission prompt appears and no camera or audio data is saved/uploaded.

Do not mark TASK-006 done until these checks pass on a real device. The project currently cannot produce an APK because Android NDK download did not complete in this environment.
