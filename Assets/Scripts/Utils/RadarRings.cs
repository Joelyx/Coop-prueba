using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates radar-style range rings dynamically
/// </summary>
public class RadarRings : MonoBehaviour
{
    [Header("Ring Settings")]
    public int numberOfRings = 4;
    public Color ringColor = new Color(0f, 1f, 0f, 0.3f);
    public float ringWidth = 1f;
    public float maxRadius = 180f;
    
    [Header("Center Dot")]
    public bool showCenterDot = true;
    public Color centerDotColor = Color.cyan;
    public float centerDotSize = 4f;
    
    [Header("Crosshairs")]
    public bool showCrosshairs = true;
    public Color crosshairColor = new Color(0f, 1f, 0f, 0.5f);
    public float crosshairWidth = 1f;
    
    private RectTransform rectTransform;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        CreateRadarElements();
    }
    
    void CreateRadarElements()
    {
        // Create range rings
        CreateRangeRings();
        
        // Create center dot
        if (showCenterDot)
            CreateCenterDot();
            
        // Create crosshairs
        if (showCrosshairs)
            CreateCrosshairs();
    }
    
    void CreateRangeRings()
    {
        for (int i = 1; i <= numberOfRings; i++)
        {
            float radius = (maxRadius / numberOfRings) * i;
            CreateRing(radius, $"RangeRing_{i}");
        }
    }
    
    void CreateRing(float radius, string name)
    {
        GameObject ringObj = new GameObject(name);
        ringObj.transform.SetParent(transform, false);
        
        // Add UI components
        ringObj.AddComponent<CanvasRenderer>();
        Image ringImage = ringObj.AddComponent<Image>();
        
        // Configure as circle
        ringImage.sprite = GetCircleSprite();
        ringImage.color = ringColor;
        ringImage.type = Image.Type.Simple;
        
        // Set size and position
        RectTransform ringRect = ringObj.GetComponent<RectTransform>();
        ringRect.anchorMin = Vector2.one * 0.5f;
        ringRect.anchorMax = Vector2.one * 0.5f;
        ringRect.anchoredPosition = Vector2.zero;
        ringRect.sizeDelta = Vector2.one * (radius * 2);
        
        // Create ring effect by adding a smaller circle inside
        GameObject innerRingObj = new GameObject($"{name}_Inner");
        innerRingObj.transform.SetParent(ringObj.transform, false);
        
        innerRingObj.AddComponent<CanvasRenderer>();
        Image innerRingImage = innerRingObj.AddComponent<Image>();
        
        // Configure inner circle (to create ring effect)
        innerRingImage.sprite = GetCircleSprite();
        innerRingImage.color = new Color(0, 0.2f, 0.1f, 0.8f); // Same as background
        innerRingImage.type = Image.Type.Simple;
        
        RectTransform innerRingRect = innerRingObj.GetComponent<RectTransform>();
        innerRingRect.anchorMin = Vector2.one * 0.5f;
        innerRingRect.anchorMax = Vector2.one * 0.5f;
        innerRingRect.anchoredPosition = Vector2.zero;
        innerRingRect.sizeDelta = Vector2.one * ((radius - ringWidth) * 2);
    }
    
    void CreateCenterDot()
    {
        GameObject centerObj = new GameObject("CenterDot");
        centerObj.transform.SetParent(transform, false);
        
        centerObj.AddComponent<CanvasRenderer>();
        Image centerImage = centerObj.AddComponent<Image>();
        
        centerImage.sprite = GetCircleSprite();
        centerImage.color = centerDotColor;
        centerImage.type = Image.Type.Simple;
        
        RectTransform centerRect = centerObj.GetComponent<RectTransform>();
        centerRect.anchorMin = Vector2.one * 0.5f;
        centerRect.anchorMax = Vector2.one * 0.5f;
        centerRect.anchoredPosition = Vector2.zero;
        centerRect.sizeDelta = Vector2.one * centerDotSize;
    }
    
    void CreateCrosshairs()
    {
        // Vertical line
        CreateLine("VerticalCrosshair", Vector2.zero, Vector2.up * maxRadius, crosshairWidth);
        CreateLine("VerticalCrosshair2", Vector2.zero, Vector2.down * maxRadius, crosshairWidth);
        
        // Horizontal line  
        CreateLine("HorizontalCrosshair", Vector2.zero, Vector2.right * maxRadius, crosshairWidth);
        CreateLine("HorizontalCrosshair2", Vector2.zero, Vector2.left * maxRadius, crosshairWidth);
    }
    
    void CreateLine(string name, Vector2 start, Vector2 end, float width)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform, false);
        
        lineObj.AddComponent<CanvasRenderer>();
        Image lineImage = lineObj.AddComponent<Image>();
        
        lineImage.color = crosshairColor;
        
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        lineRect.anchorMin = Vector2.one * 0.5f;
        lineRect.anchorMax = Vector2.one * 0.5f;
        
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        
        lineRect.anchoredPosition = (start + end) * 0.5f;
        lineRect.sizeDelta = new Vector2(width, distance);
        lineRect.rotation = Quaternion.FromToRotation(Vector2.up, direction);
    }
    
    Sprite GetCircleSprite()
    {
        // Use Unity's built-in circle sprite
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
    }
    
    void OnValidate()
    {
        // Update in editor when values change
        if (Application.isPlaying && rectTransform != null)
        {
            // Clear existing elements
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(transform.GetChild(i).gameObject);
                else
                    DestroyImmediate(transform.GetChild(i).gameObject);
            }
            
            CreateRadarElements();
        }
    }
}