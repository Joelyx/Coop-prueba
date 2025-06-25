using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Directional sonar system for submarine
/// Detects objects in a forward-facing cone when manually triggered
/// </summary>
public class SubmarineSonar : MonoBehaviour
{
    [Header("Sonar Detection")]
    public float maxRange = 100f;
    public float coneAngle = 90f; // Total cone angle in degrees
    public LayerMask wallLayers = 1; // Only detect walls/terrain
    public float pingCooldown = 2f; // Minimum time between pings
    public int raycastResolution = 32; // Number of rays in the cone
    
    [Header("Audio")]
    public AudioSource sonarAudioSource;
    public AudioClip pingSound;
    public AudioClip detectionSound;
    public AudioClip structureDetectionSound; // New sound for structure detection
    
    [Header("Visual Effects")]
    public LineRenderer sonarConeRenderer;
    public LineRenderer sweepLineRenderer; // Sweepline animation
    public ParticleSystem pingEffect;
    public Color normalPingColor = Color.cyan;
    public Color detectionPingColor = Color.red;
    public Color sweepLineColor = Color.green;
    public float sweepSpeed = 45f; // Degrees per second
    public float fadeOutDuration = 1.5f; // Fade duration for contacts
    
    [Header("Debug")]
    public bool enableDebugLogs = true; // NEW: Enable debug logging
    
    // Properties
    public bool CanPing => Time.time - lastPingTime >= pingCooldown;
    public List<SonarContact> LastDetections { get; private set; } = new List<SonarContact>();
    
    // Private variables
    private float lastPingTime = -999f;
    private Transform submarineTransform;
    private bool isSweeping = false;
    private float currentSweepAngle = 0f;
    private List<SonarContact> activeContacts = new List<SonarContact>();
    private List<SonarContactRenderer> contactRenderers = new List<SonarContactRenderer>();
    
    // Events
    public System.Action<List<SonarContact>> OnSonarPing;
    public System.Action<SonarContact> OnContactDetected;

    // Helper class for individual contact rendering
    [System.Serializable]
    private class SonarContactRenderer
    {
        public SonarContact contact;
        public GameObject visualObject;
        public Renderer renderer;
        public float fadeStartTime;
        public bool isFading;
        
        public SonarContactRenderer(SonarContact sonarContact)
        {
            contact = sonarContact;
            fadeStartTime = -1f;
            isFading = false;
        }
        
        public void StartFade()
        {
            if (!isFading)
            {
                fadeStartTime = Time.time;
                isFading = true;
            }
        }
        
        public float GetFadeProgress(float fadeDuration)
        {
            if (!isFading) return 0f;
            return Mathf.Clamp01((Time.time - fadeStartTime) / fadeDuration);
        }
        
        public bool IsCompletelyFaded(float fadeDuration)
        {
            return isFading && (Time.time - fadeStartTime) >= fadeDuration;
        }
    }
    
    [System.Serializable]
    public struct SonarContact
    {
        public GameObject target;
        public Vector3 position;
        public float distance;
        public float bearing; // Angle relative to submarine forward
        public string contactType;
        public float detectionTime; // Time when this contact was detected by sweepline
        public bool isVisible; // Whether this contact is currently visible
        
        public SonarContact(GameObject obj, Vector3 pos, Transform submarineTransform)
        {
            target = obj;
            position = pos;
            distance = Vector3.Distance(submarineTransform.position, pos);
            
            Vector3 directionToTarget = (pos - submarineTransform.position).normalized;
            bearing = Vector3.SignedAngle(submarineTransform.forward, directionToTarget, Vector3.up);
            
            contactType = DetermineContactType(obj);
            detectionTime = -1f; // Not yet detected by sweepline
            isVisible = false;
        }
        
        private static string DetermineContactType(GameObject obj)
        {
            // Classify wall/terrain types for better navigation
            string name = obj.name.ToLower();
            
            if (obj.CompareTag("Wall") || name.Contains("wall")) return "Wall";
            if (obj.CompareTag("Terrain") || name.Contains("terrain") || name.Contains("ground")) return "Terrain";
            if (name.Contains("rock") || name.Contains("stone")) return "Rock";
            if (name.Contains("coral") || name.Contains("reef")) return "Coral";
            if (name.Contains("wreck") || name.Contains("ship")) return "Wreck";
            if (obj.CompareTag("Player")) return "Submarine";
            
            return "Obstacle";
        }
    }
    
    private void Awake()
    {
        submarineTransform = transform;
        
        // Setup audio source if not assigned
        if (sonarAudioSource == null)
            sonarAudioSource = gameObject.AddComponent<AudioSource>();
            
        ConfigureAudioSource();
        SetupVisualEffects();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[SONAR] Initialized on {gameObject.name}");
            Debug.Log($"[SONAR] Position: {transform.position}");
            Debug.Log($"[SONAR] Max Range: {maxRange}");
            Debug.Log($"[SONAR] Cone Angle: {coneAngle}");
            Debug.Log($"[SONAR] Detection Layers: {wallLayers.value}");
        }
    }
    
    private void Start()
    {
        if (enableDebugLogs)
        {
            // Count potential targets on Start
            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            Debug.Log($"[SONAR] Found {walls.Length} wall objects in scene");
            
            foreach (GameObject wall in walls)
            {
                float distance = Vector3.Distance(transform.position, wall.transform.position);
                Debug.Log($"[SONAR] Wall: {wall.name} at distance {distance:F1}m, position {wall.transform.position}");
            }
        }
    }
    
    private void ConfigureAudioSource()
    {
        if (sonarAudioSource != null)
        {
            sonarAudioSource.loop = false;
            sonarAudioSource.playOnAwake = false;
            sonarAudioSource.spatialBlend = 1f; // 3D audio
            sonarAudioSource.volume = 0.7f;
            sonarAudioSource.pitch = 1f;
        }
    }
    
    private void SetupVisualEffects()
    {
        // Setup cone renderer for sonar visualization
        if (sonarConeRenderer != null)
        {
            sonarConeRenderer.enabled = false;
            sonarConeRenderer.material = new Material(Shader.Find("Sprites/Default"));
            sonarConeRenderer.material.color = normalPingColor;
            sonarConeRenderer.startWidth = 0.1f;
            sonarConeRenderer.endWidth = maxRange * Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad) * 2f;
            sonarConeRenderer.useWorldSpace = true;
        }
        
        // Setup sweepline renderer
        if (sweepLineRenderer != null)
        {
            sweepLineRenderer.enabled = false;
            sweepLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            sweepLineRenderer.material.color = sweepLineColor;
            sweepLineRenderer.startWidth = 0.2f;
            sweepLineRenderer.endWidth = 0.2f;
            sweepLineRenderer.useWorldSpace = true;
            sweepLineRenderer.positionCount = 2;
        }
    }
    
    // NEW: Add context menu for easy testing
    [ContextMenu("Test Sonar Ping")]
    public void TestSonarFromContextMenu()
    {
        Debug.Log("[SONAR] Manual ping triggered from context menu");
        TriggerSonarPing();
    }
    
    public void TriggerSonarPing()
    {
        if (enableDebugLogs)
            Debug.Log($"[SONAR] TriggerSonarPing called. CanPing: {CanPing}");
            
        if (!CanPing) 
        {
            if (enableDebugLogs)
                Debug.Log($"[SONAR] Ping blocked by cooldown. Remaining: {GetCooldownRemaining():F1}s");
            return;
        }
        
        lastPingTime = Time.time;
        
        if (enableDebugLogs)
            Debug.Log("[SONAR] Starting sonar scan...");
        
        // Perform detection
        List<SonarContact> detections = PerformSonarScan();
        LastDetections = detections;
        activeContacts = new List<SonarContact>(detections);
        
        if (enableDebugLogs)
            Debug.Log($"[SONAR] Scan complete. Found {detections.Count} contacts");
        
        // Play audio
        PlayPingAudio(detections);
        
        // Start sweep animation
        StartSweepAnimation();
        
        // Notify listeners
        OnSonarPing?.Invoke(detections);
    }
    
    private List<SonarContact> PerformSonarScan()
    {
        List<SonarContact> contacts = new List<SonarContact>();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[SONAR] Scanning from {transform.position} with {raycastResolution} rays");
            Debug.Log($"[SONAR] Forward direction: {transform.forward}");
            Debug.Log($"[SONAR] Using layer mask: {wallLayers.value}");
        }
        
        // Cast multiple rays in a cone pattern to detect walls
        for (int i = 0; i < raycastResolution; i++)
        {
            // Calculate angle for this ray within the cone
            float angleStep = coneAngle / (raycastResolution - 1);
            float currentAngle = (-coneAngle * 0.5f) + (i * angleStep);
            
            // Calculate ray direction
            Vector3 rayDirection = Quaternion.AngleAxis(currentAngle, transform.up) * transform.forward;
            
            if (enableDebugLogs && i < 3) // Log first few rays to avoid spam
                Debug.Log($"[SONAR] Ray {i}: angle={currentAngle:F1}°, direction={rayDirection}");
            
            // Cast ray to detect walls
            RaycastHit hit;
            if (Physics.Raycast(transform.position, rayDirection, out hit, maxRange, wallLayers))
            {
                if (enableDebugLogs && i < 3)
                    Debug.Log($"[SONAR] Ray {i} HIT: {hit.collider.name} at {hit.point}, distance={hit.distance:F1}m");
                
                // Only add if it's a significant wall hit (not too close)
                if (hit.distance > 2f) // Minimum 2m to avoid detecting submarine itself
                {
                    SonarContact contact = new SonarContact(hit.collider.gameObject, hit.point, transform);
                    
                    // Check if we already have a contact very close to this position
                    bool isDuplicate = false;
                    foreach (var existingContact in contacts)
                    {
                        if (Vector3.Distance(existingContact.position, hit.point) < 1f) // Within 3m
                        {
                            isDuplicate = true;
                            break;
                        }
                    }
                    
                    if (!isDuplicate)
                    {
                        contacts.Add(contact);
                        if (enableDebugLogs)
                            Debug.Log($"[SONAR] Added contact: {contact.contactType} at {contact.position}");
                    }
                    else if (enableDebugLogs)
                    {
                        Debug.Log($"[SONAR] Skipped duplicate contact at {hit.point}");
                    }
                }
                else if (enableDebugLogs && i < 3)
                {
                    Debug.Log($"[SONAR] Ray {i} hit too close: {hit.distance:F1}m < 2m minimum");
                }
            }
            else if (enableDebugLogs && i < 3)
            {
                Debug.Log($"[SONAR] Ray {i} MISS: no hit within {maxRange}m");
            }
        }
        
        return contacts;
    }
    
    private void StartSweepAnimation()
    {
        if (isSweeping) return;
        
        isSweeping = true;
        currentSweepAngle = -coneAngle * 0.5f;
        
        // Clear previous contact renderers
        CleanupContactRenderers();
        
        // Create visual objects for all contacts (initially invisible)
        foreach (var contact in activeContacts)
        {
            CreateContactVisual(contact);
        }
        
        // Show sweepline
        if (sweepLineRenderer != null)
        {
            sweepLineRenderer.enabled = true;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SONAR] Started sweep animation with {activeContacts.Count} contacts");
    }
    
    private void Update()
    {
        if (isSweeping)
        {
            UpdateSweepAnimation();
        }
        
        UpdateContactFades();
    }
    
    private void UpdateSweepAnimation()
    {
        // Move sweepline
        currentSweepAngle += sweepSpeed * Time.deltaTime;
        
        // Update sweepline visual
        UpdateSweepLineVisual();
        
        // Check which contacts should be revealed
        for (int i = 0; i < activeContacts.Count; i++)
        {
            var contact = activeContacts[i];
            if (!contact.isVisible && contact.bearing <= currentSweepAngle)
            {
                // Reveal this contact
                contact.isVisible = true;
                contact.detectionTime = Time.time;
                activeContacts[i] = contact;
                
                // Make visual object visible
                RevealContact(contact);
                
                // Notify listeners
                OnContactDetected?.Invoke(contact);
                
                if (enableDebugLogs)
                    Debug.Log($"[SONAR] CONTACT REVEALED: {contact.contactType} at bearing {contact.bearing:F1}°");
            }
        }
        
        // Check if sweep is complete
        if (currentSweepAngle >= coneAngle * 0.5f)
        {
            CompleteSweep();
        }
    }
    
    private void UpdateSweepLineVisual()
    {
        if (sweepLineRenderer == null) return;
        
        Vector3 sweepDirection = Quaternion.AngleAxis(currentSweepAngle, transform.up) * transform.forward;
        Vector3 origin = transform.position;
        Vector3 endPoint = origin + sweepDirection * maxRange;
        
        sweepLineRenderer.SetPosition(0, origin);
        sweepLineRenderer.SetPosition(1, endPoint);
    }
    
    private void CompleteSweep()
    {
        isSweeping = false;
        
        // Hide sweepline
        if (sweepLineRenderer != null)
        {
            sweepLineRenderer.enabled = false;
        }
        
        // Start fade timers for all visible contacts
        foreach (var contactRenderer in contactRenderers)
        {
            if (contactRenderer.contact.isVisible)
            {
                contactRenderer.StartFade();
            }
        }
        
        if (enableDebugLogs)
            Debug.Log("[SONAR] Sweep animation completed");
    }
    
    private void UpdateContactFades()
    {
        for (int i = contactRenderers.Count - 1; i >= 0; i--)
        {
            var contactRenderer = contactRenderers[i];
            
            if (contactRenderer.isFading)
            {
                float fadeProgress = contactRenderer.GetFadeProgress(fadeOutDuration);
                
                // Update visual alpha
                if (contactRenderer.renderer != null)
                {
                    var material = contactRenderer.renderer.material;
                    var color = material.color;
                    color.a = 1f - fadeProgress;
                    material.color = color;
                }
                
                // Remove if completely faded
                if (contactRenderer.IsCompletelyFaded(fadeOutDuration))
                {
                    if (contactRenderer.visualObject != null)
                    {
                        DestroyImmediate(contactRenderer.visualObject);
                    }
                    contactRenderers.RemoveAt(i);
                }
            }
        }
    }
    
    private void CreateContactVisual(SonarContact contact)
    {
        // Create a simple sphere to represent the contact
        GameObject contactVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        contactVisual.name = $"SonarContact_{contact.contactType}";
        contactVisual.transform.position = contact.position;
        contactVisual.transform.localScale = Vector3.one * 0.5f;
        
        // Setup material
        var renderer = contactVisual.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Standard"));
        material.color = GetContactColor(contact.contactType);
        renderer.material = material;
        
        // Initially invisible
        var color = material.color;
        color.a = 0f;
        material.color = color;
        contactVisual.SetActive(false);
        
        // Create contact renderer
        var contactRenderer = new SonarContactRenderer(contact);
        contactRenderer.visualObject = contactVisual;
        contactRenderer.renderer = renderer;
        
        contactRenderers.Add(contactRenderer);
    }
    
    private void RevealContact(SonarContact contact)
    {
        var contactRenderer = contactRenderers.Find(cr => 
            Vector3.Distance(cr.contact.position, contact.position) < 0.1f);
            
        if (contactRenderer?.visualObject != null)
        {
            contactRenderer.visualObject.SetActive(true);
            
            // Make fully visible
            if (contactRenderer.renderer != null)
            {
                var material = contactRenderer.renderer.material;
                var color = material.color;
                color.a = 1f;
                material.color = color;
            }
        }
    }
    
    private Color GetContactColor(string contactType)
    {
        switch (contactType.ToLower())
        {
            case "wall": return Color.red;
            case "terrain": return Color.yellow;
            case "rock": return Color.gray;
            case "coral": return Color.magenta;
            case "wreck": return Color.cyan;
            case "submarine": return Color.blue;
            default: return Color.white;
        }
    }
    
    private void CleanupContactRenderers()
    {
        foreach (var contactRenderer in contactRenderers)
        {
            if (contactRenderer.visualObject != null)
            {
                DestroyImmediate(contactRenderer.visualObject);
            }
        }
        contactRenderers.Clear();
    }
    
    private void OnDestroy()
    {
        CleanupContactRenderers();
    }
    
    private void PlayPingAudio(List<SonarContact> detections)
    {
        if (sonarAudioSource == null) return;
        
        // Check if we have structure detections
        bool hasStructures = detections.Any(contact => 
            contact.contactType.ToLower().Contains("wall") ||
            contact.contactType.ToLower().Contains("terrain") ||
            contact.contactType.ToLower().Contains("rock") ||
            contact.contactType.ToLower().Contains("wreck"));
            
        if (hasStructures && structureDetectionSound != null)
        {
            sonarAudioSource.pitch = 0.8f; // Lower pitch for structures
            sonarAudioSource.PlayOneShot(structureDetectionSound);
        }
        else if (detections.Count > 0 && detectionSound != null)
        {
            sonarAudioSource.pitch = 1.2f; // Higher pitch for other detections
            sonarAudioSource.PlayOneShot(detectionSound);
        }
        else if (pingSound != null)
        {
            sonarAudioSource.pitch = 1f;
            sonarAudioSource.PlayOneShot(pingSound);
        }
    }
    
    
    // Public methods for external control
    public void SetRange(float range)
    {
        maxRange = Mathf.Max(1f, range);
        SetupVisualEffects(); // Update visual cone
    }
    
    public void SetConeAngle(float angle)
    {
        coneAngle = Mathf.Clamp(angle, 10f, 180f);
        SetupVisualEffects(); // Update visual cone
    }
    
    public void SetDetectionLayers(LayerMask layers)
    {
        wallLayers = layers;
        if (enableDebugLogs)
            Debug.Log($"[SONAR] Detection layers updated to: {wallLayers.value}");
    }
    
    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, pingCooldown - (Time.time - lastPingTime));
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Draw sonar cone in editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxRange);
        
        // Draw cone boundaries
        Vector3 leftBoundary = Quaternion.AngleAxis(-coneAngle * 0.5f, transform.up) * transform.forward * maxRange;
        Vector3 rightBoundary = Quaternion.AngleAxis(coneAngle * 0.5f, transform.up) * transform.forward * maxRange;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        
        // Draw individual detection rays
        Gizmos.color = Color.green;
        for (int i = 0; i < raycastResolution; i++)
        {
            float angleStep = coneAngle / (raycastResolution - 1);
            float currentAngle = (-coneAngle * 0.5f) + (i * angleStep);
            Vector3 rayDirection = Quaternion.AngleAxis(currentAngle, transform.up) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + rayDirection * maxRange);
        }
        
        // Draw last detected contacts
        if (Application.isPlaying && LastDetections != null)
        {
            Gizmos.color = Color.red;
            foreach (var contact in LastDetections)
            {
                Gizmos.DrawSphere(contact.position, 1f);
            }
        }
    }
}