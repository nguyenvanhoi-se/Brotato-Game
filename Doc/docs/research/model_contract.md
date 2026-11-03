# Model contract — LingoSign AI

Status: **MODEL PORT TESTED; LIVE HAND PIPELINE EXPERIMENTAL** (2026-10-17, Asia/Bangkok).

The user supplied `D:\da_nen_tang\cuoi_ky\model\`. The `.pkl` files were first inspected statically, then loaded with local Python/joblib solely to export the fitted PCA and Random Forest at the user's request. Python pickle/joblib is not a mobile runtime format and loading an untrusted pickle can execute code; the Flutter app never loads pickle files.

## Observed artifacts

- `rf_model.pkl` (97,861,697 bytes, SHA-256 `E3DEC57B58CA1F54CA836ED5D22A3DF91C98B38CD781290C711FD59E323AA9A7`): static header identifies `sklearn.ensemble._forest.RandomForestClassifier`, `n_estimators=100`, `n_features_in_=16`, serialized sklearn version `1.6.1`.
- `pca_model.pkl` (7,047 bytes, SHA-256 `033FD0688592713D446C219EE2267058D84278F5C7ABA33B15B9F69200CCF174`): static header identifies `sklearn.decomposition._pca.PCA`, `n_features_in_=42`, `n_components=16`, `whiten=False`.
- `chuan_hoa_params.json` (SHA-256 `52195E0FDE051FE9CC2BE309559191A94A36F2C3716CD8AA20E01AA1FF8666E3`): arrays `mean` and `std`, both length 42. Exact normalization formula and any preceding landmark normalization must be confirmed from training code.
- `rf_predictions.json`: 990 `y_test` and 990 `y_pred` labels; 894 matches (reported `accuracy=0.9030303030303031`, `model="RF-100 (14)"`). This is supplied evaluation data; train/test split, leakage controls, provenance and device accuracy remain unverified.
- `knn_predictions.json` and `so_sanh_knn_rf.png`: comparison artifacts; no KNN model supplied or selected for integration.

Observed label union in RF predictions: `A, B, C, D, E, G, H, I, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, del` (24). Loading the user-supplied RF artifact for export confirms 29 model classes in this exact order: `A` through `Z`, `del`, `nothing`, `space`. The prediction JSON covers only 24 of these classes. The meaning and expected UI behavior of `del`, `nothing` and `space` still need product validation. These labels do not establish the sign-language variant or permission to publish a dictionary.

## Inferred pipeline to verify

The supplied `DACN1.ipynb` confirms that each CSV row is a label followed by 42 numeric values. Cell 19 reads the features in MediaPipe landmark index order as `[x0, y0, ..., x20, y20]`. Cells 25 and 31 apply `(x[j] - mean[j]) / std[j]`, then `pca.transform`, then KNN or Random Forest; the supplied RF uses 100 trees, maximum depth 14, minimum leaf size 3 and minimum split size 6. Notebook cell 21 substitutes `1.0` for a zero standard deviation when fitting the scaler. The supplied JSON has 42 positive std values, including one near zero; inference must use its exact values rather than introduce a new clamp.

The notebook **does not contain the camera-to-CSV extraction code**. The new `hand_feature_extractor.dart` translates the wrist to the origin, rotates landmark 9 to the vertical axis, divides by the maximum wrist-to-landmark distance and canonicalizes the thumb to positive x. This mapping is inferred from the fitted scaler and has not been checked against a training CSV row. It may differ from the training pipeline.

## Offline model runtime implemented

`tool/export_sklearn_model.py` loads the three user-supplied artifacts and exports `assets/model/model.json` (scaler, PCA and labels) plus `assets/model/forest.bin` (100 trees, 29-class leaf probabilities). Export uses the existing fitted models only; it does not train. The APK carries the portable assets, not Python pickle files. `OfflineForestModel.load()` reads them from the Flutter asset bundle; `predict(List<double>)` accepts an already extracted 42-value vector and returns a label, confidence and all class probabilities. The forest binary is 25,777,664 bytes. The test fixture contains 35 synthetic vectors and Python probabilities; Flutter parity tests pass with tolerance 1e-5. These vectors test the port, not recognition accuracy on real signs.

The Android camera now sends YUV frames to the on-device `hand_landmarker` plugin, which yields 21 landmarks in MediaPipe order. The extractor produces 42 values for the portable model, and the UI displays a labeled experimental prediction. On the Android Studio emulator with `webcam0`, the app opened camera, initialized MediaPipe JNI, and displayed `Chưa thấy bàn tay` when no hand was detected. A subsequent user-assisted test with a visible hand produced `Dự đoán thử nghiệm: C (66%)`; the user confirmed they intended C. General accuracy, latency and a physical Android device remain unverified.

Notebook cells 21 and 23 load `hand_data_test.csv` as both train and test. Their saved outputs report 1050 samples and a 12-component PCA. Cell 31 loads separate `hand_data.csv` and `hand_data_test.csv`, and its output reports 171444 train / 990 test samples and 16 PCA components. The supplied PCA artifact also reports 16 components. These observations imply the notebook cells were run with different data/artifact revisions; execution provenance is unresolved. The 90.3% RF score in `rf_predictions.json` cannot be accepted as an independent test result until the exact scaler/PCA provenance and person-disjoint split are established. The notebook's separate phrase LSTM experiment is outside the supplied RF artifact and has no deployable model files in `model/`.

## Required to implement TASK-007 safely

- Camera-to-CSV extraction script: x/y coordinate system, wrist translation/scale/rotation, mirrored camera handling, left/right hand policy, missing-hand handling, and any feature clipping. The notebook begins from CSV and cannot answer these points.
- Confirm normalization equation, zero/small-std handling and PCA transformation order.
- `del`/`nothing`/`space` semantics, unknown/background rule and confidence calibration/threshold. Class order is now exported from `rf_model.classes_`.
- At least one consent-safe 42-value test vector and expected class/probabilities from the exact supplied Python pipeline.
- Confirm which dataset revision produced the supplied scaler/PCA/RF, and supply the train/test CSVs or provenance logs needed to audit leakage without retraining.
- Dataset/sign-language source, ownership/license and validated catalog/media list for TASK-003 seed.
- Mobile conversion strategy (e.g. versioned portable tree/PCA data or ONNX) and exact comparison against Python predictions. The supplied `.pkl` files cannot be directly loaded by Flutter.
- Android device validation for latency, accuracy and camera lifecycle.
