using UnityEngine;

/// <summary>
/// Simple test script to trigger sonar manually
/// Add this to any GameObject and use the context menu to test
/// </summary>
public class SonarTester : MonoBehaviour
{
    [ContextMenu("Test Sonar")]
    public void TestSonar()
    {
        SubmarineSonar sonar = FindFirstObjectByType<SubmarineSonar>();
        if (sonar != null)
        {
            Debug.Log("=== TESTING SONAR ===");
            Debug.Log($"Sonar found on: {sonar.gameObject.name}");
            Debug.Log($"Position: {sonar.transform.position}");
            Debug.Log($"Forward: {sonar.transform.forward}");
            Debug.Log($"Max Range: {sonar.maxRange}");
            Debug.Log($"Cone Angle: {sonar.coneAngle}");
            Debug.Log($"Detection Layers: {sonar.wallLayers.value}");
            Debug.Log($"Can Ping: {sonar.CanPing}");
            
            // Count wall objects
            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            Debug.Log($"Wall objects in scene: {walls.Length}");
            foreach (GameObject wall in walls)
            {
                float distance = Vector3.Distance(sonar.transform.position, wall.transform.position);
                Vector3 direction = (wall.transform.position - sonar.transform.position).normalized;
                float bearing = Vector3.SignedAngle(sonar.transform.forward, direction, Vector3.up);
                Debug.Log($"  - {wall.name}: Distance={distance:F1}m, Bearing={bearing:F1}°, Position={wall.transform.position}");
            }
            
            Debug.Log("Triggering sonar ping...");
            sonar.TriggerSonarPing();
            Debug.Log($"Last detections count: {sonar.LastDetections.Count}");
            
            foreach (var detection in sonar.LastDetections)
            {
                Debug.Log($"DETECTED: {detection.contactType} at {detection.position}, Distance: {detection.distance:F1}m, Bearing: {detection.bearing:F1}°");
            }
        }
        else
        {
            Debug.LogError("No SubmarineSonar found in scene!");
        }
    }
    
    [ContextMenu("Debug Wall Objects")]
    public void DebugWallObjects()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        Debug.Log("=== ALL OBJECTS IN SCENE ===");
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("cube"))
            {
                Debug.Log($"{obj.name}: Tag={obj.tag}, Position={obj.transform.position}, Layer={LayerMask.LayerToName(obj.layer)}({obj.layer})");
                
                Collider col = obj.GetComponent<Collider>();
                if (col != null)
                {
                    Debug.Log($"  - Has Collider: {col.GetType().Name}, Enabled: {col.enabled}");
                }
            }
        }
    }
    
    [ContextMenu("Manual Raycast Test")]
    public void ManualRaycastTest()
    {
        SubmarineSonar sonar = FindFirstObjectByType<SubmarineSonar>();
        if (sonar == null)
        {
            Debug.LogError("No sonar found!");
            return;
        }
        
        Debug.Log("=== MANUAL RAYCAST TEST ===");
        Debug.Log($"Starting from: {sonar.transform.position}");
        Debug.Log($"Forward direction: {sonar.transform.forward}");
        
        // Test a few rays in different directions
        for (int i = 0; i < 5; i++)
        {
            float angle = (-30f) + (i * 15f); // -30 to +30 degrees
            Vector3 rayDirection = Quaternion.AngleAxis(angle, sonar.transform.up) * sonar.transform.forward;
            
            RaycastHit hit;
            bool hasHit = Physics.Raycast(sonar.transform.position, rayDirection, out hit, sonar.maxRange, sonar.wallLayers);
            
            Debug.Log($"Ray {i} (angle {angle}°): Direction={rayDirection}, Hit={hasHit}");
            if (hasHit)
            {
                Debug.Log($"  - Hit: {hit.collider.name} at {hit.point}, Distance: {hit.distance:F1}m");
            }
            
            // Draw ray in scene view
            Debug.DrawRay(sonar.transform.position, rayDirection * sonar.maxRange, hasHit ? Color.red : Color.green, 5f);
        }
    }
    
    [ContextMenu("Check Physics Settings")]
    public void CheckPhysicsSettings()
    {
        Debug.Log("=== PHYSICS SETTINGS ===");
        Debug.Log($"Default Raycast Layers: {Physics.DefaultRaycastLayers}");
        Debug.Log($"All Layers: {Physics.AllLayers}");
        
        // Check layers 0-31
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                Debug.Log($"Layer {i}: {layerName}");
            }
        }
        
        // Test if our layer mask includes default layer
        LayerMask testMask = -1; // All layers
        bool includesDefault = (testMask.value & (1 << 0)) != 0;
        Debug.Log($"LayerMask -1 includes Default layer: {includesDefault}");
    }
}