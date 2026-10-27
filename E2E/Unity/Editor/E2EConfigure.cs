#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Brotato.E2E.Editor
{
    // First Editor invocation: persist symbols, then quit. The following invocation
    // compiles the guarded builder/runtime hooks with these symbols already active.
    public static class E2EConfigure
    {
        public static void Configure()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!File.Exists(Path.Combine(root, ".brotato-e2e-clone")))
                throw new InvalidOperationException("E2E configuration requires an isolated clone marker.");
            string[] symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(symbol => symbol != "ALTTESTER")
                .Concat(new[] { "BROTATO_E2E" }).Distinct().ToArray();
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbols);
            AssetDatabase.SaveAssets();
            Debug.Log("E2E scripting symbols persisted for next Editor invocation: " + string.Join(";", symbols));
        }
    }
}
#endif
