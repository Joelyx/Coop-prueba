using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generador de biomas para terreno submarino procedural
/// </summary>
public static class BiomeGenerator
{
    /// <summary>
    /// Aplica características de bioma a un chunk de terreno
    /// </summary>
    public static void ApplyBiomeToChunk(TerrainChunk chunk, BiomeSettings biomeSettings, TerrainGenerator terrainGenerator)
    {
        if (biomeSettings == null || terrainGenerator == null) return;
        
        var chunkCoord = chunk.chunkCoordinate;
        int chunkSize = chunk.chunkSize;
        
        // Obtener datos de bioma para este chunk
        Dictionary<int, float> biomeWeights = CalculateBiomeWeights(chunkCoord, chunkSize, biomeSettings, terrainGenerator);
        
        // Aplicar características del bioma dominante
        int dominantBiome = GetDominantBiome(biomeWeights);
        ApplyBiomeCharacteristics(chunk, biomeSettings.biomes[dominantBiome]);
        
        // Generar objetos decorativos basados en biomas
        GenerateBiomeObjects(chunk, biomeSettings, biomeWeights, terrainGenerator);
    }
    
    private static Dictionary<int, float> CalculateBiomeWeights(Vector2Int chunkCoord, int chunkSize, BiomeSettings biomeSettings, TerrainGenerator terrainGenerator)
    {
        Dictionary<int, float> weights = new Dictionary<int, float>();
        
        // Inicializar contadores
        for (int i = 0; i < biomeSettings.biomes.Length; i++)
        {
            weights[i] = 0f;
        }
        
        // Muestrear biomas en el área del chunk
        int samples = 16; // 4x4 muestras por chunk
        float stepSize = (float)chunkSize / samples;
        
        for (int y = 0; y < samples; y++)
        {
            for (int x = 0; x < samples; x++)
            {
                Vector2 localPos = new Vector2(x * stepSize, y * stepSize);
                Vector2Int worldPos = new Vector2Int(
                    chunkCoord.x * chunkSize + Mathf.RoundToInt(localPos.x),
                    chunkCoord.y * chunkSize + Mathf.RoundToInt(localPos.y)
                );
                
                // Asegurar que está dentro de los límites del mundo
                worldPos.x = Mathf.Clamp(worldPos.x, 0, terrainGenerator.terrainSettings.worldWidth - 1);
                worldPos.y = Mathf.Clamp(worldPos.y, 0, terrainGenerator.terrainSettings.worldLength - 1);
                
                int biomeIndex = terrainGenerator.GetBiomeAtWorldPosition(worldPos.x, worldPos.y);
                weights[biomeIndex] += 1f;
            }
        }
        
        // Normalizar pesos
        float totalWeight = weights.Values.Sum();
        if (totalWeight > 0)
        {
            var normalizedWeights = new Dictionary<int, float>();
            foreach (var kvp in weights)
            {
                normalizedWeights[kvp.Key] = kvp.Value / totalWeight;
            }
            return normalizedWeights;
        }
        
        return weights;
    }
    
    private static int GetDominantBiome(Dictionary<int, float> biomeWeights)
    {
        return biomeWeights.OrderByDescending(kvp => kvp.Value).First().Key;
    }
    
    private static void ApplyBiomeCharacteristics(TerrainChunk chunk, BiomeSettings.BiomeData biome)
    {
        // Cambiar material del terreno
        if (biome.terrainMaterial != null && chunk.meshRenderer != null)
        {
            chunk.meshRenderer.material = biome.terrainMaterial;
        }
        
        // Aplicar factor de rugosidad a la navegabilidad
        if (chunk.navigabilityMap != null)
        {
            ModifyNavigabilityByRoughness(chunk, biome.roughnessFactor);
        }
    }
    
    private static void ModifyNavigabilityByRoughness(TerrainChunk chunk, float roughnessFactor)
    {
        if (roughnessFactor <= 1f) return; // No modificar si no es más rugoso
        
        int size = chunk.chunkSize + 1;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (chunk.navigabilityMap[x, y] > 0.5f) // Si es navegable
                {
                    // Reducir navegabilidad basada en rugosidad
                    float reduction = (roughnessFactor - 1f) * 0.3f; // Factor de reducción
                    chunk.navigabilityMap[x, y] = Mathf.Max(0f, chunk.navigabilityMap[x, y] - reduction);
                }
            }
        }
    }
    
    private static void GenerateBiomeObjects(TerrainChunk chunk, BiomeSettings biomeSettings, Dictionary<int, float> biomeWeights, TerrainGenerator terrainGenerator)
    {
        Transform chunkTransform = chunk.transform;
        
        foreach (var biomeWeight in biomeWeights)
        {
            if (biomeWeight.Value < 0.1f) continue; // Saltar biomas con peso muy bajo
            
            BiomeSettings.BiomeData biome = biomeSettings.biomes[biomeWeight.Key];
            if (biome.decorativeObjects == null || biome.decorativeObjects.Length == 0) continue;
            
            // Calcular número de objetos a generar
            float density = biome.objectDensity * biomeWeight.Value;
            int objectCount = Mathf.RoundToInt(density * chunk.chunkSize * chunk.chunkSize * 0.01f);
            
            for (int i = 0; i < objectCount; i++)
            {
                Vector3 localPosition = GenerateRandomPositionInChunk(chunk);
                Vector3 worldPosition = chunkTransform.position + localPosition;
                
                // Verificar que la posición es válida para objetos
                if (IsValidObjectPosition(localPosition, chunk, biome))
                {
                    GameObject selectedPrefab = biome.decorativeObjects[Random.Range(0, biome.decorativeObjects.Length)];
                    SpawnBiomeObject(selectedPrefab, worldPosition, biome, chunkTransform);
                }
            }
        }
    }
    
    private static Vector3 GenerateRandomPositionInChunk(TerrainChunk chunk)
    {
        float x = Random.Range(0f, chunk.chunkSize);
        float z = Random.Range(0f, chunk.chunkSize);
        
        // Obtener altura del terreno en esta posición
        int heightX = Mathf.Clamp(Mathf.RoundToInt(x), 0, chunk.chunkSize);
        int heightZ = Mathf.Clamp(Mathf.RoundToInt(z), 0, chunk.chunkSize);
        float y = chunk.heightMap[heightX, heightZ];
        
        return new Vector3(x, y, z);
    }
    
    private static bool IsValidObjectPosition(Vector3 localPosition, TerrainChunk chunk, BiomeSettings.BiomeData biome)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(localPosition.x), 0, chunk.chunkSize);
        int z = Mathf.Clamp(Mathf.RoundToInt(localPosition.z), 0, chunk.chunkSize);
        
        // Verificar navegabilidad si el bioma lo requiere
        if (biome.isNavigable)
        {
            return chunk.navigabilityMap[x, z] > 0.5f;
        }
        else
        {
            // Para objetos que pueden estar en zonas no navegables
            return chunk.navigabilityMap[x, z] < 0.5f;
        }
    }
    
    private static void SpawnBiomeObject(GameObject prefab, Vector3 worldPosition, BiomeSettings.BiomeData biome, Transform parent)
    {
        if (prefab == null) return;
        
        // Instanciar objeto
        GameObject spawnedObject = Object.Instantiate(prefab, worldPosition, Quaternion.identity, parent);
        
        // Aplicar escala aleatoria
        float randomScale = Random.Range(0.8f, 1.2f) * biome.objectScale;
        spawnedObject.transform.localScale = Vector3.one * randomScale;
        
        // Rotación aleatoria en Y
        float randomRotationY = Random.Range(0f, 360f);
        spawnedObject.transform.rotation = Quaternion.Euler(0, randomRotationY, 0);
        
        // Agregar componente de bioma si no existe
        BiomeObject biomeComponent = spawnedObject.GetComponent<BiomeObject>();
        if (biomeComponent == null)
        {
            biomeComponent = spawnedObject.AddComponent<BiomeObject>();
        }
        
        biomeComponent.SetBiomeData(biome);
    }
    
    /// <summary>
    /// Genera transiciones suaves entre biomas
    /// </summary>
    public static Color CalculateBiomeTransition(Vector2 worldPosition, BiomeSettings biomeSettings, TerrainGenerator terrainGenerator)
    {
        if (biomeSettings == null || terrainGenerator == null)
            return Color.white;
        
        // Obtener bioma en la posición
        int x = Mathf.RoundToInt(worldPosition.x);
        int y = Mathf.RoundToInt(worldPosition.y);
        
        // Asegurar límites
        x = Mathf.Clamp(x, 0, terrainGenerator.terrainSettings.worldWidth - 1);
        y = Mathf.Clamp(y, 0, terrainGenerator.terrainSettings.worldLength - 1);
        
        int centerBiome = terrainGenerator.GetBiomeAtWorldPosition(x, y);
        Color centerColor = biomeSettings.biomes[centerBiome].biomeColor;
        
        // Muestrear biomas vecinos para transición
        Dictionary<int, float> nearbyBiomes = new Dictionary<int, float>();
        int sampleRadius = Mathf.RoundToInt(biomeSettings.transitionSmoothness);
        
        for (int dy = -sampleRadius; dy <= sampleRadius; dy++)
        {
            for (int dx = -sampleRadius; dx <= sampleRadius; dx++)
            {
                int sampleX = Mathf.Clamp(x + dx, 0, terrainGenerator.terrainSettings.worldWidth - 1);
                int sampleY = Mathf.Clamp(y + dy, 0, terrainGenerator.terrainSettings.worldLength - 1);
                
                int biome = terrainGenerator.GetBiomeAtWorldPosition(sampleX, sampleY);
                
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float weight = 1f / (1f + distance);
                
                if (nearbyBiomes.ContainsKey(biome))
                {
                    nearbyBiomes[biome] += weight;
                }
                else
                {
                    nearbyBiomes[biome] = weight;
                }
            }
        }
        
        // Calcular color promedio ponderado
        Color blendedColor = Color.black;
        float totalWeight = 0f;
        
        foreach (var kvp in nearbyBiomes)
        {
            Color biomeColor = biomeSettings.biomes[kvp.Key].biomeColor;
            blendedColor += biomeColor * kvp.Value;
            totalWeight += kvp.Value;
        }
        
        if (totalWeight > 0)
        {
            blendedColor /= totalWeight;
        }
        
        return blendedColor;
    }
    
    /// <summary>
    /// Obtiene el tipo de bioma más apropiado para una posición y condiciones dadas
    /// </summary>
    public static int DetermineBestBiome(float height, float temperature, float moisture, BiomeSettings biomeSettings)
    {
        if (biomeSettings == null || biomeSettings.biomes.Length == 0)
            return 0;
        
        int bestBiome = 0;
        float bestScore = float.MaxValue;
        
        for (int i = 0; i < biomeSettings.biomes.Length; i++)
        {
            var biome = biomeSettings.biomes[i];
            
            // Verificar si la altura está en el rango
            if (height < biome.minHeight || height > biome.maxHeight)
                continue;
            
            // Calcular puntuación basada en temperatura y humedad
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
}

/// <summary>
/// Componente para objetos generados por biomas
/// </summary>
public class BiomeObject : MonoBehaviour
{
    [Header("Información del Bioma")]
    public string biomeName;
    public Color biomeColor = Color.white;
    public bool isNavigable = true;
    public float roughnessFactor = 1f;
    
    [Header("Efectos Ambientales")]
    public ParticleSystem ambientEffect;
    public AudioSource ambientAudio;
    public Light ambientLight;
    
    private BiomeSettings.BiomeData biomeData;
    
    public void SetBiomeData(BiomeSettings.BiomeData data)
    {
        biomeData = data;
        biomeName = data.biomeName;
        biomeColor = data.biomeColor;
        isNavigable = data.isNavigable;
        roughnessFactor = data.roughnessFactor;
        
        // Aplicar color al objeto si tiene renderer
        Renderer objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null && objectRenderer.material != null)
        {
            objectRenderer.material.color = biomeColor;
        }
        
        // Configurar efectos ambientales
        ConfigureAmbientEffects();
    }
    
    private void ConfigureAmbientEffects()
    {
        // Configurar partículas basadas en bioma
        if (ambientEffect != null)
        {
            var main = ambientEffect.main;
            main.startColor = biomeColor;
            
            // Ajustar intensidad basada en tipo de bioma
            var emission = ambientEffect.emission;
            switch (biomeName.ToLower())
            {
                case "arrecife coralino":
                    emission.rateOverTime = 5f; // Más partículas para corales
                    break;
                case "formación rocosa":
                    emission.rateOverTime = 1f; // Pocas partículas para rocas
                    break;
                default:
                    emission.rateOverTime = 2f;
                    break;
            }
        }
        
        // Configurar luz ambiental
        if (ambientLight != null)
        {
            ambientLight.color = biomeColor;
            ambientLight.intensity = 0.3f;
            ambientLight.range = 10f;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Notificar cuando el submarino entra en este bioma
        if (other.CompareTag("Player"))
        {
            BiomeManager biomeManager = FindFirstObjectByType<BiomeManager>();
            if (biomeManager != null)
            {
                biomeManager.OnBiomeEntered(biomeData);
            }
        }
    }
}