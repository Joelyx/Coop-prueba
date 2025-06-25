using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Jobs optimizados con Burst Compiler para generación de terreno
/// </summary>
namespace ProceduralTerrain.Jobs
{
    // Job para generar ruido de forma paralela
    [BurstCompile]
    public struct GenerateNoiseJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<float> noiseValues;
        [ReadOnly] public float scale;
        [ReadOnly] public int octaves;
        [ReadOnly] public float persistence;
        [ReadOnly] public float lacunarity;
        [ReadOnly] public float2 offset;
        [ReadOnly] public int seed;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public float heightMultiplier;
        
        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;
            
            float2 position = new float2(x, y);
            float noiseValue = GenerateNoise(position);
            
            noiseValues[index] = noiseValue * heightMultiplier;
        }
        
        private float GenerateNoise(float2 position)
        {
            float noiseValue = 0;
            float amplitude = 1;
            float frequency = scale;
            float maxValue = 0;
            
            for (int i = 0; i < octaves; i++)
            {
                float2 samplePoint = (position + offset) / frequency;
                float perlinValue = noise.cnoise(samplePoint + seed) * 2 - 1;
                
                noiseValue += perlinValue * amplitude;
                maxValue += amplitude;
                
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            // Normalizar entre 0 y 1
            noiseValue = (noiseValue / maxValue + 1) * 0.5f;
            return math.clamp(noiseValue, 0f, 1f);
        }
    }
    
    // Job para generar heightmap con falloff
    [BurstCompile]
    public struct GenerateHeightmapJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<float> heightMap;
        [ReadOnly] public NativeArray<float> noiseMap;
        [ReadOnly] public NativeArray<float> falloffMap;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public float heightScale;
        [ReadOnly] public bool useFalloff;
        
        public void Execute(int index)
        {
            float noiseValue = noiseMap[index];
            
            if (useFalloff)
            {
                float falloffValue = falloffMap[index];
                noiseValue = math.clamp(noiseValue - falloffValue, 0f, 1f);
            }
            
            heightMap[index] = noiseValue * heightScale;
        }
    }
    
    // Job para generar navegabilidad
    [BurstCompile]
    public struct GenerateNavigabilityJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<float> navigabilityMap;
        [ReadOnly] public NativeArray<float> heightMap;
        [ReadOnly] public float heightScale;
        [ReadOnly] public float navigableThreshold;
        
        public void Execute(int index)
        {
            float normalizedHeight = heightMap[index] / heightScale;
            navigabilityMap[index] = normalizedHeight <= navigableThreshold ? 1f : 0f;
        }
    }
    
    // Job para generar falloff map
    [BurstCompile]
    public struct GenerateFalloffJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<float> falloffMap;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public float falloffStrength;
        
        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;
            
            float xv = x / (float)width * 2 - 1;
            float yv = y / (float)height * 2 - 1;
            
            float value = math.max(math.abs(xv), math.abs(yv));
            falloffMap[index] = Evaluate(value, falloffStrength);
        }
        
        private float Evaluate(float value, float strength)
        {
            float a = 3f * strength;
            float b = 2.2f;
            
            return math.pow(value, a) / (math.pow(value, a) + math.pow(b - b * value, a));
        }
    }
    
    // Job para generar vértices de malla
    [BurstCompile]
    public struct GenerateVerticesJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<float3> vertices;
        [WriteOnly] public NativeArray<float2> uvs;
        [WriteOnly] public NativeArray<float4> colors;
        [ReadOnly] public NativeArray<float> heightMap;
        [ReadOnly] public NativeArray<float> navigabilityMap;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public float navigableThreshold;
        [ReadOnly] public float4 navigableColor;
        [ReadOnly] public float4 blockedColor;
        
        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;
            
            float height = heightMap[index];
            float navigability = navigabilityMap[index];
            
            vertices[index] = new float3(x, height, y);
            uvs[index] = new float2(x / (float)(width - 1), y / (float)(height - 1));
            
            // Color basado en navegabilidad
            colors[index] = navigability > 0.5f ? navigableColor : blockedColor;
        }
    }
    
    // Job para generar triángulos
    [BurstCompile]
    public struct GenerateTrianglesJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<int> triangles;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        
        public void Execute(int index)
        {
            int x = index % (width - 1);
            int y = index / (width - 1);
            
            int vertexIndex = y * width + x;
            int triangleIndex = index * 6;
            
            // Primer triángulo
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + width;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            
            // Segundo triángulo
            triangles[triangleIndex + 3] = vertexIndex + 1;
            triangles[triangleIndex + 4] = vertexIndex + width;
            triangles[triangleIndex + 5] = vertexIndex + width + 1;
        }
    }
    
    // Job para calcular normales
    [BurstCompile]
    public struct CalculateNormalsJob : IJobParallelFor
    {
        public NativeArray<float3> normals;
        [ReadOnly] public NativeArray<float3> vertices;
        [ReadOnly] public NativeArray<int> triangles;
        
        public void Execute(int vertexIndex)
        {
            normals[vertexIndex] = float3.zero;
            
            // Encontrar todos los triángulos que usan este vértice
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int vertA = triangles[i];
                int vertB = triangles[i + 1];
                int vertC = triangles[i + 2];
                
                if (vertA == vertexIndex || vertB == vertexIndex || vertC == vertexIndex)
                {
                    float3 triangleNormal = CalculateTriangleNormal(
                        vertices[vertA], 
                        vertices[vertB], 
                        vertices[vertC]
                    );
                    
                    normals[vertexIndex] += triangleNormal;
                }
            }
            
            normals[vertexIndex] = math.normalize(normals[vertexIndex]);
        }
        
        private float3 CalculateTriangleNormal(float3 a, float3 b, float3 c)
        {
            float3 sideAB = b - a;
            float3 sideAC = c - a;
            return math.normalize(math.cross(sideAB, sideAC));
        }
    }
    
    // Job para procesamiento de biomas
    [BurstCompile]
    public struct ProcessBiomeJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<int> biomeMap;
        [ReadOnly] public NativeArray<float> heightMap;
        [ReadOnly] public NativeArray<float> temperatureMap;
        [ReadOnly] public NativeArray<float> moistureMap;
        [ReadOnly] public NativeArray<BiomeData> biomes;
        [ReadOnly] public float heightScale;
        
        public void Execute(int index)
        {
            float height = heightMap[index] / heightScale;
            float temperature = temperatureMap[index];
            float moisture = moistureMap[index];
            
            int bestBiome = 0;
            float bestScore = float.MaxValue;
            
            for (int i = 0; i < biomes.Length; i++)
            {
                BiomeData biome = biomes[i];
                
                // Verificar rango de altura
                if (height < biome.minHeight || height > biome.maxHeight)
                    continue;
                
                // Calcular puntuación
                float tempDiff = math.abs(temperature - biome.temperature);
                float moistDiff = math.abs(moisture - biome.moisture);
                float score = tempDiff + moistDiff;
                
                if (score < bestScore)
                {
                    bestScore = score;
                    bestBiome = i;
                }
            }
            
            biomeMap[index] = bestBiome;
        }
    }
    
    // Estructura de datos para biomas (compatible con Burst)
    public struct BiomeData
    {
        public float minHeight;
        public float maxHeight;
        public float temperature;
        public float moisture;
        public float roughnessFactor;
        public int isNavigable; // 1 = true, 0 = false (bool no es compatible con Burst)
    }
    
    // Job para optimizar navegabilidad con anchura mínima
    [BurstCompile]
    public struct OptimizeNavigabilityJob : IJobParallelFor
    {
        public NativeArray<float> navigabilityMap;
        [ReadOnly] public NativeArray<float> originalMap;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public int minPassageWidth;
        
        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;
            
            if (originalMap[index] > 0.5f) // Si es navegable
            {
                // Verificar anchura mínima
                bool hasMinWidth = CheckMinimumWidth(x, y);
                
                if (!hasMinWidth)
                {
                    // Expandir área navegable
                    ExpandArea(x, y);
                }
            }
        }
        
        private bool CheckMinimumWidth(int centerX, int centerY)
        {
            int navigableCount = 0;
            int totalCount = 0;
            
            for (int y = centerY - minPassageWidth; y <= centerY + minPassageWidth; y++)
            {
                for (int x = centerX - minPassageWidth; x <= centerX + minPassageWidth; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        int idx = y * width + x;
                        if (originalMap[idx] > 0.5f) navigableCount++;
                        totalCount++;
                    }
                }
            }
            
            return totalCount > 0 && (float)navigableCount / totalCount > 0.6f;
        }
        
        private void ExpandArea(int centerX, int centerY)
        {
            for (int y = centerY - minPassageWidth; y <= centerY + minPassageWidth; y++)
            {
                for (int x = centerX - minPassageWidth; x <= centerX + minPassageWidth; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        float distance = math.sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                        if (distance <= minPassageWidth)
                        {
                            int idx = y * width + x;
                            navigabilityMap[idx] = 1f;
                        }
                    }
                }
            }
        }
    }
}