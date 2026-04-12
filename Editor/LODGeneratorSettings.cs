using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityMeshSimplifier;

namespace Plugins.AutoLODGenerator.Editor
{
    /// <summary>
    /// Configuration settings for LOD generation.
    /// </summary>
    [Serializable]
    public class LODGeneratorSettings
    {
        /// <summary>
        /// Name of this preset (for custom presets).
        /// </summary>
        public string presetName = "Custom";

        /// <summary>
        /// Number of LOD levels to generate (including LOD0).
        /// </summary>
        [Range(MinLODLevels, MaxLODLevels)]
        public int lodLevelCount = 4;

        /// <summary>
        /// Quality factors for each LOD level (0.0 to 1.0).
        /// Index 0 is always 1.0 (original mesh).
        /// </summary>
        public float[] qualityFactors = { 1.0f, 0.6f, 0.4f, 0.2f, 0.1f, 0.05f };

        /// <summary>
        /// Screen relative transition heights for each LOD level.
        /// </summary>
        public float[] screenTransitionHeights = { 0.6f, 0.4f, 0.2f, 0.1f, 0.05f, 0.01f };

        /// <summary>
        /// Whether to include a culled LOD level (object disappears at distance).
        /// </summary>
        public bool includeCulledLevel = true;

        /// <summary>
        /// Culled level screen transition height.
        /// </summary>
        public float culledTransitionHeight = 0.01f;

        #region Simplification Options

        /// <summary>
        /// Enable smart linking to prevent holes in simplified meshes.
        /// </summary>
        [Tooltip("Enables smart vertex linking to prevent holes in the simplified mesh")]
        public bool enableSmartLink = true;

        /// <summary>
        /// Maximum distance between vertices for smart linking.
        /// </summary>
        [Tooltip("Maximum distance between vertices for smart linking (higher values for large meshes)")]
        public double vertexLinkDistance = double.Epsilon;

        /// <summary>
        /// Preserve border edges during simplification.
        /// </summary>
        [Tooltip("Preserve border edges (limits simplification but prevents mesh edge deformation)")]
        public bool preserveBorders = false;

        /// <summary>
        /// Preserve UV seam edges during simplification.
        /// </summary>
        [Tooltip("Preserve UV seam edges (prevents UV stretching at seams)")]
        public bool preserveSeams = false;

        /// <summary>
        /// Preserve UV foldover edges during simplification.
        /// </summary>
        [Tooltip("Preserve UV foldover edges (prevents UV distortion on overlapping UVs)")]
        public bool preserveFoldovers = false;

        #endregion

        public const int MinLODLevels = 2;
        public const int MaxLODLevels = 6;

        /// <summary>
        /// Default folder for custom presets (inside user's project, survives package updates).
        /// </summary>
        private const string DefaultCustomPresetsFolder = "Assets/Editor/AutoLODGenerator/Presets";

        /// <summary>
        /// Legacy folder for backward compatibility (old location inside plugin folder).
        /// </summary>
        private const string LegacyCustomPresetsFolder = "Assets/Plugins/Auto-LOD-Generator/Editor/Presets";

        /// <summary>
        /// EditorPrefs key for custom preset folder path.
        /// </summary>
        private const string CustomPresetsFolderPrefsKey = "AutoLODGenerator_CustomPresetsFolder";

        /// <summary>
        /// Creates settings with default values.
        /// </summary>
        public LODGeneratorSettings()
        {
            ApplyPreset(LODPreset.Balanced);
        }

        /// <summary>
        /// Applies a preset configuration.
        /// </summary>
        public void ApplyPreset(LODPreset preset)
        {
            switch (preset)
            {
                case LODPreset.Performance:
                    presetName = "Performance";
                    lodLevelCount = 3;
                    qualityFactors = new[] { 1.0f, 0.4f, 0.15f, 0.1f, 0.05f, 0.02f };
                    screenTransitionHeights = new[] { 0.5f, 0.2f, 0.05f, 0.02f, 0.01f, 0.005f };
                    includeCulledLevel = true;
                    culledTransitionHeight = 0.01f;
                    // Performance: minimal preservation for maximum simplification
                    enableSmartLink = true;
                    preserveBorders = false;
                    preserveSeams = false;
                    preserveFoldovers = false;
                    break;

                case LODPreset.Balanced:
                    presetName = "Balanced";
                    lodLevelCount = 4;
                    qualityFactors = new[] { 1.0f, 0.65f, 0.4f, 0.2f, 0.1f, 0.05f };
                    screenTransitionHeights = new[] { 0.5f, 0.3f, 0.15f, 0.05f, 0.02f, 0.01f };
                    includeCulledLevel = true;
                    culledTransitionHeight = 0.01f;
                    // Balanced: smart linking enabled, moderate preservation
                    enableSmartLink = true;
                    preserveBorders = false;
                    preserveSeams = false;
                    preserveFoldovers = false;
                    break;

                case LODPreset.Quality:
                    presetName = "Quality";
                    lodLevelCount = 5;
                    qualityFactors = new[] { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f, 0.1f };
                    screenTransitionHeights = new[] { 0.6f, 0.4f, 0.25f, 0.12f, 0.05f, 0.02f };
                    includeCulledLevel = true;
                    culledTransitionHeight = 0.005f;
                    // Quality: preserve more for better visual fidelity
                    enableSmartLink = true;
                    preserveBorders = true;
                    preserveSeams = true;
                    preserveFoldovers = false;
                    break;

                case LODPreset.MobileLowEnd:
                    presetName = "Mobile (Low-end)";
                    lodLevelCount = 2;
                    qualityFactors = new[] { 1.0f, 0.25f, 0.1f, 0.05f, 0.02f, 0.01f };
                    screenTransitionHeights = new[] { 0.4f, 0.1f, 0.05f, 0.02f, 0.01f, 0.005f };
                    includeCulledLevel = true;
                    culledTransitionHeight = 0.02f;
                    // Mobile Low: aggressive simplification
                    enableSmartLink = true;
                    preserveBorders = false;
                    preserveSeams = false;
                    preserveFoldovers = false;
                    break;

                case LODPreset.MobileHighEnd:
                    presetName = "Mobile (High-end)";
                    lodLevelCount = 3;
                    qualityFactors = new[] { 1.0f, 0.5f, 0.25f, 0.1f, 0.05f, 0.02f };
                    screenTransitionHeights = new[] { 0.5f, 0.25f, 0.08f, 0.03f, 0.01f, 0.005f };
                    includeCulledLevel = true;
                    culledTransitionHeight = 0.015f;
                    // Mobile High: balanced for modern mobile
                    enableSmartLink = true;
                    preserveBorders = false;
                    preserveSeams = false;
                    preserveFoldovers = false;
                    break;

                case LODPreset.VR:
                    presetName = "VR";
                    lodLevelCount = 4;
                    qualityFactors = new[] { 1.0f, 0.75f, 0.5f, 0.3f, 0.15f, 0.08f };
                    screenTransitionHeights = new[] { 0.7f, 0.5f, 0.3f, 0.15f, 0.08f, 0.03f };
                    includeCulledLevel = false;
                    culledTransitionHeight = 0.01f;
                    // VR: preserve borders to avoid artifacts at edges
                    enableSmartLink = true;
                    preserveBorders = true;
                    preserveSeams = true;
                    preserveFoldovers = false;
                    break;

                case LODPreset.Custom:
                    // Keep current settings
                    break;
            }
        }

        /// <summary>
        /// Gets the quality factor for a specific LOD level.
        /// </summary>
        public float GetQualityFactor(int lodLevel)
        {
            if (lodLevel < 0 || lodLevel >= lodLevelCount)
                return 0f;

            return lodLevel == 0 ? 1.0f : qualityFactors[Mathf.Min(lodLevel, qualityFactors.Length - 1)];
        }

        /// <summary>
        /// Gets the screen transition height for a specific LOD level.
        /// </summary>
        public float GetScreenTransitionHeight(int lodLevel)
        {
            if (lodLevel < 0 || lodLevel >= lodLevelCount)
                return 0f;

            return screenTransitionHeights[Mathf.Min(lodLevel, screenTransitionHeights.Length - 1)];
        }

        /// <summary>
        /// Validates and clamps all settings to valid ranges.
        /// </summary>
        public void Validate()
        {
            lodLevelCount = Mathf.Clamp(lodLevelCount, MinLODLevels, MaxLODLevels);

            for (var i = 0; i < qualityFactors.Length; i++)
            {
                qualityFactors[i] = Mathf.Clamp01(qualityFactors[i]);
            }

            for (var i = 0; i < screenTransitionHeights.Length; i++)
            {
                screenTransitionHeights[i] = Mathf.Clamp01(screenTransitionHeights[i]);
            }

            culledTransitionHeight = Mathf.Clamp01(culledTransitionHeight);

            // Ensure vertex link distance is non-negative
            if (vertexLinkDistance < 0)
                vertexLinkDistance = 0;
        }

        /// <summary>
        /// Creates SimplificationOptions for use with MeshSimplifier.
        /// </summary>
        /// <returns>Configured SimplificationOptions.</returns>
        public SimplificationOptions CreateSimplificationOptions()
        {
            return new SimplificationOptions
            {
                EnableSmartLink = enableSmartLink,
                VertexLinkDistance = vertexLinkDistance,
                PreserveBorderEdges = preserveBorders,
                PreserveUVSeamEdges = preserveSeams,
                PreserveUVFoldoverEdges = preserveFoldovers,
                MaxIterationCount = 100,
                Agressiveness = 7.0,
                PreserveSurfaceCurvature = false,
                ManualUVComponentCount = false,
                UVComponentCount = 2
            };
        }

        #region Preset Save/Load

        /// <summary>
        /// Gets the folder path for custom presets. Uses user-defined path if set, otherwise default.
        /// </summary>
        public static string GetCustomPresetsFolder()
        {
            var userPath = EditorPrefs.GetString(CustomPresetsFolderPrefsKey, "");
            return string.IsNullOrEmpty(userPath) ? DefaultCustomPresetsFolder : userPath;
        }

        /// <summary>
        /// Sets a custom folder path for presets.
        /// </summary>
        /// <param name="path">The folder path (relative to project root, starting with 'Assets/').</param>
        public static void SetCustomPresetsFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                EditorPrefs.DeleteKey(CustomPresetsFolderPrefsKey);
            }
            else
            {
                EditorPrefs.SetString(CustomPresetsFolderPrefsKey, path);
            }
        }

        /// <summary>
        /// Saves the current settings as a custom preset.
        /// </summary>
        /// <param name="name">Name for the preset.</param>
        /// <returns>True if saved successfully.</returns>
        public bool SaveAsPreset(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.LogError("[Auto LOD] Preset name cannot be empty.");
                return false;
            }

            try
            {
                var presetsFolder = GetCustomPresetsFolder();

                // Ensure directory exists
                if (!Directory.Exists(presetsFolder))
                {
                    Directory.CreateDirectory(presetsFolder);
                }

                presetName = name;
                var json = JsonUtility.ToJson(this, true);
                var filePath = GetPresetPath(name);

                File.WriteAllText(filePath, json);
                AssetDatabase.Refresh();

                Debug.Log($"[Auto LOD] Preset '{name}' saved to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auto LOD] Failed to save preset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads a custom preset by name. Checks current location, then legacy location.
        /// </summary>
        /// <param name="name">Name of the preset to load.</param>
        /// <returns>True if loaded successfully.</returns>
        public bool LoadPreset(string name)
        {
            try
            {
                var filePath = GetPresetPath(name);
                string json;

                // Check current location first
                if (!File.Exists(filePath))
                {
                    // Try legacy location for backward compatibility
                    var legacyPath = GetLegacyPresetPath(name);
                    if (File.Exists(legacyPath))
                    {
                        // Migrate from legacy to new location
                        json = File.ReadAllText(legacyPath);
                        JsonUtility.FromJsonOverwrite(json, this);
                        
                        // Save to new location
                        SaveAsPreset(name);
                        
                        Debug.Log($"[Auto LOD] Preset '{name}' migrated from legacy location.");
                        return true;
                    }

                    Debug.LogError($"[Auto LOD] Preset '{name}' not found.");
                    return false;
                }

                json = File.ReadAllText(filePath);
                JsonUtility.FromJsonOverwrite(json, this);

                Debug.Log($"[Auto LOD] Preset '{name}' loaded successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auto LOD] Failed to load preset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a custom preset. Checks both current and legacy locations.
        /// </summary>
        /// <param name="name">Name of the preset to delete.</param>
        /// <returns>True if deleted successfully.</returns>
        public static bool DeletePreset(string name)
        {
            try
            {
                var filePath = GetPresetPath(name);
                var deleted = false;

                // Try current location
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);

                    // Also delete meta file if exists
                    var metaPath = filePath + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }

                    deleted = true;
                }

                // Also check legacy location
                var legacyPath = GetLegacyPresetPath(name);
                if (File.Exists(legacyPath))
                {
                    File.Delete(legacyPath);

                    var legacyMetaPath = legacyPath + ".meta";
                    if (File.Exists(legacyMetaPath))
                    {
                        File.Delete(legacyMetaPath);
                    }

                    deleted = true;
                }

                if (!deleted)
                {
                    Debug.LogError($"[Auto LOD] Preset '{name}' not found.");
                    return false;
                }

                AssetDatabase.Refresh();

                Debug.Log($"[Auto LOD] Preset '{name}' deleted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auto LOD] Failed to delete preset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all available custom preset names from both current and legacy locations.
        /// </summary>
        /// <returns>Array of preset names (duplicates removed).</returns>
        public static string[] GetCustomPresetNames()
        {
            var presetNames = new System.Collections.Generic.HashSet<string>();

            // Check current location
            var presetsFolder = GetCustomPresetsFolder();
            if (Directory.Exists(presetsFolder))
            {
                var files = Directory.GetFiles(presetsFolder, "*.json");
                foreach (var file in files)
                {
                    presetNames.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            // Check legacy location for backward compatibility
            if (Directory.Exists(LegacyCustomPresetsFolder))
            {
                var legacyFiles = Directory.GetFiles(LegacyCustomPresetsFolder, "*.json");
                foreach (var file in legacyFiles)
                {
                    presetNames.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            var result = new string[presetNames.Count];
            presetNames.CopyTo(result);
            System.Array.Sort(result);
            return result;
        }

        /// <summary>
        /// Creates a deep copy of this settings object.
        /// </summary>
        /// <returns>A new LODGeneratorSettings instance with the same values.</returns>
        public LODGeneratorSettings Clone()
        {
            var json = JsonUtility.ToJson(this);
            var clone = new LODGeneratorSettings();
            JsonUtility.FromJsonOverwrite(json, clone);
            return clone;
        }

        private static string GetPresetPath(string name)
        {
            // Sanitize filename
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return Path.Combine(GetCustomPresetsFolder(), $"{name}.json");
        }

        private static string GetLegacyPresetPath(string name)
        {
            // Sanitize filename
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return Path.Combine(LegacyCustomPresetsFolder, $"{name}.json");
        }

        #endregion
    }

    /// <summary>
    /// Predefined LOD configuration presets.
    /// </summary>
    public enum LODPreset
    {
        /// <summary>Aggressive simplification for maximum performance.</summary>
        Performance,

        /// <summary>Balanced quality and performance (default).</summary>
        Balanced,

        /// <summary>Higher quality with more gradual transitions.</summary>
        Quality,

        /// <summary>Optimized for low-end mobile devices.</summary>
        MobileLowEnd,

        /// <summary>Optimized for high-end mobile devices.</summary>
        MobileHighEnd,

        /// <summary>Optimized for VR applications (avoids popping).</summary>
        VR,

        /// <summary>User-defined custom settings.</summary>
        Custom
    }

    /// <summary>
    /// Result of a single LOD generation operation.
    /// </summary>
    public class LODGenerationResult
    {
        public GameObject SourceObject { get; set; }
        public GameObject GeneratedLODGroup { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int OriginalVertexCount { get; set; }
        public int[] LODVertexCounts { get; set; }
        public int OriginalTriangleCount { get; set; }
        public int[] LODTriangleCounts { get; set; }
        public Mesh[] GeneratedMeshes { get; set; }
        public string[] SavedMeshPaths { get; set; }

        public float GetTotalReduction()
        {
            if (LODVertexCounts == null || LODVertexCounts.Length == 0 || OriginalVertexCount == 0)
                return 0f;

            var lowestLODVerts = LODVertexCounts[LODVertexCounts.Length - 1];
            return 1f - ((float)lowestLODVerts / OriginalVertexCount);
        }
    }

    /// <summary>
    /// Batch processing result.
    /// </summary>
    public class BatchProcessingResult
    {
        public int TotalObjects { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public LODGenerationResult[] Results { get; set; }
        public float ProcessingTimeSeconds { get; set; }

        public bool AllSucceeded => FailureCount == 0;
    }

    /// <summary>
    /// Type of mesh renderer on a GameObject.
    /// </summary>
    public enum MeshRendererType
    {
        None,
        MeshRenderer,
        SkinnedMeshRenderer,
        /// <summary>
        /// Root has no direct renderer, but has descendant GameObjects with valid renderers.
        /// </summary>
        Composite
    }
}
