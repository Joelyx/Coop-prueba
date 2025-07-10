using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual compass display component - creates a circular compass UI
/// Works in conjunction with SubmarineCompass to display heading information
/// </summary>
public class CompassDisplay2D : MonoBehaviour
{
    private SubmarineCompass compass;
    
    [Header("Display Settings")]
    public bool showCompassRing = true;
    public bool showHeadingIndicator = true;
    public bool showMiniMap = false;
    public float displayRadius = 100f;
    public int ringSegments = 36; // Number of segments in compass ring
    
    [Header("Visual Style")]
    public Color ringColor = new Color(0f, 1f, 0f, 0.5f);
    public Color indicatorColor = Color.white;
    public Color textColor = Color.green;
    public float lineWidth = 2f;
    
    [Header("UI Components")]
    public RectTransform compassRingContainer;
    public Image compassRingImage;
    public RectTransform headingIndicator;
    public Text digitalHeadingText;
    public Text coordinatesText;
    
    private LineRenderer compassRingRenderer;
    private float currentDisplayHeading = 0f;
    private float headingSmoothVelocity = 0f;
    
    void Start()
    {
        compass = FindFirstObjectByType<SubmarineCompass>();
        
        if (compass == null)
        {
            Debug.LogWarning("[CompassDisplay2D] No SubmarineCompass found in scene!");
            return;
        }
        
        SetupCompassDisplay();
    }
    
    void SetupCompassDisplay()
    {
        // Create compass ring if needed
        if (showCompassRing && compassRingContainer == null)
        {
            GameObject ringObj = new GameObject("CompassRing");
            ringObj.transform.SetParent(transform, false);
            compassRingContainer = ringObj.AddComponent<RectTransform>();
            compassRingContainer.sizeDelta = new Vector2(displayRadius * 2, displayRadius * 2);
            
            // Create ring visual
            CreateCompassRing();
        }
        
        // Create heading indicator
        if (showHeadingIndicator && headingIndicator == null)
        {
            GameObject indicatorObj = new GameObject("HeadingIndicator");
            indicatorObj.transform.SetParent(compassRingContainer, false);
            headingIndicator = indicatorObj.AddComponent<RectTransform>();
            
            // Create arrow shape
            Image arrow = indicatorObj.AddComponent<Image>();
            arrow.color = indicatorColor;
            arrow.sprite = CreateArrowSprite();
            headingIndicator.sizeDelta = new Vector2(20, 30);
            headingIndicator.anchoredPosition = new Vector2(0, displayRadius - 15);
        }
        
        // Create digital heading display
        if (digitalHeadingText == null)
        {
            GameObject textObj = new GameObject("DigitalHeading");
            textObj.transform.SetParent(transform, false);
            digitalHeadingText = textObj.AddComponent<Text>();
            digitalHeadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            digitalHeadingText.fontSize = 32;
            digitalHeadingText.fontStyle = FontStyle.Bold;
            digitalHeadingText.alignment = TextAnchor.MiddleCenter;
            digitalHeadingText.color = textColor;
            
            RectTransform textRect = digitalHeadingText.GetComponent<RectTransform>();
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(150, 50);
        }
    }
    
    void CreateCompassRing()
    {
        // Create a circular compass ring using LineRenderer
        GameObject lineObj = new GameObject("CompassRingLine");
        lineObj.transform.SetParent(compassRingContainer, false);
        
        // Add LineRenderer component
        compassRingRenderer = lineObj.AddComponent<LineRenderer>();
        compassRingRenderer.useWorldSpace = false;
        compassRingRenderer.material = new Material(Shader.Find("Sprites/Default"));
        compassRingRenderer.material.color = ringColor;
        compassRingRenderer.startWidth = lineWidth;
        compassRingRenderer.endWidth = lineWidth;
        compassRingRenderer.loop = true;
        
        // Create circle points
        compassRingRenderer.positionCount = ringSegments + 1;
        for (int i = 0; i <= ringSegments; i++)
        {
            float angle = (float)i / ringSegments * Mathf.PI * 2f;
            float x = Mathf.Sin(angle) * displayRadius;
            float y = Mathf.Cos(angle) * displayRadius;
            compassRingRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
        
        // Add tick marks for cardinal directions
        CreateCompassTicks();
    }
    
    void CreateCompassTicks()
    {
        // Create tick marks at N, E, S, W
        string[] cardinals = { "N", "E", "S", "W" };
        float[] angles = { 0, 90, 180, 270 };
        
        for (int i = 0; i < 4; i++)
        {
            // Create tick mark
            GameObject tickObj = new GameObject($"Tick_{cardinals[i]}");
            tickObj.transform.SetParent(compassRingContainer, false);
            
            RectTransform tickRect = tickObj.AddComponent<RectTransform>();
            Image tickImage = tickObj.AddComponent<Image>();
            tickImage.color = ringColor;
            
            // Position tick
            float rad = angles[i] * Mathf.Deg2Rad;
            float x = Mathf.Sin(rad) * displayRadius;
            float y = Mathf.Cos(rad) * displayRadius;
            tickRect.anchoredPosition = new Vector2(x, y);
            tickRect.sizeDelta = new Vector2(4, 20);
            tickRect.rotation = Quaternion.Euler(0, 0, -angles[i]);
            
            // Create label
            GameObject labelObj = new GameObject($"Label_{cardinals[i]}");
            labelObj.transform.SetParent(compassRingContainer, false);
            
            Text label = labelObj.AddComponent<Text>();
            label.text = cardinals[i];
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = textColor;
            
            RectTransform labelRect = label.GetComponent<RectTransform>();
            float labelDistance = displayRadius + 25;
            labelRect.anchoredPosition = new Vector2(
                Mathf.Sin(rad) * labelDistance,
                Mathf.Cos(rad) * labelDistance
            );
            labelRect.sizeDelta = new Vector2(30, 30);
        }
        
        // Add degree marks every 30 degrees
        for (int deg = 0; deg < 360; deg += 30)
        {
            if (deg % 90 == 0) continue; // Skip cardinal directions
            
            GameObject tickObj = new GameObject($"DegreeTick_{deg}");
            tickObj.transform.SetParent(compassRingContainer, false);
            
            RectTransform tickRect = tickObj.AddComponent<RectTransform>();
            Image tickImage = tickObj.AddComponent<Image>();
            tickImage.color = ringColor * 0.5f; // Dimmer for degree marks
            
            float rad = deg * Mathf.Deg2Rad;
            float x = Mathf.Sin(rad) * displayRadius;
            float y = Mathf.Cos(rad) * displayRadius;
            tickRect.anchoredPosition = new Vector2(x, y);
            tickRect.sizeDelta = new Vector2(2, 10);
            tickRect.rotation = Quaternion.Euler(0, 0, -deg);
        }
    }
    
    Sprite CreateArrowSprite()
    {
        // Create a simple arrow sprite
        Texture2D texture = new Texture2D(20, 30);
        Color[] pixels = new Color[20 * 30];
        
        // Draw arrow shape
        for (int y = 0; y < 30; y++)
        {
            for (int x = 0; x < 20; x++)
            {
                // Simple triangle shape
                int centerX = 10;
                int tipY = 25;
                
                if (y > 15) // Arrow head
                {
                    int width = (tipY - y) * 2;
                    if (x >= centerX - width/2 && x <= centerX + width/2)
                    {
                        pixels[y * 20 + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * 20 + x] = Color.clear;
                    }
                }
                else // Arrow shaft
                {
                    if (x >= 8 && x <= 12)
                    {
                        pixels[y * 20 + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * 20 + x] = Color.clear;
                    }
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 20, 30), new Vector2(0.5f, 0.5f));
    }
    
    void Update()
    {
        if (compass == null) return;
        
        // Smooth heading display
        currentDisplayHeading = Mathf.SmoothDampAngle(
            currentDisplayHeading, 
            compass.CurrentHeading, 
            ref headingSmoothVelocity, 
            0.1f
        );
        
        // Update compass ring rotation
        if (compassRingContainer != null)
        {
            compassRingContainer.rotation = Quaternion.Euler(0, 0, currentDisplayHeading);
        }
        
        // Update digital heading
        if (digitalHeadingText != null)
        {
            digitalHeadingText.text = $"{compass.CurrentHeading:000}°";
        }
        
        // Update coordinates if enabled
        if (coordinatesText != null)
        {
            Vector3 pos = compass.transform.position;
            coordinatesText.text = $"X: {pos.x:F1} Z: {pos.z:F1}\nDepth: {compass.CurrentDepth:F1}m";
        }
        
        // Update mini map if enabled
        if (showMiniMap)
        {
            UpdateMiniMap();
        }
    }
    
    void UpdateMiniMap()
    {
        // This would show a top-down view of nearby waypoints
        // Implementation depends on specific requirements
    }
    
    public void ToggleCompassVisibility()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
    
    public void SetCompassStyle(Color newRingColor, Color newTextColor)
    {
        ringColor = newRingColor;
        textColor = newTextColor;
        
        if (compassRingRenderer != null)
        {
            compassRingRenderer.material.color = ringColor;
        }
        
        if (digitalHeadingText != null)
        {
            digitalHeadingText.color = textColor;
        }
        
        // Update all tick marks and labels
        foreach (Transform child in compassRingContainer)
        {
            Image img = child.GetComponent<Image>();
            if (img != null && child.name.Contains("Tick"))
            {
                img.color = child.name.Contains("Degree") ? ringColor * 0.5f : ringColor;
            }
            
            Text txt = child.GetComponent<Text>();
            if (txt != null)
            {
                txt.color = textColor;
            }
        }
    }
    
    public void EnableMiniMap(bool enable)
    {
        showMiniMap = enable;
        // Additional setup for mini map would go here
    }
}
