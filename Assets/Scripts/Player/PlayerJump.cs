using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles advanced jump mechanics with charge system
/// Includes coyote time, jump buffering, and variable jump height
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerJump : MonoBehaviour
{
    [Header("Jump Settings")]
    public float minJumpHeight = 3f;
    public float maxJumpHeight = 9f;
    public float maxChargeTime = 0.8f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    
    [Header("Gravity Modifiers")]
    public float fallMultiplier = 1.5f;
    public float lowJumpMultiplier = 2f;
    
    [Header("Jump Crouch")]
    public float crouchAmount = 0.2f;
    public float crouchSpeed = 8f;
    public Transform cameraHolder;
    
    // Properties for external access
    public bool IsJumping { get; private set; }
    public bool IsChargingJump { get; private set; }
    public float ChargePercent { get; private set; }
    
    // Private variables
    private Rigidbody rb;
    private PlayerMovement playerMovement;
    private float minJumpForce;
    private float maxJumpForce;
    private float lastGroundedTime;
    private float jumpChargeStartTime;
    private bool wasGroundedLastFrame;
    private Vector3 originalCameraHolderPos;
    private float targetCrouchOffset = 0f;
    private bool jumpButtonPressed = false;
    
    // Input action - using the InputActions asset
    [Header("Input")]
    public InputActionReference jumpInputAction;
    
    // Events
    public System.Action OnJumpStart;
    public System.Action OnJumpLand;
    public System.Action<float> OnChargeChanged;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        
        SetupInputAction();
        CalculateJumpForces();
        
        if (cameraHolder != null)
            originalCameraHolderPos = cameraHolder.localPosition;
    }
    
    private void Start()
    {
        // Subscribe to movement events
        if (playerMovement != null)
        {
            playerMovement.OnGroundedChanged += OnGroundedChanged;
            // Initialize lastGroundedTime if player starts grounded
            if (playerMovement.IsGrounded)
            {
                lastGroundedTime = Time.time;
                Debug.Log($"Player starts grounded at time: {lastGroundedTime}");
            }
        }
    }
    
    private void OnDestroy()
    {
        if (playerMovement != null)
            playerMovement.OnGroundedChanged -= OnGroundedChanged;
    }
    
    private void SetupInputAction()
    {
        // Use InputActionReference if assigned, otherwise create fallback
        if (jumpInputAction != null)
        {
            return; // InputActionReference will be used
        }
        
        // Fallback - create simple input for testing
        Debug.LogWarning("No InputActionReference assigned. Using Keyboard.current for jump input.");
    }
    
    private void OnEnable()
    {
        if (jumpInputAction != null)
            jumpInputAction.action.Enable();
    }
    
    private void OnDisable()
    {
        if (jumpInputAction != null)
            jumpInputAction.action.Disable();
    }
    
    private void Update()
    {
        HandleJumpInput();
        HandleJumpCrouch();
    }
    
    private void FixedUpdate()
    {
        HandleVariableJump();
        // Temporarily disable HandleBetterGravity to debug
        // HandleBetterGravity();
    }
    
    private void CalculateJumpForces()
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        float forceMultiplier = 4f; // Increased from 2f to 4f
        minJumpForce = Mathf.Sqrt(2f * gravity * minJumpHeight) * rb.mass * forceMultiplier;
        maxJumpForce = Mathf.Sqrt(2f * gravity * maxJumpHeight) * rb.mass * forceMultiplier;
        
        Debug.Log($"Jump forces calculated - Min: {minJumpForce}, Max: {maxJumpForce}, Gravity: {gravity}, Mass: {rb.mass}");
    }
    
    private void OnGroundedChanged(bool isGrounded)
    {
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            Debug.Log($"Player grounded at time: {lastGroundedTime}");
            if (IsJumping)
            {
                IsJumping = false;
                OnJumpLand?.Invoke();
            }
        }
        else
        {
            Debug.Log($"Player left ground at time: {Time.time}");
        }
        
        wasGroundedLastFrame = isGrounded;
    }
    
    private void HandleJumpInput()
    {
        bool currentlyPressed = false;
        
        // Check input from InputActionReference or fallback to keyboard
        if (jumpInputAction != null && jumpInputAction.action != null)
        {
            currentlyPressed = jumpInputAction.action.IsPressed();
        }
        else
        {
            // Fallback to direct keyboard input
            currentlyPressed = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        }
        
        // Debug input detection
        if (currentlyPressed != jumpButtonPressed)
        {
            Debug.Log($"Jump input changed: {currentlyPressed}, IsGrounded: {playerMovement?.IsGrounded}, CanJump: {CanJump()}");
        }
        
        // Start charging jump when space is pressed
        if (currentlyPressed && !jumpButtonPressed && !IsChargingJump && !IsJumping)
        {
            bool canStartJump = playerMovement != null && playerMovement.IsGrounded;
            Debug.Log($"Attempting to start jump - CanStart: {canStartJump}, IsGrounded: {playerMovement?.IsGrounded}");
            
            if (canStartJump)
            {
                IsChargingJump = true;
                jumpChargeStartTime = Time.time;
                targetCrouchOffset = -crouchAmount;
                Debug.Log("Jump charge started!");
            }
        }
        
        // Update charge percent
        if (IsChargingJump)
        {
            float chargeTime = Time.time - jumpChargeStartTime;
            ChargePercent = Mathf.Clamp01(chargeTime / maxChargeTime);
            OnChargeChanged?.Invoke(ChargePercent);
        }
        
        // Execute jump when space is released
        if (!currentlyPressed && jumpButtonPressed && IsChargingJump)
        {
            ExecuteJump();
            Debug.Log("Jump executed from charge release!");
        }
        
        // Cancel jump charge if we fall off platform
        if (IsChargingJump && playerMovement != null && !playerMovement.IsGrounded)
        {
            CancelJumpCharge();
            Debug.Log("Jump charge cancelled - not grounded!");
        }
        
        jumpButtonPressed = currentlyPressed;
    }
    
    private void ExecuteJump()
    {
        float chargeTime = Time.time - jumpChargeStartTime;
        float chargePercent = Mathf.Clamp01(chargeTime / maxChargeTime);
        float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, chargePercent);
        
        IsChargingJump = false;
        IsJumping = true;
        targetCrouchOffset = 0f;
        ChargePercent = 0f;
        
        Debug.Log($"Applying jump force: {jumpForce}, Player mass: {rb.mass}, Final force: {Vector3.up * jumpForce}");
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        OnJumpStart?.Invoke();
    }
    
    private void CancelJumpCharge()
    {
        IsChargingJump = false;
        targetCrouchOffset = 0f;
        ChargePercent = 0f;
    }
    
    private void HandleVariableJump()
    {
        // Variable jump height - shorter jump if button released early
        bool jumpPressed = false;
        if (jumpInputAction != null && jumpInputAction.action != null)
        {
            jumpPressed = jumpInputAction.action.IsPressed();
        }
        else
        {
            jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        }
        
        if (IsJumping && !jumpPressed && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f, rb.linearVelocity.z);
        }
    }
    
    private void HandleBetterGravity()
    {
        // Check if jump button is pressed
        bool jumpPressed = false;
        if (jumpInputAction != null && jumpInputAction.action != null)
        {
            jumpPressed = jumpInputAction.action.IsPressed();
        }
        else
        {
            jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        }
        
        // Apply different gravity multipliers based on jump state
        if (rb.linearVelocity.y < 0)
        {
            // Falling - increase gravity for snappier landings
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !jumpPressed)
        {
            // Rising but jump button released - apply low jump multiplier
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }
    
    private void HandleJumpCrouch()
    {
        if (cameraHolder == null) return;
        
        // Only modify Y position for crouch, preserve X and Z
        Vector3 currentPos = cameraHolder.localPosition;
        float targetY = originalCameraHolderPos.y + targetCrouchOffset;
        float newY = Mathf.Lerp(currentPos.y, targetY, crouchSpeed * Time.deltaTime);
        cameraHolder.localPosition = new Vector3(currentPos.x, newY, currentPos.z);
    }
    
    // Public methods for external control
    public void ForceJump(float force)
    {
        if (playerMovement != null && playerMovement.IsGrounded)
        {
            IsJumping = true;
            rb.AddForce(Vector3.up * force, ForceMode.Impulse);
            OnJumpStart?.Invoke();
        }
    }
    
    public void CancelJump()
    {
        if (IsChargingJump)
            CancelJumpCharge();
    }
    
    public bool CanJump()
    {
        return playerMovement != null && 
               (Time.time - lastGroundedTime <= coyoteTime) && 
               !IsJumping;
    }
}