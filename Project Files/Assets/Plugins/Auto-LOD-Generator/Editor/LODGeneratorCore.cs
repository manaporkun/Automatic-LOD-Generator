using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityMeshSimplifier;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Plugins.AutoLODGenerator.Editor
{
    /// <summary>
    /// Core LOD generation functionality.
    /// Handles mesh simplification and LOD group creation.
    /// </summary>
    public static class LODGeneratorCore
    {
        private const string DefaultMeshSaveFolder = "Assets/GeneratedLODs";

        #region LOD Group Generation

        /// <summary>
        /// Generates a LOD group for a single GameObject.
        /// Supports both MeshRenderer and SkinnedMeshRenderer.
        /// </summary>
        /// <param name="sourceObject">The source GameObject with a mesh.</param>
        /// <param name="settings">LOD generation settings.</param>
        /// <param name="saveMeshesToAssets">Whether to save generated meshes as asset files.</param>
        /// <param name="meshSavePath">Custom path for saving meshes (optional).</param>
        /// <returns>Result containing the generated LOD group and statistics.</returns>
        public static LODGenerationResult GenerateLODGroup(
            GameObject sourceObject,
            LODGeneratorSettings settings,
            bool saveMeshesToAssets = false,
            string meshSavePath = null)
        {
            var result = new LODGenerationResult
            {
                SourceObject = sourceObject,
                Success = false
            };

            try
            {
                // Validate input
                if (sourceObject == null)
                {
                    result.ErrorMessage = "Source object is null.";
                    return result;
                }

                var rendererType = GetMeshRendererType(sourceObject);
                if (rendererType == MeshRendererType.None)
                {
                    result.ErrorMessage = $"'{sourceObject.name}' does not have a valid mesh renderer.";
                    return result;
                }

                settings.Validate();

                // Get mesh and materials based on renderer type
                Mesh originalMesh;
                Material[] originalMaterials;
                Transform originalTransform = sourceObject.transform;

                if (rendererType == MeshRendererType.SkinnedMeshRenderer)
                {
                    var skinnedRenderer = sourceObject.GetComponent<SkinnedMeshRenderer>();
                    originalMesh = skinnedRenderer.sharedMesh;
                    originalMaterials = skinnedRenderer.sharedMaterials;
                }
                else
                {
                    var meshFilter = sourceObject.GetComponent<MeshFilter>();
                    var meshRenderer = sourceObject.GetComponent<MeshRenderer>();
                    originalMesh = meshFilter.sharedMesh;
                    originalMaterials = meshRenderer.sharedMaterials;
                }

                if (originalMesh == null)
                {
                    result.ErrorMessage = $"'{sourceObject.name}' does not have a valid mesh assigned.";
                    return result;
                }

                result.OriginalVertexCount = originalMesh.vertexCount;
                result.OriginalTriangleCount = originalMesh.triangles.Length / 3;
                result.LODVertexCounts = new int[settings.lodLevelCount];
                result.LODTriangleCounts = new int[settings.lodLevelCount];
                result.GeneratedMeshes = new Mesh[settings.lodLevelCount];

                // Setup mesh save path if needed
                if (saveMeshesToAssets)
                {
                    if (string.IsNullOrEmpty(meshSavePath))
                    {
                        meshSavePath = DefaultMeshSaveFolder;
                    }
                    EnsureDirectoryExists(meshSavePath);
                    result.SavedMeshPaths = new string[settings.lodLevelCount];
                }

                // Create parent LOD group object
                var lodGroupObject = new GameObject($"{sourceObject.name}_LODGroup");
                lodGroupObject.transform.position = originalTransform.position;
                lodGroupObject.transform.rotation = originalTransform.rotation;
                lodGroupObject.transform.localScale = Vector3.one;

                var lodGroup = lodGroupObject.AddComponent<LODGroup>();

                // Determine total LOD count (including optional culled level)
                int totalLODCount = settings.includeCulledLevel ? settings.lodLevelCount + 1 : settings.lodLevelCount;
                var lods = new LOD[totalLODCount];

                // Generate each LOD level
                for (int i = 0; i < settings.lodLevelCount; i++)
                {
                    float quality = settings.GetQualityFactor(i);
                    float screenHeight = settings.GetScreenTransitionHeight(i);

                    Mesh lodMesh;
                    if (i == 0)
                    {
                        // LOD0 uses original mesh
                        lodMesh = originalMesh;
                    }
                    else
                    {
                        // Simplify mesh for this LOD level
                        lodMesh = SimplifyMesh(originalMesh, quality);
                        lodMesh.name = $"{originalMesh.name}_LOD{i}";
                    }

                    result.LODVertexCounts[i] = lodMesh.vertexCount;
                    result.LODTriangleCounts[i] = lodMesh.triangles.Length / 3;
                    result.GeneratedMeshes[i] = lodMesh;

                    // Save mesh to assets if requested
                    if (saveMeshesToAssets && i > 0)
                    {
                        string savedPath = SaveMeshAsset(lodMesh, meshSavePath, $"{sourceObject.name}_LOD{i}");
                        result.SavedMeshPaths[i] = savedPath;

                        // Reload the saved mesh
                        if (!string.IsNullOrEmpty(savedPath))
                        {
                            lodMesh = AssetDatabase.LoadAssetAtPath<Mesh>(savedPath);
                        }
                    }

                    // Create LOD GameObject based on renderer type
                    GameObject lodObject;
                    Renderer lodRenderer;

                    if (rendererType == MeshRendererType.SkinnedMeshRenderer)
                    {
                        lodObject = CreateSkinnedLODObject(
                            sourceObject,
                            lodGroupObject.transform,
                            lodMesh,
                            originalMaterials,
                            i);
                        lodRenderer = lodObject.GetComponent<SkinnedMeshRenderer>();
                    }
                    else
                    {
                        lodObject = CreateStaticLODObject(
                            sourceObject.name,
                            lodGroupObject.transform,
                            originalTransform,
                            lodMesh,
                            originalMaterials,
                            i,
                            sourceObject.GetComponent<MeshRenderer>());
                        lodRenderer = lodObject.GetComponent<MeshRenderer>();
                    }

                    lods[i] = new LOD(screenHeight, new Renderer[] { lodRenderer });
                }

                // Add culled level if enabled
                if (settings.includeCulledLevel)
                {
                    lods[settings.lodLevelCount] = new LOD(settings.culledTransitionHeight, new Renderer[0]);
                }

                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();

                // Register undo
                Undo.RegisterCreatedObjectUndo(lodGroupObject, $"Generate LOD Group for {sourceObject.name}");

                result.GeneratedLODGroup = lodGroupObject;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error generating LOD group: {ex.Message}";
                Debug.LogException(ex);
            }

            return result;
        }

        private static GameObject CreateStaticLODObject(
            string baseName,
            Transform parent,
            Transform originalTransform,
            Mesh mesh,
            Material[] materials,
            int lodIndex,
            MeshRenderer sourceRenderer)
        {
            var lodObject = new GameObject($"{baseName}_LOD{lodIndex}");
            lodObject.transform.SetParent(parent);
            lodObject.transform.localPosition = Vector3.zero;
            lodObject.transform.localRotation = Quaternion.identity;
            lodObject.transform.localScale = originalTransform.localScale;

            var lodMeshFilter = lodObject.AddComponent<MeshFilter>();
            lodMeshFilter.sharedMesh = mesh;

            var lodMeshRenderer = lodObject.AddComponent<MeshRenderer>();
            lodMeshRenderer.sharedMaterials = materials;

            // Copy additional renderer settings
            if (sourceRenderer != null)
            {
                lodMeshRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                lodMeshRenderer.receiveShadows = sourceRenderer.receiveShadows;
                lodMeshRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
                lodMeshRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
            }

            return lodObject;
        }

        private static GameObject CreateSkinnedLODObject(
            GameObject sourceObject,
            Transform parent,
            Mesh mesh,
            Material[] materials,
            int lodIndex)
        {
            var sourceRenderer = sourceObject.GetComponent<SkinnedMeshRenderer>();
            var originalTransform = sourceObject.transform;

            var lodObject = new GameObject($"{sourceObject.name}_LOD{lodIndex}");
            lodObject.transform.SetParent(parent);
            lodObject.transform.localPosition = Vector3.zero;
            lodObject.transform.localRotation = Quaternion.identity;
            lodObject.transform.localScale = originalTransform.localScale;

            var lodRenderer = lodObject.AddComponent<SkinnedMeshRenderer>();
            lodRenderer.sharedMesh = mesh;
            lodRenderer.sharedMaterials = materials;

            // Copy skinning data
            lodRenderer.bones = sourceRenderer.bones;
            lodRenderer.rootBone = sourceRenderer.rootBone;
            lodRenderer.quality = sourceRenderer.quality;

            // Copy additional renderer settings
            lodRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            lodRenderer.receiveShadows = sourceRenderer.receiveShadows;
            lodRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
            lodRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
            lodRenderer.updateWhenOffscreen = sourceRenderer.updateWhenOffscreen;

            return lodObject;
        }

        #endregion

        #region Mesh Simplification

        /// <summary>
        /// Generates a simplified version of a mesh.
        /// Supports both MeshRenderer and SkinnedMeshRenderer.
        /// </summary>
        /// <param name="sourceObject">The source GameObject with a mesh.</param>
        /// <param name="quality">Quality factor (0.0 to 1.0).</param>
        /// <param name="suffix">Suffix to append to the object name.</param>
        /// <param name="saveMeshToAssets">Whether to save the mesh as an asset file.</param>
        /// <param name="meshSavePath">Custom path for saving mesh (optional).</param>
        /// <returns>Result containing the simplified mesh object and statistics.</returns>
        public static LODGenerationResult GenerateSimplifiedMesh(
            GameObject sourceObject,
            float quality,
            string suffix = "_Simplified",
            bool saveMeshToAssets = false,
            string meshSavePath = null)
        {
            var result = new LODGenerationResult
            {
                SourceObject = sourceObject,
                Success = false
            };

            try
            {
                // Validate input
                if (sourceObject == null)
                {
                    result.ErrorMessage = "Source object is null.";
                    return result;
                }

                var rendererType = GetMeshRendererType(sourceObject);
                if (rendererType == MeshRendererType.None)
                {
                    result.ErrorMessage = $"'{sourceObject.name}' does not have a valid mesh renderer.";
                    return result;
                }

                quality = Mathf.Clamp01(quality);

                // Get mesh and materials based on renderer type
                Mesh originalMesh;
                Material[] originalMaterials;
                Transform originalTransform = sourceObject.transform;

                if (rendererType == MeshRendererType.SkinnedMeshRenderer)
                {
                    var skinnedRenderer = sourceObject.GetComponent<SkinnedMeshRenderer>();
                    originalMesh = skinnedRenderer.sharedMesh;
                    originalMaterials = skinnedRenderer.sharedMaterials;
                }
                else
                {
                    var meshFilter = sourceObject.GetComponent<MeshFilter>();
                    var meshRenderer = sourceObject.GetComponent<MeshRenderer>();
                    originalMesh = meshFilter.sharedMesh;
                    originalMaterials = meshRenderer.sharedMaterials;
                }

                if (originalMesh == null)
                {
                    result.ErrorMessage = $"'{sourceObject.name}' does not have a valid mesh assigned.";
                    return result;
                }

                result.OriginalVertexCount = originalMesh.vertexCount;
                result.OriginalTriangleCount = originalMesh.triangles.Length / 3;

                // Simplify mesh
                var simplifiedMesh = SimplifyMesh(originalMesh, quality);
                simplifiedMesh.name = $"{originalMesh.name}{suffix}";

                result.LODVertexCounts = new[] { simplifiedMesh.vertexCount };
                result.LODTriangleCounts = new[] { simplifiedMesh.triangles.Length / 3 };
                result.GeneratedMeshes = new[] { simplifiedMesh };

                // Save mesh to assets if requested
                if (saveMeshToAssets)
                {
                    if (string.IsNullOrEmpty(meshSavePath))
                    {
                        meshSavePath = DefaultMeshSaveFolder;
                    }
                    EnsureDirectoryExists(meshSavePath);

                    string savedPath = SaveMeshAsset(simplifiedMesh, meshSavePath, $"{sourceObject.name}{suffix}");
                    result.SavedMeshPaths = new[] { savedPath };

                    // Reload the saved mesh
                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        simplifiedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(savedPath);
                    }
                }

                // Create simplified GameObject
                var simplifiedObject = new GameObject($"{sourceObject.name}{suffix}");
                simplifiedObject.transform.position = originalTransform.position;
                simplifiedObject.transform.rotation = originalTransform.rotation;
                simplifiedObject.transform.localScale = originalTransform.localScale;

                if (rendererType == MeshRendererType.SkinnedMeshRenderer)
                {
                    var sourceRenderer = sourceObject.GetComponent<SkinnedMeshRenderer>();
                    var newRenderer = simplifiedObject.AddComponent<SkinnedMeshRenderer>();
                    newRenderer.sharedMesh = simplifiedMesh;
                    newRenderer.sharedMaterials = originalMaterials;
                    newRenderer.bones = sourceRenderer.bones;
                    newRenderer.rootBone = sourceRenderer.rootBone;
                    newRenderer.quality = sourceRenderer.quality;
                    newRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                    newRenderer.receiveShadows = sourceRenderer.receiveShadows;
                }
                else
                {
                    var sourceRenderer = sourceObject.GetComponent<MeshRenderer>();
                    var newMeshFilter = simplifiedObject.AddComponent<MeshFilter>();
                    newMeshFilter.sharedMesh = simplifiedMesh;

                    var newMeshRenderer = simplifiedObject.AddComponent<MeshRenderer>();
                    newMeshRenderer.sharedMaterials = originalMaterials;
                    newMeshRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                    newMeshRenderer.receiveShadows = sourceRenderer.receiveShadows;
                }

                // Register undo
                Undo.RegisterCreatedObjectUndo(simplifiedObject, $"Simplify {sourceObject.name}");

                result.GeneratedLODGroup = simplifiedObject;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error simplifying mesh: {ex.Message}";
                Debug.LogException(ex);
            }

            return result;
        }

        /// <summary>
        /// Simplifies a mesh to the target quality.
        /// </summary>
        /// <param name="sourceMesh">The source mesh to simplify.</param>
        /// <param name="quality">Quality factor (0.0 to 1.0).</param>
        /// <returns>A new simplified mesh.</returns>
        public static Mesh SimplifyMesh(Mesh sourceMesh, float quality)
        {
            if (sourceMesh == null)
                throw new ArgumentNullException(nameof(sourceMesh));

            quality = Mathf.Clamp01(quality);

            // If quality is 1.0, return a copy of the original
            if (Mathf.Approximately(quality, 1f))
            {
                return Object.Instantiate(sourceMesh);
            }

            var meshSimplifier = new MeshSimplifier();
            meshSimplifier.Initialize(sourceMesh);
            meshSimplifier.SimplifyMesh(quality);

            return meshSimplifier.ToMesh();
        }

        #endregion

        #region Batch Processing

        /// <summary>
        /// Processes multiple GameObjects in batch.
        /// </summary>
        /// <param name="sourceObjects">Array of source GameObjects.</param>
        /// <param name="settings">LOD generation settings.</param>
        /// <param name="progressCallback">Optional callback for progress updates (0.0 to 1.0).</param>
        /// <param name="saveMeshesToAssets">Whether to save generated meshes as asset files.</param>
        /// <param name="meshSavePath">Custom path for saving meshes (optional).</param>
        /// <returns>Batch processing result with statistics.</returns>
        public static BatchProcessingResult ProcessBatch(
            GameObject[] sourceObjects,
            LODGeneratorSettings settings,
            Action<float, string> progressCallback = null,
            bool saveMeshesToAssets = false,
            string meshSavePath = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var results = new List<LODGenerationResult>();
            int successCount = 0;
            int failureCount = 0;

            for (int i = 0; i < sourceObjects.Length; i++)
            {
                var obj = sourceObjects[i];
                float progress = (float)i / sourceObjects.Length;
                progressCallback?.Invoke(progress, $"Processing {obj.name}... ({i + 1}/{sourceObjects.Length})");

                var result = GenerateLODGroup(obj, settings, saveMeshesToAssets, meshSavePath);
                results.Add(result);

                if (result.Success)
                    successCount++;
                else
                    failureCount++;
            }

            stopwatch.Stop();
            progressCallback?.Invoke(1f, "Complete!");

            return new BatchProcessingResult
            {
                TotalObjects = sourceObjects.Length,
                SuccessCount = successCount,
                FailureCount = failureCount,
                Results = results.ToArray(),
                ProcessingTimeSeconds = (float)stopwatch.Elapsed.TotalSeconds
            };
        }

        #endregion

        #region Mesh Asset Saving

        /// <summary>
        /// Saves a mesh as an asset file.
        /// </summary>
        /// <param name="mesh">The mesh to save.</param>
        /// <param name="folderPath">The folder path to save to.</param>
        /// <param name="meshName">The name for the mesh asset.</param>
        /// <returns>The asset path of the saved mesh, or null if failed.</returns>
        public static string SaveMeshAsset(Mesh mesh, string folderPath, string meshName)
        {
            if (mesh == null)
            {
                Debug.LogError("[Auto LOD] Cannot save null mesh.");
                return null;
            }

            try
            {
                EnsureDirectoryExists(folderPath);

                // Sanitize mesh name
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    meshName = meshName.Replace(c, '_');
                }

                string assetPath = $"{folderPath}/{meshName}.asset";

                // Handle duplicates
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                // Create a copy of the mesh to save
                Mesh meshToSave = Object.Instantiate(mesh);
                meshToSave.name = meshName;

                AssetDatabase.CreateAsset(meshToSave, assetPath);
                AssetDatabase.SaveAssets();

                Debug.Log($"[Auto LOD] Mesh saved to: {assetPath}");
                return assetPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auto LOD] Failed to save mesh: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves all meshes from a LOD group result as asset files.
        /// </summary>
        /// <param name="result">The LOD generation result.</param>
        /// <param name="folderPath">The folder path to save to.</param>
        /// <returns>Array of saved asset paths.</returns>
        public static string[] SaveAllMeshesFromResult(LODGenerationResult result, string folderPath = null)
        {
            if (result == null || result.GeneratedMeshes == null)
            {
                Debug.LogError("[Auto LOD] Invalid result or no meshes to save.");
                return new string[0];
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = DefaultMeshSaveFolder;
            }

            var paths = new List<string>();
            string baseName = result.SourceObject != null ? result.SourceObject.name : "Mesh";

            for (int i = 0; i < result.GeneratedMeshes.Length; i++)
            {
                var mesh = result.GeneratedMeshes[i];
                if (mesh != null && i > 0) // Skip LOD0 as it's the original mesh
                {
                    string path = SaveMeshAsset(mesh, folderPath, $"{baseName}_LOD{i}");
                    if (!string.IsNullOrEmpty(path))
                    {
                        paths.Add(path);
                    }
                }
            }

            return paths.ToArray();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        #endregion

        #region Validation and Utilities

        /// <summary>
        /// Gets the type of mesh renderer on a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to check.</param>
        /// <returns>The type of mesh renderer found.</returns>
        public static MeshRendererType GetMeshRendererType(GameObject gameObject)
        {
            if (gameObject == null)
                return MeshRendererType.None;

            // Check for SkinnedMeshRenderer first
            var skinnedRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null)
            {
                return MeshRendererType.SkinnedMeshRenderer;
            }

            // Check for standard MeshFilter + MeshRenderer
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshFilter != null && meshFilter.sharedMesh != null && meshRenderer != null)
            {
                return MeshRendererType.MeshRenderer;
            }

            return MeshRendererType.None;
        }

        /// <summary>
        /// Validates if a GameObject is suitable for LOD generation.
        /// Supports both MeshRenderer and SkinnedMeshRenderer.
        /// </summary>
        /// <param name="gameObject">The GameObject to validate.</param>
        /// <param name="errorMessage">Error message if validation fails.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateForLODGeneration(GameObject gameObject, out string errorMessage)
        {
            errorMessage = null;

            if (gameObject == null)
            {
                errorMessage = "GameObject is null.";
                return false;
            }

            var rendererType = GetMeshRendererType(gameObject);
            if (rendererType == MeshRendererType.None)
            {
                errorMessage = $"'{gameObject.name}' does not have a valid mesh. Requires MeshFilter+MeshRenderer or SkinnedMeshRenderer.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates estimated vertex count after simplification.
        /// </summary>
        /// <param name="originalVertexCount">Original vertex count.</param>
        /// <param name="quality">Quality factor (0.0 to 1.0).</param>
        /// <returns>Estimated vertex count.</returns>
        public static int EstimateVertexCount(int originalVertexCount, float quality)
        {
            // This is an estimation - actual results depend on mesh topology
            return Mathf.Max(3, Mathf.RoundToInt(originalVertexCount * quality));
        }

        /// <summary>
        /// Gets mesh statistics for a GameObject.
        /// Supports both MeshRenderer and SkinnedMeshRenderer.
        /// </summary>
        /// <param name="gameObject">The GameObject to analyze.</param>
        /// <returns>Tuple of (vertexCount, triangleCount, rendererType) or (-1, -1, None) if invalid.</returns>
        public static (int vertices, int triangles, MeshRendererType type) GetMeshStatistics(GameObject gameObject)
        {
            if (gameObject == null)
                return (-1, -1, MeshRendererType.None);

            var rendererType = GetMeshRendererType(gameObject);
            Mesh mesh = null;

            switch (rendererType)
            {
                case MeshRendererType.SkinnedMeshRenderer:
                    mesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                    break;
                case MeshRendererType.MeshRenderer:
                    mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                    break;
            }

            if (mesh == null)
                return (-1, -1, MeshRendererType.None);

            return (mesh.vertexCount, mesh.triangles.Length / 3, rendererType);
        }

        /// <summary>
        /// Gets mesh statistics for a GameObject (legacy overload for compatibility).
        /// </summary>
        public static (int vertices, int triangles) GetMeshStatistics(GameObject gameObject, bool legacy = true)
        {
            var stats = GetMeshStatistics(gameObject);
            return (stats.vertices, stats.triangles);
        }

        #endregion
    }
}
