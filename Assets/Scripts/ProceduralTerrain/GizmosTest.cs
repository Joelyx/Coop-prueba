using UnityEngine;

/// <summary>
/// Script simple para verificar que no hay errores de NullReference en OnDrawGizmos
/// </summary>
public class GizmosTest : MonoBehaviour
{
    [Header("Test de Gizmos")]
    public bool enableGizmosTest = true;
    
    [ContextMenu("Test All Gizmos")]
    public void TestAllGizmos()
    {
        Debug.Log("=== TEST DE GIZMOS ===");
        
        // Buscar todos los componentes que tienen OnDrawGizmos
        BiomeManager[] biomeManagers = FindObjectsByType<BiomeManager>(FindObjectsSortMode.None);
        ChunkManager[] chunkManagers = FindObjectsByType<ChunkManager>(FindObjectsSortMode.None);
        CollisionManager[] collisionManagers = FindObjectsByType<CollisionManager>(FindObjectsSortMode.None);
        NavigationManager[] navigationManagers = FindObjectsByType<NavigationManager>(FindObjectsSortMode.None);
        WorldBoundarySystem[] boundarySystem = FindObjectsByType<WorldBoundarySystem>(FindObjectsSortMode.None);
        TerrainTestSuite[] testSuites = FindObjectsByType<TerrainTestSuite>(FindObjectsSortMode.None);
        
        Debug.Log($"Encontrados en escena:");
        Debug.Log($"- BiomeManager: {biomeManagers.Length}");
        Debug.Log($"- ChunkManager: {chunkManagers.Length}");
        Debug.Log($"- CollisionManager: {collisionManagers.Length}");
        Debug.Log($"- NavigationManager: {navigationManagers.Length}");
        Debug.Log($"- WorldBoundarySystem: {boundarySystem.Length}");
        Debug.Log($"- TerrainTestSuite: {testSuites.Length}");
        
        // Verificar configuraciones
        foreach (BiomeManager bm in biomeManagers)
        {
            if (bm.biomeSettings == null)
                Debug.LogWarning($"BiomeManager en {bm.gameObject.name} no tiene biomeSettings asignado");
        }
        
        foreach (ChunkManager cm in chunkManagers)
        {
            if (cm.terrainSettings == null)
                Debug.LogWarning($"ChunkManager en {cm.gameObject.name} no tiene terrainSettings asignado");
        }
        
        foreach (WorldBoundarySystem ws in boundarySystem)
        {
            if (ws.terrainSettings == null)
                Debug.LogWarning($"WorldBoundarySystem en {ws.gameObject.name} no tiene terrainSettings asignado");
        }
        
        Debug.Log("✅ Test de Gizmos completado - revisa Scene View para verificar");
        Debug.Log("=== FIN TEST DE GIZMOS ===");
    }
    
    private void OnDrawGizmos()
    {
        if (!enableGizmosTest) return;
        
        // Dibujar un indicador simple para este GameObject
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        
        // Dibujar texto indicativo
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, "Gizmos Test");
        #endif
    }
}