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
                var originalTransform = sourceObject.transform;

                if (!TryGetMeshData(sourceObject, rendererType, out var originalMesh, out var originalMaterials))
                {
                    result.ErrorMessage = $"'{sourceObject.name}' does not have a valid mesh assigned.";
                    return result;
                }

                // Cache source renderers before the loop
                MeshRenderer sourceMeshRenderer = null;
                SkinnedMeshRenderer sourceSkinnedRenderer = null;

                if (rendererType == MeshRendererType.SkinnedMeshRenderer)
                {
                    sourceSkinnedRenderer = sourceObject.GetComponent<SkinnedMeshRenderer>();
                }
                else
                {
                    sourceMeshRenderer = sourceObject.GetComponent<MeshRenderer>();
                }

                result.OriginalVertexCount = originalMesh.vertexCount;
                result.OriginalTriangleCount = GetTriangleCount(originalMesh);
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
                var lodGroupObject = new GameObject($"{sourceObject.name}_LODGroup")
                {
                    transform =
                    {
                        position = originalTransform.position,
                        rotation = originalTransform.rotation,
                        localScale = Vector3.one
                    }
                };
                lodGroupObject.transform.SetParent(originalTransform.parent, true);

                var lodGroup = lodGroupObject.AddComponent<LODGroup>();

                // Determine total LOD count (including optional culled level)
                var totalLODCount = settings.includeCulledLevel ? settings.lodLevelCount + 1 : settings.lodLevelCount;
                var lods = new LOD[totalLODCount];

                var simplificationOptions = settings.CreateSimplificationOptions();

                // Generate each LOD level
                for (var i = 0; i < settings.lodLevelCount; i++)
                {
                    var quality = settings.GetQualityFactor(i);
                    var screenHeight = settings.GetScreenTransitionHeight(i);

                    Mesh lodMesh;
                    if (i == 0)
                    {
                        // LOD0 uses original mesh
                        lodMesh = originalMesh;
                    }
                    else
                    {
                        // Simplify mesh for this LOD level
                        lodMesh = SimplifyMesh(originalMesh, quality, simplificationOptions);
                        lodMesh.name = $"{originalMesh.name}_LOD{i}";
                    }

                    result.LODVertexCounts[i] = lodMesh.vertexCount;
                    result.LODTriangleCounts[i] = GetTriangleCount(lodMesh);
                    result.GeneratedMeshes[i] = lodMesh;

                    // Save mesh to assets if requested
                    if (saveMeshesToAssets && i > 0)
                    {
                        var savedPath = SaveMeshAsset(lodMesh, meshSavePath, $"{sourceObject.name}_LOD{i}");
                        result.SavedMeshPaths[i] = savedPath;

                        // Reload the saved mesh
                        if (!string.IsNullOrEmpty(savedPath))
                        {
                            lodMesh = AssetDatabase.LoadAssetAtPath<Mesh>(savedPath);
                        }
                    }

                    // Create LOD GameObject based on renderer type
                    Renderer lodRenderer;

                    if (rendererType == MeshRendererType.SkinnedMeshRenderer)
                    {
                        lodRenderer = CreateSkinnedLODObject(
                            sourceObject,
                            sourceSkinnedRenderer,
                            lodGroupObject.transform,
                            lodMesh,
                            originalMaterials,
                            i);
                    }
                    else
                    {
                        lodRenderer = CreateStaticLODObject(
                            sourceObject.name,
                            lodGroupObject.transform,
                            originalTransform,
                            lodMesh,
                            originalMaterials,
                            i,
                            sourceMeshRenderer);
                    }

                    lods[i] = new LOD(screenHeight, new[] { lodRenderer });
                }

                // Add culled level if enabled
                if (settings.includeCulledLevel)
                {
                    lods[settings.lodLevelCount] = new LOD(settings.culledTransitionHeight, Array.Empty<Renderer>());
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

        private static Renderer CreateStaticLODObject(
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
                CopyRendererSettings(sourceRenderer, lodMeshRenderer);
            }

            return lodMeshRenderer;
        }

        private static Renderer CreateSkinnedLODObject(
            GameObject sourceObject,
            SkinnedMeshRenderer sourceRenderer,
            Transform parent,
            Mesh mesh,
            Material[] materials,
            int lodIndex)
        {
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
            CopyRendererSettings(sourceRenderer, lodRenderer);
            lodRenderer.updateWhenOffscreen = sourceRenderer.updateWhenOffscreen;

            return lodRenderer;
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
            string meshSavePath = null,
            LODGeneratorSettings settings = null)
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
                var originalTransform = sourceObject.transform;

                if (!TryGetMeshData(sourceObject, rendererType, out var originalMesh, out var originalMaterials))
                {
                    result.ErrorMessage = $"'{sourceObject.name}' does not have a valid mesh assigned.";
                    return result;
                }

                result.OriginalVertexCount = originalMesh.vertexCount;
                result.OriginalTriangleCount = GetTriangleCount(originalMesh);

                // Simplify mesh
                var simplifiedMesh = SimplifyMesh(originalMesh, quality, settings?.CreateSimplificationOptions());
                simplifiedMesh.name = $"{originalMesh.name}{suffix}";

                result.LODVertexCounts = new[] { simplifiedMesh.vertexCount };
                result.LODTriangleCounts = new[] { GetTriangleCount(simplifiedMesh) };
                result.GeneratedMeshes = new[] { simplifiedMesh };

                // Save mesh to assets if requested
                if (saveMeshToAssets)
                {
                    if (string.IsNullOrEmpty(meshSavePath))
                    {
                        meshSavePath = DefaultMeshSaveFolder;
                    }
                    EnsureDirectoryExists(meshSavePath);

                    var savedPath = SaveMeshAsset(simplifiedMesh, meshSavePath, $"{sourceObject.name}{suffix}");
                    result.SavedMeshPaths = new[] { savedPath };

                    // Reload the saved mesh
                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        simplifiedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(savedPath);
                    }
                }

                // Create simplified GameObject
                var simplifiedObject = new GameObject($"{sourceObject.name}{suffix}")
                {
                    transform =
                    {
                        position = originalTransform.position,
                        rotation = originalTransform.rotation,
                        localScale = originalTransform.localScale
                    }
                };

                if (rendererType == MeshRendererType.SkinnedMeshRenderer)
                {
                    var sourceRenderer = sourceObject.GetComponent<SkinnedMeshRenderer>();
                    var newRenderer = simplifiedObject.AddComponent<SkinnedMeshRenderer>();
                    newRenderer.sharedMesh = simplifiedMesh;
                    newRenderer.sharedMaterials = originalMaterials;
                    newRenderer.bones = sourceRenderer.bones;
                    newRenderer.rootBone = sourceRenderer.rootBone;
                    newRenderer.quality = sourceRenderer.quality;
                    CopyRendererSettings(sourceRenderer, newRenderer);
                    newRenderer.updateWhenOffscreen = sourceRenderer.updateWhenOffscreen;
                }
                else
                {
                    var sourceRenderer = sourceObject.GetComponent<MeshRenderer>();
                    var newMeshFilter = simplifiedObject.AddComponent<MeshFilter>();
                    newMeshFilter.sharedMesh = simplifiedMesh;

                    var newMeshRenderer = simplifiedObject.AddComponent<MeshRenderer>();
                    newMeshRenderer.sharedMaterials = originalMaterials;
                    CopyRendererSettings(sourceRenderer, newMeshRenderer);
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
        public static Mesh SimplifyMesh(Mesh sourceMesh, float quality, SimplificationOptions? options = null)
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
            if (options.HasValue)
                meshSimplifier.SimplificationOptions = options.Value;
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
            var successCount = 0;
            var failureCount = 0;

            for (var i = 0; i < sourceObjects.Length; i++)
            {
                var obj = sourceObjects[i];
                var progress = (float)i / sourceObjects.Length;
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
        /// Checks if an LOD group has any unsaved (in-memory) meshes that would be lost when converting to prefab.
        /// </summary>
        /// <param name="lodGroupObject">The LOD group GameObject to check.</param>
        /// <returns>True if there are unsaved meshes.</returns>
        public static bool HasUnsavedMeshes(GameObject lodGroupObject)
        {
            if (lodGroupObject == null) return false;

            var lodGroup = lodGroupObject.GetComponent<LODGroup>();
            if (lodGroup == null) return false;

            var lods = lodGroup.GetLODs();
            foreach (var lod in lods)
            {
                foreach (var renderer in lod.renderers)
                {
                    if (renderer == null) continue;

                    Mesh mesh = null;
                    if (renderer is SkinnedMeshRenderer skinnedRenderer)
                    {
                        mesh = skinnedRenderer.sharedMesh;
                    }
                    else if (renderer.TryGetComponent<MeshFilter>(out var meshFilter))
                    {
                        mesh = meshFilter.sharedMesh;
                    }

                    if (mesh != null && !AssetDatabase.Contains(mesh))
                    {
                        return true; // Mesh exists in memory but not as an asset
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Saves all unsaved meshes from an LOD group to asset files.
        /// Useful for retroactively saving meshes when converting to prefab.
        /// </summary>
        /// <param name="lodGroupObject">The LOD group GameObject.</param>
        /// <param name="folderPath">The folder path to save to.</param>
        /// <returns>Array of saved asset paths.</returns>
        public static string[] SaveLODMeshesToAssets(GameObject lodGroupObject, string folderPath = null)
        {
            if (lodGroupObject == null)
            {
                Debug.LogError("[Auto LOD] Cannot save meshes from null object.");
                return Array.Empty<string>();
            }

            var lodGroup = lodGroupObject.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                Debug.LogError("[Auto LOD] Object does not have an LODGroup component.");
                return Array.Empty<string>();
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = DefaultMeshSaveFolder;
            }

            EnsureDirectoryExists(folderPath);

            var savedPaths = new List<string>();
            var lods = lodGroup.GetLODs();
            var baseName = lodGroupObject.name.Replace("_LODGroup", "");

            for (var lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                var lod = lods[lodIndex];
                foreach (var renderer in lod.renderers)
                {
                    if (renderer == null) continue;

                    Mesh mesh = null;
                    bool isSkinned = false;
                    MeshFilter meshFilter = null;
                    
                    if (renderer is SkinnedMeshRenderer skinnedRenderer)
                    {
                        mesh = skinnedRenderer.sharedMesh;
                        isSkinned = true;
                    }
                    else if (renderer.TryGetComponent<MeshFilter>(out meshFilter))
                    {
                        mesh = meshFilter.sharedMesh;
                    }

                    if (mesh != null && !AssetDatabase.Contains(mesh))
                    {
                        // This is an unsaved mesh - save it
                        var meshName = $"{baseName}_LOD{lodIndex}";
                        var savedPath = SaveMeshAsset(mesh, folderPath, meshName);
                        
                        if (!string.IsNullOrEmpty(savedPath))
                        {
                            savedPaths.Add(savedPath);
                            
                            // Reload the saved mesh and assign it back
                            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(savedPath);
                            if (savedMesh != null)
                            {
                                if (isSkinned)
                                {
                                    ((SkinnedMeshRenderer)renderer).sharedMesh = savedMesh;
                                }
                                else
                                {
                                    meshFilter.sharedMesh = savedMesh;
                                }
                            }
                        }
                    }
                }
            }

            if (savedPaths.Count > 0)
            {
                Debug.Log($"[Auto LOD] Saved {savedPaths.Count} meshes to assets in '{folderPath}'.");
            }
            else
            {
                Debug.Log("[Auto LOD] No unsaved meshes found - all meshes are already assets.");
            }

            return savedPaths.ToArray();
        }

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
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    meshName = meshName.Replace(c, '_');
                }

                var assetPath = $"{folderPath}/{meshName}.asset";

                // Handle duplicates
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                // Save the mesh directly (meshes passed in are always newly created)
                mesh.name = meshName;
                AssetDatabase.CreateAsset(mesh, assetPath);
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
                return Array.Empty<string>();
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = DefaultMeshSaveFolder;
            }

            var paths = new List<string>();
            var baseName = result.SourceObject != null ? result.SourceObject.name : "Mesh";

            for (var i = 0; i < result.GeneratedMeshes.Length; i++)
            {
                var mesh = result.GeneratedMeshes[i];
                if (mesh != null && i > 0) // Skip LOD0 as it's the original mesh
                {
                    var path = SaveMeshAsset(mesh, folderPath, $"{baseName}_LOD{i}");
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

            return mesh == null
                ? (-1, -1, MeshRendererType.None)
                : (mesh.vertexCount, GetTriangleCount(mesh), rendererType);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Gets the triangle count of a mesh without allocating a copy of the index buffer.
        /// </summary>
        private static int GetTriangleCount(Mesh mesh)
        {
            var indexCount = 0;
            for (var i = 0; i < mesh.subMeshCount; i++)
                indexCount += (int)mesh.GetIndexCount(i);
            return indexCount / 3;
        }

        /// <summary>
        /// Retrieves mesh and materials from a GameObject based on renderer type.
        /// </summary>
        private static bool TryGetMeshData(GameObject gameObject, MeshRendererType rendererType,
            out Mesh mesh, out Material[] materials)
        {
            mesh = null;
            materials = null;

            if (rendererType == MeshRendererType.SkinnedMeshRenderer)
            {
                var skinnedRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
                mesh = skinnedRenderer.sharedMesh;
                materials = skinnedRenderer.sharedMaterials;
            }
            else
            {
                var meshFilter = gameObject.GetComponent<MeshFilter>();
                var meshRenderer = gameObject.GetComponent<MeshRenderer>();
                mesh = meshFilter.sharedMesh;
                materials = meshRenderer.sharedMaterials;
            }

            return mesh != null;
        }

        /// <summary>
        /// Copies common renderer settings from one renderer to another.
        /// </summary>
        private static void CopyRendererSettings(Renderer source, Renderer destination)
        {
            destination.shadowCastingMode = source.shadowCastingMode;
            destination.receiveShadows = source.receiveShadows;
            destination.lightProbeUsage = source.lightProbeUsage;
            destination.reflectionProbeUsage = source.reflectionProbeUsage;
        }

        #endregion
    }
}
