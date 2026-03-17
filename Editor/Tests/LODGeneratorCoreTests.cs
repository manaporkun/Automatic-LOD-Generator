using NUnit.Framework;
using UnityEngine;

namespace Plugins.AutoLODGenerator.Editor.Tests
{
    /// <summary>
    /// Unit tests for LODGeneratorCore functionality.
    /// </summary>
    public class LODGeneratorCoreTests
    {
        [Test]
        public void EstimateVertexCount_WithFullQuality_ReturnsOriginalCount()
        {
            const int originalCount = 1000;
            var estimated = LODGeneratorCore.EstimateVertexCount(originalCount, 1.0f);
            
            Assert.AreEqual(originalCount, estimated);
        }

        [Test]
        public void EstimateVertexCount_WithHalfQuality_ReturnsHalfCount()
        {
            const int originalCount = 1000;
            var estimated = LODGeneratorCore.EstimateVertexCount(originalCount, 0.5f);
            
            Assert.AreEqual(500, estimated);
        }

        [Test]
        public void EstimateVertexCount_WithZeroQuality_ReturnsMinimumCount()
        {
            const int originalCount = 1000;
            var estimated = LODGeneratorCore.EstimateVertexCount(originalCount, 0f);
            
            // Should return at least 3 vertices (minimum for a triangle)
            Assert.GreaterOrEqual(estimated, 3);
        }

        [Test]
        public void GetMeshRendererType_WithNullObject_ReturnsNone()
        {
            var result = LODGeneratorCore.GetMeshRendererType(null);
            
            Assert.AreEqual(MeshRendererType.None, result);
        }

        [Test]
        public void ValidateForLODGeneration_WithNullObject_ReturnsFalse()
        {
            var isValid = LODGeneratorCore.ValidateForLODGeneration(null, out var errorMessage);
            
            Assert.IsFalse(isValid);
            Assert.IsNotNull(errorMessage);
            StringAssert.Contains("null", errorMessage.ToLower());
        }

        [Test]
        public void ValidateForLODGeneration_WithEmptyGameObject_ReturnsFalse()
        {
            var go = new GameObject("TestEmpty");
            
            var isValid = LODGeneratorCore.ValidateForLODGeneration(go, out var errorMessage);
            
            Assert.IsFalse(isValid);
            Assert.IsNotNull(errorMessage);
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ValidateForLODGeneration_WithMeshFilter_ReturnsTrue()
        {
            var go = new GameObject("TestMesh");
            go.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            go.AddComponent<MeshRenderer>();
            
            var isValid = LODGeneratorCore.ValidateForLODGeneration(go, out var errorMessage);
            
            Assert.IsTrue(isValid);
            Assert.IsNull(errorMessage);
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ValidateForLODGeneration_WithSkinnedMeshRenderer_ReturnsTrue()
        {
            var go = new GameObject("TestSkinnedMesh");
            var skinnedRenderer = go.AddComponent<SkinnedMeshRenderer>();
            skinnedRenderer.sharedMesh = new Mesh();
            
            var isValid = LODGeneratorCore.ValidateForLODGeneration(go, out var errorMessage);
            
            Assert.IsTrue(isValid);
            Assert.IsNull(errorMessage);
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GetMeshStatistics_WithNullObject_ReturnsNegativeValues()
        {
            var (vertices, triangles, type) = LODGeneratorCore.GetMeshStatistics(null);
            
            Assert.AreEqual(-1, vertices);
            Assert.AreEqual(-1, triangles);
            Assert.AreEqual(MeshRendererType.None, type);
        }

        [Test]
        public void GetMeshStatistics_WithValidMesh_ReturnsCorrectValues()
        {
            var mesh = new Mesh();
            mesh.vertices = new Vector3[100];
            mesh.triangles = new int[99]; // 33 triangles
            
            var go = new GameObject("TestStats");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            
            var (vertices, triangles, type) = LODGeneratorCore.GetMeshStatistics(go);
            
            Assert.AreEqual(100, vertices);
            Assert.AreEqual(33, triangles);
            Assert.AreEqual(MeshRendererType.MeshRenderer, type);
            
            Object.DestroyImmediate(go);
        }
    }

    /// <summary>
    /// Tests for result data classes.
    /// </summary>
    public class LODGenerationResultTests
    {
        [Test]
        public void GetTotalReduction_WithValidData_ReturnsCorrectPercentage()
        {
            var result = new LODGenerationResult
            {
                OriginalVertexCount = 1000,
                LODVertexCounts = new[] { 1000, 500, 250 }
            };
            
            var reduction = result.GetTotalReduction();
            
            // 1000 -> 250 is 75% reduction
            Assert.AreEqual(0.75f, reduction, 0.001f);
        }

        [Test]
        public void GetTotalReduction_WithZeroOriginal_ReturnsZero()
        {
            var result = new LODGenerationResult
            {
                OriginalVertexCount = 0,
                LODVertexCounts = new[] { 0 }
            };
            
            var reduction = result.GetTotalReduction();
            
            Assert.AreEqual(0f, reduction);
        }

        [Test]
        public void GetTotalReduction_WithNullLODCounts_ReturnsZero()
        {
            var result = new LODGenerationResult
            {
                OriginalVertexCount = 1000,
                LODVertexCounts = null
            };
            
            var reduction = result.GetTotalReduction();
            
            Assert.AreEqual(0f, reduction);
        }
    }

    /// <summary>
    /// Tests for batch processing results.
    /// </summary>
    public class BatchProcessingResultTests
    {
        [Test]
        public void AllSucceeded_WithNoFailures_ReturnsTrue()
        {
            var result = new BatchProcessingResult
            {
                TotalObjects = 3,
                SuccessCount = 3,
                FailureCount = 0
            };
            
            Assert.IsTrue(result.AllSucceeded);
        }

        [Test]
        public void AllSucceeded_WithFailures_ReturnsFalse()
        {
            var result = new BatchProcessingResult
            {
                TotalObjects = 3,
                SuccessCount = 2,
                FailureCount = 1
            };
            
            Assert.IsFalse(result.AllSucceeded);
        }
    }
}
