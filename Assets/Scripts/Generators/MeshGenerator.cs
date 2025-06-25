using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Generador de mallas optimizado para terreno procedural
/// Utiliza Unity Job System para mejor rendimiento
/// </summary>
public static class MeshGenerator
{
    /// <summary>
    /// Genera una malla de terreno a partir de un heightmap
    /// </summary>
    public static MeshData GenerateTerrainMesh(float[,] heightMap, TerrainSettings settings, bool useLOD = false, int lodLevel = 0)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // Aplicar LOD reduciendo la resolución
        int meshSimplificationIncrement = useLOD ? (lodLevel + 1) * 2 : 1;
        int meshWidth = (width - 1) / meshSimplificationIncrement + 1;
        int meshHeight = (height - 1) / meshSimplificationIncrement + 1;
        
        MeshData meshData = new MeshData(meshWidth, meshHeight);
        
        // Generar vértices
        for (int y = 0; y < meshHeight; y++)
        {
            for (int x = 0; x < meshWidth; x++)
            {
                int heightMapX = x * meshSimplificationIncrement;
                int heightMapY = y * meshSimplificationIncrement;
                
                // Asegurar que no excedemos los límites
                heightMapX = Mathf.Min(heightMapX, width - 1);
                heightMapY = Mathf.Min(heightMapY, height - 1);
                
                float vertexHeight = heightMap[heightMapX, heightMapY];
                Vector3 vertexPosition = new Vector3(heightMapX, vertexHeight, heightMapY);
                Vector2 uv = new Vector2(x / (float)(meshWidth - 1), y / (float)(meshHeight - 1));
                
                // Determinar color basado en altura y navegabilidad
                Color vertexColor = GetVertexColor(vertexHeight, settings);
                
                meshData.AddVertex(vertexPosition, uv, vertexColor);
            }
        }
        
        // Generar triángulos
        for (int y = 0; y < meshHeight - 1; y++)
        {
            for (int x = 0; x < meshWidth - 1; x++)
            {
                int vertexIndex = y * meshWidth + x;
                
                // Primer triángulo (arriba-izquierda)
                meshData.AddTriangle(vertexIndex, vertexIndex + meshWidth, vertexIndex + 1);
                
                // Segundo triángulo (abajo-derecha)
                meshData.AddTriangle(vertexIndex + 1, vertexIndex + meshWidth, vertexIndex + meshWidth + 1);
            }
        }
        
        return meshData;
    }
    
    /// <summary>
    /// Genera malla usando Unity Job System para mejor rendimiento
    /// </summary>
    public static JobHandle GenerateTerrainMeshAsync(float[,] heightMap, TerrainSettings settings, System.Action<MeshData> onComplete)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // Convertir heightmap a NativeArray para Jobs
        NativeArray<float> heightArray = new NativeArray<float>(width * height, Allocator.TempJob);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                heightArray[y * width + x] = heightMap[x, y];
            }
        }
        
        // Preparar arrays para el resultado
        int vertexCount = width * height;
        int triangleCount = (width - 1) * (height - 1) * 6;
        
        NativeArray<float3> vertices = new NativeArray<float3>(vertexCount, Allocator.TempJob);
        NativeArray<float2> uvs = new NativeArray<float2>(vertexCount, Allocator.TempJob);
        NativeArray<float4> colors = new NativeArray<float4>(vertexCount, Allocator.TempJob);
        NativeArray<int> triangles = new NativeArray<int>(triangleCount, Allocator.TempJob);
        
        // Crear y programar jobs
        var vertexJob = new GenerateVerticesJob
        {
            heightArray = heightArray,
            vertices = vertices,
            uvs = uvs,
            colors = colors,
            width = width,
            height = height,
            heightScale = settings.heightScale,
            navigableThreshold = settings.navigableHeightThreshold
        };
        
        var triangleJob = new GenerateTrianglesJob
        {
            triangles = triangles,
            width = width,
            height = height
        };
        
        // Ejecutar jobs en paralelo
        JobHandle vertexHandle = vertexJob.Schedule(vertexCount, 64);
        JobHandle triangleHandle = triangleJob.Schedule(triangleCount / 3, 32);
        
        // Combinar handles
        JobHandle combinedHandle = JobHandle.CombineDependencies(vertexHandle, triangleHandle);
        
        // Programar job de finalización
        var finalizeJob = new FinalizeMeshJob
        {
            vertices = vertices,
            uvs = uvs,
            colors = colors,
            triangles = triangles,
            heightArray = heightArray,
            onComplete = onComplete
        };
        
        return finalizeJob.Schedule(combinedHandle);
    }
    
    /// <summary>
    /// Genera collider simplificado para navegación
    /// </summary>
    public static MeshData GenerateNavigationMesh(float[,] heightMap, float[,] navigabilityMap, TerrainSettings settings)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // Reducir resolución para collider de navegación
        int simplification = 4; // Cada 4 puntos
        int navWidth = width / simplification;
        int navHeight = height / simplification;
        
        MeshData meshData = new MeshData(navWidth, navHeight);
        
        // Solo generar malla para áreas navegables
        for (int y = 0; y < navHeight; y++)
        {
            for (int x = 0; x < navWidth; x++)
            {
                int heightMapX = x * simplification;
                int heightMapY = y * simplification;
                
                // Verificar navegabilidad del área
                bool isNavigable = IsAreaNavigable(navigabilityMap, heightMapX, heightMapY, simplification);
                
                if (isNavigable)
                {
                    float avgHeight = GetAverageHeight(heightMap, heightMapX, heightMapY, simplification);
                    Vector3 vertexPosition = new Vector3(heightMapX, avgHeight, heightMapY);
                    Vector2 uv = new Vector2(x / (float)(navWidth - 1), y / (float)(navHeight - 1));
                    
                    meshData.AddVertex(vertexPosition, uv, Color.blue);
                }
                else
                {
                    // Vértice inválido para áreas no navegables
                    meshData.AddVertex(Vector3.zero, Vector2.zero, Color.clear);
                }
            }
        }
        
        // Generar triángulos solo para áreas navegables
        for (int y = 0; y < navHeight - 1; y++)
        {
            for (int x = 0; x < navWidth - 1; x++)
            {
                int vertexIndex = y * navWidth + x;
                
                // Verificar que todos los vértices del cuad sean navegables
                bool allNavigable = true;
                for (int dy = 0; dy <= 1; dy++)
                {
                    for (int dx = 0; dx <= 1; dx++)
                    {
                        int checkX = (x + dx) * simplification;
                        int checkY = (y + dy) * simplification;
                        
                        if (checkX < width && checkY < height)
                        {
                            if (navigabilityMap[checkX, checkY] < 0.5f)
                            {
                                allNavigable = false;
                                break;
                            }
                        }
                    }
                    if (!allNavigable) break;
                }
                
                if (allNavigable)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + navWidth, vertexIndex + 1);
                    meshData.AddTriangle(vertexIndex + 1, vertexIndex + navWidth, vertexIndex + navWidth + 1);
                }
            }
        }
        
        return meshData;
    }
    
    private static Color GetVertexColor(float height, TerrainSettings settings)
    {
        float normalizedHeight = height / settings.heightScale;
        
        if (normalizedHeight <= settings.navigableHeightThreshold)
        {
            // Zona navegable - azul
            return Color.Lerp(Color.blue, Color.cyan, normalizedHeight / settings.navigableHeightThreshold);
        }
        else
        {
            // Zona no navegable - gris a marrón
            float t = (normalizedHeight - settings.navigableHeightThreshold) / (1f - settings.navigableHeightThreshold);
            return Color.Lerp(Color.gray, Color.brown, t);
        }
    }
    
    private static bool IsAreaNavigable(float[,] navigabilityMap, int centerX, int centerY, int radius)
    {
        int navigableCount = 0;
        int totalCount = 0;
        
        for (int y = centerY; y < centerY + radius && y < navigabilityMap.GetLength(1); y++)
        {
            for (int x = centerX; x < centerX + radius && x < navigabilityMap.GetLength(0); x++)
            {
                if (navigabilityMap[x, y] > 0.5f) navigableCount++;
                totalCount++;
            }
        }
        
        return totalCount > 0 && (float)navigableCount / totalCount > 0.8f; // 80% navegable
    }
    
    private static float GetAverageHeight(float[,] heightMap, int centerX, int centerY, int radius)
    {
        float totalHeight = 0f;
        int count = 0;
        
        for (int y = centerY; y < centerY + radius && y < heightMap.GetLength(1); y++)
        {
            for (int x = centerX; x < centerX + radius && x < heightMap.GetLength(0); x++)
            {
                totalHeight += heightMap[x, y];
                count++;
            }
        }
        
        return count > 0 ? totalHeight / count : 0f;
    }
}

// Jobs para generación paralela
[System.Serializable]
public struct GenerateVerticesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> heightArray;
    public NativeArray<float3> vertices;
    public NativeArray<float2> uvs;
    public NativeArray<float4> colors;
    
    [ReadOnly] public int width;
    [ReadOnly] public int height;
    [ReadOnly] public float heightScale;
    [ReadOnly] public float navigableThreshold;
    
    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;
        
        float vertexHeight = heightArray[index];
        vertices[index] = new float3(x, vertexHeight, y);
        uvs[index] = new float2(x / (float)(width - 1), y / (float)(height - 1));
        
        // Calcular color basado en navegabilidad
        float normalizedHeight = vertexHeight / heightScale;
        if (normalizedHeight <= navigableThreshold)
        {
            colors[index] = new float4(0, 0, 1, 1); // Azul para navegable
        }
        else
        {
            colors[index] = new float4(0.5f, 0.5f, 0.5f, 1); // Gris para no navegable
        }
    }
}

[System.Serializable]
public struct GenerateTrianglesJob : IJobParallelFor
{
    public NativeArray<int> triangles;
    
    [ReadOnly] public int width;
    [ReadOnly] public int height;
    
    public void Execute(int index)
    {
        int quadIndex = index;
        int x = quadIndex % (width - 1);
        int y = quadIndex / (width - 1);
        
        int vertexIndex = y * width + x;
        int triangleIndex = quadIndex * 6;
        
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

[System.Serializable]
public struct FinalizeMeshJob : IJob
{
    public NativeArray<float3> vertices;
    public NativeArray<float2> uvs;
    public NativeArray<float4> colors;
    public NativeArray<int> triangles;
    public NativeArray<float> heightArray;
    
    public System.Action<MeshData> onComplete;
    
    public void Execute()
    {
        // Convertir datos nativos a MeshData
        int width = (int)math.sqrt(vertices.Length);
        int height = width;
        
        MeshData meshData = new MeshData(width, height);
        
        // Copiar vértices
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
            Vector2 uv = new Vector2(uvs[i].x, uvs[i].y);
            Color color = new Color(colors[i].x, colors[i].y, colors[i].z, colors[i].w);
            
            meshData.vertices[i] = vertex;
            meshData.uvs[i] = uv;
            meshData.colors[i] = color;
        }
        
        // Copiar triángulos
        for (int i = 0; i < triangles.Length; i++)
        {
            meshData.triangles[i] = triangles[i];
        }
        
        meshData.vertexIndex = vertices.Length;
        meshData.triangleIndex = triangles.Length;
        
        // Limpiar arrays nativos
        vertices.Dispose();
        uvs.Dispose();
        colors.Dispose();
        triangles.Dispose();
        heightArray.Dispose();
        
        // Llamar callback
        onComplete?.Invoke(meshData);
    }
}