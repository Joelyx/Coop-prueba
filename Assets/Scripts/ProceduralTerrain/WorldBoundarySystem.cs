using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de límites naturales del mundo con montañas y barreras
/// </summary>
public class WorldBoundarySystem : MonoBehaviour
{
    [Header("Configuración de Límites")]
    public TerrainSettings terrainSettings;
    public float boundaryWidth = 50f;
    public float boundaryHeight = 100f;
    public AnimationCurve boundaryHeightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Montañas Límite")]
    public GameObject[] mountainPrefabs;
    public float mountainDensity = 0.3f;
    public Vector2 mountainScaleRange = new Vector2(0.8f, 1.5f);
    public float mountainHeightVariation = 20f;
    
    [Header("Barreras Invisibles")]
    public bool useInvisibleBarriers = true;
    public float barrierHeight = 200f;
    public LayerMask barrierLayer = 1;
    
    [Header("Efectos de Límite")]
    public ParticleSystem boundaryEffect;
    public AudioSource boundaryAudio;
    public Color boundaryFogColor = new Color(0.2f, 0.1f, 0.1f, 1f);
    
    [Header("Debug")]
    public bool showBoundaryGizmos = true;
    public Color boundaryGizmosColor = Color.red;
    
    // Componentes del sistema
    private List<GameObject> boundaryMountains = new List<GameObject>();
    private List<GameObject> invisibleBarriers = new List<GameObject>();
    private Transform player;
    
    // Estado del sistema
    private bool boundariesGenerated = false;
    private Bounds worldBounds;
    
    // Eventos
    public System.Action OnBoundariesGenerated;
    public System.Action<Vector3> OnPlayerApproachingBoundary;
    public System.Action OnPlayerExitedBoundaryZone;
    
    private void Start()
    {
        // Encontrar jugador
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        
        // Configurar límites del mundo
        if (terrainSettings != null)
        {
            worldBounds = new Bounds(
                new Vector3(terrainSettings.worldWidth * 0.5f, 0, terrainSettings.worldLength * 0.5f),
                new Vector3(terrainSettings.worldWidth, boundaryHeight, terrainSettings.worldLength)
            );
        }
        
        // Generar límites
        GenerateWorldBoundaries();
        
        Debug.Log("[WorldBoundarySystem] Inicializado");
    }
    
    private void Update()
    {
        if (player != null && boundariesGenerated)
        {
            CheckPlayerProximityToBoundary();
        }
    }
    
    /// <summary>
    /// Genera todos los elementos de límite del mundo
    /// </summary>
    public void GenerateWorldBoundaries()
    {
        if (terrainSettings == null)
        {
            Debug.LogError("[WorldBoundarySystem] TerrainSettings no asignado!");
            return;
        }
        
        Debug.Log("[WorldBoundarySystem] Generando límites del mundo...");
        
        // Limpiar límites existentes
        ClearExistingBoundaries();
        
        // Generar montañas en los bordes
        GenerateBoundaryMountains();
        
        // Crear barreras invisibles
        if (useInvisibleBarriers)
        {
            CreateInvisibleBarriers();
        }
        
        // Configurar efectos de límite
        SetupBoundaryEffects();
        
        boundariesGenerated = true;
        OnBoundariesGenerated?.Invoke();
        
        Debug.Log($"[WorldBoundarySystem] Límites generados: {boundaryMountains.Count} montañas, {invisibleBarriers.Count} barreras");
    }
    
    private void GenerateBoundaryMountains()
    {
        if (mountainPrefabs == null || mountainPrefabs.Length == 0) return;
        
        // Crear contenedor para montañas
        GameObject mountainContainer = new GameObject("BoundaryMountains");
        mountainContainer.transform.SetParent(transform);
        
        // Generar montañas en cada borde
        GenerateMountainsOnEdge(EdgeType.North, mountainContainer.transform);
        GenerateMountainsOnEdge(EdgeType.South, mountainContainer.transform);
        GenerateMountainsOnEdge(EdgeType.East, mountainContainer.transform);
        GenerateMountainsOnEdge(EdgeType.West, mountainContainer.transform);
        
        // Generar montañas en las esquinas
        GenerateCornerMountains(mountainContainer.transform);
    }
    
    private void GenerateMountainsOnEdge(EdgeType edge, Transform parent)
    {
        Vector3 startPos, endPos, direction;
        GetEdgeParameters(edge, out startPos, out endPos, out direction);
        
        float edgeLength = Vector3.Distance(startPos, endPos);
        int mountainCount = Mathf.RoundToInt(edgeLength * mountainDensity * 0.01f);
        
        for (int i = 0; i < mountainCount; i++)
        {
            float t = (float)i / (mountainCount - 1);
            Vector3 position = Vector3.Lerp(startPos, endPos, t);
            
            // Añadir variación aleatoria
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
            position += perpendicular * Random.Range(-boundaryWidth * 0.3f, boundaryWidth * 0.3f);
            position += direction * Random.Range(-20f, 20f);
            
            // Calcular altura de la montaña
            float distanceFromEdge = GetDistanceFromWorldEdge(position);
            float heightMultiplier = boundaryHeightCurve.Evaluate(1f - (distanceFromEdge / boundaryWidth));
            float mountainHeight = boundaryHeight * heightMultiplier + Random.Range(-mountainHeightVariation, mountainHeightVariation);
            
            position.y = mountainHeight;
            
            // Instanciar montaña
            GameObject mountainPrefab = mountainPrefabs[Random.Range(0, mountainPrefabs.Length)];
            GameObject mountain = Instantiate(mountainPrefab, position, Quaternion.identity, parent);
            
            // Configurar escala y rotación
            float scale = Random.Range(mountainScaleRange.x, mountainScaleRange.y);
            mountain.transform.localScale = Vector3.one * scale;
            mountain.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            // Agregar componente de montaña límite
            BoundaryMountain boundaryComponent = mountain.AddComponent<BoundaryMountain>();
            boundaryComponent.Initialize(edge, distanceFromEdge);
            
            boundaryMountains.Add(mountain);
        }
    }
    
    private void GenerateCornerMountains(Transform parent)
    {
        Vector3[] corners = GetWorldCorners();
        
        foreach (Vector3 corner in corners)
        {
            // Generar varias montañas alrededor de cada esquina
            int cornerMountains = Random.Range(3, 6);
            
            for (int i = 0; i < cornerMountains; i++)
            {
                Vector3 position = corner + Random.insideUnitSphere * boundaryWidth * 0.5f;
                position.y = boundaryHeight + Random.Range(-mountainHeightVariation, mountainHeightVariation);
                
                GameObject mountainPrefab = mountainPrefabs[Random.Range(0, mountainPrefabs.Length)];
                GameObject mountain = Instantiate(mountainPrefab, position, Quaternion.identity, parent);
                
                float scale = Random.Range(mountainScaleRange.x * 1.2f, mountainScaleRange.y * 1.2f); // Esquinas más grandes
                mountain.transform.localScale = Vector3.one * scale;
                mountain.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                
                boundaryMountains.Add(mountain);
            }
        }
    }
    
    private void CreateInvisibleBarriers()
    {
        GameObject barrierContainer = new GameObject("InvisibleBarriers");
        barrierContainer.transform.SetParent(transform);
        barrierContainer.layer = (int)Mathf.Log(barrierLayer.value, 2);
        
        // Crear barreras en cada lado del mundo
        CreateBarrierWall(EdgeType.North, barrierContainer.transform);
        CreateBarrierWall(EdgeType.South, barrierContainer.transform);
        CreateBarrierWall(EdgeType.East, barrierContainer.transform);
        CreateBarrierWall(EdgeType.West, barrierContainer.transform);
    }
    
    private void CreateBarrierWall(EdgeType edge, Transform parent)
    {
        Vector3 startPos, endPos, direction;
        GetEdgeParameters(edge, out startPos, out endPos, out direction);
        
        // Extender más allá del mundo
        Vector3 extendedStart = startPos - direction * boundaryWidth;
        Vector3 extendedEnd = endPos + direction * boundaryWidth;
        
        GameObject barrier = new GameObject($"Barrier_{edge}");
        barrier.transform.SetParent(parent);
        
        // Posicionar en el centro de la barrera
        Vector3 center = (extendedStart + extendedEnd) * 0.5f;
        center.y = barrierHeight * 0.5f;
        barrier.transform.position = center;
        
        // Crear box collider
        BoxCollider boxCollider = barrier.AddComponent<BoxCollider>();
        
        float length = Vector3.Distance(extendedStart, extendedEnd);
        Vector3 size = new Vector3(
            edge == EdgeType.North || edge == EdgeType.South ? length : boundaryWidth,
            barrierHeight,
            edge == EdgeType.East || edge == EdgeType.West ? length : boundaryWidth
        );
        
        boxCollider.size = size;
        boxCollider.isTrigger = false;
        
        // Agregar componente de barrera
        BoundaryBarrier barrierComponent = barrier.AddComponent<BoundaryBarrier>();
        barrierComponent.Initialize(this);
        
        invisibleBarriers.Add(barrier);
    }
    
    private void SetupBoundaryEffects()
    {
        // Configurar efectos de partículas si están asignados
        if (boundaryEffect != null)
        {
            var main = boundaryEffect.main;
            main.startColor = boundaryFogColor;
            main.maxParticles = 1000;
            
            var shape = boundaryEffect.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(terrainSettings.worldWidth, boundaryHeight, terrainSettings.worldLength);
        }
        
        // Configurar audio ambiental
        if (boundaryAudio != null)
        {
            boundaryAudio.loop = true;
            boundaryAudio.volume = 0.3f;
            boundaryAudio.spatialBlend = 1f; // 3D audio
        }
    }
    
    private void CheckPlayerProximityToBoundary()
    {
        if (player == null) return;
        
        float distanceToEdge = GetDistanceFromWorldEdge(player.position);
        
        if (distanceToEdge <= boundaryWidth)
        {
            // Jugador se acerca al límite
            OnPlayerApproachingBoundary?.Invoke(player.position);
            
            // Aplicar efectos progresivos
            ApplyBoundaryEffects(distanceToEdge / boundaryWidth);
        }
        else
        {
            // Jugador salió de la zona de límite
            OnPlayerExitedBoundaryZone?.Invoke();
            RemoveBoundaryEffects();
        }
    }
    
    private void ApplyBoundaryEffects(float proximityFactor)
    {
        // proximityFactor: 1 = lejos del borde, 0 = en el borde
        float intensity = 1f - proximityFactor;
        
        // Efecto de niebla
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, boundaryFogColor, intensity * 0.5f);
        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, 0.1f, intensity);
        
        // Efectos de partículas
        if (boundaryEffect != null && !boundaryEffect.isPlaying)
        {
            boundaryEffect.Play();
        }
        
        // Audio ambiental
        if (boundaryAudio != null && !boundaryAudio.isPlaying)
        {
            boundaryAudio.Play();
        }
        
        if (boundaryAudio != null)
        {
            boundaryAudio.volume = intensity * 0.5f;
        }
    }
    
    private void RemoveBoundaryEffects()
    {
        // Restaurar efectos normales gradualmente
        if (boundaryEffect != null && boundaryEffect.isPlaying)
        {
            boundaryEffect.Stop();
        }
        
        if (boundaryAudio != null && boundaryAudio.isPlaying)
        {
            boundaryAudio.Stop();
        }
    }
    
    // Métodos de utilidad
    private void GetEdgeParameters(EdgeType edge, out Vector3 startPos, out Vector3 endPos, out Vector3 direction)
    {
        switch (edge)
        {
            case EdgeType.North:
                startPos = new Vector3(0, 0, terrainSettings.worldLength);
                endPos = new Vector3(terrainSettings.worldWidth, 0, terrainSettings.worldLength);
                direction = Vector3.forward;
                break;
            case EdgeType.South:
                startPos = new Vector3(0, 0, 0);
                endPos = new Vector3(terrainSettings.worldWidth, 0, 0);
                direction = Vector3.back;
                break;
            case EdgeType.East:
                startPos = new Vector3(terrainSettings.worldWidth, 0, 0);
                endPos = new Vector3(terrainSettings.worldWidth, 0, terrainSettings.worldLength);
                direction = Vector3.right;
                break;
            case EdgeType.West:
                startPos = new Vector3(0, 0, 0);
                endPos = new Vector3(0, 0, terrainSettings.worldLength);
                direction = Vector3.left;
                break;
            default:
                startPos = endPos = Vector3.zero;
                direction = Vector3.forward;
                break;
        }
    }
    
    private Vector3[] GetWorldCorners()
    {
        return new Vector3[]
        {
            new Vector3(0, 0, 0), // Suroeste
            new Vector3(terrainSettings.worldWidth, 0, 0), // Sureste
            new Vector3(0, 0, terrainSettings.worldLength), // Noroeste
            new Vector3(terrainSettings.worldWidth, 0, terrainSettings.worldLength) // Noreste
        };
    }
    
    private float GetDistanceFromWorldEdge(Vector3 position)
    {
        float distanceToNorth = terrainSettings.worldLength - position.z;
        float distanceToSouth = position.z;
        float distanceToEast = terrainSettings.worldWidth - position.x;
        float distanceToWest = position.x;
        
        return Mathf.Min(distanceToNorth, distanceToSouth, distanceToEast, distanceToWest);
    }
    
    private void ClearExistingBoundaries()
    {
        // Limpiar montañas
        foreach (GameObject mountain in boundaryMountains)
        {
            if (mountain != null)
            {
                DestroyImmediate(mountain);
            }
        }
        boundaryMountains.Clear();
        
        // Limpiar barreras
        foreach (GameObject barrier in invisibleBarriers)
        {
            if (barrier != null)
            {
                DestroyImmediate(barrier);
            }
        }
        invisibleBarriers.Clear();
    }
    
    // Métodos públicos
    public bool IsPositionNearBoundary(Vector3 position, float threshold = 50f)
    {
        return GetDistanceFromWorldEdge(position) <= threshold;
    }
    
    public Vector3 GetNearestSafePosition(Vector3 position, float safeDistance = 30f)
    {
        Vector3 safePosition = position;
        
        // Empujar hacia el centro si está muy cerca del borde
        if (position.x < safeDistance)
            safePosition.x = safeDistance;
        else if (position.x > terrainSettings.worldWidth - safeDistance)
            safePosition.x = terrainSettings.worldWidth - safeDistance;
            
        if (position.z < safeDistance)
            safePosition.z = safeDistance;
        else if (position.z > terrainSettings.worldLength - safeDistance)
            safePosition.z = terrainSettings.worldLength - safeDistance;
        
        return safePosition;
    }
    
    // Enums y clases auxiliares
    public enum EdgeType
    {
        North, South, East, West
    }
    
    // Debug
    private void OnDrawGizmos()
    {
        if (!showBoundaryGizmos || terrainSettings == null) return;
        
        Gizmos.color = boundaryGizmosColor;
        
        // Dibujar límites del mundo
        Vector3 center = new Vector3(terrainSettings.worldWidth * 0.5f, 0, terrainSettings.worldLength * 0.5f);
        Vector3 size = new Vector3(terrainSettings.worldWidth, 10f, terrainSettings.worldLength);
        Gizmos.DrawWireCube(center, size);
        
        // Dibujar zona de límite
        Gizmos.color = Color.yellow;
        Vector3 boundarySize = size + Vector3.one * boundaryWidth * 2;
        Gizmos.DrawWireCube(center, boundarySize);
    }
    
    [ContextMenu("Regenerate Boundaries")]
    private void RegenerateBoundaries()
    {
        if (Application.isPlaying)
        {
            GenerateWorldBoundaries();
        }
    }
}

/// <summary>
/// Componente para montañas del límite
/// </summary>
public class BoundaryMountain : MonoBehaviour
{
    public WorldBoundarySystem.EdgeType edge;
    public float distanceFromEdge;
    
    public void Initialize(WorldBoundarySystem.EdgeType edgeType, float distance)
    {
        edge = edgeType;
        distanceFromEdge = distance;
    }
}

/// <summary>
/// Componente para barreras invisibles
/// </summary>
public class BoundaryBarrier : MonoBehaviour
{
    private WorldBoundarySystem boundarySystem;
    
    public void Initialize(WorldBoundarySystem system)
    {
        boundarySystem = system;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("[BoundaryBarrier] Jugador alcanzó el límite del mundo");
            
            // Empujar al jugador hacia una posición segura
            Vector3 safePosition = boundarySystem.GetNearestSafePosition(collision.transform.position);
            collision.transform.position = safePosition;
        }
    }
}