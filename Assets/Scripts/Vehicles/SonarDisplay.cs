using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Visual sonar display UI - naval radar style
/// Shows detected contacts on a circular display
/// </summary>
public class SonarDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public RectTransform displayPanel;
    public Image radarBackground;
    public RectTransform sweepLine;
    public float displayRadius = 200f;
    
    [Header("Contact Visualization")]
    public GameObject contactPrefab;
    public Color playerContactColor = Color.blue;
    public Color hostileContactColor = Color.red;
    public Color neutralContactColor = Color.green;
    public Color terrainContactColor = Color.yellow;
    
    [Header("Sweep Animation")]
    public float sweepDuration = 2f;
    public Color sweepColor = Color.cyan;
    public AnimationCurve sweepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Range Rings")]
    public LineRenderer[] rangeRings;
    public Color rangeRingColor = Color.white;
    
    // Properties
    public bool IsActive { get; set; } = true;
    public float MaxDisplayRange { get; private set; } = 100f;
    
    // Private variables
    private SubmarineSonar sonarSystem;
    private List<SonarContactUI> activeContacts = new List<SonarContactUI>();
    private bool isSweeping = false;
    private Canvas displayCanvas;
    
    private struct SonarContactUI
    {
        public GameObject uiObject;
        public SubmarineSonar.SonarContact data;
        public float fadeTime;
        
        public SonarContactUI(GameObject obj, SubmarineSonar.SonarContact contact)
        {
            uiObject = obj;
            data = contact;
            fadeTime = Time.time + 5f; // Contacts fade after 5 seconds
        }
    }
    
    private void Awake()
    {
        sonarSystem = GetComponentInParent<SubmarineSonar>();
        displayCanvas = GetComponentInParent<Canvas>();
        
        SetupDisplay();
        SetupRangeRings();
    }
    
    private void Start()
    {
        if (sonarSystem != null)
        {
            sonarSystem.OnSonarPing += HandleSonarPing;
            MaxDisplayRange = sonarSystem.maxRange;
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
        if (rangeRings == null) return;
        
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
        if (!IsActive) return;
        
        // Show display when sonar activates
        SetDisplayActive(true);
        
        // Start sweep animation
        StartSweepAnimation();
        
        // Clear old contacts
        ClearAllContacts();
        
        // Add new contacts
        foreach (var contact in detections)
        {
            AddContactToDisplay(contact);
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
        
        // Hide display after sweep
        yield return new WaitForSeconds(1f);
        SetDisplayActive(false);
    }
    
    private void AddContactToDisplay(SubmarineSonar.SonarContact contact)
    {
        if (contactPrefab == null || displayPanel == null) return;
        
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
        
        SonarContactUI uiContact = new SonarContactUI(contactUI, contact);
        activeContacts.Add(uiContact);
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
    
    private void SetDisplayActive(bool active)
    {
        if (displayPanel != null)
            displayPanel.gameObject.SetActive(active);
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
}