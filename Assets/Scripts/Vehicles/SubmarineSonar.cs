using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Directional sonar system for submarine
/// Detects objects in a forward-facing cone when manually triggered
/// </summary>
public class SubmarineSonar : MonoBehaviour
{
    [Header("Sonar Detection")]
    public float maxRange = 100f;
    public float coneAngle = 60f; // Total cone angle in degrees
    public LayerMask detectionLayers = -1;
    public float pingCooldown = 2f; // Minimum time between pings
    
    [Header("Audio")]
    public AudioSource sonarAudioSource;
    public AudioClip pingSound;
    public AudioClip detectionSound;
    
    [Header("Visual Effects")]
    public LineRenderer sonarConeRenderer;
    public ParticleSystem pingEffect;
    public Color normalPingColor = Color.cyan;
    public Color detectionPingColor = Color.red;
    
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
            if (obj.CompareTag("Player")) return "Submarine";
            if (obj.CompareTag("Enemy")) return "Hostile";
            if (obj.name.ToLower().Contains("rock")) return "Terrain";
            if (obj.name.ToLower().Contains("fish")) return "Biological";
            return "Unknown";
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
    
    public void TriggerSonarPing()
    {
        if (!CanPing) return;
        
        lastPingTime = Time.time;
        
        // Perform detection
        List<SonarContact> detections = PerformSonarScan();
        LastDetections = detections;
        
        // Play audio
        PlayPingAudio(detections.Count > 0);
        
        // Show visual effects
        ShowPingEffect(detections.Count > 0);
        
        // Notify listeners
        OnSonarPing?.Invoke(detections);
        
        // Individual contact notifications
        foreach (var contact in detections)
        {
            OnContactDetected?.Invoke(contact);
        }
    }
    
    private List<SonarContact> PerformSonarScan()
    {
        List<SonarContact> contacts = new List<SonarContact>();
        
        // Get all colliders in range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, maxRange, detectionLayers);
        
        foreach (Collider collider in hitColliders)
        {
            // Skip self
            if (collider.transform == transform || collider.transform.IsChildOf(transform))
                continue;
                
            Vector3 directionToTarget = collider.transform.position - transform.position;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            
            // Check if target is within sonar cone
            if (angleToTarget <= coneAngle * 0.5f)
            {
                // Perform raycast to check line of sight
                RaycastHit hit;
                if (Physics.Raycast(transform.position, directionToTarget.normalized, out hit, maxRange, detectionLayers))
                {
                    if (hit.collider == collider)
                    {
                        SonarContact contact = new SonarContact(collider.gameObject, hit.point, transform);
                        contacts.Add(contact);
                    }
                }
            }
        }
        
        return contacts;
    }
    
    private void PlayPingAudio(bool hasDetections)
    {
        if (sonarAudioSource == null) return;
        
        if (hasDetections && detectionSound != null)
        {
            sonarAudioSource.pitch = 1.2f; // Higher pitch for detections
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
        detectionLayers = layers;
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
        
        // Draw cone
        Vector3 leftBoundary = Quaternion.AngleAxis(-coneAngle * 0.5f, transform.up) * transform.forward * maxRange;
        Vector3 rightBoundary = Quaternion.AngleAxis(coneAngle * 0.5f, transform.up) * transform.forward * maxRange;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        Gizmos.DrawLine(transform.position + leftBoundary, transform.position + rightBoundary);
    }
}