using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple compass component for submarine navigation
/// </summary>
public class SimpleCompass : MonoBehaviour
{
    [Header("Compass Settings")]
    public float updateInterval = 0.1f;
    
    [Header("UI References")]
    public Text headingText;
    public Transform compassNeedle;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // Properties
    public float CurrentHeading { get; private set; }
    public float CurrentDepth { get; private set; }
    
    private Transform submarineTransform;
    private float lastUpdateTime;
    
    void Awake()
    {
        submarineTransform = transform;
        Debug.Log("[SimpleCompass] Initialized");
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateCompass();
            lastUpdateTime = Time.time;
        }
    }
    
    void UpdateCompass()
    {
        // Calculate heading
        Vector3 forward = submarineTransform.forward;
        forward.y = 0;
        if (forward != Vector3.zero)
        {
            CurrentHeading = Quaternion.LookRotation(forward).eulerAngles.y;
        }
        
        // Calculate depth
        CurrentDepth = -submarineTransform.position.y;
        
        // Update UI
        if (headingText != null)
        {
            headingText.text = string.Format("{0:000}°", CurrentHeading);
        }
        
        if (compassNeedle != null)
        {
            compassNeedle.rotation = Quaternion.Euler(0, 0, CurrentHeading);
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUI.color = Color.green;
        GUI.Label(new Rect(10, 10, 200, 20), string.Format("Heading: {0:F1}°", CurrentHeading));
        GUI.Label(new Rect(10, 30, 200, 20), string.Format("Depth: {0:F1}m", CurrentDepth));
    }
}
