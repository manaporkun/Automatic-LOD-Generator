using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Plugins.Auto_LOD_Generator.Editor
{
    public class MeshSimplify : MonoBehaviour
    {
        public static void Simplify([NotNull] GameObject originalObject, float qualityFactor, string name)
        {
            if (originalObject == null) throw new ArgumentNullException(nameof(originalObject));
            var originalMesh = originalObject.GetComponent<MeshFilter>().sharedMesh;
            var originalMaterial = originalObject.GetComponent<MeshRenderer>().sharedMaterial;
            var originalTransform = originalObject.transform;
            
            var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
            meshSimplifier.Initialize(originalMesh);
            meshSimplifier.SimplifyMesh(qualityFactor);
            
            var destMesh = meshSimplifier.ToMesh();
            
            var newGameObj = new GameObject(originalObject.name + name);
            newGameObj.AddComponent<MeshFilter>();
            newGameObj.AddComponent<MeshRenderer>();
            
            newGameObj.GetComponent<MeshFilter>().sharedMesh = destMesh;
            newGameObj.GetComponent<MeshRenderer>().sharedMaterial = originalMaterial;
            
            newGameObj.transform.position = originalTransform.position;
            newGameObj.transform.rotation = originalTransform.rotation;
            newGameObj.transform.localScale = originalTransform.localScale;

        }
    }
}
