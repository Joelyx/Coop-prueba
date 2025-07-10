using UnityEngine;

/// <summary>
/// Arregla automáticamente los MeshColliders cóncavos del submarino convirtiéndolos a convexos
/// </summary>
public class SubmarineMeshColliderFixer : MonoBehaviour
{
    [Header("Auto Fix Settings")]
    public bool autoFixOnStart = true;
    public bool debugMode = true;
    
    [Header("Replacement Settings")]
    public bool replaceWithBoxColliders = true;
    public bool makeConvex = true;
    
    private void Start()
    {
        if (autoFixOnStart)
        {
            FixMeshColliders();
        }
    }
    
    [ContextMenu("Fix Mesh Colliders")]
    public void FixMeshColliders()
    {
        // Buscar todos los MeshColliders en el submarino y sus hijos
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>();
        
        int fixedCount = 0;
        
        foreach (MeshCollider meshCollider in meshColliders)
        {
            if (meshCollider != null)
            {
                // Verificar si el objeto tiene un Rigidbody (directamente o en padre)
                Rigidbody rb = meshCollider.GetComponentInParent<Rigidbody>();
                
                if (rb != null && !rb.isKinematic)
                {
                    if (debugMode)
                        Debug.Log($"[MESH COLLIDER FIX] Fixing {meshCollider.gameObject.name}");
                    
                    if (replaceWithBoxColliders)
                    {
                        ReplaceMeshWithBoxCollider(meshCollider);
                    }
                    else if (makeConvex)
                    {
                        meshCollider.convex = true;
                        if (debugMode)
                            Debug.Log($"[MESH COLLIDER FIX] Made convex: {meshCollider.gameObject.name}");
                    }
                    
                    fixedCount++;
                }
            }
        }
        
        if (debugMode)
            Debug.Log($"[MESH COLLIDER FIX] Fixed {fixedCount} mesh colliders");
    }
    
    private void ReplaceMeshWithBoxCollider(MeshCollider meshCollider)
    {
        GameObject obj = meshCollider.gameObject;
        
        // Obtener el bounds del mesh collider antes de destruirlo
        Bounds bounds = meshCollider.bounds;
        
        // Destruir el mesh collider
        DestroyImmediate(meshCollider);
        
        // Añadir BoxCollider
        BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
        
        // Configurar el BoxCollider para que coincida aproximadamente con el mesh
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            boxCollider.size = renderer.bounds.size;
            boxCollider.center = obj.transform.InverseTransformPoint(renderer.bounds.center);
        }
        else
        {
            // Fallback: usar un tamaño por defecto
            boxCollider.size = Vector3.one;
            boxCollider.center = Vector3.zero;
        }
        
        if (debugMode)
            Debug.Log($"[MESH COLLIDER FIX] Replaced MeshCollider with BoxCollider on {obj.name}");
    }
    
    [ContextMenu("Find All Mesh Colliders")]
    public void FindAllMeshColliders()
    {
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>();
        
        Debug.Log("=== MESH COLLIDERS FOUND ===");
        foreach (MeshCollider mc in meshColliders)
        {
            Rigidbody rb = mc.GetComponentInParent<Rigidbody>();
            Debug.Log($"- {mc.gameObject.name}: convex={mc.convex}, hasRigidbody={rb != null}, isKinematic={rb?.isKinematic}");
        }
        Debug.Log($"Total found: {meshColliders.Length}");
    }
    
    [ContextMenu("Remove All Mesh Colliders")]
    public void RemoveAllMeshColliders()
    {
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>();
        
        int removedCount = 0;
        foreach (MeshCollider mc in meshColliders)
        {
            if (mc != null)
            {
                Debug.Log($"[MESH COLLIDER FIX] Removing MeshCollider from {mc.gameObject.name}");
                DestroyImmediate(mc);
                removedCount++;
            }
        }
        
        Debug.Log($"[MESH COLLIDER FIX] Removed {removedCount} mesh colliders");
    }
}