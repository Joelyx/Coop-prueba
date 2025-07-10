using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple test script for the submarine compass
/// Allows manual testing of compass functionality
/// </summary>
public class CompassTester : MonoBehaviour
{
    [Header("Compass Reference")]
    public SubmarineCompass compass;
    
    [Header("Test Settings")]
    public bool autoAddTestWaypoints = true;
    public float waypointSpawnRadius = 200f;
    public int numberOfTestWaypoints = 5;
    
    [Header("Manual Controls")]
    public bool enableManualRotation = true;
    public float rotationSpeed = 30f; // Degrees per second
    public bool enableWaypointSpawning = true;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private void Start()
    {
        // Find compass if not assigned
        if (compass == null)
        {
            compass = GetComponent<SubmarineCompass>();
            if (compass == null)
            {
                compass = FindFirstObjectByType<SubmarineCompass>();
            }
        }
        
        if (compass == null)
        {
            Debug.LogError("[CompassTester] No SubmarineCompass found!");
            return;
        }
        
        // Add test waypoints if enabled
        if (autoAddTestWaypoints)
        {
            CreateTestWaypoints();
        }
    }
    
    private void CreateTestWaypoints()
    {
        for (int i = 0; i < numberOfTestWaypoints; i++)
        {
            // Create waypoint at random position
            float angle = (360f / numberOfTestWaypoints) * i;
            float distance = Random.Range(50f, waypointSpawnRadius);
            
            Vector3 waypointPos = transform.position + Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
            waypointPos.y = Random.Range(-50f, -10f); // Random depth
            
            SubmarineCompass.NavigationWaypoint waypoint = new SubmarineCompass.NavigationWaypoint(
                $"TestWP_{i + 1}",
                waypointPos
            );
            
            // Make some waypoints hazards
            if (i % 3 == 0)
            {
                waypoint.isHazard = true;
                waypoint.markerColor = Color.red;
            }
            else
            {
                waypoint.markerColor = new Color(
                    Random.Range(0.5f, 1f),
                    Random.Range(0.5f, 1f),
                    Random.Range(0.5f, 1f)
                );
            }
            
            compass.AddWaypoint(waypoint);
            
            Debug.Log($"[CompassTester] Created waypoint '{waypoint.name}' at {waypoint.position}");
        }
    }
    
    private void Update()
    {
        if (compass == null) return;
        
        HandleInput();
        
        if (showDebugInfo)
        {
            ShowDebugInfo();
        }
    }
    
    private void HandleInput()
    {
        // Manual rotation control
        if (enableManualRotation)
        {
            float rotation = 0f;
            
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    rotation = -rotationSpeed;
                }
                else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    rotation = rotationSpeed;
                }
                
                if (rotation != 0)
                {
                    transform.Rotate(0, rotation * Time.deltaTime, 0);
                }
            }
        }
        
        // Waypoint spawning
        if (enableWaypointSpawning && Keyboard.current != null)
        {
            // Press W to spawn waypoint at current position
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                SpawnWaypointAtPosition(transform.position + transform.forward * 50f);
            }
            
            // Press H to spawn hazard waypoint
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                SpawnHazardWaypoint();
            }
            
            // Press C to clear all waypoints
            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                compass.ClearWaypoints();
                Debug.Log("[CompassTester] Cleared all waypoints");
            }
            
            // Press N to find nearest waypoint
            if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                var nearest = compass.GetNearestWaypoint();
                if (nearest != null)
                {
                    float bearing = compass.GetBearingToWaypoint(nearest);
                    float distance = Vector3.Distance(transform.position, nearest.position);
                    Debug.Log($"[CompassTester] Nearest waypoint: {nearest.name} at bearing {bearing:F1}°, distance {distance:F1}m");
                }
            }
        }
        
        // Toggle compass visibility
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            compass.SetCompassVisible(!compass.compassCanvas.gameObject.activeSelf);
        }
    }
    
    private void SpawnWaypointAtPosition(Vector3 position)
    {
        int waypointCount = compass.ActiveWaypoints.Count;
        SubmarineCompass.NavigationWaypoint waypoint = new SubmarineCompass.NavigationWaypoint(
            $"UserWP_{waypointCount + 1}",
            position
        );
        
        waypoint.markerColor = Color.cyan;
        compass.AddWaypoint(waypoint);
        
        Debug.Log($"[CompassTester] Spawned waypoint at {position}");
    }
    
    private void SpawnHazardWaypoint()
    {
        // Spawn hazard in front of submarine
        Vector3 hazardPos = transform.position + transform.forward * Random.Range(30f, 100f);
        hazardPos += transform.right * Random.Range(-20f, 20f);
        
        SubmarineCompass.NavigationWaypoint hazard = new SubmarineCompass.NavigationWaypoint(
            $"Hazard_{compass.ActiveWaypoints.Count + 1}",
            hazardPos
        );
        
        hazard.isHazard = true;
        hazard.markerColor = Color.red;
        hazard.radius = 20f;
        
        compass.AddWaypoint(hazard);
        
        Debug.Log($"[CompassTester] Spawned hazard at {hazardPos}");
    }
    
    private void ShowDebugInfo()
    {
        // This will be shown in OnGUI for easy visualization
    }
    
    private void OnGUI()
    {
        if (!showDebugInfo || compass == null) return;
        
        GUI.color = Color.green;
        
        // Display compass info
        int y = 10;
        GUI.Label(new Rect(10, y, 300, 20), $"Heading: {compass.CurrentHeading:F1}°");
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), $"Depth: {compass.CurrentDepth:F1}m");
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), $"Speed: {compass.CurrentSpeed:F1} m/s");
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), $"Active Waypoints: {compass.ActiveWaypoints.Count}");
        y += 30;
        
        // Controls help
        GUI.color = Color.yellow;
        GUI.Label(new Rect(10, y, 300, 20), "=== CONTROLS ===");
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), "A/D or Arrow Keys - Rotate");
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), "W - Spawn Waypoint");
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), "H - Spawn Hazard");
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), "C - Clear Waypoints");
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), "N - Find Nearest Waypoint");
        y += 20;
        GUI.Label(new Rect(10, y, 300, 20), "V - Toggle Compass Visibility");
        y += 30;
        
        // Waypoint list
        if (compass.ActiveWaypoints.Count > 0)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(10, y, 300, 20), "=== WAYPOINTS ===");
            y += 20;
            
            foreach (var wp in compass.ActiveWaypoints)
            {
                float distance = Vector3.Distance(transform.position, wp.position);
                float bearing = compass.GetBearingToWaypoint(wp);
                
                GUI.color = wp.isHazard ? Color.red : Color.green;
                GUI.Label(new Rect(10, y, 400, 20), 
                    $"{wp.name}: {distance:F1}m at {bearing:F1}°");
                y += 20;
                
                if (y > Screen.height - 50) break; // Don't overflow screen
            }
        }
    }
    
    // Visual debugging
    private void OnDrawGizmos()
    {
        if (compass == null || !showDebugInfo) return;
        
        // Draw spawn radius
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, waypointSpawnRadius);
        
        // Draw current heading
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 20f);
        
        // Draw lines to all waypoints
        foreach (var wp in compass.ActiveWaypoints)
        {
            Gizmos.color = wp.isHazard ? Color.red : Color.yellow;
            Gizmos.DrawLine(transform.position, wp.position);
            Gizmos.DrawWireSphere(wp.position, 5f);
        }
    }
}
