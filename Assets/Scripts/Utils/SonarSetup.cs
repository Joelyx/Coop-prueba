using UnityEngine;

/// <summary>
/// Utility script to automatically configure the sonar system
/// Run this in the editor to fix common sonar setup issues
/// </summary>
public class SonarSetup : MonoBehaviour
{
    [Header("Auto-Setup Options")]
    public bool setupLayers = true;
    public bool setupTags = true;
    public bool setupSonarComponents = true;
    public bool setupAudio = true;
    public bool setupVisuals = true;
    
    [ContextMenu("Setup Sonar System")]
    public void SetupSonarSystem()
    {
        SubmarineSonar sonar = FindFirstObjectByType<SubmarineSonar>();
        if (sonar == null)
        {
            Debug.LogError("No SubmarineSonar component found in the scene!");
            return;
        }
        
        Debug.Log("Starting Sonar System Setup...");
        
        if (setupLayers) SetupLayers(sonar);
        if (setupTags) SetupTags();
        if (setupSonarComponents) SetupSonarComponents(sonar);
        if (setupAudio) SetupAudio(sonar);
        if (setupVisuals) SetupVisuals(sonar);
        
        Debug.Log("Sonar System Setup Complete!");
    }
    
    private void SetupLayers(SubmarineSonar sonar)
    {
        Debug.Log("Setting up detection layers...");
        
        // Configure sonar to detect Default layer and any Wall objects
        LayerMask detectionMask = 0;
        detectionMask |= (1 << 0); // Default layer
        
        // Check if we have custom layers defined
        for (int i = 8; i < 32; i++) // User layers start at 8
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                if (layerName.ToLower().Contains("wall") || 
                    layerName.ToLower().Contains("terrain") || 
                    layerName.ToLower().Contains("obstacle"))
                {
                    detectionMask |= (1 << i);
                }
            }
        }
        
        sonar.SetDetectionLayers(detectionMask);
        Debug.Log($"Detection layers set to: {detectionMask.value}");
    }
    
    private void SetupTags()
    {
        Debug.Log("Setting up wall object tags...");
        
        // Find all objects that should be walls
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int wallCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            // Check if object name suggests it's a wall/obstacle
            string name = obj.name.ToLower();
            if (name.Contains("wall") || name.Contains("cube") && obj.name.Contains("("))
            {
                if (!obj.CompareTag("Wall"))
                {
                    try
                    {
                        obj.tag = "Wall";
                        wallCount++;
                        Debug.Log($"Tagged {obj.name} as Wall");
                    }
                    catch (UnityException e)
                    {
                        Debug.LogWarning($"Could not tag {obj.name}: {e.Message}");
                    }
                }
            }
        }
        
        Debug.Log($"Tagged {wallCount} objects as walls");
    }
    
    private void SetupSonarComponents(SubmarineSonar sonar)
    {
        Debug.Log("Setting up sonar components...");
        
        // Setup audio source
        if (sonar.sonarAudioSource == null)
        {
            AudioSource audioSource = sonar.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = sonar.gameObject.AddComponent<AudioSource>();
            }
            
            // Configure audio source for sonar
            audioSource.spatialBlend = 1f; // 3D audio
            audioSource.volume = 0.7f;
            audioSource.pitch = 1f;
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            
            // Use reflection to set the field since it's public
            var field = typeof(SubmarineSonar).GetField("sonarAudioSource");
            field.SetValue(sonar, audioSource);
            
            Debug.Log("Audio source configured");
        }
        
        // Setup LineRenderer for sonar cone visualization
        if (sonar.sonarConeRenderer == null)
        {
            LineRenderer lineRenderer = sonar.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = sonar.gameObject.AddComponent<LineRenderer>();
            }
            
            // Configure line renderer
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.positionCount = 0;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
            
            // Use reflection to set the field
            var field = typeof(SubmarineSonar).GetField("sonarConeRenderer");
            field.SetValue(sonar, lineRenderer);
            
            Debug.Log("Line renderer configured");
        }
    }
    
    private void SetupAudio(SubmarineSonar sonar)
    {
        Debug.Log("Setting up audio clips...");
        
        // Create simple audio clips if none exist
        // Note: In a real project, you'd assign actual audio files
        if (sonar.pingSound == null || sonar.detectionSound == null)
        {
            Debug.Log("Audio clips not assigned - assign them manually in the inspector");
        }
    }
    
    private void SetupVisuals(SubmarineSonar sonar)
    {
        Debug.Log("Setting up visual effects...");
        
        // Create particle system for ping effect if needed
        if (sonar.pingEffect == null)
        {
            GameObject pingEffectObj = new GameObject("SonarPingEffect");
            pingEffectObj.transform.SetParent(sonar.transform);
            pingEffectObj.transform.localPosition = Vector3.zero;
            
            ParticleSystem particles = pingEffectObj.AddComponent<ParticleSystem>();
            
            // Configure particle system for sonar ping
            var main = particles.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 10f;
            main.startSize = 0.1f;
            main.startColor = Color.cyan;
            main.maxParticles = 50;
            
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0.0f, 25)
            });
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = sonar.coneAngle;
            
            // Use reflection to set the field
            var field = typeof(SubmarineSonar).GetField("pingEffect");
            field.SetValue(sonar, particles);
            
            Debug.Log("Particle system created and configured");
        }
    }
    
    [ContextMenu("Test Sonar Ping")]
    public void TestSonarPing()
    {
        SubmarineSonar sonar = FindFirstObjectByType<SubmarineSonar>();
        if (sonar != null)
        {
            Debug.Log("Testing sonar ping...");
            sonar.TriggerSonarPing();
        }
        else
        {
            Debug.LogError("No SubmarineSonar found!");
        }
    }
    
    [ContextMenu("Debug Sonar Info")]
    public void DebugSonarInfo()
    {
        SubmarineSonar sonar = FindFirstObjectByType<SubmarineSonar>();
        if (sonar == null)
        {
            Debug.LogError("No SubmarineSonar found!");
            return;
        }
        
        Debug.Log("=== SONAR DEBUG INFO ===");
        Debug.Log($"Max Range: {sonar.maxRange}");
        Debug.Log($"Cone Angle: {sonar.coneAngle}");
        Debug.Log($"Wall Layers: {sonar.wallLayers.value}");
        Debug.Log($"Raycast Resolution: {sonar.raycastResolution}");
        Debug.Log($"Can Ping: {sonar.CanPing}");
        Debug.Log($"Audio Source: {(sonar.sonarAudioSource != null ? "Assigned" : "NULL")}");
        Debug.Log($"Line Renderer: {(sonar.sonarConeRenderer != null ? "Assigned" : "NULL")}");
        Debug.Log($"Ping Effect: {(sonar.pingEffect != null ? "Assigned" : "NULL")}");
        
        // Count wall objects
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        Debug.Log($"Wall objects found: {walls.Length}");
        
        foreach (GameObject wall in walls)
        {
            Debug.Log($"  - {wall.name} at {wall.transform.position}");
        }
    }
}