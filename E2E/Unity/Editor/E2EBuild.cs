#if UNITY_EDITOR && BROTATO_E2E
using System;
using System.IO;
using System.Linq;
using AltTester.AltTesterSDK.Driver;
using AltTester.AltTesterUnitySDK.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Brotato.E2E.Editor
{
    public static class E2EBuild
    {
        public static void Build()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!File.Exists(Path.Combine(root, ".brotato-e2e-clone")))
                throw new InvalidOperationException("Refusing to instrument a project without .brotato-e2e-clone marker.");
#if !ALTTESTER
            throw new InvalidOperationException("Run E2EConfigure.Configure in a separate Editor invocation before building.");
#else
            string output = Path.GetFullPath(Required("BROTATO_E2E_OUTPUT"));
            if (!string.Equals(Path.GetExtension(output), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("BROTATO_E2E_OUTPUT must be a Windows .exe path.");
            string host = Required("BROTATO_E2E_HOST");
            string appName = Required("BROTATO_E2E_APP_NAME");
            int port = int.Parse(Required("BROTATO_E2E_PORT"));
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException("BROTATO_E2E_PORT");
            string revision = Required("BROTATO_E2E_SOURCE_REVISION");
            string fingerprint = Required("BROTATO_E2E_SOURCE_FINGERPRINT");
            string testFingerprint = Required("BROTATO_E2E_TEST_BUILD_FINGERPRINT");
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No enabled build scenes.");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Enemy.prefab");
            if (prefab == null || prefab.GetComponent<EnemyController>() == null)
                throw new InvalidOperationException("Verified Enemy prefab/EnemyController missing; inspect project inventory before updating the harness.");
            var instrumentation = new AltInstrumentationSettings
            {
                AltServerHost = host,
                AltServerPort = port,
                AppName = appName,
                ResetConnectionData = true,
                UID = Guid.NewGuid().ToString("N"),
                hideGreenPopup = false
            };
            AltBuilder.CreateJsonFileForInputMappingOfAxis();
            foreach (string scenePath in scenes)
            {
                // Each saved scene contains instrumentation and a fresh local
                // harness. AltRunner itself owns its SDK DontDestroyOnLoad policy.
                AltBuilder.InsertAltTesterInScene(scenePath, instrumentation);
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene.GetRootGameObjects().Any(item => item.name == "__E2EHarness"))
                    throw new InvalidOperationException("Clone was already instrumented: " + scenePath);
                var owner = new GameObject("__E2EHarness");
                E2EHarness harness = owner.AddComponent<E2EHarness>();
                var serialized = new SerializedObject(harness);
                serialized.FindProperty("enemyPrefab").objectReferenceValue = prefab;
                serialized.FindProperty("sourceFingerprint").stringValue = fingerprint;
                serialized.FindProperty("testBuildFingerprint").stringValue = testFingerprint;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Unable to save instrumented clone scene " + scenePath);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string manifestPath = output + ".e2e.json";
            // A failed rebuild must never leave an earlier success manifest.
            if (File.Exists(manifestPath)) File.Delete(manifestPath);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development | BuildOptions.IncludeTestAssemblies,
                extraScriptingDefines = new[] { "ALTTESTER", "BROTATO_E2E" }
            });
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0 || !File.Exists(output))
                throw new InvalidOperationException("E2E build failed: " + report.summary.result + "; errors=" + report.summary.totalErrors);
            var manifest = new Manifest
            {
                sourceRevision = revision,
                sourceFingerprint = fingerprint,
                testBuildFingerprint = testFingerprint,
                sdkVersion = "2.3.2",
                scenePaths = scenes,
                harness = true,
                appName = appName,
                host = host,
                port = port,
                builtUtc = DateTime.UtcNow.ToString("O"),
                executable = output
            };
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
            Debug.Log("E2E build succeeded. Manifest: " + manifestPath);
#endif
        }

        private static string Required(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Missing environment variable " + name);
            return value;
        }

        [Serializable]
        private sealed class Manifest
        {
            public string sourceRevision, sourceFingerprint, testBuildFingerprint, sdkVersion, appName, host, builtUtc, executable;
            public string[] scenePaths;
            public bool harness;
            public int port;
        }
    }
}
#endif
