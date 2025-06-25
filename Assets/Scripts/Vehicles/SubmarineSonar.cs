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
    public ParticleSystem pingEffect;
    public Color normalPingColor = Color.cyan;
    public Color detectionPingColor = Color.red;
    
    [Header("Debug")]
    public bool enableDebugLogs = true; // NEW: Enable debug logging
    
    // Properties
    public bool CanPing => Time.time - lastPingTime >= pingCooldown;
    public List<SonarContact> LastDetections { get; private set; } = new List<SonarContact>();
    
    // Private variables
    private float lastPingTime = -999f;
    private Transform submarineTransform;
    
    // Events
    public System.Action<List<SonarContact>> OnSonarPing;
    public System.Action<SonarContact> OnContactDetected;
    
    [System.Serializable]
    public struct SonarContact
    {
        public GameObject target;
        public Vector3 position;
        public float distance;
        public float bearing; // Angle relative to submarine forward
        public string contactType;
        
        public SonarContact(GameObject obj, Vector3 pos, Transform submarineTransform)
        {
            target = obj;
            position = pos;
            distance = Vector3.Distance(submarineTransform.position, pos);
            
            Vector3 directionToTarget = (pos - submarineTransform.position).normalized;
            bearing = Vector3.SignedAngle(submarineTransform.forward, directionToTarget, Vector3.up);
            
            contactType = DetermineContactType(obj);
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
        
        if (enableDebugLogs)
            Debug.Log($"[SONAR] Scan complete. Found {detections.Count} contacts");
        
        // Play audio
        PlayPingAudio(detections);
        
        // Show visual effects
        ShowPingEffect(detections.Count > 0);
        
        // Notify listeners
        OnSonarPing?.Invoke(detections);
        
        // Individual contact notifications
        foreach (var contact in detections)
        {
            OnContactDetected?.Invoke(contact);
            if (enableDebugLogs)
                Debug.Log($"[SONAR] CONTACT: {contact.contactType} at {contact.position}, Distance: {contact.distance:F1}m, Bearing: {contact.bearing:F1}°");
        }
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
                        if (Vector3.Distance(existingContact.position, hit.point) < 3f) // Within 3m
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
    
    private void ShowPingEffect(bool hasDetections)
    {
        // Show particle effect
        if (pingEffect != null)
        {
            var main = pingEffect.main;
            main.startColor = hasDetections ? detectionPingColor : normalPingColor;
            pingEffect.Play();
        }
        
        // Show sonar cone briefly
        if (sonarConeRenderer != null)
        {
            StartCoroutine(ShowSonarCone(hasDetections));
        }
    }
    
    private System.Collections.IEnumerator ShowSonarCone(bool hasDetections)
    {
        sonarConeRenderer.material.color = hasDetections ? detectionPingColor : normalPingColor;
        sonarConeRenderer.enabled = true;
        
        // Draw cone lines
        DrawSonarCone();
        
        // Keep visible for a short time
        yield return new WaitForSeconds(0.5f);
        
        sonarConeRenderer.enabled = false;
    }
    
    private void DrawSonarCone()
    {
        if (sonarConeRenderer == null) return;
        
        int segments = 20;
        sonarConeRenderer.positionCount = segments + 1;
        
        Vector3 origin = transform.position;
        sonarConeRenderer.SetPosition(0, origin);
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = (-coneAngle * 0.5f) + (coneAngle * i / segments);
            Vector3 direction = Quaternion.AngleAxis(angle, transform.up) * transform.forward;
            Vector3 point = origin + direction * maxRange;
            sonarConeRenderer.SetPosition(i, point);
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