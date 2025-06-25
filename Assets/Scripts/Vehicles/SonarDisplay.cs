using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Visual sonar display UI - naval radar style
/// Shows detected contacts on a circular display with wall line detection
/// </summary>
public class SonarDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public RectTransform displayPanel;
    public Image radarBackground;
    public RectTransform sweepLine;
    public float displayRadius = 180f;
    
    [Header("Contact Visualization")]
    public GameObject contactPrefab;
    public Color playerContactColor = Color.blue;
    public Color hostileContactColor = Color.red;
    public Color neutralContactColor = Color.green;
    public Color terrainContactColor = Color.yellow;
    
    [Header("Wall Line Settings")]
    public bool showWallLines = true;
    public Color wallLineColor = new Color(0f, 1f, 0f, 0.8f);
    public float wallLineWidth = 2f;
    public float wallConnectionDistance = 25f; // Distance to connect wall points into lines
    public GameObject wallLinePrefab;
    
    [Header("Sweep Animation")]
    public float sweepDuration = 2f;
    public Color sweepColor = Color.cyan;
    public AnimationCurve sweepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Range Rings")]
    public LineRenderer[] rangeRings;
    public Color rangeRingColor = Color.white;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Properties
    public bool IsActive { get; set; } = true;
    public float MaxDisplayRange { get; private set; } = 100f;
    
    // Private variables
    private SubmarineSonar sonarSystem;
    private List<SonarContactUI> activeContacts = new List<SonarContactUI>();
    private List<GameObject> activeWallLines = new List<GameObject>();
    private bool isSweeping = false;
    private Canvas displayCanvas;
    
    private struct SonarContactUI
    {
        public GameObject uiObject;
        public SubmarineSonar.SonarContact data;
        public Vector2 screenPosition;
        public float fadeTime;
        
        public SonarContactUI(GameObject obj, SubmarineSonar.SonarContact contact, Vector2 screenPos)
        {
            uiObject = obj;
            data = contact;
            screenPosition = screenPos;
            fadeTime = Time.time + 8f; // Contacts fade after 8 seconds
        }
    }
    
    private void Awake()
    {
        // Find sonar system in the scene
        sonarSystem = FindObjectOfType<SubmarineSonar>();
        displayCanvas = GetComponent<Canvas>();
        
        SetupDisplay();
        SetupRangeRings();
        CreateWallLinePrefab();
        
        if (enableDebugLogs)
            Debug.Log("[SONAR DISPLAY] Initialized");
    }
    
    private void Start()
    {
        if (sonarSystem != null)
        {
            sonarSystem.OnSonarPing += HandleSonarPing;
            MaxDisplayRange = sonarSystem.maxRange;
            
            if (enableDebugLogs)
                Debug.Log($"[SONAR DISPLAY] Connected to sonar system. Range: {MaxDisplayRange}m");
        }
        else
        {
            Debug.LogError("[SONAR DISPLAY] No SubmarineSonar found in scene!");
        }
        
        // Initially hide display
        SetDisplayActive(false);
    }
    
    private void OnDestroy()
    {
        if (sonarSystem != null)
            sonarSystem.OnSonarPing -= HandleSonarPing;
    }
    
    private void Update()
    {
        if (IsActive)
        {
            UpdateContactPositions();
            FadeOldContacts();
        }
    }
    
    private void CreateWallLinePrefab()
    {
        if (wallLinePrefab == null)
        {
            // Create a simple line prefab using UI Image
            wallLinePrefab = new GameObject("WallLinePrefab");
            wallLinePrefab.AddComponent<RectTransform>();
            wallLinePrefab.AddComponent<CanvasRenderer>();
            Image lineImage = wallLinePrefab.AddComponent<Image>();
            lineImage.color = wallLineColor;
            
            // Make it inactive so it doesn't show in hierarchy
            wallLinePrefab.SetActive(false);
        }
    }
    
    private void SetupDisplay()
    {
        if (displayPanel == null) return;
        
        // Setup radar background
        if (radarBackground != null)
        {
            radarBackground.color = new Color(0f, 0.2f, 0.1f, 0.8f); // Dark green radar screen
        }
        
        // Setup sweep line
        if (sweepLine != null)
        {
            Image sweepImage = sweepLine.GetComponent<Image>();
            if (sweepImage != null)
            {
                sweepImage.color = sweepColor;
            }
            sweepLine.gameObject.SetActive(false);
        }
    }
    
    private void SetupRangeRings()
    {
        if (rangeRings == null || rangeRings.Length == 0) return;
        
        for (int i = 0; i < rangeRings.Length; i++)
        {
            if (rangeRings[i] != null)
            {
                DrawRangeRing(rangeRings[i], (i + 1) * (MaxDisplayRange / rangeRings.Length));
            }
        }
    }
    
    private void DrawRangeRing(LineRenderer ring, float range)
    {
        int segments = 64;
        ring.positionCount = segments + 1;
        ring.material = new Material(Shader.Find("Sprites/Default"));
        ring.material.color = rangeRingColor;
        ring.startWidth = ring.endWidth = 0.5f;
        ring.useWorldSpace = false;
        
        float radius = (range / MaxDisplayRange) * displayRadius;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * 360f * Mathf.Deg2Rad;
            Vector3 position = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
            ring.SetPosition(i, position);
        }
    }
    
    private void HandleSonarPing(List<SubmarineSonar.SonarContact> detections)
    {
        if (!IsActive) 
        {
            IsActive = true; // Auto-activate when sonar pings
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SONAR DISPLAY] Received ping with {detections.Count} detections");
        
        // Show display when sonar activates
        SetDisplayActive(true);
        
        // Start sweep animation
        StartSweepAnimation();
        
        // Clear old contacts and lines
        ClearAllContacts();
        ClearAllWallLines();
        
        // Process contacts and create wall lines
        ProcessContactsAndCreateWalls(detections);
        
        if (enableDebugLogs)
            Debug.Log($"[SONAR DISPLAY] Added {detections.Count} contacts to display");
    }
    
    private void ProcessContactsAndCreateWalls(List<SubmarineSonar.SonarContact> detections)
    {
        // Separate wall contacts from other contacts
        List<SubmarineSonar.SonarContact> wallContacts = detections.Where(c => 
            c.contactType.ToLower().Contains("wall") || 
            c.contactType.ToLower().Contains("terrain") ||
            c.contactType.ToLower().Contains("rock")).ToList();
        
        List<SubmarineSonar.SonarContact> otherContacts = detections.Where(c => 
            !wallContacts.Contains(c)).ToList();
        
        // Add individual contact points for non-wall objects
        foreach (var contact in otherContacts)
        {
            AddContactToDisplay(contact);
        }
        
        // Create wall lines for wall contacts
        if (showWallLines && wallContacts.Count > 0)
        {
            CreateWallLines(wallContacts);
        }
        else
        {
            // If wall lines disabled, show wall contacts as individual points
            foreach (var contact in wallContacts)
            {
                AddContactToDisplay(contact);
            }
        }
    }
    
    private void CreateWallLines(List<SubmarineSonar.SonarContact> wallContacts)
    {
        if (wallContacts.Count < 2) 
        {
            // Not enough points for lines, show as individual contacts
            foreach (var contact in wallContacts)
            {
                AddContactToDisplay(contact);
            }
            return;
        }
        
        // Convert contacts to screen positions
        List<Vector2> wallPoints = new List<Vector2>();
        foreach (var contact in wallContacts)
        {
            Vector2 screenPos = CalculateScreenPosition(contact);
            wallPoints.Add(screenPos);
        }
        
        // Group nearby points into wall segments
        List<List<Vector2>> wallSegments = GroupPointsIntoSegments(wallPoints);
        
        // Create visual lines for each segment
        foreach (var segment in wallSegments)
        {
            if (segment.Count >= 2)
            {
                CreateWallLineSegment(segment);
            }
            else if (segment.Count == 1)
            {
                // Single point, create a small contact dot
                CreateSingleWallPoint(segment[0]);
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SONAR DISPLAY] Created {wallSegments.Count} wall segments from {wallContacts.Count} contacts");
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
            
            // Find all connected points
            bool foundConnection = true;
            while (foundConnection)
            {
                foundConnection = false;
                
                for (int i = remainingPoints.Count - 1; i >= 0; i--)
                {
                    Vector2 testPoint = remainingPoints[i];
                    
                    // Check if this point is close to any point in current segment
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
            
            // Sort points in segment to create a logical line
            if (currentSegment.Count > 2)
            {
                currentSegment = SortPointsForLine(currentSegment);
            }
            
            segments.Add(currentSegment);
        }
        
        return segments;
    }
    
    private List<Vector2> SortPointsForLine(List<Vector2> points)
    {
        if (points.Count <= 2) return points;
        
        List<Vector2> sortedPoints = new List<Vector2>();
        List<Vector2> remainingPoints = new List<Vector2>(points);
        
        // Start with the first point
        sortedPoints.Add(remainingPoints[0]);
        remainingPoints.RemoveAt(0);
        
        // Keep adding the closest remaining point
        while (remainingPoints.Count > 0)
        {
            Vector2 lastPoint = sortedPoints[sortedPoints.Count - 1];
            int closestIndex = 0;
            float closestDistance = Vector2.Distance(lastPoint, remainingPoints[0]);
            
            for (int i = 1; i < remainingPoints.Count; i++)
            {
                float distance = Vector2.Distance(lastPoint, remainingPoints[i]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }
            
            sortedPoints.Add(remainingPoints[closestIndex]);
            remainingPoints.RemoveAt(closestIndex);
        }
        
        return sortedPoints;
    }
    
    private void CreateWallLineSegment(List<Vector2> points)
    {
        // Create a line connecting all points in the segment
        for (int i = 0; i < points.Count - 1; i++)
        {
            CreateLineBetweenPoints(points[i], points[i + 1]);
        }
        
        // Also add some glow points along the line for better visibility
        foreach (Vector2 point in points)
        {
            CreateWallGlowPoint(point);
        }
    }
    
    private void CreateLineBetweenPoints(Vector2 start, Vector2 end)
    {
        GameObject lineObj = Instantiate(wallLinePrefab, displayPanel);
        lineObj.SetActive(true);
        lineObj.name = "WallLine";
        
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        Image lineImage = lineObj.GetComponent<Image>();
        
        // Configure line appearance
        lineImage.color = wallLineColor;
        
        // Position and rotate the line
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        Vector2 center = (start + end) * 0.5f;
        
        lineRect.anchorMin = Vector2.one * 0.5f;
        lineRect.anchorMax = Vector2.one * 0.5f;
        lineRect.anchoredPosition = center;
        lineRect.sizeDelta = new Vector2(wallLineWidth, distance);
        
        // Rotate to align with direction
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        lineRect.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        activeWallLines.Add(lineObj);
    }
    
    private void CreateWallGlowPoint(Vector2 position)
    {
        if (contactPrefab == null) return;
        
        GameObject glowPoint = Instantiate(contactPrefab, displayPanel);
        glowPoint.name = "WallGlow";
        
        RectTransform glowRect = glowPoint.GetComponent<RectTransform>();
        Image glowImage = glowPoint.GetComponent<Image>();
        
        if (glowRect != null)
        {
            glowRect.anchoredPosition = position;
        }
        
        if (glowImage != null)
        {
            glowImage.color = new Color(wallLineColor.r, wallLineColor.g, wallLineColor.b, 0.6f);
        }
        
        activeWallLines.Add(glowPoint);
    }
    
    private void CreateSingleWallPoint(Vector2 position)
    {
        if (contactPrefab == null) return;
        
        GameObject contactUI = Instantiate(contactPrefab, displayPanel);
        contactUI.name = "WallPoint";
        
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
    
    private void StartSweepAnimation()
    {
        if (isSweeping || sweepLine == null) return;
        
        StartCoroutine(SweepAnimation());
    }
    
    private System.Collections.IEnumerator SweepAnimation()
    {
        isSweeping = true;
        sweepLine.gameObject.SetActive(true);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < sweepDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / sweepDuration;
            
            // Rotate sweep line
            float angle = sweepCurve.Evaluate(progress) * 360f;
            sweepLine.rotation = Quaternion.Euler(0f, 0f, angle);
            
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
        
        sweepLine.gameObject.SetActive(false);
        isSweeping = false;
        
        // Keep display visible longer to see contacts
        yield return new WaitForSeconds(3f);
        SetDisplayActive(false);
    }
    
    private void AddContactToDisplay(SubmarineSonar.SonarContact contact)
    {
        if (contactPrefab == null || displayPanel == null) 
        {
            if (enableDebugLogs)
                Debug.LogWarning("[SONAR DISPLAY] ContactPrefab or DisplayPanel is null!");
            return;
        }
        
        GameObject contactUI = Instantiate(contactPrefab, displayPanel);
        Vector2 screenPosition = CalculateScreenPosition(contact);
        
        RectTransform contactRect = contactUI.GetComponent<RectTransform>();
        if (contactRect != null)
        {
            contactRect.anchoredPosition = screenPosition;
        }
        
        // Set contact color based on type
        Image contactImage = contactUI.GetComponent<Image>();
        if (contactImage != null)
        {
            contactImage.color = GetContactColor(contact.contactType);
        }
        
        // Add tooltip or text if available
        Text contactText = contactUI.GetComponentInChildren<Text>();
        if (contactText != null)
        {
            contactText.text = $"{contact.contactType}\n{contact.distance:F0}m";
        }
        
        SonarContactUI uiContact = new SonarContactUI(contactUI, contact, screenPosition);
        activeContacts.Add(uiContact);
        
        if (enableDebugLogs)
            Debug.Log($"[SONAR DISPLAY] Added contact {contact.contactType} at screen pos {screenPosition}");
    }
    
    private Vector2 CalculateScreenPosition(SubmarineSonar.SonarContact contact)
    {
        // Convert world position to radar screen position
        float normalizedDistance = Mathf.Clamp01(contact.distance / MaxDisplayRange);
        float screenDistance = normalizedDistance * displayRadius;
        
        // Convert bearing to screen angle (radar uses different coordinate system)
        float screenAngle = -contact.bearing * Mathf.Deg2Rad; // Negative for correct rotation
        
        Vector2 position = new Vector2(
            Mathf.Sin(screenAngle) * screenDistance,
            Mathf.Cos(screenAngle) * screenDistance
        );
        
        if (enableDebugLogs)
            Debug.Log($"[SONAR DISPLAY] Contact at distance {contact.distance:F1}m, bearing {contact.bearing:F1}° → screen pos {position}");
        
        return position;
    }
    
    private Color GetContactColor(string contactType)
    {
        switch (contactType.ToLower())
        {
            case "submarine":
            case "player":
                return playerContactColor;
            case "hostile":
            case "enemy":
                return hostileContactColor;
            case "wall":
            case "terrain":
            case "rock":
                return terrainContactColor;
            default:
                return neutralContactColor;
        }
    }
    
    private void UpdateContactPositions()
    {
        // If submarine is moving, contacts should move relative to submarine
        // This could be enhanced to show relative motion
    }
    
    private void FadeOldContacts()
    {
        for (int i = activeContacts.Count - 1; i >= 0; i--)
        {
            if (Time.time > activeContacts[i].fadeTime)
            {
                if (activeContacts[i].uiObject != null)
                {
                    Destroy(activeContacts[i].uiObject);
                }
                activeContacts.RemoveAt(i);
            }
        }
    }
    
    private void ClearAllContacts()
    {
        foreach (var contact in activeContacts)
        {
            if (contact.uiObject != null)
            {
                Destroy(contact.uiObject);
            }
        }
        activeContacts.Clear();
    }
    
    private void ClearAllWallLines()
    {
        foreach (var line in activeWallLines)
        {
            if (line != null)
            {
                Destroy(line);
            }
        }
        activeWallLines.Clear();
    }
    
    private void SetDisplayActive(bool active)
    {
        if (displayPanel != null)
            displayPanel.gameObject.SetActive(active);
            
        if (enableDebugLogs)
            Debug.Log($"[SONAR DISPLAY] Display set to {(active ? "ACTIVE" : "INACTIVE")}");
    }
    
    // Public methods for external control
    public void SetMaxRange(float range)
    {
        MaxDisplayRange = range;
        SetupRangeRings();
    }
    
    public void SetDisplayRadius(float radius)
    {
        displayRadius = radius;
        SetupRangeRings();
    }
    
    public void ToggleDisplay()
    {
        IsActive = !IsActive;
        SetDisplayActive(IsActive);
    }
    
    public void ToggleWallLines()
    {
        showWallLines = !showWallLines;
    }
}