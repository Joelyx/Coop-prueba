using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Submarine compass system - displays heading information and navigation points
/// Similar to the sonar display but focused on directional orientation
/// </summary>
public class SubmarineCompass : MonoBehaviour
{
    [Header("Compass Settings")]
    public float compassRadius = 150f; // Radius of the compass display
    public float updateInterval = 0.1f; // How often to update the compass
    public bool showCardinalPoints = true; // Show N, S, E, W markers
    public bool showDegrees = true; // Show degree markings
    public bool showWaypoints = true; // Show navigation waypoints
    public float waypointDetectionRange = 500f; // Range to detect waypoints
    
    [Header("Visual Settings")]
    public Color compassColor = Color.green;
    public Color cardinalColor = Color.white;
    public Color waypointColor = Color.yellow;
    public Color dangerColor = Color.red; // For hazards/obstacles
    public float compassAlpha = 0.8f;
    public float markerSize = 10f;
    
    [Header("UI References")]
    public Canvas compassCanvas; // Canvas for compass UI
    public RectTransform compassContainer; // Container for compass elements
    public Text headingText; // Text showing current heading
    public Text depthText; // Text showing current depth
    public Text speedText; // Text showing current speed
    
    [Header("Prefabs")]
    public GameObject compassMarkerPrefab; // Prefab for compass markers
    public GameObject waypointMarkerPrefab; // Prefab for waypoint markers
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip waypointProximitySound;
    public AudioClip hazardWarningSound;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool showDebugGizmos = true;
    
    // Properties
    public float CurrentHeading { get; private set; }
    public float CurrentDepth { get; private set; }
    public float CurrentSpeed { get; private set; }
    public List<NavigationWaypoint> ActiveWaypoints { get; private set; } = new List<NavigationWaypoint>();
    
    // Private variables
    private Transform submarineTransform;
    private Rigidbody submarineRigidbody;
    private float lastUpdateTime;
    private Dictionary<string, RectTransform> compassMarkers = new Dictionary<string, RectTransform>();
    private Dictionary<NavigationWaypoint, RectTransform> waypointMarkers = new Dictionary<NavigationWaypoint, RectTransform>();
    private List<GameObject> markerPool = new List<GameObject>();
    
    // Events
    public System.Action<float> OnHeadingChanged;
    public System.Action<NavigationWaypoint> OnWaypointProximity;
    public System.Action<GameObject> OnHazardDetected;
    
    [System.Serializable]
    public class NavigationWaypoint
    {
        public string name;
        public Vector3 position;
        public float radius = 10f;
        public Color markerColor = Color.yellow;
        public bool isHazard = false;
        public bool isActive = true;
        
        public NavigationWaypoint(string waypointName, Vector3 waypointPosition)
        {
            name = waypointName;
            position = waypointPosition;
        }
    }
    
    private void Awake()
    {
        submarineTransform = transform;
        submarineRigidbody = GetComponent<Rigidbody>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        SetupCompassUI();
        CreateCompassMarkers();
        
        if (enableDebugLogs)
            Debug.Log($"[COMPASS] Initialized on {gameObject.name}");
    }
    
    private void Start()
    {
        // Find any existing waypoints in the scene
        FindSceneWaypoints();
        
        if (enableDebugLogs)
            Debug.Log($"[COMPASS] Found {ActiveWaypoints.Count} waypoints in scene");
    }
    
    private void SetupCompassUI()
    {
        if (compassCanvas == null)
        {
            // Create compass canvas if not assigned
            GameObject canvasObj = new GameObject("CompassCanvas");
            canvasObj.transform.SetParent(transform);
            compassCanvas = canvasObj.AddComponent<Canvas>();
            compassCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        if (compassContainer == null)
        {
            // Create compass container
            GameObject containerObj = new GameObject("CompassContainer");
            containerObj.transform.SetParent(compassCanvas.transform, false);
            compassContainer = containerObj.AddComponent<RectTransform>();
            
            // Position at top center of screen
            compassContainer.anchorMin = new Vector2(0.5f, 1f);
            compassContainer.anchorMax = new Vector2(0.5f, 1f);
            compassContainer.pivot = new Vector2(0.5f, 1f);
            compassContainer.anchoredPosition = new Vector2(0, -50);
            compassContainer.sizeDelta = new Vector2(compassRadius * 2, 100);
        }
        
        // Create heading text if not assigned
        if (headingText == null)
        {
            GameObject textObj = new GameObject("HeadingText");
            textObj.transform.SetParent(compassContainer, false);
            headingText = textObj.AddComponent<Text>();
            headingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headingText.fontSize = 24;
            headingText.alignment = TextAnchor.MiddleCenter;
            headingText.color = compassColor;
            
            RectTransform textRect = headingText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        
        // Create depth text
        if (depthText == null)
        {
            GameObject depthObj = new GameObject("DepthText");
            depthObj.transform.SetParent(compassContainer, false);
            depthText = depthObj.AddComponent<Text>();
            depthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            depthText.fontSize = 18;
            depthText.alignment = TextAnchor.MiddleCenter;
            depthText.color = compassColor;
            
            RectTransform depthRect = depthText.GetComponent<RectTransform>();
            depthRect.anchorMin = new Vector2(0.5f, 0);
            depthRect.anchorMax = new Vector2(0.5f, 0);
            depthRect.pivot = new Vector2(0.5f, 1);
            depthRect.anchoredPosition = new Vector2(0, -30);
            depthRect.sizeDelta = new Vector2(200, 30);
        }
    }
    
    private void CreateCompassMarkers()
    {
        if (showCardinalPoints)
        {
            CreateCardinalMarker("N", 0);
            CreateCardinalMarker("E", 90);
            CreateCardinalMarker("S", 180);
            CreateCardinalMarker("W", 270);
        }
        
        if (showDegrees)
        {
            // Create degree markers every 30 degrees
            for (int i = 0; i < 360; i += 30)
            {
                if (i % 90 != 0) // Skip cardinal points
                {
                    CreateDegreeMarker(i.ToString(), i);
                }
            }
        }
    }
    
    private void CreateCardinalMarker(string label, float angle)
    {
        GameObject markerObj = new GameObject($"Cardinal_{label}");
        markerObj.transform.SetParent(compassContainer, false);
        
        Text markerText = markerObj.AddComponent<Text>();
        markerText.text = label;
        markerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        markerText.fontSize = 20;
        markerText.fontStyle = FontStyle.Bold;
        markerText.alignment = TextAnchor.MiddleCenter;
        markerText.color = cardinalColor;
        
        RectTransform markerRect = markerObj.GetComponent<RectTransform>();
        markerRect.sizeDelta = new Vector2(30, 30);
        
        compassMarkers[label] = markerRect;
    }
    
    private void CreateDegreeMarker(string label, float angle)
    {
        GameObject markerObj = new GameObject($"Degree_{label}");
        markerObj.transform.SetParent(compassContainer, false);
        
        Text markerText = markerObj.AddComponent<Text>();
        markerText.text = label + "°";
        markerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        markerText.fontSize = 14;
        markerText.alignment = TextAnchor.MiddleCenter;
        markerText.color = compassColor * 0.7f;
        
        RectTransform markerRect = markerObj.GetComponent<RectTransform>();
        markerRect.sizeDelta = new Vector2(40, 20);
        
        compassMarkers[label] = markerRect;
    }
    
    private void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateCompass();
            lastUpdateTime = Time.time;
        }
    }
    
    private void UpdateCompass()
    {
        // Calculate current heading
        Vector3 forward = submarineTransform.forward;
        forward.y = 0; // Project to horizontal plane
        CurrentHeading = Quaternion.LookRotation(forward).eulerAngles.y;
        
        // Calculate depth (negative Y position)
        CurrentDepth = -submarineTransform.position.y;
        
        // Calculate speed
        if (submarineRigidbody != null)
        {
            CurrentSpeed = submarineRigidbody.linearVelocity.magnitude;
        }
        
        // Update UI
        UpdateHeadingDisplay();
        UpdateCompassMarkers();
        UpdateWaypoints();
        
        // Notify listeners
        OnHeadingChanged?.Invoke(CurrentHeading);
    }
    
    private void UpdateHeadingDisplay()
    {
        if (headingText != null)
        {
            headingText.text = $"{CurrentHeading:000}°";
        }
        
        if (depthText != null)
        {
            depthText.text = $"Depth: {CurrentDepth:F1}m";
        }
        
        if (speedText != null)
        {
            speedText.text = $"Speed: {CurrentSpeed:F1} m/s";
        }
    }
    
    private void UpdateCompassMarkers()
    {
        foreach (var marker in compassMarkers)
        {
            float markerAngle = 0;
            
            // Parse angle from marker name
            if (marker.Key == "N") markerAngle = 0;
            else if (marker.Key == "E") markerAngle = 90;
            else if (marker.Key == "S") markerAngle = 180;
            else if (marker.Key == "W") markerAngle = 270;
            else if (int.TryParse(marker.Key, out int angle))
            {
                markerAngle = angle;
            }
            
            // Calculate relative angle
            float relativeAngle = Mathf.DeltaAngle(CurrentHeading, markerAngle);
            
            // Only show markers within 90 degrees of current heading
            if (Mathf.Abs(relativeAngle) <= 90)
            {
                marker.Value.gameObject.SetActive(true);
                
                // Position marker on compass arc
                float x = Mathf.Sin(relativeAngle * Mathf.Deg2Rad) * compassRadius;
                marker.Value.anchoredPosition = new Vector2(x, 0);
                
                // Fade markers at edges
                float alpha = 1f - (Mathf.Abs(relativeAngle) / 90f) * 0.5f;
                var text = marker.Value.GetComponent<Text>();
                if (text != null)
                {
                    Color color = text.color;
                    color.a = alpha;
                    text.color = color;
                }
            }
            else
            {
                marker.Value.gameObject.SetActive(false);
            }
        }
    }
    
    private void UpdateWaypoints()
    {
        if (!showWaypoints) return;
        
        foreach (var waypoint in ActiveWaypoints)
        {
            if (!waypoint.isActive) continue;
            
            Vector3 directionToWaypoint = waypoint.position - submarineTransform.position;
            float distance = directionToWaypoint.magnitude;
            
            // Check if waypoint is within detection range
            if (distance <= waypointDetectionRange)
            {
                // Get or create marker for this waypoint
                RectTransform marker = GetOrCreateWaypointMarker(waypoint);
                
                // Calculate bearing to waypoint
                directionToWaypoint.y = 0; // Project to horizontal plane
                float bearingToWaypoint = Quaternion.LookRotation(directionToWaypoint).eulerAngles.y;
                float relativeBearing = Mathf.DeltaAngle(CurrentHeading, bearingToWaypoint);
                
                // Only show if within compass view
                if (Mathf.Abs(relativeBearing) <= 90)
                {
                    marker.gameObject.SetActive(true);
                    
                    // Position on compass
                    float x = Mathf.Sin(relativeBearing * Mathf.Deg2Rad) * compassRadius;
                    marker.anchoredPosition = new Vector2(x, -40); // Below main compass
                    
                    // Update marker appearance based on distance
                    UpdateWaypointMarkerAppearance(marker, waypoint, distance);
                    
                    // Check proximity
                    if (distance <= waypoint.radius)
                    {
                        OnWaypointProximity?.Invoke(waypoint);
                        PlayProximitySound(waypoint);
                    }
                }
                else
                {
                    marker.gameObject.SetActive(false);
                }
            }
            else if (waypointMarkers.ContainsKey(waypoint))
            {
                waypointMarkers[waypoint].gameObject.SetActive(false);
            }
        }
    }
    
    private RectTransform GetOrCreateWaypointMarker(NavigationWaypoint waypoint)
    {
        if (!waypointMarkers.ContainsKey(waypoint))
        {
            GameObject markerObj = GetPooledMarker();
            markerObj.name = $"Waypoint_{waypoint.name}";
            markerObj.transform.SetParent(compassContainer, false);
            
            // Add visual components
            if (markerObj.GetComponent<Image>() == null)
            {
                Image markerImage = markerObj.AddComponent<Image>();
                markerImage.color = waypoint.isHazard ? dangerColor : waypointColor;
                markerImage.sprite = CreateCircleSprite();
            }
            
            RectTransform markerRect = markerObj.GetComponent<RectTransform>();
            markerRect.sizeDelta = new Vector2(markerSize, markerSize);
            
            waypointMarkers[waypoint] = markerRect;
        }
        
        return waypointMarkers[waypoint];
    }
    
    private void UpdateWaypointMarkerAppearance(RectTransform marker, NavigationWaypoint waypoint, float distance)
    {
        Image markerImage = marker.GetComponent<Image>();
        if (markerImage != null)
        {
            // Pulse effect for close waypoints
            if (distance <= waypoint.radius * 2)
            {
                float pulse = Mathf.Sin(Time.time * 3f) * 0.2f + 0.8f;
                markerImage.color = waypoint.markerColor * pulse;
                
                if (waypoint.isHazard)
                {
                    markerImage.color = Color.Lerp(dangerColor, Color.white, pulse * 0.5f);
                }
            }
            else
            {
                // Fade with distance
                float alpha = 1f - (distance / waypointDetectionRange) * 0.5f;
                Color color = waypoint.isHazard ? dangerColor : waypoint.markerColor;
                color.a = alpha;
                markerImage.color = color;
            }
            
            // Scale based on distance
            float scale = Mathf.Lerp(1.5f, 0.8f, distance / waypointDetectionRange);
            marker.localScale = Vector3.one * scale;
        }
    }
    
    private GameObject GetPooledMarker()
    {
        foreach (var marker in markerPool)
        {
            if (!marker.activeInHierarchy)
            {
                marker.SetActive(true);
                return marker;
            }
        }
        
        // Create new marker if none available
        GameObject newMarker = new GameObject("PooledMarker");
        markerPool.Add(newMarker);
        return newMarker;
    }
    
    private Sprite CreateCircleSprite()
    {
        // Create a simple circle sprite procedurally
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = x - 16;
                float dy = y - 16;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (distance <= 14)
                {
                    pixels[y * 32 + x] = Color.white;
                }
                else
                {
                    pixels[y * 32 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
    
    private void FindSceneWaypoints()
    {
        // Find GameObjects tagged as waypoints
        GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("Waypoint");
        
        foreach (var obj in waypointObjects)
        {
            NavigationWaypoint waypoint = new NavigationWaypoint(obj.name, obj.transform.position);
            
            // Check if it's a hazard
            if (obj.CompareTag("Hazard") || obj.name.ToLower().Contains("hazard"))
            {
                waypoint.isHazard = true;
                waypoint.markerColor = dangerColor;
            }
            
            AddWaypoint(waypoint);
        }
    }
    
    private void PlayProximitySound(NavigationWaypoint waypoint)
    {
        if (audioSource == null) return;
        
        if (waypoint.isHazard && hazardWarningSound != null)
        {
            audioSource.PlayOneShot(hazardWarningSound);
        }
        else if (waypointProximitySound != null)
        {
            audioSource.PlayOneShot(waypointProximitySound);
        }
    }
    
    // Public methods
    public void AddWaypoint(NavigationWaypoint waypoint)
    {
        if (!ActiveWaypoints.Contains(waypoint))
        {
            ActiveWaypoints.Add(waypoint);
            
            if (enableDebugLogs)
                Debug.Log($"[COMPASS] Added waypoint: {waypoint.name} at {waypoint.position}");
        }
    }
    
    public void RemoveWaypoint(NavigationWaypoint waypoint)
    {
        if (ActiveWaypoints.Remove(waypoint))
        {
            if (waypointMarkers.ContainsKey(waypoint))
            {
                waypointMarkers[waypoint].gameObject.SetActive(false);
                waypointMarkers.Remove(waypoint);
            }
            
            if (enableDebugLogs)
                Debug.Log($"[COMPASS] Removed waypoint: {waypoint.name}");
        }
    }
    
    public void ClearWaypoints()
    {
        foreach (var marker in waypointMarkers.Values)
        {
            marker.gameObject.SetActive(false);
        }
        
        waypointMarkers.Clear();
        ActiveWaypoints.Clear();
        
        if (enableDebugLogs)
            Debug.Log("[COMPASS] Cleared all waypoints");
    }
    
    public NavigationWaypoint GetNearestWaypoint()
    {
        NavigationWaypoint nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var waypoint in ActiveWaypoints)
        {
            if (!waypoint.isActive) continue;
            
            float distance = Vector3.Distance(submarineTransform.position, waypoint.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = waypoint;
            }
        }
        
        return nearest;
    }
    
    public float GetBearingToWaypoint(NavigationWaypoint waypoint)
    {
        Vector3 direction = waypoint.position - submarineTransform.position;
        direction.y = 0;
        
        float bearing = Quaternion.LookRotation(direction).eulerAngles.y;
        return bearing;
    }
    
    public void SetCompassVisible(bool visible)
    {
        if (compassCanvas != null)
            compassCanvas.gameObject.SetActive(visible);
    }
    
    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        if (Application.isPlaying)
        {
            // Draw current heading
            Gizmos.color = Color.green;
            Vector3 forward = transform.forward;
            forward.y = 0;
            Gizmos.DrawRay(transform.position, forward.normalized * 10f);
            
            // Draw waypoints
            foreach (var waypoint in ActiveWaypoints)
            {
                Gizmos.color = waypoint.isHazard ? Color.red : Color.yellow;
                Gizmos.DrawWireSphere(waypoint.position, waypoint.radius);
                
                // Draw line to waypoint if in range
                float distance = Vector3.Distance(transform.position, waypoint.position);
                if (distance <= waypointDetectionRange)
                {
                    Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                    Gizmos.DrawLine(transform.position, waypoint.position);
                }
            }
            
            // Draw detection range
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, waypointDetectionRange);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Draw compass visualization
        Gizmos.color = compassColor;
        
        // Draw cardinal directions
        Gizmos.DrawRay(transform.position, Vector3.forward * 5f); // North
        Gizmos.DrawRay(transform.position, Vector3.right * 5f); // East
        Gizmos.DrawRay(transform.position, -Vector3.forward * 5f); // South
        Gizmos.DrawRay(transform.position, -Vector3.right * 5f); // West
        
        // Label cardinal points
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.forward * 5.5f, "N");
        UnityEditor.Handles.Label(transform.position + Vector3.right * 5.5f, "E");
        UnityEditor.Handles.Label(transform.position - Vector3.forward * 5.5f, "S");
        UnityEditor.Handles.Label(transform.position - Vector3.right * 5.5f, "W");
        #endif
    }
}
