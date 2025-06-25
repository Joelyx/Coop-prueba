using UnityEngine;

public class QuickSonarTest : MonoBehaviour
{
    void Start()
    {
        // Wait a bit then test the sonar
        Invoke("TestSonar", 1f);
    }
    
    void TestSonar()
    {
        Debug.Log("=== QUICK SONAR TEST ===");
        
        SubmarineSonar sonar = FindFirstObjectByType<SubmarineSonar>();
        if (sonar == null)
        {
            Debug.LogError("No SubmarineSonar found!");
            return;
        }
        
        Debug.Log($"Sonar found on: {sonar.gameObject.name}");
        Debug.Log($"Position: {sonar.transform.position}");
        Debug.Log($"Forward: {sonar.transform.forward}");
        Debug.Log($"Can Ping: {sonar.CanPing}");
        
        // Count walls
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        Debug.Log($"Wall objects: {walls.Length}");
        
        foreach (GameObject wall in walls)
        {
            float distance = Vector3.Distance(sonar.transform.position, wall.transform.position);
            Debug.Log($"  {wall.name}: {distance:F1}m away at {wall.transform.position}");
        }
        
        // Trigger sonar
        Debug.Log("Triggering sonar...");
        sonar.TriggerSonarPing();
        
        Debug.Log($"Detections: {sonar.LastDetections.Count}");
        foreach (var contact in sonar.LastDetections)
        {
            Debug.Log($"DETECTED: {contact.contactType} - {contact.distance:F1}m - {contact.bearing:F1}°");
        }
    }
    
    void Update()
    {
        // Press SPACE to test sonar manually
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SubmarineSonar sonar = FindFirstObjectByType<SubmarineSonar>();
            if (sonar != null)
            {
                Debug.Log("Manual sonar test (SPACE pressed)");
                sonar.TriggerSonarPing();
            }
        }
    }
}