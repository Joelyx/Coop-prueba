using UnityEngine;

public class QuickMaterialApplier : MonoBehaviour
{
    [ContextMenu("Apply Materials Now")]
    public void ApplyMaterialsNow()
    {
        // Buscar todos los chunks
        TerrainChunk[] chunks = FindObjectsByType<TerrainChunk>(FindObjectsSortMode.None);
        
        // Buscar los materiales por GUID
        Material seafloorMat = null;
        Material mountainMat = null;
        
        // Buscar materiales en el proyecto
        Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
        foreach (Material mat in materials)
        {
            if (mat.name == "SeafloorMaterial")
                seafloorMat = mat;
            else if (mat.name == "MountainMaterial")
                mountainMat = mat;
        }
        
        if (seafloorMat == null)
        {
            Debug.LogError("No se encontró SeafloorMaterial");
            return;
        }
        
        int applied = 0;
        foreach (TerrainChunk chunk in chunks)
        {
            if (chunk.meshRenderer != null)
            {
                chunk.meshRenderer.material = seafloorMat;
                applied++;
            }
        }
        
        Debug.Log($"Material aplicado a {applied} chunks de {chunks.Length} totales");
    }
}
