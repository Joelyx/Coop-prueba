using UnityEngine;

/// <summary>
/// Representa un chunk individual del terreno
/// </summary>
public class TerrainChunk : MonoBehaviour
{
    [Header("Información del Chunk")]
    public Vector2Int chunkCoordinate;
    public int chunkSize;
    public bool isActive;
    public bool meshGenerated;
    
    [Header("Datos de Altura")]
    public float[,] heightMap;
    public float[,] navigabilityMap; // 0 = no navegable, 1 = navegable
    
    [Header("Componentes")]
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;
    
    [Header("LOD")]
    public int currentLOD = 0;
    public int maxLOD = 3;
    
    private TerrainSettings settings;
    private MeshData meshData;
    
    public void Initialize(Vector2Int coordinate, int size, TerrainSettings terrainSettings)
    {
        chunkCoordinate = coordinate;
        chunkSize = size;
        settings = terrainSettings;
        
        // Configurar componentes
        SetupComponents();
        
        // Generar heightmap
        GenerateHeightMap();
        
        // Generar navegabilidad
        GenerateNavigabilityMap();
        
        // Generar malla
        GenerateMesh();
        
        meshGenerated = true;
    }
    
    private void SetupComponents()
    {
        // MeshFilter
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
            
        // MeshRenderer
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = settings.seafloorMaterial;
        }
        
        // MeshCollider
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.convex = false; // Para terreno estático
        }
    }
    
    private void GenerateHeightMap()
    {
        heightMap = new float[chunkSize + 1, chunkSize + 1];
        
        Vector2 worldPosition = new Vector2(
            chunkCoordinate.x * chunkSize,
            chunkCoordinate.y * chunkSize
        );
        
        for (int y = 0; y <= chunkSize; y++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                Vector2 samplePoint = worldPosition + new Vector2(x, y);
                float noiseValue = NoiseGenerator.GenerateNoise(samplePoint, settings.terrainNoise);
                
                // Aplicar curva de altura
                heightMap[x, y] = settings.terrainNoise.heightCurve.Evaluate(noiseValue) * settings.heightScale;
            }
        }
    }
    
    private void GenerateNavigabilityMap()
    {
        navigabilityMap = new float[chunkSize + 1, chunkSize + 1];
        
        for (int y = 0; y <= chunkSize; y++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                float normalizedHeight = heightMap[x, y] / settings.heightScale;
                
                // Área navegable si está por debajo del umbral
                if (normalizedHeight <= settings.navigableHeightThreshold)
                {
                    navigabilityMap[x, y] = 1f; // Navegable
                }
                else
                {
                    navigabilityMap[x, y] = 0f; // No navegable
                }
            }
        }
    }
    
    private void GenerateMesh()
    {
        meshData = new MeshData(chunkSize + 1, chunkSize + 1);
        
        // Generar vértices
        for (int y = 0; y <= chunkSize; y++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                Vector3 vertexPosition = new Vector3(x, heightMap[x, y], y);
                Vector2 uv = new Vector2(x / (float)chunkSize, y / (float)chunkSize);
                
                // Color basado en navegabilidad
                Color vertexColor = navigabilityMap[x, y] > 0.5f ? Color.blue : Color.gray;
                
                meshData.AddVertex(vertexPosition, uv, vertexColor);
            }
        }
        
        // Generar triángulos
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int vertexIndex = y * (chunkSize + 1) + x;
                
                // Primer triángulo
                meshData.AddTriangle(vertexIndex, vertexIndex + chunkSize + 1, vertexIndex + 1);
                
                // Segundo triángulo
                meshData.AddTriangle(vertexIndex + 1, vertexIndex + chunkSize + 1, vertexIndex + chunkSize + 2);
            }
        }
        
        // Crear y asignar malla
        Mesh mesh = meshData.CreateMesh();
        mesh.name = $"TerrainChunk_{chunkCoordinate.x}_{chunkCoordinate.y}";
        
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        gameObject.SetActive(active);
    }
    
    public void SetLOD(int lodLevel)
    {
        currentLOD = Mathf.Clamp(lodLevel, 0, maxLOD);
        // Implementar diferentes niveles de detalle si es necesario
    }
    
    public bool IsPointNavigable(Vector2 localPoint)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(localPoint.x), 0, chunkSize);
        int y = Mathf.Clamp(Mathf.RoundToInt(localPoint.y), 0, chunkSize);
        
        return navigabilityMap[x, y] > 0.5f;
    }
    
    public float GetHeightAtPoint(Vector2 localPoint)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(localPoint.x), 0, chunkSize);
        int y = Mathf.Clamp(Mathf.RoundToInt(localPoint.y), 0, chunkSize);
        
        return heightMap[x, y];
    }
}