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
    public Color wallLineColor = new Color(0f, 1f, 0f, 0.8f);
    public Color terrainContactColor = Color.yellow;
    public float wallLineWidth = 3f;
    public float wallConnectionDistance = 25f;
    
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
    private List<GameObject> activeWallLines = new List<GameObject>();
    private GameObject coneBackground;
    private GameObject sweepLine;
    private List<GameObject> coneBorders = new List<GameObject>();
    private bool isSweeping = false;
    private Canvas displayCanvas;
    
    // Store contacts for progressive reveal
    private List<SubmarineSonar.SonarContact> pendingContacts = new List<SubmarineSonar.SonarContact>();
    private List<SubmarineSonar.SonarContact> wallContactsToProcess = new List<SubmarineSonar.SonarContact>();
    
    private void Awake()
    {
        sonarSystem = FindObjectOfType<SubmarineSonar>();
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
        wallContactsToProcess.Clear();
        
        // Filter and store contacts
        var validContacts = detections.Where(contact => 
            Mathf.Abs(contact.bearing) <= coneAngle * 0.5f).ToList();
        
        foreach (var contact in validContacts)
        {
            if (contact.contactType.ToLower().Contains("wall") || 
                contact.contactType.ToLower().Contains("terrain") ||
                contact.contactType.ToLower().Contains("rock"))
            {
                wallContactsToProcess.Add(contact);
            }
            else
            {
                pendingContacts.Add(contact);
            }
        }
        
        // Sort contacts by bearing (left to right) for progressive reveal since we sweep from right to left
        pendingContacts = pendingContacts.OrderBy(c => c.bearing).ToList();
        wallContactsToProcess = wallContactsToProcess.OrderBy(c => c.bearing).ToList();
        
        StartSweepAnimation();
        
        if (enableDebugLogs)
            Debug.Log($"[CONIC SONAR DISPLAY] Prepared {validContacts.Count} contacts for progressive reveal");
    }
    
    private void ProcessContactsForCone(List<SubmarineSonar.SonarContact> detections)
    {
        // Filter contacts that are within the cone
        List<SubmarineSonar.SonarContact> validContacts = detections.Where(contact => 
            Mathf.Abs(contact.bearing) <= coneAngle * 0.5f).ToList();
        
        // Separate wall contacts from others
        List<SubmarineSonar.SonarContact> wallContacts = validContacts.Where(c => 
            c.contactType.ToLower().Contains("wall") || 
            c.contactType.ToLower().Contains("terrain") ||
            c.contactType.ToLower().Contains("rock")).ToList();
        
        List<SubmarineSonar.SonarContact> otherContacts = validContacts.Where(c => 
            !wallContacts.Contains(c)).ToList();
        
        // Add other contacts as points
        foreach (var contact in otherContacts)
        {
            AddContactToCone(contact);
        }
        
        // Create wall lines for wall contacts
        if (wallContacts.Count > 0)
        {
            CreateWallLinesInCone(wallContacts);
        }
        
        if (enableDebugLogs)
            Debug.Log($"[CONIC SONAR DISPLAY] Added {validContacts.Count} valid contacts ({wallContacts.Count} walls, {otherContacts.Count} others)");
    }
    
    private void AddContactToCone(SubmarineSonar.SonarContact contact)
    {
        if (contactPrefab == null || displayPanel == null) return;
        
        Vector2 conePosition = CalculateConePosition(contact);
        
        GameObject contactUI = Instantiate(contactPrefab, displayPanel);
        RectTransform contactRect = contactUI.GetComponent<RectTransform>();
        Image contactImage = contactUI.GetComponent<Image>();
        
        if (contactRect != null)
        {
            contactRect.anchoredPosition = conePosition;
        }
        
        if (contactImage != null)
        {
            contactImage.color = GetContactColor(contact.contactType);
        }
        
        activeContacts.Add(contactUI);
    }
    
    private void CreateWallLinesInCone(List<SubmarineSonar.SonarContact> wallContacts)
    {
        // Convert to cone positions
        List<Vector2> wallPoints = wallContacts.Select(contact => CalculateConePosition(contact)).ToList();
        
        // Group into segments
        List<List<Vector2>> wallSegments = GroupPointsIntoSegments(wallPoints);
        
        // Create visual lines
        foreach (var segment in wallSegments)
        {
            if (segment.Count >= 2)
            {
                CreateWallLineSegmentInCone(segment);
            }
            else if (segment.Count == 1)
            {
                CreateSingleWallPointInCone(segment[0]);
            }
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
    
    private List<List<Vector2>> GroupPointsIntoSegments(List<Vector2> points)
    {
        List<List<Vector2>> segments = new List<List<Vector2>>();
        List<Vector2> remainingPoints = new List<Vector2>(points);
        
        while (remainingPoints.Count > 0)
        {
            List<Vector2> currentSegment = new List<Vector2>();
            Vector2 currentPoint = remainingPoints[0];
            remainingPoints.RemoveAt(0);
            currentSegment.Add(currentPoint);
            
            // Find connected points
            bool foundConnection = true;
            while (foundConnection)
            {
                foundConnection = false;
                
                for (int i = remainingPoints.Count - 1; i >= 0; i--)
                {
                    Vector2 testPoint = remainingPoints[i];
                    
                    bool isConnected = false;
                    foreach (Vector2 segmentPoint in currentSegment)
                    {
                        if (Vector2.Distance(testPoint, segmentPoint) <= wallConnectionDistance)
                        {
                            isConnected = true;
                            break;
                        }
                    }
                    
                    if (isConnected)
                    {
                        currentSegment.Add(testPoint);
                        remainingPoints.RemoveAt(i);
                        foundConnection = true;
                    }
                }
            }
            
            segments.Add(currentSegment);
        }
        
        return segments;
    }
    
    private void CreateWallLineSegmentInCone(List<Vector2> points)
    {
        // Create lines between consecutive points
        for (int i = 0; i < points.Count - 1; i++)
        {
            CreateLineBetweenPoints(points[i], points[i + 1]);
        }
        
        // Add glow points
        foreach (Vector2 point in points)
        {
            CreateWallGlowPoint(point);
        }
    }
    
    private void CreateLineBetweenPoints(Vector2 start, Vector2 end)
    {
        GameObject lineObj = new GameObject("WallLine");
        lineObj.transform.SetParent(displayPanel, false);
        
        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
        lineObj.AddComponent<CanvasRenderer>();
        Image lineImage = lineObj.AddComponent<Image>();
        
        lineImage.color = wallLineColor;
        
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        Vector2 center = (start + end) * 0.5f;
        
        lineRect.anchorMin = Vector2.one * 0.5f;
        lineRect.anchorMax = Vector2.one * 0.5f;
        lineRect.anchoredPosition = center;
        lineRect.sizeDelta = new Vector2(wallLineWidth, distance);
        
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        lineRect.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        activeWallLines.Add(lineObj);
    }
    
    private void CreateWallGlowPoint(Vector2 position)
    {
        if (contactPrefab == null) return;
        
        GameObject glowPoint = Instantiate(contactPrefab, displayPanel);
        RectTransform glowRect = glowPoint.GetComponent<RectTransform>();
        Image glowImage = glowPoint.GetComponent<Image>();
        
        if (glowRect != null)
        {
            glowRect.anchoredPosition = position;
            glowRect.sizeDelta = Vector2.one * 4f; // Smaller glow points
        }
        
        if (glowImage != null)
        {
            glowImage.color = new Color(wallLineColor.r, wallLineColor.g, wallLineColor.b, 0.6f);
        }
        
        activeWallLines.Add(glowPoint);
    }
    
    private void CreateSingleWallPointInCone(Vector2 position)
    {
        if (contactPrefab == null) return;
        
        GameObject contactUI = Instantiate(contactPrefab, displayPanel);
        RectTransform contactRect = contactUI.GetComponent<RectTransform>();
        Image contactImage = contactUI.GetComponent<Image>();
        
        if (contactRect != null)
        {
            contactRect.anchoredPosition = position;
        }
        
        if (contactImage != null)
        {
            contactImage.color = terrainContactColor;
        }
        
        activeWallLines.Add(contactUI);
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
      float startAngle = coneAngle * 0.5f;  // Start from right
      float endAngle = -coneAngle * 0.5f;   // End at left
      
      // Lists to track which contacts have been revealed
      List<SubmarineSonar.SonarContact> revealedContacts = new List<SubmarineSonar.SonarContact>();
      List<SubmarineSonar.SonarContact> revealedWalls = new List<SubmarineSonar.SonarContact>();
      
      while (elapsedTime < sweepDuration)
      {
          elapsedTime += Time.deltaTime;
          float progress = elapsedTime / sweepDuration;
          
          // Sweep from right to left across the cone
          float currentAngle = Mathf.Lerp(startAngle, endAngle, sweepCurve.Evaluate(progress));
          sweepLine.transform.rotation = Quaternion.AngleAxis(currentAngle, Vector3.forward);
          
          // Reveal contacts as the sweep passes over them (now sweeping right to left)
          foreach (var contact in pendingContacts)
          {
              if (!revealedContacts.Contains(contact) && contact.bearing <= currentAngle)
              {
                  AddContactToCone(contact);
                  revealedContacts.Add(contact);
              }
          }
          
          // Reveal wall contacts
          foreach (var wallContact in wallContactsToProcess)
          {
              if (!revealedWalls.Contains(wallContact) && wallContact.bearing <= currentAngle)
              {
                  revealedWalls.Add(wallContact);
              }
          }
          
          // Update wall visualization when new wall points are revealed
          if (revealedWalls.Count > 0)
          {
              // Clear existing wall lines and recreate with revealed points
              foreach (var line in activeWallLines)
              {
                  if (line != null) Destroy(line);
              }
              activeWallLines.Clear();
              
              if (revealedWalls.Count > 0)
              {
                  CreateWallLinesInCone(revealedWalls);
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
        
        foreach (var line in activeWallLines)
        {
            if (line != null) Destroy(line);
        }
        activeWallLines.Clear();
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