using UnityEngine;

/// <summary>
/// Herramienta para aplicar materiales a chunks de terreno existentes
/// </summary>
public class MaterialApplier : MonoBehaviour
{
    [Header("Configuración")]
    public BiomeSettings biomeSettings;
    public Material defaultMaterial;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    [ContextMenu("Apply Materials to All Chunks")]
    public void ApplyMaterialsToAllChunks()
    {
        Debug.Log("[MaterialApplier] Iniciando aplicación de materiales...");
        
        // Buscar todos los chunks en la escena
        TerrainChunk[] chunks = FindObjectsByType<TerrainChunk>(FindObjectsSortMode.None);
        
        if (chunks.Length == 0)
        {
            Debug.LogWarning("[MaterialApplier] No se encontraron chunks en la escena");
            return;
        }
        
        TerrainGenerator terrainGen = FindFirstObjectByType<TerrainGenerator>();
        int materialsApplied = 0;
        
        foreach (TerrainChunk chunk in chunks)
        {
            if (chunk.meshRenderer != null)
            {
                Material materialToApply = GetMaterialForChunk(chunk, terrainGen);
                
                if (materialToApply != null)
                {
                    chunk.meshRenderer.material = materialToApply;
                    materialsApplied++;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[MaterialApplier] Material aplicado a chunk {chunk.chunkCoordinate}: {materialToApply.name}");
                    }
                }
                else if (defaultMaterial != null)
                {
                    chunk.meshRenderer.material = defaultMaterial;
                    materialsApplied++;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[MaterialApplier] Material por defecto aplicado a chunk {chunk.chunkCoordinate}");
                    }
                }
            }
        }
        
        Debug.Log($"[MaterialApplier] Materiales aplicados a {materialsApplied} de {chunks.Length} chunks");
    }
    
    private Material GetMaterialForChunk(TerrainChunk chunk, TerrainGenerator terrainGen)
    {
        if (biomeSettings == null || biomeSettings.biomes.Length == 0)
        {
            return defaultMaterial;
        }
        
        // Si tenemos acceso al terreno generado, usar bioma dominante
        if (terrainGen != null && terrainGen.IsWorldGenerated)
        {
            int dominantBiome = GetDominantBiomeForChunk(chunk, terrainGen);
            
            if (dominantBiome >= 0 && dominantBiome < biomeSettings.biomes.Length)
            {
                Material biomeMaterial = biomeSettings.biomes[dominantBiome].terrainMaterial;
                
                if (biomeMaterial != null)
                {
                    return biomeMaterial;
                }
            }
        }
        
        // Por defecto, usar material del primer bioma o default
        if (biomeSettings.biomes[0].terrainMaterial != null)
        {
            return biomeSettings.biomes[0].terrainMaterial;
        }
        
        return defaultMaterial;
    }
    
    private int GetDominantBiomeForChunk(TerrainChunk chunk, TerrainGenerator terrainGen)
    {
        // Muestrear biomas en el área del chunk
        Vector2Int chunkCoord = chunk.chunkCoordinate;
        int chunkSize = chunk.chunkSize;
        
        int[] biomeCounts = new int[biomeSettings.biomes.Length];
        int totalSamples = 0;
        
        // Muestrear cada 8 unidades para optimización
        for (int y = 0; y < chunkSize; y += 8)
        {
            for (int x = 0; x < chunkSize; x += 8)
            {
                int worldX = chunkCoord.x * chunkSize + x;
                int worldY = chunkCoord.y * chunkSize + y;
                
                // Verificar límites
                if (worldX < terrainGen.terrainSettings.worldWidth && 
                    worldY < terrainGen.terrainSettings.worldLength)
                {
                    int biomeIndex = terrainGen.GetBiomeAtWorldPosition(worldX, worldY);
                    
                    if (biomeIndex >= 0 && biomeIndex < biomeCounts.Length)
                    {
                        biomeCounts[biomeIndex]++;
                        totalSamples++;
                    }
                }
            }
        }
        
        // Encontrar bioma dominante
        int dominantBiome = 0;
        int maxCount = biomeCounts[0];
        
        for (int i = 1; i < biomeCounts.Length; i++)
        {
            if (biomeCounts[i] > maxCount)
            {
                maxCount = biomeCounts[i];
                dominantBiome = i;
            }
        }
        
        if (showDebugInfo && totalSamples > 0)
        {
            float percentage = (float)maxCount / totalSamples * 100f;
            string biomeName = biomeSettings.biomes[dominantBiome].biomeName;
            Debug.Log($"[MaterialApplier] Chunk {chunkCoord}: Bioma dominante '{biomeName}' ({percentage:F0}%)");
        }
        
        return dominantBiome;
    }
    
    [ContextMenu("Apply Default Material to All")]
    public void ApplyDefaultMaterialToAll()
    {
        if (defaultMaterial == null)
        {
            Debug.LogError("[MaterialApplier] No hay material por defecto asignado");
            return;
        }
        
        TerrainChunk[] chunks = FindObjectsByType<TerrainChunk>(FindObjectsSortMode.None);
        int applied = 0;
        
        foreach (TerrainChunk chunk in chunks)
        {
            if (chunk.meshRenderer != null)
            {
                chunk.meshRenderer.material = defaultMaterial;
                applied++;
            }
        }
        
        Debug.Log($"[MaterialApplier] Material por defecto aplicado a {applied} chunks");
    }
    
    [ContextMenu("List All Chunk Materials")]
    public void ListAllChunkMaterials()
    {
        TerrainChunk[] chunks = FindObjectsByType<TerrainChunk>(FindObjectsSortMode.None);
        
        Debug.Log($"[MaterialApplier] Listando materiales de {chunks.Length} chunks:");
        
        foreach (TerrainChunk chunk in chunks)
        {
            if (chunk.meshRenderer != null)
            {
                Material mat = chunk.meshRenderer.material;
                string materialName = mat != null ? mat.name : "NULL";
                Debug.Log($"- Chunk {chunk.chunkCoordinate}: {materialName}");
            }
            else
            {
                Debug.Log($"- Chunk {chunk.chunkCoordinate}: Sin MeshRenderer");
            }
        }
    }
    
    [ContextMenu("Force Refresh All Chunks")]
    public void ForceRefreshAllChunks()
    {
        Debug.Log("[MaterialApplier] Forzando refresh de todos los chunks...");
        
        // Buscar ChunkManager
        ChunkManager chunkManager = FindFirstObjectByType<ChunkManager>();
        
        if (chunkManager != null)
        {
            Debug.Log("[MaterialApplier] ChunkManager encontrado");
        }
        
        // Aplicar materiales
        ApplyMaterialsToAllChunks();
        
        Debug.Log("[MaterialApplier] Refresh completado");
    }
}