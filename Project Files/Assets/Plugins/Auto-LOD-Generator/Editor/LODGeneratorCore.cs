using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityMeshSimplifier;
using Debug = UnityEngine.Debug;

namespace Plugins.AutoLODGenerator.Editor
{
    /// <summary>
    /// Core LOD generation functionality.
    /// Handles mesh simplification and LOD group creation.
    /// </summary>
    public static class LODGeneratorCore
    {
        /// <summary>
        /// Generates a LOD group for a single GameObject.
        /// </summary>
        /// <param name="sourceObject">The source GameObject with a MeshFilter.</param>
        /// <param name="settings">LOD generation settings.</param>
        /// <returns>Result containing the generated LOD group and statistics.</returns>
        public static LODGenerationResult GenerateLODGroup(GameObject sourceObject, LODGeneratorSettings settings)
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

                var meshFilter = sourceObject.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    result.ErrorMessage = $"'{sourceObject.name}' does not have a valid MeshFilter with a mesh.";
                    return result;
                }

                var meshRenderer = sourceObject.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    result.ErrorMessage = $"'{sourceObject.name}' does not have a MeshRenderer component.";
                    return result;
                }

                settings.Validate();

                var originalMesh = meshFilter.sharedMesh;
                var originalMaterials = meshRenderer.sharedMaterials;
                var originalTransform = sourceObject.transform;

                result.OriginalVertexCount = originalMesh.vertexCount;
                result.OriginalTriangleCount = originalMesh.triangles.Length / 3;
                result.LODVertexCounts = new int[settings.lodLevelCount];
                result.LODTriangleCounts = new int[settings.lodLevelCount];

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

                    // Create LOD GameObject
                    var lodObject = new GameObject($"{sourceObject.name}_LOD{i}");
                    lodObject.transform.SetParent(lodGroupObject.transform);
                    lodObject.transform.localPosition = Vector3.zero;
                    lodObject.transform.localRotation = Quaternion.identity;
                    lodObject.transform.localScale = originalTransform.localScale;

                    var lodMeshFilter = lodObject.AddComponent<MeshFilter>();
                    lodMeshFilter.sharedMesh = lodMesh;

                    var lodMeshRenderer = lodObject.AddComponent<MeshRenderer>();
                    lodMeshRenderer.sharedMaterials = originalMaterials;

                    // Copy additional renderer settings
                    lodMeshRenderer.shadowCastingMode = meshRenderer.shadowCastingMode;
                    lodMeshRenderer.receiveShadows = meshRenderer.receiveShadows;

                    lods[i] = new LOD(screenHeight, new Renderer[] { lodMeshRenderer });
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

        /// <summary>
        /// Generates a simplified version of a mesh.
        /// </summary>
        /// <param name="sourceObject">The source GameObject with a MeshFilter.</param>
        /// <param name="quality">Quality factor (0.0 to 1.0).</param>
        /// <param name="suffix">Suffix to append to the object name.</param>
        /// <returns>Result containing the simplified mesh object and statistics.</returns>
        public static LODGenerationResult GenerateSimplifiedMesh(GameObject sourceObject, float quality, string suffix = "_Simplified")
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

                var meshFilter = sourceObject.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    result.ErrorMessage = $"'{sourceObject.name}' does not have a valid MeshFilter with a mesh.";
                    return result;
                }

                var meshRenderer = sourceObject.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    result.ErrorMessage = $"'{sourceObject.name}' does not have a MeshRenderer component.";
                    return result;
                }

                quality = Mathf.Clamp01(quality);

                var originalMesh = meshFilter.sharedMesh;
                var originalMaterials = meshRenderer.sharedMaterials;
                var originalTransform = sourceObject.transform;

                result.OriginalVertexCount = originalMesh.vertexCount;
                result.OriginalTriangleCount = originalMesh.triangles.Length / 3;

                // Simplify mesh
                var simplifiedMesh = SimplifyMesh(originalMesh, quality);
                simplifiedMesh.name = $"{originalMesh.name}{suffix}";

                result.LODVertexCounts = new[] { simplifiedMesh.vertexCount };
                result.LODTriangleCounts = new[] { simplifiedMesh.triangles.Length / 3 };

                // Create simplified GameObject
                var simplifiedObject = new GameObject($"{sourceObject.name}{suffix}");
                simplifiedObject.transform.position = originalTransform.position;
                simplifiedObject.transform.rotation = originalTransform.rotation;
                simplifiedObject.transform.localScale = originalTransform.localScale;

                var newMeshFilter = simplifiedObject.AddComponent<MeshFilter>();
                newMeshFilter.sharedMesh = simplifiedMesh;

                var newMeshRenderer = simplifiedObject.AddComponent<MeshRenderer>();
                newMeshRenderer.sharedMaterials = originalMaterials;
                newMeshRenderer.shadowCastingMode = meshRenderer.shadowCastingMode;
                newMeshRenderer.receiveShadows = meshRenderer.receiveShadows;

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
        /// Processes multiple GameObjects in batch.
        /// </summary>
        /// <param name="sourceObjects">Array of source GameObjects.</param>
        /// <param name="settings">LOD generation settings.</param>
        /// <param name="progressCallback">Optional callback for progress updates (0.0 to 1.0).</param>
        /// <returns>Batch processing result with statistics.</returns>
        public static BatchProcessingResult ProcessBatch(
            GameObject[] sourceObjects,
            LODGeneratorSettings settings,
            Action<float, string> progressCallback = null)
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

                var result = GenerateLODGroup(obj, settings);
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
                return UnityEngine.Object.Instantiate(sourceMesh);
            }

            var meshSimplifier = new MeshSimplifier();
            meshSimplifier.Initialize(sourceMesh);
            meshSimplifier.SimplifyMesh(quality);

            return meshSimplifier.ToMesh();
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
        /// Validates if a GameObject is suitable for LOD generation.
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

            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                errorMessage = $"'{gameObject.name}' does not have a MeshFilter component.";
                return false;
            }

            if (meshFilter.sharedMesh == null)
            {
                errorMessage = $"'{gameObject.name}' MeshFilter does not have a mesh assigned.";
                return false;
            }

            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                errorMessage = $"'{gameObject.name}' does not have a MeshRenderer component.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets mesh statistics for a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to analyze.</param>
        /// <returns>Tuple of (vertexCount, triangleCount) or (-1, -1) if invalid.</returns>
        public static (int vertices, int triangles) GetMeshStatistics(GameObject gameObject)
        {
            if (gameObject == null)
                return (-1, -1);

            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return (-1, -1);

            var mesh = meshFilter.sharedMesh;
            return (mesh.vertexCount, mesh.triangles.Length / 3);
        }
    }
}
