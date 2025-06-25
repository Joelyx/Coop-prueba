using UnityEngine;
using System.Collections;

/// <summary>
/// Clase principal que coordina toda la generación de terreno procedural
/// </summary>
public class TerrainGenerator : MonoBehaviour
{
    [Header("Configuración Principal")]
    public TerrainSettings terrainSettings;
    public BiomeSettings biomeSettings;
    
    [Header("Managers")]
    public ChunkManager chunkManager;
    
    [Header("Generación")]
    public bool generateOnStart = true;
    public bool useWorldBounds = true;
    
    [Header("Debug")]
    public bool showGenerationProgress = true;
    
    // Mapas globales para el mundo completo
    [System.NonSerialized]
    public float[,] globalHeightMap;
    [System.NonSerialized]
    public float[,] globalNavigabilityMap;
    [System.NonSerialized]
    public int[,] globalBiomeMap;
    
    // Estado de generación
    private bool isGenerating = false;
    private bool worldGenerated = false;
    
    // Eventos
    public System.Action OnWorldGenerationStarted;
    public System.Action OnWorldGenerationCompleted;
    public System.Action<float> OnGenerationProgress;
    
    private void Start()
    {
        if (generateOnStart)
        {
            StartCoroutine(GenerateWorld());
        }
    }
    
    /// <summary>
    /// Inicia la generación completa del mundo
    /// </summary>
    public void StartWorldGeneration()
    {
        if (!isGenerating)
        {
            StartCoroutine(GenerateWorld());
        }
    }
    
    private IEnumerator GenerateWorld()
    {
        if (terrainSettings == null)
        {
            Debug.LogError("[TerrainGenerator] TerrainSettings no asignado!");
            yield break;
        }
        
        isGenerating = true;
        OnWorldGenerationStarted?.Invoke();
        
        Debug.Log("[TerrainGenerator] Iniciando generación del mundo...");
        
        // Paso 1: Generar mapa de altura global
        yield return StartCoroutine(GenerateGlobalHeightMap());
        OnGenerationProgress?.Invoke(0.33f);
        
        // Paso 2: Generar mapa de biomas
        yield return StartCoroutine(GenerateGlobalBiomeMap());
        OnGenerationProgress?.Invoke(0.66f);
        
        // Paso 3: Generar mapa de navegabilidad
        yield return StartCoroutine(GenerateGlobalNavigabilityMap());
        OnGenerationProgress?.Invoke(1f);
        
        // Paso 4: Configurar ChunkManager
        if (chunkManager != null)
        {
            chunkManager.terrainSettings = terrainSettings;
        }
        
        worldGenerated = true;
        isGenerating = false;
        
        Debug.Log("[TerrainGenerator] Generación del mundo completada!");
        OnWorldGenerationCompleted?.Invoke();
    }
    
    private IEnumerator GenerateGlobalHeightMap()
    {
        Debug.Log("[TerrainGenerator] Generando mapa de altura global...");
        
        int width = terrainSettings.worldWidth;
        int height = terrainSettings.worldLength;
        
        globalHeightMap = new float[width, height];
        
        // Generar mapa de falloff para bordes naturales si está habilitado
        float[,] falloffMap = null;
        if (useWorldBounds)
        {
            falloffMap = NoiseGenerator.GenerateFalloffMap(width, height);
        }
        
        int processedPoints = 0;
        int totalPoints = width * height;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2(x, y);
                
                // Ruido base del terreno
                float terrainNoise = NoiseGenerator.GenerateNoise(position, terrainSettings.terrainNoise);
                
                // Ruido de detalle si está configurado
                if (terrainSettings.detailNoise != null)
                {
                    float detailNoise = NoiseGenerator.GenerateNoise(position, terrainSettings.detailNoise);
                    terrainNoise = Mathf.Lerp(terrainNoise, detailNoise, 0.3f);
                }
                
                // Aplicar falloff para crear bordes naturales
                if (useWorldBounds && falloffMap != null)
                {
                    terrainNoise = Mathf.Clamp01(terrainNoise - falloffMap[x, y]);
                }
                
                // Aplicar curva de altura
                float finalHeight = terrainSettings.terrainNoise.heightCurve.Evaluate(terrainNoise);
                globalHeightMap[x, y] = finalHeight * terrainSettings.heightScale;
                
                processedPoints++;
                
                // Yield cada cierto número de puntos para no bloquear el hilo principal
                if (processedPoints % 1000 == 0)
                {
                    if (showGenerationProgress)
                    {
                        float progress = (float)processedPoints / totalPoints;
                        Debug.Log($"[TerrainGenerator] Progreso altura: {progress:P1}");
                    }
                    yield return null;
                }
            }
        }
        
        Debug.Log("[TerrainGenerator] Mapa de altura global generado!");
    }
    
    private IEnumerator GenerateGlobalBiomeMap()
    {
        if (biomeSettings == null || biomeSettings.biomes.Length == 0)
        {
            Debug.LogWarning("[TerrainGenerator] BiomeSettings no configurado, saltando generación de biomas");
            yield break;
        }
        
        Debug.Log("[TerrainGenerator] Generando mapa de biomas global...");
        
        int width = terrainSettings.worldWidth;
        int height = terrainSettings.worldLength;
        
        globalBiomeMap = new int[width, height];
        
        int processedPoints = 0;
        int totalPoints = width * height;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2(x, y);
                float heightValue = globalHeightMap[x, y] / terrainSettings.heightScale; // Normalizado
                
                // Generar valores de temperatura y humedad
                float temperature = NoiseGenerator.GenerateBiomeNoise(position, biomeSettings.biomeNoise);
                float moisture = NoiseGenerator.GenerateBiomeNoise(position + Vector2.one * 1000, biomeSettings.biomeNoise);
                
                // Determinar bioma basado en altura, temperatura y humedad
                int selectedBiome = DetermineBiome(heightValue, temperature, moisture);
                globalBiomeMap[x, y] = selectedBiome;
                
                processedPoints++;
                
                if (processedPoints % 1000 == 0)
                {
                    if (showGenerationProgress)
                    {
                        float progress = (float)processedPoints / totalPoints;
                        Debug.Log($"[TerrainGenerator] Progreso biomas: {progress:P1}");
                    }
                    yield return null;
                }
            }
        }
        
        Debug.Log("[TerrainGenerator] Mapa de biomas global generado!");
    }
    
    private IEnumerator GenerateGlobalNavigabilityMap()
    {
        Debug.Log("[TerrainGenerator] Generando mapa de navegabilidad global...");
        
        int width = terrainSettings.worldWidth;
        int height = terrainSettings.worldLength;
        
        globalNavigabilityMap = new float[width, height];
        
        int processedPoints = 0;
        int totalPoints = width * height;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedHeight = globalHeightMap[x, y] / terrainSettings.heightScale;
                
                // Área navegable si está por debajo del umbral
                if (normalizedHeight <= terrainSettings.navigableHeightThreshold)
                {
                    globalNavigabilityMap[x, y] = 1f; // Navegable
                }
                else
                {
                    globalNavigabilityMap[x, y] = 0f; // No navegable
                }
                
                processedPoints++;
                
                if (processedPoints % 2000 == 0)
                {
                    if (showGenerationProgress)
                    {
                        float progress = (float)processedPoints / totalPoints;
                        Debug.Log($"[TerrainGenerator] Progreso navegabilidad: {progress:P1}");
                    }
                    yield return null;
                }
            }
        }
        
        // Post-procesamiento para asegurar anchura mínima de pasillos
        yield return StartCoroutine(EnsureMinimumPassageWidth());
        
        Debug.Log("[TerrainGenerator] Mapa de navegabilidad global generado!");
    }
    
    private IEnumerator EnsureMinimumPassageWidth()
    {
        Debug.Log("[TerrainGenerator] Asegurando anchura mínima de pasillos...");
        
        int width = terrainSettings.worldWidth;
        int height = terrainSettings.worldLength;
        int minPassagePixels = Mathf.RoundToInt(terrainSettings.minPassageWidth);
        
        // Crear copia para no modificar durante la iteración
        float[,] originalMap = (float[,])globalNavigabilityMap.Clone();
        
        for (int y = minPassagePixels; y < height - minPassagePixels; y++)
        {
            for (int x = minPassagePixels; x < width - minPassagePixels; x++)
            {
                if (originalMap[x, y] > 0.5f) // Si es navegable
                {
                    // Verificar que tenga suficiente espacio alrededor
                    bool hasMinWidth = CheckMinimumWidth(originalMap, x, y, minPassagePixels);
                    
                    if (!hasMinWidth)
                    {
                        // Expandir área navegable
                        ExpandNavigableArea(x, y, minPassagePixels);
                    }
                }
            }
            
            // Yield cada fila procesada
            if (y % 10 == 0)
            {
                yield return null;
            }
        }
    }
    
    private bool CheckMinimumWidth(float[,] map, int centerX, int centerY, int radius)
    {
        int navigableCount = 0;
        int totalCount = 0;
        
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                if (x >= 0 && x < map.GetLength(0) && y >= 0 && y < map.GetLength(1))
                {
                    if (map[x, y] > 0.5f) navigableCount++;
                    totalCount++;
                }
            }
        }
        
        return (float)navigableCount / totalCount > 0.6f; // 60% del área debe ser navegable
    }
    
    private void ExpandNavigableArea(int centerX, int centerY, int radius)
    {
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                if (x >= 0 && x < globalNavigabilityMap.GetLength(0) && 
                    y >= 0 && y < globalNavigabilityMap.GetLength(1))
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (distance <= radius)
                    {
                        globalNavigabilityMap[x, y] = 1f; // Hacer navegable
                        // También reducir altura para mantener consistencia
                        globalHeightMap[x, y] = terrainSettings.navigableHeightThreshold * terrainSettings.heightScale * 0.5f;
                    }
                }
            }
        }
    }
    
    private int DetermineBiome(float height, float temperature, float moisture)
    {
        int bestBiome = 0;
        float bestScore = float.MaxValue;
        
        for (int i = 0; i < biomeSettings.biomes.Length; i++)
        {
            var biome = biomeSettings.biomes[i];
            
            // Verificar si la altura está en el rango del bioma
            if (height < biome.minHeight || height > biome.maxHeight)
                continue;
                
            // Calcular distancia a las condiciones ideales del bioma
            float tempDiff = Mathf.Abs(temperature - biome.temperature);
            float moistDiff = Mathf.Abs(moisture - biome.moisture);
            float score = tempDiff + moistDiff;
            
            if (score < bestScore)
            {
                bestScore = score;
                bestBiome = i;
            }
        }
        
        return bestBiome;
    }
    
    // Métodos públicos para acceso a datos
    public float GetHeightAtWorldPosition(int x, int y)
    {
        if (globalHeightMap == null) return 0f;
        
        x = Mathf.Clamp(x, 0, terrainSettings.worldWidth - 1);
        y = Mathf.Clamp(y, 0, terrainSettings.worldLength - 1);
        
        return globalHeightMap[x, y];
    }
    
    public bool IsWorldPositionNavigable(int x, int y)
    {
        if (globalNavigabilityMap == null) return false;
        
        x = Mathf.Clamp(x, 0, terrainSettings.worldWidth - 1);
        y = Mathf.Clamp(y, 0, terrainSettings.worldLength - 1);
        
        return globalNavigabilityMap[x, y] > 0.5f;
    }
    
    public int GetBiomeAtWorldPosition(int x, int y)
    {
        if (globalBiomeMap == null) return 0;
        
        x = Mathf.Clamp(x, 0, terrainSettings.worldWidth - 1);
        y = Mathf.Clamp(y, 0, terrainSettings.worldLength - 1);
        
        return globalBiomeMap[x, y];
    }
    
    // Métodos de utilidad
    public bool IsWorldGenerated => worldGenerated;
    public bool IsGenerating => isGenerating;
    
    [ContextMenu("Generate World")]
    private void GenerateWorldFromMenu()
    {
        StartWorldGeneration();
    }
    
    [ContextMenu("Clear World Data")]
    private void ClearWorldData()
    {
        globalHeightMap = null;
        globalNavigabilityMap = null;
        globalBiomeMap = null;
        worldGenerated = false;
        Debug.Log("[TerrainGenerator] Datos del mundo limpiados");
    }
}