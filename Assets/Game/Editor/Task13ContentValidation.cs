using System;
using System.Collections.Generic;
using Praxen.Game.Content.Definitions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Praxen.Game.Editor
{
    public sealed class Task13ContentValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => ValidateAll();

        [MenuItem("Praxen/Content/Validate Authored Definitions")]
        public static void ValidateAll()
        {
            var ids = new Dictionary<string, ContentDefinition>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:ContentDefinition", new[] { "Assets/Game/Content" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                asset.Validate().ThrowIfInvalid();
                if (ids.TryGetValue(asset.Id, out var other) && other != asset)
                    throw new BuildFailedException("Duplicate authored content ID: " + asset.Id);
                ids[asset.Id] = asset;
            }
            UnityEngine.Debug.Log("Validated " + ids.Count + " authored definitions and stable IDs.");
        }
    }
}
