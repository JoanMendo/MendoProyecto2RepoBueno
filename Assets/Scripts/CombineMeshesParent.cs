using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CombinemeshesParent : MonoBehaviour
{
    [Tooltip("Desactiva los hijos luego de combinar.")]
    public bool disableChildrenAfterCombine = true;

    void Start()
    {
        CombineMeshes();
    }

    void CombineMeshes()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        List<CombineInstance> combineInstances = new List<CombineInstance>();
        List<Material> collectedMaterials = new List<Material>();

        foreach (var mf in meshFilters)
        {
            if (mf == GetComponent<MeshFilter>())
                continue;

            MeshRenderer childRenderer = mf.GetComponent<MeshRenderer>();
            if (childRenderer == null)
            {
                Debug.LogWarning($"El objeto '{mf.name}' no tiene MeshRenderer.");
                continue;
            }

            Mesh mesh = mf.sharedMesh;
            if (mesh == null)
            {
                Debug.LogWarning($"El objeto '{mf.name}' no tiene mesh asignado.");
                continue;
            }

            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                CombineInstance ci = new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = sub,
                    transform = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix
                };
                combineInstances.Add(ci);

                if (sub < childRenderer.sharedMaterials.Length)
                    collectedMaterials.Add(childRenderer.sharedMaterials[sub]);
                else
                    collectedMaterials.Add(childRenderer.sharedMaterials[0]); // fallback
            }

            if (disableChildrenAfterCombine)
                mf.gameObject.SetActive(false);
        }

        if (combineInstances.Count == 0)
        {
            Debug.LogWarning("No se encontraron submeshes válidas para combinar.");
            return;
        }

        Mesh combinedMesh = new Mesh
        {
            name = "CombinedMesh"
        };
        combinedMesh.CombineMeshes(combineInstances.ToArray(), false, true); // false = mantener submeshes

        GetComponent<MeshFilter>().mesh = combinedMesh;
        GetComponent<MeshRenderer>().materials = collectedMaterials.ToArray();
    }

}
