using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Cone-shaped sonar display that shows the actual detection area
/// </summary>
public class ConicSonarDisplay : MonoBehaviour
{
    [Header("Cone Display Settings")]
    public RectTransform displayPanel;
    public float coneRadius = 180f;
    public float coneAngle; // Should match sonar cone angle
    public Color coneBackgroundColor = new Color(0f, 0.2f, 0.1f, 0.4f);
    
    [Header("Contact Visualization")]
    public GameObject contactPrefab;
    public Color terrainContactColor = Color.yellow;
    
    [Header("Radar Noise Effect")]
    public int noisePointsPerContact = 5;
    public float noiseRadius = 8f;
    public float noiseIntensity = 0.6f;
    public float noiseFadeTime = 0.8f;
    
    [Header("Cone Visual Elements")]
    public bool showCenterLine = true;
    public Color centerLineColor = new Color(0f, 1f, 1f, 0.4f);
    public bool showConeBorders = true;
    public Color borderColor = new Color(0f, 1f, 0f, 0.6f);
    
    [Header("Sweep Animation")]
    public float sweepDuration = 2f;
    public Color sweepColor = Color.cyan;
    public AnimationCurve sweepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Debug")]
    public bool enableDebugLogs = false; // Disabled by default
    
    // Properties
    public bool IsActive { get; set; } = true;
    public float MaxDisplayRange { get; private set; } = 20f;
    
    // Private variables
    private SubmarineSonar sonarSystem;
    private List<GameObject> activeContacts = new List<GameObject>();
    private GameObject coneBackground;
    private GameObject sweepLine;
    private List<GameObject> coneBorders = new List<GameObject>();
    private bool isSweeping = false;
    private Canvas displayCanvas;
    
    // Store contacts for progressive reveal
    private List<SubmarineSonar.SonarContact> pendingContacts = new List<SubmarineSonar.SonarContact>();
    
    private void Awake()
    {
        sonarSystem = FindFirstObjectByType<SubmarineSonar>();
        displayCanvas = GetComponent<Canvas>();
        
        if (enableDebugLogs)
            Debug.Log("[CONIC SONAR DISPLAY] Initialized");
    }
    
    private void Start()
    {
        if (sonarSystem != null)
        {
            sonarSystem.OnSonarPing += HandleSonarPing;
            MaxDisplayRange = sonarSystem.maxRange;
            coneAngle = sonarSystem.coneAngle;
            
            if (enableDebugLogs)
                Debug.Log($"[CONIC SONAR DISPLAY] Connected to sonar. Range: {MaxDisplayRange}m, Angle: {coneAngle}°");
        }
        else
        {
            Debug.LogError("[CONIC SONAR DISPLAY] No SubmarineSonar found!");
        }
        
        CreateConeDisplay();
        SetDisplayActive(true); // Keep display always active
    }
    
    private void OnDestroy()
    {
        if (sonarSystem != null)
            sonarSystem.OnSonarPing -= HandleSonarPing;
    }
    
    private void CreateConeDisplay()
    {
        // Clear any existing elements first
        ClearDisplayElements();
        
        CreateConeBackground();
            
        if (showCenterLine)
            CreateCenterLine();
            
        if (showConeBorders)
            CreateConeBorders();
            
        CreateSweepLine();
    }
    
    private void ClearDisplayElements()
    {
        // Clear borders
        foreach (var border in coneBorders)
        {
            if (border != null) DestroyImmediate(border);
        }
        coneBorders.Clear();
        
        // Clear background
        if (coneBackground != null)
        {
            DestroyImmediate(coneBackground);
            coneBackground = null;
        }
        
        // Clear sweep line
        if (sweepLine != null)
        {
            DestroyImmediate(sweepLine);
            sweepLine = null;
        }
    }
    
    private void CreateConeBackground()
    {
        coneBackground = new GameObject("ConeBackground");
        coneBackground.transform.SetParent(displayPanel, false);
        
        RectTransform bgRect = coneBackground.AddComponent<RectTransform>();
        coneBackground.AddComponent<CanvasRenderer>();
        
        // Use a custom cone mesh instead of Image for better cone shape
        ConicMesh coneComponent = coneBackground.AddComponent<ConicMesh>();
        coneComponent.coneAngle = coneAngle;
        coneComponent.coneRadius = coneRadius;
        coneComponent.coneColor = coneBackgroundColor;
        
        bgRect.anchorMin = Vector2.one * 0.5f;
        bgRect.anchorMax = Vector2.one * 0.5f;
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = Vector2.one * coneRadius * 2;
    }
    
    private void CreateCenterLine()
    {
        GameObject centerLineObj = new GameObject("CenterLine");
        centerLineObj.transform.SetParent(displayPanel, false);
        
        RectTransform lineRect = centerLineObj.AddComponent<RectTransform>();
        centerLineObj.AddComponent<CanvasRenderer>();
        Image lineImage = centerLineObj.AddComponent<Image>();
        
        lineImage.color = centerLineColor;
        
        lineRect.anchorMin = Vector2.one * 0.5f;
        lineRect.anchorMax = Vector2.one * 0.5f;
        lineRect.anchoredPosition = new Vector2(0, coneRadius * 0.5f);
        lineRect.sizeDelta = new Vector2(1f, coneRadius);
    }
    
    private void CreateConeBorders()
    {
        // Left border line
        CreateBorderLine(-coneAngle * 0.5f, "LeftBorder");
        // Right border line  
        CreateBorderLine(coneAngle * 0.5f, "RightBorder");
    }
    
    private void CreateBorderLine(float angle, string name)
    {
        GameObject borderObj = new GameObject(name);
        borderObj.transform.SetParent(displayPanel, false);
        
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderObj.AddComponent<CanvasRenderer>();
        Image borderImage = borderObj.AddComponent<Image>();
        
        borderImage.color = borderColor;
        
        borderRect.anchorMin = Vector2.one * 0.5f;
        borderRect.anchorMax = Vector2.one * 0.5f;
        
        // Position and rotate the border line
        float angleRad = angle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
        Vector2 position = direction * (coneRadius * 0.5f);
        
        borderRect.anchoredPosition = position;
        borderRect.sizeDelta = new Vector2(1f, coneRadius);
        borderRect.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        coneBorders.Add(borderObj);
    }
    
    private void CreateSweepLine()
    {
        sweepLine = new GameObject("SweepLine");
        sweepLine.transform.SetParent(displayPanel, false);
        
        RectTransform sweepRect = sweepLine.AddComponent<RectTransform>();
        sweepLine.AddComponent<CanvasRenderer>();
        Image sweepImage = sweepLine.AddComponent<Image>();
        
        sweepImage.color = sweepColor;
        
        sweepRect.anchorMin = Vector2.one * 0.5f;
        sweepRect.anchorMax = Vector2.one * 0.5f;
        sweepRect.anchoredPosition = new Vector2(0, 0);
        sweepRect.sizeDelta = new Vector2(2f, coneRadius * 2f);
        
        sweepLine.SetActive(false);
    }
    
    private void HandleSonarPing(List<SubmarineSonar.SonarContact> detections)
    {
        if (!IsActive) 
        {
            IsActive = true;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[CONIC SONAR DISPLAY] Received ping with {detections.Count} detections");
        
        // Clear previous contacts and prepare new ones
        ClearAllContacts();
        
        // Store contacts for progressive reveal during sweep
        pendingContacts.Clear();
        
        // Filter and store contacts
        var validContacts = detections.Where(contact => 
            Mathf.Abs(contact.bearing) <= coneAngle * 0.5f).ToList();
        
        // Store all contacts (no separation between walls and others)
        foreach (var contact in validContacts)
        {
            pendingContacts.Add(contact);
        }
        
        // Sort contacts by bearing for progressive reveal
        pendingContacts = pendingContacts.OrderByDescending(c => c.bearing).ToList();
        
        StartSweepAnimation();
        
        if (enableDebugLogs)
            Debug.Log($"[CONIC SONAR DISPLAY] Prepared {validContacts.Count} contacts for progressive reveal");
    }
    
    private void AddContactToCone(SubmarineSonar.SonarContact contact)
    {
        if (contactPrefab == null || displayPanel == null) return;
        
        Vector2 conePosition = CalculateConePosition(contact);
        
        // Create radar noise effect instead of single contact
        CreateRadarNoiseEffect(conePosition, contact);
    }
    
    private void CreateRadarNoiseEffect(Vector2 centerPosition, SubmarineSonar.SonarContact contact)
    {
        Color baseColor = GetContactColor(contact.contactType);
        
        for (int i = 0; i < noisePointsPerContact; i++)
        {
            GameObject noisePoint = Instantiate(contactPrefab, displayPanel);
            RectTransform noiseRect = noisePoint.GetComponent<RectTransform>();
            Image noiseImage = noisePoint.GetComponent<Image>();
            
            // Random position around center
            Vector2 randomOffset = Random.insideUnitCircle * noiseRadius;
            Vector2 noisePosition = centerPosition + randomOffset;
            
            if (noiseRect != null)
            {
                noiseRect.anchoredPosition = noisePosition;
                // Random size variation
                float sizeVariation = Random.Range(0.5f, 1.5f);
                noiseRect.sizeDelta = noiseRect.sizeDelta * sizeVariation;
            }
            
            if (noiseImage != null)
            {
                // Random intensity variation
                Color noiseColor = baseColor;
                noiseColor.a = Random.Range(noiseIntensity * 0.3f, noiseIntensity);
                noiseImage.color = noiseColor;
            }
            
            // Add fade animation
            StartCoroutine(FadeNoisePoint(noisePoint, noiseImage));
            
            activeContacts.Add(noisePoint);
        }
    }
    
    private System.Collections.IEnumerator FadeNoisePoint(GameObject noisePoint, Image noiseImage)
    {
        if (noiseImage == null) yield break;
        
        Color initialColor = noiseImage.color;
        float elapsedTime = 0f;
        
        while (elapsedTime < noiseFadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(initialColor.a, 0f, elapsedTime / noiseFadeTime);
            
            if (noiseImage != null)
            {
                Color currentColor = noiseImage.color;
                currentColor.a = alpha;
                noiseImage.color = currentColor;
            }
            
            yield return null;
        }
        
        // Ensure complete fade
        if (noiseImage != null)
        {
            Color finalColor = noiseImage.color;
            finalColor.a = 0f;
            noiseImage.color = finalColor;
        }
    }
    
    private Vector2 CalculateConePosition(SubmarineSonar.SonarContact contact)
    {
        // Convert distance and bearing to cone coordinates
        float normalizedDistance = Mathf.Clamp01(contact.distance / MaxDisplayRange);
        float distanceOnCone = normalizedDistance * coneRadius;
        
        // Convert bearing to cone angle (bearing 0 = straight ahead = top of cone)
        float bearingRad = contact.bearing * Mathf.Deg2Rad;
        
        // In cone coordinates: y+ is forward, x is side-to-side
        Vector2 position = new Vector2(
            Mathf.Sin(bearingRad) * distanceOnCone,
            Mathf.Cos(bearingRad) * distanceOnCone
        );
        
        return position;
    }
    
    private Color GetContactColor(string contactType)
    {
        switch (contactType.ToLower())
        {
            case "wall":
            case "terrain":
            case "rock":
                return terrainContactColor;
            case "submarine":
            case "player":
                return Color.blue;
            case "hostile":
            case "enemy":
                return Color.red;
            default:
                return Color.green;
        }
    }
    
    private void StartSweepAnimation()
    {
        if (isSweeping || sweepLine == null) return;
        
        StartCoroutine(SweepAnimation());
    }
    
  private System.Collections.IEnumerator SweepAnimation()
  {
      isSweeping = true;
      sweepLine.SetActive(true);
      
      float elapsedTime = 0f;
      float startAngle = -coneAngle * 0.5f;  // Start from left
      float endAngle = coneAngle * 0.5f;    // End at right
      
      // Lists to track which contacts have been revealed
      List<SubmarineSonar.SonarContact> revealedContacts = new List<SubmarineSonar.SonarContact>();
      
      while (elapsedTime < sweepDuration)
      {
          elapsedTime += Time.deltaTime;
          float progress = elapsedTime / sweepDuration;
          
          // Sweep from left to right across the cone
          float currentAngle = Mathf.Lerp(startAngle, endAngle, sweepCurve.Evaluate(progress));
          sweepLine.transform.rotation = Quaternion.AngleAxis(-currentAngle, Vector3.forward);
          
          // Reveal contacts as the sweep passes over them (sweeping left to right)
          foreach (var contact in pendingContacts)
          {
              if (!revealedContacts.Contains(contact) && contact.bearing <= currentAngle)
              {
                  AddContactToCone(contact);
                  revealedContacts.Add(contact);
              }
          }
          
          // Fade sweep line
          Image sweepImage = sweepLine.GetComponent<Image>();
          if (sweepImage != null)
          {
              Color color = sweepColor;
              color.a = 1f - progress;
              sweepImage.color = color;
          }
          
          yield return null;
      }
  
      sweepLine.SetActive(false);
      isSweeping = false;
  
      // Wait before clearing contacts
      yield return new WaitForSeconds(3f);
      ClearAllContacts(); // Clear only the contacts, not the display
  }
    
    private void ClearAllContacts()
    {
        foreach (var contact in activeContacts)
        {
            if (contact != null) Destroy(contact);
        }
        activeContacts.Clear();
    }
    
    private void SetDisplayActive(bool active)
    {
        if (displayPanel != null)
            displayPanel.gameObject.SetActive(active);
            
        if (enableDebugLogs)
            Debug.Log($"[CONIC SONAR DISPLAY] Display set to {(active ? "ACTIVE" : "INACTIVE")}");
    }
    
    // Public methods
    public void SetMaxRange(float range)
    {
        MaxDisplayRange = range;
    }
    
    public void SetConeAngle(float angle)
    {
        coneAngle = angle;
        // Update visual elements if needed
        CreateConeDisplay();
    }
    
    // Editor method to refresh display
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void OnValidate()
    {
        if (Application.isPlaying && displayPanel != null)
        {
            CreateConeDisplay();
        }
    }
}