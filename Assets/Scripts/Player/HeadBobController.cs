using UnityEngine;

/// <summary>
/// Handles camera head bobbing effect during movement
/// Provides realistic walking animation for first-person view
/// </summary>
public class HeadBobController : MonoBehaviour
{
    [Header("Head-Bob Settings")]
    public float bobFrequency = 4.3f;
    public float bobHorizontalAmplitude = 0.05f;
    public float bobVerticalAmplitude = 0.08f;
    
    [Header("References")]
    public Camera playerCamera;
    public PlayerMovement playerMovement;
    
    // Properties
    public bool IsActive { get; set; } = true;
    public float CurrentBobValue { get; private set; }
    
    // Private variables
    private Vector3 camInitLocalPos;
    private float bobTimer;
    
    // Events for footstep synchronization
    public System.Action<float> OnBobStep;
    
    private void Awake()
    {
        // Auto-find references if not assigned
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();
            
        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
    }
    
    private void Start()
    {
        if (playerCamera != null)
            camInitLocalPos = playerCamera.transform.localPosition;
    }
    
    private void Update()
    {
        if (IsActive)
            HandleHeadBob();
    }
    
    private void HandleHeadBob()
    {
        if (playerMovement == null || playerCamera == null) return;
        
        bool isMoving = playerMovement.IsMoving();
        
        if (!isMoving || playerMovement.CurrentSpeed < 0.05f) 
        { 
            ResetCamera(); 
            return; 
        }

        // Calculate bob based on movement
        float speedNormalized = Mathf.Clamp01(playerMovement.CurrentSpeed / 6f); // Normalize to walk speed
        float bobIntensity = 0.8f + (speedNormalized * 0.2f); // Range from 0.8 to 1.0
        
        bobTimer += Time.deltaTime * bobFrequency;
        
        // Calculate bob offsets
        float hOffset = Mathf.Sin(bobTimer) * bobHorizontalAmplitude * bobIntensity;
        float vOffset = Mathf.Abs(Mathf.Sin(bobTimer * 2f)) * bobVerticalAmplitude * bobIntensity;
        
        CurrentBobValue = vOffset;
        
        // Notify listeners for footstep timing
        OnBobStep?.Invoke(CurrentBobValue);
        
        // Apply head bob to camera position
        Vector3 bobOffset = new Vector3(hOffset, vOffset, 0f);
        playerCamera.transform.localPosition = camInitLocalPos + bobOffset;
    }
    
    private void ResetCamera()
    {
        if (playerCamera == null) return;
        
        bobTimer = 0f;
        playerCamera.transform.localPosition = Vector3.Lerp(
            playerCamera.transform.localPosition, 
            camInitLocalPos, 
            Time.deltaTime * 5f
        );
        
        CurrentBobValue = 0f;
    }
    
    // Public methods for external control
    public void SetBobIntensity(float horizontalAmp, float verticalAmp)
    {
        bobHorizontalAmplitude = horizontalAmp;
        bobVerticalAmplitude = verticalAmp;
    }
    
    public void SetBobFrequency(float frequency)
    {
        bobFrequency = frequency;
    }
    
    public void ResetBobTimer()
    {
        bobTimer = 0f;
    }
    
    public void SetActive(bool active)
    {
        IsActive = active;
        if (!active)
            ResetCamera();
    }
}