#if UNITY_EDITOR && BROTATO_E2E
using System;
using System.IO;
using System.Linq;
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
                throw new InvalidOperationException("Refusing to instrument the source project without clone marker.");
            string output = Path.GetFullPath(Required("BROTATO_E2E_OUTPUT"));
            string sourceFingerprint = Required("BROTATO_E2E_SOURCE_FINGERPRINT");
            string testFingerprint = Required("BROTATO_E2E_TEST_BUILD_FINGERPRINT");
            string[] scenes = EditorBuildSettings.scenes.Where(item => item.enabled).Select(item => item.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No enabled build scenes");
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Enemy.prefab");
            if (enemyPrefab == null) throw new InvalidOperationException("Expected Enemy prefab absent; update project adapter after inspection.");
            foreach (string path in scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var owner = new GameObject("__E2EHarness");
                E2EHarness harness = owner.AddComponent<E2EHarness>();
                var serialized = new SerializedObject(harness);
                serialized.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
                serialized.FindProperty("sourceFingerprint").stringValue = sourceFingerprint;
                serialized.FindProperty("testBuildFingerprint").stringValue = testFingerprint;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save clone scene " + path);
            }
            PlayerSettings.runInBackground = true;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string manifestPath = output + ".e2e.json";
            if (File.Exists(manifestPath)) File.Delete(manifestPath);
            BuildReport result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development, extraScriptingDefines = new[] { "BROTATO_E2E" }
            });
            if (result.summary.result != BuildResult.Succeeded || result.summary.totalErrors != 0 || !File.Exists(output))
                throw new InvalidOperationException("Independent E2E build failed: " + result.summary.result);
            File.WriteAllText(manifestPath, JsonUtility.ToJson(new Manifest
            {
                driver = "unity-e2e-tcp", protocolVersion = "1", sourceFingerprint = sourceFingerprint,
                testBuildFingerprint = testFingerprint, sourceRevision = Required("BROTATO_E2E_SOURCE_REVISION"),
                scenePaths = scenes, harness = true, builtUtc = DateTime.UtcNow.ToString("O")
            }, true));
            Debug.Log("Independent E2E build succeeded. Manifest: " + manifestPath);
        }
        private static string Required(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Missing " + name);
            return value;
        }
        [Serializable]
        private sealed class Manifest
        {
            public string driver, protocolVersion, sourceFingerprint, testBuildFingerprint, sourceRevision, builtUtc;
            public string[] scenePaths;
            public bool harness;
        }
    }
}
#endif
