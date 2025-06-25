using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gestiona la navegabilidad del terreno y pathfinding para submarinos
/// </summary>
public class NavigationManager : MonoBehaviour
{
    [Header("Configuración")]
    public TerrainSettings terrainSettings;
    public ChunkManager chunkManager;
    
    [Header("Pathfinding")]
    public float pathfindingCellSize = 2f;
    public int maxPathLength = 1000;
    public bool useAStar = true;
    
    [Header("Validación de Rutas")]
    public float submarineRadius = 3f;
    public float submarineHeight = 8f;
    public LayerMask obstacleLayerMask = -1;
    
    [Header("Debug")]
    public bool showNavigationMesh = false;
    public bool showPaths = false;
    public Color navigableColor = Color.green;
    public Color blockedColor = Color.red;
    public Color pathColor = Color.blue;
    
    // Grid de navegación
    private NavigationNode[,] navigationGrid;
    private int gridWidth;
    private int gridHeight;
    
    // Cache de rutas calculadas
    private Dictionary<Vector2Int, List<Vector3>> pathCache = new Dictionary<Vector2Int, List<Vector3>>();
    private const int MAX_CACHE_SIZE = 100;
    
    // Eventos
    public System.Action<Vector3> OnNavigationBlocked;
    public System.Action<List<Vector3>> OnPathCalculated;
    
    [System.Serializable]
    private class NavigationNode
    {
        public Vector2Int gridPosition;
        public Vector3 worldPosition;
        public bool isNavigable;
        public float cost = 1f;
        public int biomeType = 0;
        
        // Para A*
        public float gCost = float.MaxValue;
        public float hCost = 0f;
        public float fCost => gCost + hCost;
        public NavigationNode parent;
        
        public NavigationNode(Vector2Int gridPos, Vector3 worldPos, bool navigable)
        {
            gridPosition = gridPos;
            worldPosition = worldPos;
            isNavigable = navigable;
        }
        
        public void Reset()
        {
            gCost = float.MaxValue;
            hCost = 0f;
            parent = null;
        }
    }
    
    private void Start()
    {
        if (terrainSettings == null)
        {
            Debug.LogError("[NavigationManager] TerrainSettings no asignado!");
            return;
        }
        
        // Esperar a que el terreno se genere
        StartCoroutine(WaitForTerrainGeneration());
    }
    
    private System.Collections.IEnumerator WaitForTerrainGeneration()
    {
        TerrainGenerator terrainGen = FindFirstObjectByType<TerrainGenerator>();
        
        if (terrainGen != null)
        {
            // Esperar a que el mundo se genere
            while (!terrainGen.IsWorldGenerated)
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            GenerateNavigationGrid();
        }
        else
        {
            Debug.LogWarning("[NavigationManager] No se encontró TerrainGenerator, generando grid básico");
            GenerateBasicNavigationGrid();
        }
    }
    
    private void GenerateNavigationGrid()
    {
        Debug.Log("[NavigationManager] Generando grid de navegación...");
        
        gridWidth = Mathf.CeilToInt(terrainSettings.worldWidth / pathfindingCellSize);
        gridHeight = Mathf.CeilToInt(terrainSettings.worldLength / pathfindingCellSize);
        
        navigationGrid = new NavigationNode[gridWidth, gridHeight];
        
        TerrainGenerator terrainGen = FindFirstObjectByType<TerrainGenerator>();
        
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = GridToWorldPosition(gridPos);
                
                // Verificar navegabilidad desde el terreno generado
                bool isNavigable = true;
                if (terrainGen != null)
                {
                    int terrainX = Mathf.RoundToInt(worldPos.x);
                    int terrainZ = Mathf.RoundToInt(worldPos.z);
                    isNavigable = terrainGen.IsWorldPositionNavigable(terrainX, terrainZ);
                    
                    // También verificar altura
                    if (isNavigable)
                    {
                        float height = terrainGen.GetHeightAtWorldPosition(terrainX, terrainZ);
                        worldPos.y = height;
                        
                        // Verificar clearance vertical para el submarino
                        isNavigable = ValidateVerticalClearance(worldPos);
                    }
                }
                
                NavigationNode node = new NavigationNode(gridPos, worldPos, isNavigable);
                
                // Configurar costo basado en bioma si está disponible
                if (terrainGen != null && isNavigable)
                {
                    int biome = terrainGen.GetBiomeAtWorldPosition(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
                    node.biomeType = biome;
                    node.cost = GetBiomeCost(biome);
                }
                
                navigationGrid[x, y] = node;
            }
        }
        
        Debug.Log($"[NavigationManager] Grid de navegación generado: {gridWidth}x{gridHeight} nodos");
    }
    
    private void GenerateBasicNavigationGrid()
    {
        Debug.Log("[NavigationManager] Generando grid básico de navegación...");
        
        gridWidth = Mathf.CeilToInt(terrainSettings.worldWidth / pathfindingCellSize);
        gridHeight = Mathf.CeilToInt(terrainSettings.worldLength / pathfindingCellSize);
        
        navigationGrid = new NavigationNode[gridWidth, gridHeight];
        
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = GridToWorldPosition(gridPos);
                
                // Navegabilidad básica - todo navegable excepto bordes
                bool isNavigable = x > 0 && x < gridWidth - 1 && y > 0 && y < gridHeight - 1;
                
                navigationGrid[x, y] = new NavigationNode(gridPos, worldPos, isNavigable);
            }
        }
        
        Debug.Log($"[NavigationManager] Grid básico generado: {gridWidth}x{gridHeight} nodos");
    }
    
    private bool ValidateVerticalClearance(Vector3 position)
    {
        // Verificar que hay suficiente espacio vertical para el submarino
        Vector3 topPosition = position + Vector3.up * submarineHeight;
        
        // Raycast hacia arriba para verificar obstáculos
        return !Physics.Raycast(position, Vector3.up, submarineHeight, obstacleLayerMask);
    }
    
    private float GetBiomeCost(int biomeIndex)
    {
        // Costo basado en tipo de bioma (puede expandirse)
        switch (biomeIndex)
        {
            case 0: return 1f;    // Llanura Abisal - fácil navegación
            case 1: return 3f;    // Formación Rocosa - navegación difícil
            case 2: return 1.5f;  // Cañón Submarino - navegación moderada
            case 3: return 2f;    // Arrecife Coralino - navegación complicada
            default: return 1f;
        }
    }
    
    /// <summary>
    /// Calcula una ruta desde origen a destino
    /// </summary>
    public List<Vector3> CalculatePath(Vector3 startPos, Vector3 endPos)
    {
        Vector2Int startGrid = WorldToGridPosition(startPos);
        Vector2Int endGrid = WorldToGridPosition(endPos);
        
        // Verificar cache
        Vector2Int cacheKey = new Vector2Int(
            startGrid.x * 10000 + startGrid.y,
            endGrid.x * 10000 + endGrid.y
        );
        
        if (pathCache.TryGetValue(cacheKey, out List<Vector3> cachedPath))
        {
            return cachedPath;
        }
        
        List<Vector3> path = useAStar ? 
            CalculatePathAStar(startGrid, endGrid) : 
            CalculatePathDijkstra(startGrid, endGrid);
        
        // Guardar en cache
        if (path != null && pathCache.Count < MAX_CACHE_SIZE)
        {
            pathCache[cacheKey] = path;
        }
        
        OnPathCalculated?.Invoke(path);
        return path;
    }
    
    private List<Vector3> CalculatePathAStar(Vector2Int start, Vector2Int end)
    {
        if (navigationGrid == null) return null;
        
        // Verificar que start y end son válidos
        if (!IsValidGridPosition(start) || !IsValidGridPosition(end)) return null;
        if (!navigationGrid[start.x, start.y].isNavigable || !navigationGrid[end.x, end.y].isNavigable) return null;
        
        // Reset del grid
        ResetNavigationGrid();
        
        List<NavigationNode> openSet = new List<NavigationNode>();
        HashSet<NavigationNode> closedSet = new HashSet<NavigationNode>();
        
        NavigationNode startNode = navigationGrid[start.x, start.y];
        NavigationNode endNode = navigationGrid[end.x, end.y];
        
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, endNode);
        openSet.Add(startNode);
        
        while (openSet.Count > 0 && openSet.Count < maxPathLength)
        {
            NavigationNode currentNode = openSet.OrderBy(n => n.fCost).ThenBy(n => n.hCost).First();
            
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            
            if (currentNode == endNode)
            {
                return RetracePath(startNode, endNode);
            }
            
            foreach (NavigationNode neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.isNavigable || closedSet.Contains(neighbor)) continue;
                
                float newGCost = currentNode.gCost + GetDistance(currentNode, neighbor) * neighbor.cost;
                
                if (newGCost < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newGCost;
                    neighbor.hCost = GetDistance(neighbor, endNode);
                    neighbor.parent = currentNode;
                    
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
        
        return null; // No se encontró ruta
    }
    
    private List<Vector3> CalculatePathDijkstra(Vector2Int start, Vector2Int end)
    {
        // Implementación simplificada de Dijkstra
        if (navigationGrid == null) return null;
        
        ResetNavigationGrid();
        
        var unvisited = new List<NavigationNode>();
        NavigationNode startNode = navigationGrid[start.x, start.y];
        NavigationNode endNode = navigationGrid[end.x, end.y];
        
        startNode.gCost = 0;
        
        // Agregar todos los nodos navegables
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (navigationGrid[x, y].isNavigable)
                {
                    unvisited.Add(navigationGrid[x, y]);
                }
            }
        }
        
        while (unvisited.Count > 0)
        {
            NavigationNode current = unvisited.OrderBy(n => n.gCost).First();
            unvisited.Remove(current);
            
            if (current == endNode)
            {
                return RetracePath(startNode, endNode);
            }
            
            foreach (NavigationNode neighbor in GetNeighbors(current))
            {
                if (!neighbor.isNavigable || !unvisited.Contains(neighbor)) continue;
                
                float newCost = current.gCost + GetDistance(current, neighbor) * neighbor.cost;
                
                if (newCost < neighbor.gCost)
                {
                    neighbor.gCost = newCost;
                    neighbor.parent = current;
                }
            }
        }
        
        return null;
    }
    
    private List<NavigationNode> GetNeighbors(NavigationNode node)
    {
        List<NavigationNode> neighbors = new List<NavigationNode>();
        
        // 8 direcciones (incluye diagonales)
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                
                Vector2Int neighborPos = node.gridPosition + new Vector2Int(x, y);
                
                if (IsValidGridPosition(neighborPos))
                {
                    neighbors.Add(navigationGrid[neighborPos.x, neighborPos.y]);
                }
            }
        }
        
        return neighbors;
    }
    
    private float GetDistance(NavigationNode nodeA, NavigationNode nodeB)
    {
        Vector2Int posA = nodeA.gridPosition;
        Vector2Int posB = nodeB.gridPosition;
        
        int distX = Mathf.Abs(posA.x - posB.x);
        int distY = Mathf.Abs(posA.y - posB.y);
        
        // Distancia euclidiana para movimiento más suave
        return Mathf.Sqrt(distX * distX + distY * distY);
    }
    
    private List<Vector3> RetracePath(NavigationNode startNode, NavigationNode endNode)
    {
        List<Vector3> path = new List<Vector3>();
        NavigationNode currentNode = endNode;
        
        while (currentNode != startNode && currentNode != null)
        {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;
        }
        
        path.Add(startNode.worldPosition);
        path.Reverse();
        
        // Suavizar el path
        return SmoothPath(path);
    }
    
    private List<Vector3> SmoothPath(List<Vector3> originalPath)
    {
        if (originalPath.Count <= 2) return originalPath;
        
        List<Vector3> smoothedPath = new List<Vector3> { originalPath[0] };
        
        for (int i = 1; i < originalPath.Count - 1; i++)
        {
            Vector3 current = originalPath[i];
            Vector3 previous = smoothedPath[smoothedPath.Count - 1];
            Vector3 next = originalPath[i + 1];
            
            // Verificar si podemos hacer línea directa desde previous a next
            if (!HasObstaclesBetween(previous, next))
            {
                continue; // Saltar este punto
            }
            
            smoothedPath.Add(current);
        }
        
        smoothedPath.Add(originalPath[originalPath.Count - 1]);
        return smoothedPath;
    }
    
    private bool HasObstaclesBetween(Vector3 start, Vector3 end)
    {
        // Verificar si hay obstáculos entre dos puntos
        float distance = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;
        
        // Raycast con el radio del submarino
        return Physics.SphereCast(start, submarineRadius, direction, out RaycastHit hit, distance, obstacleLayerMask);
    }
    
    private void ResetNavigationGrid()
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                navigationGrid[x, y].Reset();
            }
        }
    }
    
    private bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth && gridPos.y >= 0 && gridPos.y < gridHeight;
    }
    
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / pathfindingCellSize);
        int y = Mathf.RoundToInt(worldPos.z / pathfindingCellSize);
        
        return new Vector2Int(
            Mathf.Clamp(x, 0, gridWidth - 1),
            Mathf.Clamp(y, 0, gridHeight - 1)
        );
    }
    
    private Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * pathfindingCellSize,
            0f,
            gridPos.y * pathfindingCellSize
        );
    }
    
    // Métodos públicos de utilidad
    public bool IsPositionNavigable(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        
        if (!IsValidGridPosition(gridPos)) return false;
        
        return navigationGrid[gridPos.x, gridPos.y].isNavigable;
    }
    
    public Vector3 GetNearestNavigablePosition(Vector3 worldPosition, float searchRadius = 10f)
    {
        Vector2Int centerGrid = WorldToGridPosition(worldPosition);
        int searchCells = Mathf.CeilToInt(searchRadius / pathfindingCellSize);
        
        float bestDistance = float.MaxValue;
        Vector3 bestPosition = worldPosition;
        
        for (int y = -searchCells; y <= searchCells; y++)
        {
            for (int x = -searchCells; x <= searchCells; x++)
            {
                Vector2Int checkPos = centerGrid + new Vector2Int(x, y);
                
                if (IsValidGridPosition(checkPos) && navigationGrid[checkPos.x, checkPos.y].isNavigable)
                {
                    Vector3 checkWorld = GridToWorldPosition(checkPos);
                    float distance = Vector3.Distance(worldPosition, checkWorld);
                    
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestPosition = checkWorld;
                    }
                }
            }
        }
        
        return bestPosition;
    }
    
    public void ClearPathCache()
    {
        pathCache.Clear();
    }
    
    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!showNavigationMesh || navigationGrid == null) return;
        
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                NavigationNode node = navigationGrid[x, y];
                
                Gizmos.color = node.isNavigable ? navigableColor : blockedColor;
                Gizmos.DrawWireCube(node.worldPosition, Vector3.one * pathfindingCellSize * 0.8f);
            }
        }
    }
    
    [ContextMenu("Regenerate Navigation Grid")]
    private void RegenerateNavigationGrid()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(WaitForTerrainGeneration());
        }
    }
}