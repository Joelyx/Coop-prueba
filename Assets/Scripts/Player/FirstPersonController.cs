using UnityEngine;

/// <summary>
/// Main coordinator for first-person player controller
/// Manages all player subsystems and provides unified interface
/// </summary>
public class FirstPersonController : MonoBehaviour
{
    [Header("Component References")]
    public PlayerMovement movement;
    public PlayerJump jump;
    public MouseLook mouseLook;
    public HeadBobController headBob;
    public FootstepAudio footstepAudio;
    
    [Header("Camera Setup")]
    public Camera playerCamera;
    public Transform cameraHolder;
    
    // Properties for external access
    public bool IsGrounded => movement != null ? movement.IsGrounded : false;
    public bool IsMoving => movement != null ? movement.IsMoving() : false;
    public bool IsJumping => jump != null ? jump.IsJumping : false;
    public float CurrentSpeed => movement != null ? movement.CurrentSpeed : 0f;
    
    // Events for other systems
    public System.Action<bool> OnGroundedChanged;
    public System.Action<bool> OnMovementChanged;
    public System.Action OnJumpStart;
    public System.Action OnJumpLand;
    
    private void Awake()
    {
        SetupCameraHierarchy();
        FindComponents();
        ValidateSetup();
    }
    
    private void Start()
    {
        ConfigureComponents();
        SubscribeToEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void SetupCameraHierarchy()
    {
        // Find camera if not assigned
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
            
        if (playerCamera == null)
        {
            Debug.LogError("No camera found for FirstPersonController!");
            return;
        }
        
        // Store original camera position before moving hierarchy
        Vector3 originalCameraPos = playerCamera.transform.localPosition;
        
        // Create camera holder if it doesn't exist
        if (cameraHolder == null)
        {
            GameObject cameraHolderGO = new GameObject("CameraHolder");
            cameraHolder = cameraHolderGO.transform;
            cameraHolder.SetParent(transform);
            cameraHolder.localPosition = Vector3.zero;
            cameraHolder.localRotation = Quaternion.identity;
            
            // Move camera to be child of camera holder and restore position
            playerCamera.transform.SetParent(cameraHolder);
            playerCamera.transform.localPosition = originalCameraPos;
            playerCamera.transform.localRotation = Quaternion.identity;
        }
    }
    
    private void FindComponents()
    {
        // Find components if not assigned
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
            
        if (jump == null)
            jump = GetComponent<PlayerJump>();
            
        if (mouseLook == null)
            mouseLook = GetComponentInChildren<MouseLook>();
            
        if (headBob == null)
            headBob = GetComponentInChildren<HeadBobController>();
            
        if (footstepAudio == null)
            footstepAudio = GetComponentInChildren<FootstepAudio>();
    }
    
    private void ValidateSetup()
    {
        bool hasErrors = false;
        
        if (movement == null)
        {
            Debug.LogError("PlayerMovement component missing!", this);
            hasErrors = true;
        }
        
        if (jump == null)
        {
            Debug.LogError("PlayerJump component missing!", this);
            hasErrors = true;
        }
        
        if (mouseLook == null)
        {
            Debug.LogError("MouseLook component missing!", this);
            hasErrors = true;
        }
        
        if (playerCamera == null)
        {
            Debug.LogError("Camera missing!", this);
            hasErrors = true;
        }
        
        if (hasErrors)
        {
            Debug.LogError("FirstPersonController setup incomplete. Please assign missing components.");
            enabled = false;
        }
    }
    
    private void ConfigureComponents()
    {
        // Configure MouseLook references
        if (mouseLook != null)
        {
            mouseLook.playerBody = transform;
            mouseLook.cameraHolder = cameraHolder;
        }
        
        // Configure PlayerJump references
        if (jump != null && cameraHolder != null)
        {
            jump.cameraHolder = cameraHolder;
        }
        
        // Configure HeadBobController references
        if (headBob != null)
        {
            headBob.playerCamera = playerCamera;
            headBob.playerMovement = movement;
        }
        
        // Configure FootstepAudio references
        if (footstepAudio != null)
        {
            footstepAudio.playerMovement = movement;
            footstepAudio.headBobController = headBob;
        }
    }
    
    private void SubscribeToEvents()
    {
        // Subscribe to movement events
        if (movement != null)
        {
            movement.OnGroundedChanged += HandleGroundedChanged;
            movement.OnSpeedChanged += HandleSpeedChanged;
        }
        
        // Subscribe to jump events
        if (jump != null)
        {
            jump.OnJumpStart += HandleJumpStart;
            jump.OnJumpLand += HandleJumpLand;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        // Unsubscribe from movement events
        if (movement != null)
        {
            movement.OnGroundedChanged -= HandleGroundedChanged;
            movement.OnSpeedChanged -= HandleSpeedChanged;
        }
        
        // Unsubscribe from jump events
        if (jump != null)
        {
            jump.OnJumpStart -= HandleJumpStart;
            jump.OnJumpLand -= HandleJumpLand;
        }
    }
    
    private void HandleGroundedChanged(bool isGrounded)
    {
        OnGroundedChanged?.Invoke(isGrounded);
    }
    
    private void HandleSpeedChanged(float speed)
    {
        bool isMoving = speed > 0.1f;
        OnMovementChanged?.Invoke(isMoving);
    }
    
    private void HandleJumpStart()
    {
        OnJumpStart?.Invoke();
    }
    
    private void HandleJumpLand()
    {
        OnJumpLand?.Invoke();
    }
    
    // Public methods for external control
    public void SetMouseSensitivity(float sensitivity)
    {
        if (mouseLook != null)
            mouseLook.SetSensitivity(sensitivity);
    }
    
    public void SetCursorLocked(bool locked)
    {
        if (mouseLook != null)
            mouseLook.SetLockState(locked);
    }
    
    public void SetHeadBobActive(bool active)
    {
        if (headBob != null)
            headBob.SetActive(active);
    }
    
    public void SetFootstepAudioEnabled(bool enabled)
    {
        if (footstepAudio != null)
            footstepAudio.IsEnabled = enabled;
    }
    
    public void AddForce(Vector3 force, ForceMode forceMode = ForceMode.Force)
    {
        if (movement != null)
            movement.AddForce(force, forceMode);
    }
    
    public void ForceJump(float force)
    {
        if (jump != null)
            jump.ForceJump(force);
    }
    
    public Vector2 GetMovementInput()
    {
        return movement != null ? movement.GetMovementInput() : Vector2.zero;
    }
    
    public Vector2 GetLookAngles()
    {
        return mouseLook != null ? mouseLook.GetLookAngles() : Vector2.zero;
    }
}