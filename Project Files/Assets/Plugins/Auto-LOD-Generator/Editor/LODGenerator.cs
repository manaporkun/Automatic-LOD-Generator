using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Plugins.Auto_LOD_Generator.Editor
{
    public class LODGenerator : MonoBehaviour
    {
        public static void Generator([NotNull] GameObject originalObject, float qualityFactor)
        {
            const int count = 4;

            if (originalObject == null) throw new ArgumentNullException(nameof(originalObject));
            var originalMesh = originalObject.GetComponent<MeshFilter>().sharedMesh;
            var originalMaterial = originalObject.GetComponent<MeshRenderer>().sharedMaterial;
            var originalTransform = originalObject.transform;

            var newParent = new GameObject(originalObject.name + " LOD Group");
            newParent.AddComponent<LODGroup>();

            var lods = new LOD[count];

            var qualityFactors = new List<float>()
            {
                1f,
                0.6f,
                0.4f
            };

            for (var i = 0; i < count; i++)
            {
                var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
                meshSimplifier.Initialize(originalMesh);
                meshSimplifier.SimplifyMesh(i == 0 ? 1 : qualityFactor * qualityFactors[i-1]);
            
                var destMesh = meshSimplifier.ToMesh();
                    
                var newGameObj = Instantiate(originalObject, newParent.transform);
                newGameObj.name = originalObject.name + "_LOD" + i;
                
                newGameObj.GetComponent<MeshFilter>().sharedMesh = i == 0 ? originalMesh : destMesh;
                newGameObj.GetComponent<MeshRenderer>().sharedMaterial = originalMaterial;
            
                newGameObj.transform.position = originalTransform.position;
                newGameObj.transform.rotation = originalTransform.rotation;
                newGameObj.transform.localScale = originalTransform.localScale;

                //newGameObj.transform.SetParent(newParent.transform);
                var renderers = new Renderer[1];
                renderers[0] = newGameObj.GetComponent<Renderer>();
                lods[i] = new LOD(0.5F / (i+1), renderers);
            }
            
            newParent.GetComponent<LODGroup>().SetLODs(lods);
            newParent.GetComponent<LODGroup>().RecalculateBounds();
        }
    }
}