using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

/// <summary>
/// Gestiona colliders optimizados para navegación de submarinos
/// Utiliza simplificación de mallas y pooling para mejor rendimiento
/// </summary>
public class CollisionManager : MonoBehaviour
{
    [Header("Configuración de Colliders")]
    public bool useSimplifiedColliders = true;
    public int colliderResolution = 4; // Simplificación: cada N vértices
    public float minColliderHeight = 2f;
    public LayerMask collisionLayers = -1;
    
    [Header("Pool de Colliders")]
    public int poolSize = 20;
    public bool enableColliderPool = true;
    
    [Header("Optimización")]
    public bool useConcaveColliders = false;
    public bool enableColliderBaking = true;
    public float colliderCullDistance = 200f;
    
    [Header("Debug")]
    public bool showColliderBounds = false;
    public Color colliderBoundsColor = Color.yellow;
    
    // Pool de mesh colliders
    private Queue<MeshCollider> colliderPool = new Queue<MeshCollider>();
    private Dictionary<Vector2Int, MeshCollider> activeColliders = new Dictionary<Vector2Int, MeshCollider>();
    
    // Cache de mallas simplificadas
    private Dictionary<Vector2Int, Mesh> simplifiedMeshCache = new Dictionary<Vector2Int, Mesh>();
    private const int MAX_CACHE_SIZE = 50;
    
    // Referencias
    private ChunkManager chunkManager;
    private Transform player;
    
    // Eventos
    public System.Action<Vector2Int> OnColliderGenerated;
    public System.Action<Vector2Int> OnColliderRemoved;
    
    private void Start()
    {
        chunkManager = FindFirstObjectByType<ChunkManager>();
        
        // Encontrar jugador
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        
        // Inicializar pool
        if (enableColliderPool)
        {
            InitializeColliderPool();
        }
        
        Debug.Log("[CollisionManager] Inicializado");
    }
    
    private void Update()
    {
        if (player != null)
        {
            UpdateColliderVisibility();
        }
    }
    
    private void InitializeColliderPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject colliderObj = new GameObject("PooledCollider");
            colliderObj.transform.SetParent(transform);
            colliderObj.SetActive(false);
            
            MeshCollider meshCollider = colliderObj.AddComponent<MeshCollider>();
            meshCollider.convex = !useConcaveColliders;
            meshCollider.enabled = false;
            
            colliderPool.Enqueue(meshCollider);
        }
        
        Debug.Log($"[CollisionManager] Pool inicializado con {poolSize} colliders");
    }
    
    /// <summary>
    /// Genera collider optimizado para un chunk
    /// </summary>
    public void GenerateColliderForChunk(TerrainChunk chunk)
    {
        if (chunk == null) return;
        
        Vector2Int chunkCoord = chunk.chunkCoordinate;
        
        // Verificar si ya existe
        if (activeColliders.ContainsKey(chunkCoord)) return;
        
        // Generar malla simplificada
        Mesh simplifiedMesh = GetSimplifiedMesh(chunk);
        if (simplifiedMesh == null) return;
        
        // Obtener collider del pool o crear nuevo
        MeshCollider meshCollider = GetColliderFromPool();
        if (meshCollider == null) return;
        
        // Configurar collider
        ConfigureCollider(meshCollider, simplifiedMesh, chunk);
        
        // Registrar como activo
        activeColliders[chunkCoord] = meshCollider;
        
        OnColliderGenerated?.Invoke(chunkCoord);
    }
    
    /// <summary>
    /// Remueve collider de un chunk
    /// </summary>
    public void RemoveColliderForChunk(Vector2Int chunkCoord)
    {
        if (activeColliders.TryGetValue(chunkCoord, out MeshCollider collider))
        {
            activeColliders.Remove(chunkCoord);
            ReturnColliderToPool(collider);
            
            OnColliderRemoved?.Invoke(chunkCoord);
        }
    }
    
    private Mesh GetSimplifiedMesh(TerrainChunk chunk)
    {
        Vector2Int chunkCoord = chunk.chunkCoordinate;
        
        // Verificar cache
        if (simplifiedMeshCache.TryGetValue(chunkCoord, out Mesh cachedMesh))
        {
            return cachedMesh;
        }
        
        // Generar nueva malla simplificada
        Mesh simplifiedMesh = useSimplifiedColliders ? 
            GenerateSimplifiedMesh(chunk) : 
            chunk.meshFilter.sharedMesh;
        
        // Guardar en cache
        if (simplifiedMesh != null && simplifiedMeshCache.Count < MAX_CACHE_SIZE)
        {
            simplifiedMeshCache[chunkCoord] = simplifiedMesh;
        }
        
        return simplifiedMesh;
    }
    
    private Mesh GenerateSimplifiedMesh(TerrainChunk chunk)
    {
        if (chunk.heightMap == null) return null;
        
        int originalSize = chunk.chunkSize + 1;
        int simplifiedSize = Mathf.CeilToInt((float)originalSize / colliderResolution);
        
        // Generar vértices simplificados
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        for (int y = 0; y < simplifiedSize; y++)
        {
            for (int x = 0; x < simplifiedSize; x++)
            {
                // Mapear a coordenadas originales
                int origX = Mathf.Min(x * colliderResolution, originalSize - 1);
                int origY = Mathf.Min(y * colliderResolution, originalSize - 1);
                
                float height = chunk.heightMap[origX, origY];
                
                // Solo incluir si cumple altura mínima para colliders
                if (height >= minColliderHeight)
                {
                    vertices.Add(new Vector3(origX, height, origY));
                }
                else
                {
                    vertices.Add(new Vector3(origX, minColliderHeight, origY));
                }
            }
        }
        
        // Generar triángulos simplificados
        for (int y = 0; y < simplifiedSize - 1; y++)
        {
            for (int x = 0; x < simplifiedSize - 1; x++)
            {
                int vertexIndex = y * simplifiedSize + x;
                
                // Solo generar triángulos para áreas navegables
                if (IsAreaNavigableForCollider(chunk, x, y))
                {
                    // Primer triángulo
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + simplifiedSize);
                    triangles.Add(vertexIndex + 1);
                    
                    // Segundo triángulo
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + simplifiedSize);
                    triangles.Add(vertexIndex + simplifiedSize + 1);
                }
            }
        }
        
        // Crear malla
        Mesh mesh = new Mesh();
        mesh.name = $"SimplifiedCollider_{chunk.chunkCoordinate.x}_{chunk.chunkCoordinate.y}";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // Optimizar para colliders
        if (enableColliderBaking)
        {
            mesh.Optimize();
        }
        
        return mesh;
    }
    
    private bool IsAreaNavigableForCollider(TerrainChunk chunk, int x, int y)
    {
        int origX = x * colliderResolution;
        int origY = y * colliderResolution;
        
        // Verificar área alrededor del punto
        int checkRadius = colliderResolution / 2;
        
        for (int dy = -checkRadius; dy <= checkRadius; dy++)
        {
            for (int dx = -checkRadius; dx <= checkRadius; dx++)
            {
                int checkX = Mathf.Clamp(origX + dx, 0, chunk.chunkSize);
                int checkY = Mathf.Clamp(origY + dy, 0, chunk.chunkSize);
                
                if (chunk.navigabilityMap[checkX, checkY] < 0.5f)
                {
                    return true; // Hay área no navegable, necesita collider
                }
            }
        }
        
        return false; // Área completamente navegable, no necesita collider denso
    }
    
    private MeshCollider GetColliderFromPool()
    {
        if (enableColliderPool && colliderPool.Count > 0)
        {
            MeshCollider collider = colliderPool.Dequeue();
            collider.gameObject.SetActive(true);
            collider.enabled = true;
            return collider;
        }
        
        // Crear nuevo si no hay disponibles
        GameObject colliderObj = new GameObject("TerrainCollider");
        colliderObj.transform.SetParent(transform);
        colliderObj.layer = (int)Mathf.Log(collisionLayers.value, 2);
        
        MeshCollider meshCollider = colliderObj.AddComponent<MeshCollider>();
        meshCollider.convex = !useConcaveColliders;
        
        return meshCollider;
    }
    
    private void ReturnColliderToPool(MeshCollider collider)
    {
        if (collider == null) return;
        
        // Limpiar configuración
        collider.sharedMesh = null;
        collider.enabled = false;
        collider.gameObject.SetActive(false);
        
        // Devolver al pool si hay espacio
        if (enableColliderPool && colliderPool.Count < poolSize)
        {
            colliderPool.Enqueue(collider);
        }
        else
        {
            // Destruir si el pool está lleno
            DestroyImmediate(collider.gameObject);
        }
    }
    
    private void ConfigureCollider(MeshCollider meshCollider, Mesh mesh, TerrainChunk chunk)
    {
        // Asignar malla
        meshCollider.sharedMesh = mesh;
        
        // Posicionar
        meshCollider.transform.position = chunk.transform.position;
        meshCollider.transform.rotation = chunk.transform.rotation;
        
        // Configurar propiedades
        meshCollider.convex = !useConcaveColliders;
        meshCollider.enabled = true;
        
        // Asignar material físico si está disponible
        PhysicsMaterial physicsMaterial = GetPhysicsMaterialForChunk(chunk);
        if (physicsMaterial != null)
        {
            meshCollider.material = physicsMaterial;
        }
    }
    
    private PhysicsMaterial GetPhysicsMaterialForChunk(TerrainChunk chunk)
    {
        // Crear material físico basado en propiedades del chunk
        // Esto puede expandirse para incluir diferentes materiales por bioma
        
        PhysicsMaterial material = new PhysicsMaterial("TerrainPhysics");
        material.dynamicFriction = 0.6f;
        material.staticFriction = 0.6f;
        material.bounciness = 0.0f;
        material.frictionCombine = PhysicsMaterialCombine.Average;
        material.bounceCombine = PhysicsMaterialCombine.Minimum;
        
        return material;
    }
    
    private void UpdateColliderVisibility()
    {
        if (player == null) return;
        
        Vector3 playerPos = player.position;
        
        // Lista de colliders a remover (fuera de distancia)
        List<Vector2Int> toRemove = new List<Vector2Int>();
        
        foreach (var kvp in activeColliders)
        {
            Vector2Int chunkCoord = kvp.Key;
            MeshCollider collider = kvp.Value;
            
            if (collider != null)
            {
                float distance = Vector3.Distance(playerPos, collider.transform.position);
                
                if (distance > colliderCullDistance)
                {
                    toRemove.Add(chunkCoord);
                }
            }
        }
        
        // Remover colliders distantes
        foreach (var coord in toRemove)
        {
            RemoveColliderForChunk(coord);
        }
    }
    
    // Métodos públicos de utilidad
    public bool HasColliderForChunk(Vector2Int chunkCoord)
    {
        return activeColliders.ContainsKey(chunkCoord);
    }
    
    public MeshCollider GetColliderForChunk(Vector2Int chunkCoord)
    {
        activeColliders.TryGetValue(chunkCoord, out MeshCollider collider);
        return collider;
    }
    
    public void ClearAllColliders()
    {
        List<Vector2Int> allCoords = new List<Vector2Int>(activeColliders.Keys);
        
        foreach (var coord in allCoords)
        {
            RemoveColliderForChunk(coord);
        }
        
        // Limpiar cache
        foreach (var mesh in simplifiedMeshCache.Values)
        {
            if (mesh != null)
            {
                DestroyImmediate(mesh);
            }
        }
        simplifiedMeshCache.Clear();
        
        Debug.Log("[CollisionManager] Todos los colliders limpiados");
    }
    
    public void SetColliderResolution(int resolution)
    {
        colliderResolution = Mathf.Max(1, resolution);
        
        // Limpiar cache para regenerar con nueva resolución
        simplifiedMeshCache.Clear();
    }
    
    public void SetUseConcaveColliders(bool useConcave)
    {
        useConcaveColliders = useConcave;
        
        // Actualizar colliders existentes
        foreach (var collider in activeColliders.Values)
        {
            if (collider != null)
            {
                collider.convex = !useConcaveColliders;
            }
        }
    }
    
    // Debug y herramientas
    private void OnDrawGizmos()
    {
        if (!showColliderBounds) return;
        
        Gizmos.color = colliderBoundsColor;
        
        foreach (var collider in activeColliders.Values)
        {
            if (collider != null && collider.sharedMesh != null)
            {
                Gizmos.DrawWireCube(
                    collider.bounds.center, 
                    collider.bounds.size
                );
            }
        }
    }
    
    [ContextMenu("Regenerate All Colliders")]
    private void RegenerateAllColliders()
    {
        ClearAllColliders();
        
        if (chunkManager != null)
        {
            // Regenerar colliders para chunks activos
            // Esto requeriría acceso a los chunks activos del ChunkManager
            Debug.Log("[CollisionManager] Regeneración de colliders iniciada");
        }
    }
    
    [ContextMenu("Print Collider Stats")]
    private void PrintColliderStats()
    {
        Debug.Log($"[CollisionManager] Estadísticas:");
        Debug.Log($"- Colliders activos: {activeColliders.Count}");
        Debug.Log($"- Pool disponible: {colliderPool.Count}");
        Debug.Log($"- Cache de mallas: {simplifiedMeshCache.Count}");
        Debug.Log($"- Resolución: {colliderResolution}");
        Debug.Log($"- Usa cóncavos: {useConcaveColliders}");
    }
}