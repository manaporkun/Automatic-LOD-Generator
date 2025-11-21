using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Plugins.AutoLODGenerator.Editor
{
    /// <summary>
    /// Menu entry points and context menu integration for Auto LOD Generator.
    /// </summary>
    public static class AutoLOD
    {
        private const string MenuRoot = "Tools/Auto LOD Generator";
        private const string ContextMenuRoot = "GameObject/Auto LOD";

        #region Main Menu Items

        /// <summary>
        /// Opens the main Auto LOD Generator window.
        /// </summary>
        [MenuItem(MenuRoot + "/Open Window", false, 0)]
        public static void OpenWindow()
        {
            AutoLODWindow.ShowWindow();
        }

        /// <summary>
        /// Quick generate LOD group for selected objects.
        /// </summary>
        [MenuItem(MenuRoot + "/Quick Generate LOD Group %&l", false, 20)]
        public static void QuickGenerateLODGroup()
        {
            var selectedObjects = GetValidSelectedObjects();

            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Auto LOD Generator",
                    "Please select one or more GameObjects with MeshFilter and MeshRenderer components.",
                    "OK");
                return;
            }

            var settings = new LODGeneratorSettings();
            settings.ApplyPreset(LODPreset.Balanced);

            int successCount = 0;
            int failCount = 0;

            foreach (var obj in selectedObjects)
            {
                var result = LODGeneratorCore.GenerateLODGroup(obj, settings);
                if (result.Success)
                {
                    successCount++;
                    Debug.Log($"[Auto LOD] Generated LOD group for '{obj.name}'");
                }
                else
                {
                    failCount++;
                    Debug.LogWarning($"[Auto LOD] Failed to generate LOD for '{obj.name}': {result.ErrorMessage}");
                }
            }

            if (failCount == 0)
            {
                Debug.Log($"[Auto LOD] Successfully generated {successCount} LOD group(s)");
            }
            else
            {
                Debug.LogWarning($"[Auto LOD] Generated {successCount} LOD group(s), {failCount} failed");
            }
        }

        [MenuItem(MenuRoot + "/Quick Generate LOD Group %&l", true)]
        public static bool ValidateQuickGenerateLODGroup()
        {
            return GetValidSelectedObjects().Length > 0;
        }

        /// <summary>
        /// Quick simplify mesh for selected objects.
        /// </summary>
        [MenuItem(MenuRoot + "/Quick Simplify (50%)", false, 21)]
        public static void QuickSimplify()
        {
            var selectedObjects = GetValidSelectedObjects();

            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Auto LOD Generator",
                    "Please select one or more GameObjects with MeshFilter and MeshRenderer components.",
                    "OK");
                return;
            }

            foreach (var obj in selectedObjects)
            {
                var result = LODGeneratorCore.GenerateSimplifiedMesh(obj, 0.5f);
                if (result.Success)
                {
                    Debug.Log($"[Auto LOD] Simplified '{obj.name}': {result.OriginalVertexCount} -> {result.LODVertexCounts[0]} vertices");
                }
                else
                {
                    Debug.LogWarning($"[Auto LOD] Failed to simplify '{obj.name}': {result.ErrorMessage}");
                }
            }
        }

        [MenuItem(MenuRoot + "/Quick Simplify (50%)", true)]
        public static bool ValidateQuickSimplify()
        {
            return GetValidSelectedObjects().Length > 0;
        }

        #endregion

        #region Context Menu Items (Right-click in Hierarchy)

        /// <summary>
        /// Context menu item to generate LOD group.
        /// </summary>
        [MenuItem(ContextMenuRoot + "/Generate LOD Group", false, 0)]
        public static void ContextGenerateLODGroup()
        {
            QuickGenerateLODGroup();
        }

        [MenuItem(ContextMenuRoot + "/Generate LOD Group", true)]
        public static bool ValidateContextGenerateLODGroup()
        {
            return GetValidSelectedObjects().Length > 0;
        }

        /// <summary>
        /// Context menu item to simplify mesh.
        /// </summary>
        [MenuItem(ContextMenuRoot + "/Simplify Mesh (50%)", false, 1)]
        public static void ContextSimplify50()
        {
            SimplifySelected(0.5f);
        }

        [MenuItem(ContextMenuRoot + "/Simplify Mesh (25%)", false, 2)]
        public static void ContextSimplify25()
        {
            SimplifySelected(0.25f);
        }

        [MenuItem(ContextMenuRoot + "/Simplify Mesh (50%)", true)]
        public static bool ValidateContextSimplify50()
        {
            return GetValidSelectedObjects().Length > 0;
        }

        [MenuItem(ContextMenuRoot + "/Simplify Mesh (25%)", true)]
        public static bool ValidateContextSimplify25()
        {
            return GetValidSelectedObjects().Length > 0;
        }

        /// <summary>
        /// Context menu item to open the generator window with selection.
        /// </summary>
        [MenuItem(ContextMenuRoot + "/Open Generator Window...", false, 20)]
        public static void ContextOpenWindow()
        {
            AutoLODWindow.ShowWindow();
        }

        #endregion

        #region Preset Quick Access

        [MenuItem(MenuRoot + "/Generate with Preset/Performance", false, 40)]
        public static void GeneratePresetPerformance()
        {
            GenerateWithPreset(LODPreset.Performance);
        }

        [MenuItem(MenuRoot + "/Generate with Preset/Balanced", false, 41)]
        public static void GeneratePresetBalanced()
        {
            GenerateWithPreset(LODPreset.Balanced);
        }

        [MenuItem(MenuRoot + "/Generate with Preset/Quality", false, 42)]
        public static void GeneratePresetQuality()
        {
            GenerateWithPreset(LODPreset.Quality);
        }

        [MenuItem(MenuRoot + "/Generate with Preset/Mobile (Low-end)", false, 50)]
        public static void GeneratePresetMobileLow()
        {
            GenerateWithPreset(LODPreset.MobileLowEnd);
        }

        [MenuItem(MenuRoot + "/Generate with Preset/Mobile (High-end)", false, 51)]
        public static void GeneratePresetMobileHigh()
        {
            GenerateWithPreset(LODPreset.MobileHighEnd);
        }

        [MenuItem(MenuRoot + "/Generate with Preset/VR", false, 52)]
        public static void GeneratePresetVR()
        {
            GenerateWithPreset(LODPreset.VR);
        }

        // Validation for all preset menu items
        [MenuItem(MenuRoot + "/Generate with Preset/Performance", true)]
        [MenuItem(MenuRoot + "/Generate with Preset/Balanced", true)]
        [MenuItem(MenuRoot + "/Generate with Preset/Quality", true)]
        [MenuItem(MenuRoot + "/Generate with Preset/Mobile (Low-end)", true)]
        [MenuItem(MenuRoot + "/Generate with Preset/Mobile (High-end)", true)]
        [MenuItem(MenuRoot + "/Generate with Preset/VR", true)]
        public static bool ValidateGenerateWithPreset()
        {
            return GetValidSelectedObjects().Length > 0;
        }

        #endregion

        #region Helper Methods

        private static void GenerateWithPreset(LODPreset preset)
        {
            var selectedObjects = GetValidSelectedObjects();

            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Auto LOD Generator",
                    "Please select one or more GameObjects with MeshFilter and MeshRenderer components.",
                    "OK");
                return;
            }

            var settings = new LODGeneratorSettings();
            settings.ApplyPreset(preset);

            int successCount = 0;

            foreach (var obj in selectedObjects)
            {
                var result = LODGeneratorCore.GenerateLODGroup(obj, settings);
                if (result.Success)
                {
                    successCount++;
                    Debug.Log($"[Auto LOD] Generated LOD group for '{obj.name}' using {preset} preset");
                }
                else
                {
                    Debug.LogWarning($"[Auto LOD] Failed: {result.ErrorMessage}");
                }
            }

            Debug.Log($"[Auto LOD] Generated {successCount}/{selectedObjects.Length} LOD groups with {preset} preset");
        }

        private static void SimplifySelected(float quality)
        {
            var selectedObjects = GetValidSelectedObjects();

            foreach (var obj in selectedObjects)
            {
                var result = LODGeneratorCore.GenerateSimplifiedMesh(obj, quality);
                if (result.Success)
                {
                    Debug.Log($"[Auto LOD] Simplified '{obj.name}' to {quality * 100}%: {result.OriginalVertexCount} -> {result.LODVertexCounts[0]} vertices");
                }
                else
                {
                    Debug.LogWarning($"[Auto LOD] Failed to simplify '{obj.name}': {result.ErrorMessage}");
                }
            }
        }

        private static GameObject[] GetValidSelectedObjects()
        {
            return Selection.gameObjects
                .Where(go => LODGeneratorCore.ValidateForLODGeneration(go, out _))
                .ToArray();
        }

        #endregion
    }
}
