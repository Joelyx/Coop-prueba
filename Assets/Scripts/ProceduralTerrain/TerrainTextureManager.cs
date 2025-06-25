using UnityEngine;

/// <summary>
/// Gestor de texturas para terreno procedural con blending automático
/// </summary>
public class TerrainTextureManager : MonoBehaviour
{
    [Header("Texturas del Terreno")]
    public Texture2D sandTexture;
    public Texture2D rockTexture;
    public Texture2D coralTexture;
    public Texture2D deepSeaTexture;
    
    [Header("Normal Maps")]
    public Texture2D sandNormal;
    public Texture2D rockNormal;
    public Texture2D coralNormal;
    public Texture2D deepSeaNormal;
    
    [Header("Configuración de Blending")]
    [Range(0f, 100f)]
    public float textureScale = 50f;
    
    [Range(0f, 1f)]
    public float blendStrength = 0.5f;
    
    [Header("Altura para Texturas")]
    [Range(0f, 1f)]
    public float sandHeightMax = 0.3f;
    
    [Range(0f, 1f)]
    public float rockHeightMin = 0.4f;
    
    [Range(0f, 1f)]
    public float coralHeightRange = 0.6f;
    
    private Material terrainMaterial;
    private TerrainGenerator terrainGenerator;
    
    private void Start()
    {
        terrainGenerator = FindFirstObjectByType<TerrainGenerator>();
        CreateTerrainMaterial();
    }
    
    /// <summary>
    /// Crea material con multiple texturas para el terreno
    /// </summary>
    private void CreateTerrainMaterial()
    {
        // Crear shader personalizado o usar shader estándar con blending
        terrainMaterial = new Material(Shader.Find("Standard"));
        
        // Configurar textura principal (sand por defecto)
        if (sandTexture != null)
        {
            terrainMaterial.mainTexture = sandTexture;
            terrainMaterial.mainTextureScale = Vector2.one * textureScale;
        }
        
        if (sandNormal != null)
        {
            terrainMaterial.SetTexture("_BumpMap", sandNormal);
        }
        
        // Aplicar a chunks existentes
        ApplyMaterialToChunks();
    }
    
    /// <summary>
    /// Aplica material a todos los chunks del terreno
    /// </summary>
    private void ApplyMaterialToChunks()
    {
        ChunkManager chunkManager = FindFirstObjectByType<ChunkManager>();
        if (chunkManager == null) return;
        
        // Buscar todos los chunks en la escena
        TerrainChunk[] chunks = FindObjectsByType<TerrainChunk>(FindObjectsSortMode.None);
        
        foreach (TerrainChunk chunk in chunks)
        {
            if (chunk.meshRenderer != null)
            {
                chunk.meshRenderer.material = terrainMaterial;
            }
        }
        
        Debug.Log($"[TerrainTextureManager] Material aplicado a {chunks.Length} chunks");
    }
    
    /// <summary>
    /// Crea textura blended basada en altura y bioma
    /// </summary>
    public Texture2D CreateBlendedTexture(float[,] heightMap, int[,] biomeMap, int width, int height)
    {
        Texture2D blendedTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedHeight = heightMap[x, y] / 100f; // Normalizar altura
                int biomeIndex = biomeMap != null ? biomeMap[x, y] : 0;
                
                Color pixelColor = GetColorForHeightAndBiome(normalizedHeight, biomeIndex);
                blendedTexture.SetPixel(x, y, pixelColor);
            }
        }
        
        blendedTexture.Apply();
        return blendedTexture;
    }
    
    private Color GetColorForHeightAndBiome(float height, int biomeIndex)
    {
        Color baseColor = Color.white;
        
        // Color basado en altura
        if (height <= sandHeightMax)
        {
            // Arena - zonas bajas
            baseColor = new Color(0.96f, 0.87f, 0.70f); // Color arena
        }
        else if (height >= rockHeightMin)
        {
            // Rocas - zonas altas  
            baseColor = new Color(0.5f, 0.4f, 0.3f); // Color roca
        }
        else
        {
            // Transición
            float t = (height - sandHeightMax) / (rockHeightMin - sandHeightMax);
            baseColor = Color.Lerp(new Color(0.96f, 0.87f, 0.70f), new Color(0.5f, 0.4f, 0.3f), t);
        }
        
        // Modificar por bioma
        switch (biomeIndex)
        {
            case 0: // Llanura Abisal - azulado
                baseColor = Color.Lerp(baseColor, new Color(0.2f, 0.3f, 0.8f), 0.3f);
                break;
            case 1: // Formación Rocosa - más gris
                baseColor = Color.Lerp(baseColor, Color.gray, 0.4f);
                break;
            case 2: // Cañón - más oscuro
                baseColor = Color.Lerp(baseColor, new Color(0.1f, 0.1f, 0.2f), 0.5f);
                break;
            case 3: // Arrecife - rojizo/rosa
                baseColor = Color.Lerp(baseColor, new Color(0.8f, 0.3f, 0.5f), 0.3f);
                break;
        }
        
        return baseColor;
    }
    
    /// <summary>
    /// Actualiza textura cuando cambia el terreno
    /// </summary>
    public void UpdateTerrainTexture()
    {
        if (terrainGenerator == null || !terrainGenerator.IsWorldGenerated) return;
        
        // Crear nueva textura blended
        Texture2D newTexture = CreateBlendedTexture(
            terrainGenerator.globalHeightMap,
            terrainGenerator.globalBiomeMap,
            terrainGenerator.terrainSettings.worldWidth,
            terrainGenerator.terrainSettings.worldLength
        );
        
        // Aplicar nueva textura
        if (terrainMaterial != null)
        {
            terrainMaterial.mainTexture = newTexture;
            ApplyMaterialToChunks();
        }
        
        Debug.Log("[TerrainTextureManager] Textura actualizada");
    }
    
    /// <summary>
    /// Cambia textura específica por tipo
    /// </summary>
    public void ChangeTexture(TerrainTextureType textureType, Texture2D newTexture, Texture2D newNormal = null)
    {
        switch (textureType)
        {
            case TerrainTextureType.Sand:
                sandTexture = newTexture;
                sandNormal = newNormal;
                break;
            case TerrainTextureType.Rock:
                rockTexture = newTexture;
                rockNormal = newNormal;
                break;
            case TerrainTextureType.Coral:
                coralTexture = newTexture;
                coralNormal = newNormal;
                break;
            case TerrainTextureType.DeepSea:
                deepSeaTexture = newTexture;
                deepSeaNormal = newNormal;
                break;
        }
        
        // Recrear material
        CreateTerrainMaterial();
        
        Debug.Log($"[TerrainTextureManager] Textura {textureType} cambiada");
    }
    
    // Context menus para testing
    [ContextMenu("Apply Current Material")]
    public void ApplyCurrentMaterial()
    {
        ApplyMaterialToChunks();
    }
    
    [ContextMenu("Update Terrain Texture")]
    public void UpdateTextureFromMenu()
    {
        UpdateTerrainTexture();
    }
    
    [ContextMenu("Create Test Material")]
    public void CreateTestMaterial()
    {
        CreateTerrainMaterial();
    }
}

public enum TerrainTextureType
{
    Sand,
    Rock, 
    Coral,
    DeepSea
}