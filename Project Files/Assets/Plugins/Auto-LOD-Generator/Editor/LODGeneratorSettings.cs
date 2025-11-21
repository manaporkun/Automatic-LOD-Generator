using System;
using UnityEngine;

namespace Plugins.AutoLODGenerator.Editor
{
    /// <summary>
    /// Configuration settings for LOD generation.
    /// </summary>
    [Serializable]
    public class LODGeneratorSettings
    {
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

        public const int MinLODLevels = 2;
        public const int MaxLODLevels = 6;

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
                    lodLevelCount = 3;
                    qualityFactors = new[] { 1.0f, 0.4f, 0.15f, 0.1f, 0.05f, 0.02f };
                    screenTransitionHeights = new[] { 0.5f, 0.2f, 0.05f, 0.02f, 0.01f, 0.005f };
                    includeCulledLevel = true;
                    culledTransitionHeight = 0.01f;
                    break;

                case LODPreset.Balanced:
                    lodLevelCount = 4;
                    qualityFactors = new[] { 1.0f, 0.65f, 0.4f, 0.2f, 0.1f, 0.05f };
                    screenTransitionHeights = new[] { 0.5f, 0.3f, 0.15f, 0.05f, 0.02f, 0.01f };
                    includeCulledLevel = true;
                    culledTransitionHeight = 0.01f;
                    break;

                case LODPreset.Quality:
                    lodLevelCount = 5;
                    qualityFactors = new[] { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f, 0.1f };
                    screenTransitionHeights = new[] { 0.6f, 0.4f, 0.25f, 0.12f, 0.05f, 0.02f };
                    includeCulledLevel = true;
                    culledTransitionHeight = 0.005f;
                    break;

                case LODPreset.MobileLowEnd:
                    lodLevelCount = 2;
                    qualityFactors = new[] { 1.0f, 0.25f, 0.1f, 0.05f, 0.02f, 0.01f };
                    screenTransitionHeights = new[] { 0.4f, 0.1f, 0.05f, 0.02f, 0.01f, 0.005f };
                    includeCulledLevel = true;
                    culledTransitionHeight = 0.02f;
                    break;

                case LODPreset.MobileHighEnd:
                    lodLevelCount = 3;
                    qualityFactors = new[] { 1.0f, 0.5f, 0.25f, 0.1f, 0.05f, 0.02f };
                    screenTransitionHeights = new[] { 0.5f, 0.25f, 0.08f, 0.03f, 0.01f, 0.005f };
                    includeCulledLevel = true;
                    culledTransitionHeight = 0.015f;
                    break;

                case LODPreset.VR:
                    lodLevelCount = 4;
                    qualityFactors = new[] { 1.0f, 0.75f, 0.5f, 0.3f, 0.15f, 0.08f };
                    screenTransitionHeights = new[] { 0.7f, 0.5f, 0.3f, 0.15f, 0.08f, 0.03f };
                    includeCulledLevel = false;
                    culledTransitionHeight = 0.01f;
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

            for (int i = 0; i < qualityFactors.Length; i++)
            {
                qualityFactors[i] = Mathf.Clamp01(qualityFactors[i]);
            }

            for (int i = 0; i < screenTransitionHeights.Length; i++)
            {
                screenTransitionHeights[i] = Mathf.Clamp01(screenTransitionHeights[i]);
            }

            culledTransitionHeight = Mathf.Clamp01(culledTransitionHeight);
        }
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

        public float GetTotalReduction()
        {
            if (LODVertexCounts == null || LODVertexCounts.Length == 0 || OriginalVertexCount == 0)
                return 0f;

            int lowestLODVerts = LODVertexCounts[LODVertexCounts.Length - 1];
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
}
