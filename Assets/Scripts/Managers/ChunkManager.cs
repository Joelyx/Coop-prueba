using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Gestiona la carga, descarga y optimización de chunks de terreno
/// </summary>
public class ChunkManager : MonoBehaviour
{
    [Header("Configuración")]
    public TerrainSettings terrainSettings;
    public Transform viewer; // Jugador/cámara
    
    [Header("Pool de Chunks")]
    public GameObject chunkPrefab;
    public int poolSize = 50;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Diccionarios para gestión eficiente
    private Dictionary<Vector2Int, TerrainChunk> activeChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private Dictionary<Vector2Int, TerrainChunk> inactiveChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private Queue<TerrainChunk> chunkPool = new Queue<TerrainChunk>();
    
    // Coordenadas del viewer
    private Vector2Int currentViewerChunk;
    private Vector2Int previousViewerChunk;
    
    // Límites del mundo en chunks
    private int worldChunksX;
    private int worldChunksY;
    
    private void Start()
    {
        if (terrainSettings == null)
        {
            Debug.LogError("[ChunkManager] TerrainSettings no asignado!");
            return;
        }
        
        // Calcular límites del mundo
        worldChunksX = Mathf.CeilToInt((float)terrainSettings.worldWidth / terrainSettings.chunkSize);
        worldChunksY = Mathf.CeilToInt((float)terrainSettings.worldLength / terrainSettings.chunkSize);
        
        // Inicializar pool
        InitializeChunkPool();
        
        // Generar chunks iniciales
        UpdateChunks();
        
        Debug.Log($"[ChunkManager] Inicializado. Mundo: {worldChunksX}x{worldChunksY} chunks");
    }
    
    private void Update()
    {
        if (viewer != null)
        {
            // Calcular chunk actual del viewer
            currentViewerChunk = GetChunkCoordinateFromPosition(viewer.position);
            
            // Actualizar chunks si el viewer se ha movido a un chunk diferente
            if (currentViewerChunk != previousViewerChunk)
            {
                UpdateChunks();
                previousViewerChunk = currentViewerChunk;
            }
        }
    }
    
    private void InitializeChunkPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject chunkObj = CreateChunkGameObject();
            TerrainChunk chunk = chunkObj.GetComponent<TerrainChunk>();
            
            if (chunk == null)
                chunk = chunkObj.AddComponent<TerrainChunk>();
                
            chunk.SetActive(false);
            chunkPool.Enqueue(chunk);
        }
        
        Debug.Log($"[ChunkManager] Pool inicializado con {poolSize} chunks");
    }
    
    private GameObject CreateChunkGameObject()
    {
        GameObject chunkObj;
        
        if (chunkPrefab != null)
        {
            chunkObj = Instantiate(chunkPrefab, transform);
        }
        else
        {
            chunkObj = new GameObject("TerrainChunk");
            chunkObj.transform.SetParent(transform);
        }
        
        return chunkObj;
    }
    
    private void UpdateChunks()
    {
        // Determinar qué chunks deben estar activos
        HashSet<Vector2Int> chunksToActivate = new HashSet<Vector2Int>();
        
        for (int yOffset = -terrainSettings.renderDistance; yOffset <= terrainSettings.renderDistance; yOffset++)
        {
            for (int xOffset = -terrainSettings.renderDistance; xOffset <= terrainSettings.renderDistance; xOffset++)
            {
                Vector2Int chunkCoord = currentViewerChunk + new Vector2Int(xOffset, yOffset);
                
                // Verificar que esté dentro de los límites del mundo
                if (chunkCoord.x >= 0 && chunkCoord.x < worldChunksX &&
                    chunkCoord.y >= 0 && chunkCoord.y < worldChunksY)
                {
                    chunksToActivate.Add(chunkCoord);
                }
            }
        }
        
        // Desactivar chunks que ya no se necesitan
        List<Vector2Int> chunksToDeactivate = new List<Vector2Int>();
        foreach (var kvp in activeChunks)
        {
            if (!chunksToActivate.Contains(kvp.Key))
            {
                chunksToDeactivate.Add(kvp.Key);
            }
        }
        
        foreach (var coord in chunksToDeactivate)
        {
            DeactivateChunk(coord);
        }
        
        // Activar nuevos chunks
        foreach (var coord in chunksToActivate)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                ActivateChunk(coord);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[ChunkManager] Chunks activos: {activeChunks.Count}, Posición viewer: {currentViewerChunk}");
        }
    }
    
    private void ActivateChunk(Vector2Int chunkCoord)
    {
        TerrainChunk chunk = null;
        
        // Intentar reutilizar chunk inactivo
        if (inactiveChunks.TryGetValue(chunkCoord, out chunk))
        {
            inactiveChunks.Remove(chunkCoord);
        }
        // Obtener chunk del pool
        else if (chunkPool.Count > 0)
        {
            chunk = chunkPool.Dequeue();
            
            // Inicializar chunk
            Vector3 worldPosition = new Vector3(
                chunkCoord.x * terrainSettings.chunkSize,
                0,
                chunkCoord.y * terrainSettings.chunkSize
            );
            
            chunk.transform.position = worldPosition;
            chunk.Initialize(chunkCoord, terrainSettings.chunkSize, terrainSettings);
        }
        // Crear nuevo chunk si el pool está vacío
        else
        {
            GameObject chunkObj = CreateChunkGameObject();
            chunk = chunkObj.GetComponent<TerrainChunk>();
            
            if (chunk == null)
                chunk = chunkObj.AddComponent<TerrainChunk>();
                
            Vector3 worldPosition = new Vector3(
                chunkCoord.x * terrainSettings.chunkSize,
                0,
                chunkCoord.y * terrainSettings.chunkSize
            );
            
            chunk.transform.position = worldPosition;
            chunk.Initialize(chunkCoord, terrainSettings.chunkSize, terrainSettings);
            
            Debug.LogWarning($"[ChunkManager] Pool vacío, creando nuevo chunk en {chunkCoord}");
        }
        
        if (chunk != null)
        {
            chunk.SetActive(true);
            activeChunks[chunkCoord] = chunk;
            
            // Configurar LOD basado en distancia
            float distanceToViewer = Vector2.Distance(chunkCoord, currentViewerChunk);
            int lodLevel = Mathf.FloorToInt(distanceToViewer);
            chunk.SetLOD(lodLevel);
        }
    }
    
    private void DeactivateChunk(Vector2Int chunkCoord)
    {
        if (activeChunks.TryGetValue(chunkCoord, out TerrainChunk chunk))
        {
            activeChunks.Remove(chunkCoord);
            chunk.SetActive(false);
            
            // Mover a inactivos o devolver al pool
            if (inactiveChunks.Count < poolSize)
            {
                inactiveChunks[chunkCoord] = chunk;
            }
            else
            {
                chunkPool.Enqueue(chunk);
            }
        }
    }
    
    private Vector2Int GetChunkCoordinateFromPosition(Vector3 worldPosition)
    {
        int chunkX = Mathf.FloorToInt(worldPosition.x / terrainSettings.chunkSize);
        int chunkY = Mathf.FloorToInt(worldPosition.z / terrainSettings.chunkSize);
        
        return new Vector2Int(chunkX, chunkY);
    }
    
    // Métodos públicos para consultas
    public TerrainChunk GetChunkAtPosition(Vector3 worldPosition)
    {
        Vector2Int chunkCoord = GetChunkCoordinateFromPosition(worldPosition);
        activeChunks.TryGetValue(chunkCoord, out TerrainChunk chunk);
        return chunk;
    }
    
    public bool IsPointNavigable(Vector3 worldPosition)
    {
        TerrainChunk chunk = GetChunkAtPosition(worldPosition);
        if (chunk == null) return false;
        
        // Convertir a coordenadas locales del chunk
        Vector2 localPoint = new Vector2(
            worldPosition.x - chunk.transform.position.x,
            worldPosition.z - chunk.transform.position.z
        );
        
        return chunk.IsPointNavigable(localPoint);
    }
    
    public float GetHeightAtPosition(Vector3 worldPosition)
    {
        TerrainChunk chunk = GetChunkAtPosition(worldPosition);
        if (chunk == null) return 0f;
        
        // Convertir a coordenadas locales del chunk
        Vector2 localPoint = new Vector2(
            worldPosition.x - chunk.transform.position.x,
            worldPosition.z - chunk.transform.position.z
        );
        
        return chunk.GetHeightAtPoint(localPoint);
    }
    
    // Debug
    private void OnDrawGizmos()
    {
        if (!showDebugInfo || terrainSettings == null) return;
        
        // Dibujar límites del mundo
        Gizmos.color = Color.yellow;
        Vector3 worldSize = new Vector3(terrainSettings.worldWidth, 0, terrainSettings.worldLength);
        Gizmos.DrawWireCube(worldSize * 0.5f, worldSize);
        
        // Dibujar chunks activos
        Gizmos.color = Color.green;
        foreach (var chunk in activeChunks.Values)
        {
            if (chunk != null)
            {
                Vector3 chunkSize = Vector3.one * terrainSettings.chunkSize;
                Vector3 chunkCenter = chunk.transform.position + chunkSize * 0.5f;
                Gizmos.DrawWireCube(chunkCenter, chunkSize);
            }
        }
        
        // Dibujar posición del viewer
        if (viewer != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(viewer.position, 5f);
        }
    }
}