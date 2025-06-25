using UnityEngine;

/// <summary>
/// Static utility class to test sonar functionality
/// </summary>
public static class SonarDebugger
{
    public static void TestSonar()
    {
        Debug.Log("=== SONAR DEBUG START ===");
        
        SubmarineSonar sonar = Object.FindFirstObjectByType<SubmarineSonar>();
        if (sonar == null)
        {
            Debug.LogError("No SubmarineSonar found in scene!");
            return;
        }
        
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
            if (wall != null)
            {
                float distance = Vector3.Distance(sonar.transform.position, wall.transform.position);
                Vector3 direction = (wall.transform.position - sonar.transform.position).normalized;
                float bearing = Vector3.SignedAngle(sonar.transform.forward, direction, Vector3.up);
                Debug.Log($"  - {wall.name}: Distance={distance:F1}m, Bearing={bearing:F1}°, Position={wall.transform.position}");
                
                // Check if this wall should be detected by sonar
                bool inRange = distance <= sonar.maxRange;
                bool inCone = Mathf.Abs(bearing) <= sonar.coneAngle * 0.5f;
                Debug.Log($"    In Range: {inRange}, In Cone: {inCone}");
                
                // Test direct raycast to this wall
                Vector3 directionToWall = (wall.transform.position - sonar.transform.position).normalized;
                RaycastHit hit;
                bool raycastHit = Physics.Raycast(sonar.transform.position, directionToWall, out hit, distance + 1f, sonar.wallLayers);
                Debug.Log($"    Direct raycast hit: {raycastHit}");
                if (raycastHit)
                {
                    Debug.Log($"    Hit object: {hit.collider.name} at distance {hit.distance:F1}m");
                }
            }
        }
        
        Debug.Log("Triggering sonar ping...");
        sonar.TriggerSonarPing();
        
        Debug.Log($"Last detections count: {sonar.LastDetections.Count}");
        foreach (var detection in sonar.LastDetections)
        {
            Debug.Log($"DETECTED: {detection.contactType} at {detection.position}, Distance: {detection.distance:F1}m, Bearing: {detection.bearing:F1}°");
        }
        
        Debug.Log("=== SONAR DEBUG END ===");
    }
    
    public static void TestRaycast()
    {
        Debug.Log("=== RAYCAST TEST START ===");
        
        SubmarineSonar sonar = Object.FindFirstObjectByType<SubmarineSonar>();
        if (sonar == null)
        {
            Debug.LogError("No SubmarineSonar found!");
            return;
        }
        
        // Test a simple forward raycast
        Vector3 origin = sonar.transform.position;
        Vector3 direction = sonar.transform.forward;
        float maxDistance = sonar.maxRange;
        LayerMask layers = sonar.wallLayers;
        
        Debug.Log($"Raycast from {origin} in direction {direction} for {maxDistance}m on layers {layers.value}");
        
        RaycastHit hit;
        bool hasHit = Physics.Raycast(origin, direction, out hit, maxDistance, layers);
        
        Debug.Log($"Raycast result: {hasHit}");
        if (hasHit)
        {
            Debug.Log($"Hit: {hit.collider.name} at {hit.point}, distance: {hit.distance:F1}m");
            Debug.Log($"Hit object tag: {hit.collider.tag}");
            Debug.Log($"Hit object layer: {hit.collider.gameObject.layer}");
        }
        
        // Test with Physics.RaycastAll to see all hits
        RaycastHit[] allHits = Physics.RaycastAll(origin, direction, maxDistance, layers);
        Debug.Log($"RaycastAll found {allHits.Length} hits");
        
        for (int i = 0; i < allHits.Length; i++)
        {
            Debug.Log($"Hit {i}: {allHits[i].collider.name} at distance {allHits[i].distance:F1}m");
        }
        
        Debug.Log("=== RAYCAST TEST END ===");
    }
}