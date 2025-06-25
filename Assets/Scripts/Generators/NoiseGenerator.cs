using UnityEngine;

/// <summary>
/// Generador de ruido Perlin para terreno procedural
/// </summary>
public static class NoiseGenerator
{
    /// <summary>
    /// Genera ruido Perlin de múltiples octavas
    /// </summary>
    public static float GenerateNoise(Vector2 position, NoiseSettings settings)
    {
        float noiseValue = 0;
        float amplitude = 1;
        float frequency = settings.scale;
        float maxValue = 0; // Normalización
        
        for (int i = 0; i < settings.octaves; i++)
        {
            Vector2 samplePoint = (position + settings.offset) / frequency;
            float perlinValue = Mathf.PerlinNoise(samplePoint.x + settings.seed, samplePoint.y + settings.seed) * 2 - 1;
            
            noiseValue += perlinValue * amplitude;
            maxValue += amplitude;
            
            amplitude *= settings.persistence;
            frequency *= settings.lacunarity;
        }
        
        // Normalizar entre 0 y 1
        noiseValue = (noiseValue / maxValue + 1) * 0.5f;
        return Mathf.Clamp01(noiseValue);
    }
    
    /// <summary>
    /// Genera mapa de ruido 2D
    /// </summary>
    public static float[,] GenerateNoiseMap(int width, int height, Vector2 offset, NoiseSettings settings)
    {
        float[,] noiseMap = new float[width, height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2(x, y) + offset;
                noiseMap[x, y] = GenerateNoise(position, settings);
            }
        }
        
        return noiseMap;
    }
    
    /// <summary>
    /// Genera ruido ridged (para montañas)
    /// </summary>
    public static float GenerateRidgedNoise(Vector2 position, NoiseSettings settings)
    {
        float noiseValue = 0;
        float amplitude = 1;
        float frequency = settings.scale;
        float weight = 1;
        
        for (int i = 0; i < settings.octaves; i++)
        {
            Vector2 samplePoint = (position + settings.offset) / frequency;
            float perlinValue = Mathf.PerlinNoise(samplePoint.x + settings.seed, samplePoint.y + settings.seed);
            
            // Inversión y elevación al cuadrado para crear crestas
            perlinValue = 1 - Mathf.Abs(perlinValue);
            perlinValue *= perlinValue;
            perlinValue *= weight;
            
            weight = Mathf.Clamp01(perlinValue * 2);
            
            noiseValue += perlinValue * amplitude;
            amplitude *= settings.persistence;
            frequency *= settings.lacunarity;
        }
        
        return Mathf.Clamp01(noiseValue);
    }
    
    /// <summary>
    /// Genera ruido para biomas (más suave)
    /// </summary>
    public static float GenerateBiomeNoise(Vector2 position, NoiseSettings settings)
    {
        Vector2 samplePoint = (position + settings.offset) / settings.scale;
        return Mathf.PerlinNoise(samplePoint.x + settings.seed, samplePoint.y + settings.seed);
    }
    
    /// <summary>
    /// Función de falloff para crear bordes naturales del mundo
    /// </summary>
    public static float[,] GenerateFalloffMap(int width, int height)
    {
        float[,] falloffMap = new float[width, height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float xv = x / (float)width * 2 - 1;
                float yv = y / (float)height * 2 - 1;
                
                float value = Mathf.Max(Mathf.Abs(xv), Mathf.Abs(yv));
                falloffMap[x, y] = Evaluate(value);
            }
        }
        
        return falloffMap;
    }
    
    /// <summary>
    /// Función de evaluación para falloff suave
    /// </summary>
    private static float Evaluate(float value)
    {
        float a = 3f;
        float b = 2.2f;
        
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}