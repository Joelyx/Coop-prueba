using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple on-screen display for sonar information
/// </summary>
public class SonarDisplay2D : MonoBehaviour
{
    private SubmarineSonar sonar;
    private GUIStyle labelStyle;
    
    [Header("Display Settings")]
    public bool showDebugInfo = false; // Toggle to show/hide debug information
    public bool enableSpaceKeyInput = true; // Toggle for space key input
    
    void Start()
    {
        sonar = FindFirstObjectByType<SubmarineSonar>();
        
        // Create GUI style
        labelStyle = new GUIStyle();
        labelStyle.fontSize = 16;
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontStyle = FontStyle.Bold;
    }
    
    void OnGUI()
    {
        if (sonar == null || !showDebugInfo) return;
        
        // Only show minimal debug info if enabled
        float y = 10;
        GUI.Label(new Rect(10, y, 280, 20), $"Contacts: {sonar.LastDetections.Count}", labelStyle);
        
        // Show detected contacts in a simple list
        if (sonar.LastDetections.Count > 0)
        {
            y += 25;
            foreach (var contact in sonar.LastDetections)
            {
                if (y > 200) break; // Don't overflow
                
                string contactInfo = $"• {contact.contactType}: {contact.distance:F1}m";
                GUI.Label(new Rect(10, y, 270, 15), contactInfo);
                y += 18;
            }
        }
    }
    
    void Update()
    {
        // Allow manual sonar activation with SPACE
        if (enableSpaceKeyInput && Keyboard.current != null && 
            Keyboard.current.spaceKey.wasPressedThisFrame && sonar != null)
        {
            Debug.Log("SPACE pressed - triggering sonar");
            sonar.TriggerSonarPing();
        }
    }
}