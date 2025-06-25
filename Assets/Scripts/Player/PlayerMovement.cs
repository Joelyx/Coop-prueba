using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles physics-based player movement
/// Manages ground detection, slope handling, and step climbing
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Physics")]
    public float walkSpeed = 6f;
    public float runSpeed = 10f;
    public float acceleration = 40f;
    public float deceleration = 30f;
    public float airControl = 0.2f;
    
    [Header("Ground Detection")]
    public LayerMask groundMask = 1;
    public float groundDistance = 0.1f;
    public Transform groundCheck;
    
    [Header("Step Climbing")]
    public float stepHeight = 0.3f;
    public float stepSmoothness = 10f;
    public float stepCooldown = 0.1f;
    
    [Header("Slope Handling")]
    public float maxSlope = 45f;
    public float slopeForce = 3f;
    
    // Properties for external access
    public bool IsGrounded { get; private set; }
    public float CurrentSpeed { get; private set; }
    public Vector3 GroundNormal { get; private set; }
    public Vector3 Velocity => rb.linearVelocity;
    
    // Private components and variables
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector3 lastGroundedPosition;
    private float lastStepTime;
    
    // Input actions
    private InputAction moveAction;
    private InputAction sprintAction;
    
    // Events for other systems
    public System.Action<bool> OnGroundedChanged;
    public System.Action<float> OnSpeedChanged;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        
        ConfigureRigidbody();
        SetupGroundCheck();
        SetupInputActions();
    }
    
    private void ConfigureRigidbody()
    {
        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.mass = 60f;
        rb.linearDamping = 5f;
        rb.angularDamping = 0f;
        
        // Set gravity for better jump feeling
        Physics.gravity = new Vector3(0, -15f, 0);
        
        Debug.Log($"Player Rigidbody configured - Mass: {rb.mass}, Gravity: {Physics.gravity}");
    }
    
    private void SetupGroundCheck()
    {
        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -capsuleCollider.height / 2 - 0.05f, 0);
        }
    }
    
    private void SetupInputActions()
    {
        // Movement: WASD / Left Stick
        moveAction = new InputAction("Move");
        var moveComposite = moveAction.AddCompositeBinding("2DVector");
        moveComposite.With("Up", "<Keyboard>/w");
        moveComposite.With("Up", "<Keyboard>/upArrow");
        moveComposite.With("Down", "<Keyboard>/s");
        moveComposite.With("Down", "<Keyboard>/downArrow");
        moveComposite.With("Left", "<Keyboard>/a");
        moveComposite.With("Left", "<Keyboard>/leftArrow");
        moveComposite.With("Right", "<Keyboard>/d");
        moveComposite.With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        // Sprint: Left Shift / Left Stick Press
        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");
    }
    
    private void OnEnable()
    {
        moveAction?.Enable();
        sprintAction?.Enable();
    }
    
    private void OnDisable()
    {
        moveAction?.Disable();
        sprintAction?.Disable();
    }
    
    private void Update()
    {
        CheckGrounded();
    }
    
    private void LateUpdate()
    {
        // Only handle input reading in LateUpdate, not physics
    }
    
    private void FixedUpdate()
    {
        // Handle all physics in FixedUpdate for consistent timing
        HandleMovement();
        HandleStepClimbing();
        UpdateCurrentSpeed();
        DebugVelocityChanges();
    }
    
    private Vector3 lastVelocity = Vector3.zero;
    
    private void DebugVelocityChanges()
    {
        // Log if velocity is unusually high
        if (rb.linearVelocity.magnitude > 20f)
        {
            Debug.LogWarning($"HIGH VELOCITY DETECTED: {rb.linearVelocity.magnitude} - Velocity: {rb.linearVelocity}");
        }
        
        // Log if velocity changes drastically between frames
        Vector3 velocityChange = rb.linearVelocity - lastVelocity;
        if (velocityChange.magnitude > 10f)
        {
            Debug.LogWarning($"SUDDEN VELOCITY CHANGE: {velocityChange.magnitude} - From: {lastVelocity} To: {rb.linearVelocity}");
        }
        lastVelocity = rb.linearVelocity;
    }
    
    private void CheckGrounded()
    {
        bool wasGrounded = IsGrounded;
        IsGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (IsGrounded != wasGrounded)
        {
            OnGroundedChanged?.Invoke(IsGrounded);
        }
        
        if (IsGrounded)
        {
            RaycastHit hit;
            if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, groundDistance + 0.1f, groundMask))
            {
                GroundNormal = hit.normal;
                lastGroundedPosition = transform.position;
            }
        }
    }
    
    private void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(input.x, 0, input.y).normalized;
        
        // Use current transform rotation for immediate direction update
        Vector3 worldInputDirection = transform.TransformDirection(inputDirection);
        
        bool isSprinting = sprintAction.IsPressed();
        float targetSpeed = isSprinting ? runSpeed : walkSpeed;
        
        if (isSprinting && inputDirection.magnitude > 0.1f)
        {
            Debug.Log($"Sprinting - Target speed: {targetSpeed}, Current speed: {new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude}");
        }
        Vector3 targetVelocity = worldInputDirection * targetSpeed;
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        Vector3 velocityChange;
        if (inputDirection.magnitude > 0.1f)
        {
            // Calculate velocity difference to target
            Vector3 velocityDifference = targetVelocity - currentVelocity;
            float currentAcceleration = IsGrounded ? acceleration : acceleration * airControl;
            velocityChange = Vector3.ClampMagnitude(velocityDifference, currentAcceleration * Time.fixedDeltaTime);
            
            if (isSprinting && inputDirection.magnitude > 0.1f)
            {
                Debug.Log($"Velocity diff: {velocityDifference.magnitude}, Max change: {currentAcceleration * Time.fixedDeltaTime}, Actual change: {velocityChange.magnitude}");
            }
            
            // Ensure we never exceed target speed (reduce target speed in air)
            float currentMaxSpeed = IsGrounded ? targetSpeed : targetSpeed * airControl;
            Vector3 newVelocity = currentVelocity + velocityChange;
            if (newVelocity.magnitude > currentMaxSpeed)
            {
                newVelocity = newVelocity.normalized * currentMaxSpeed;
                velocityChange = newVelocity - currentVelocity;
            }
        }
        else
        {
            // Stop when no input (less stopping power in air)
            float stoppingPower = IsGrounded ? 0.8f : 0.1f;
            velocityChange = -currentVelocity * stoppingPower;
        }
        
        // Apply slope forces only when grounded
        if (IsGrounded)
        {
            HandleSlopeForces();
        }
        
        // Apply movement
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
        
        // Temporarily disabled velocity clamp to test sprint
        /*
        Vector3 currentVel = rb.linearVelocity;
        float maxSpeed = IsGrounded ? (isSprinting ? runSpeed + 2f : walkSpeed + 2f) : 20f;
        if (new Vector3(currentVel.x, 0, currentVel.z).magnitude > maxSpeed)
        {
            Vector3 clampedVel = new Vector3(currentVel.x, 0, currentVel.z).normalized * maxSpeed;
            rb.linearVelocity = new Vector3(clampedVel.x, currentVel.y, clampedVel.z);
            Debug.LogWarning($"Velocity clamped to: {rb.linearVelocity}");
        }
        */
        
        if (isSprinting && inputDirection.magnitude > 0.1f)
        {
            Debug.Log($"Final velocity after sprint: {new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude}");
        }
    }
    
    private void HandleSlopeForces()
    {
        if (IsGrounded && Vector3.Angle(GroundNormal, Vector3.up) > 5f)
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, GroundNormal).normalized;
            float slopeAngle = Vector3.Angle(GroundNormal, Vector3.up);
            
            if (slopeAngle <= maxSlope)
            {
                rb.AddForce(slopeDirection * slopeForce * (slopeAngle / maxSlope), ForceMode.Acceleration);
            }
        }
    }
    
    private void HandleStepClimbing()
    {
        if (!IsGrounded || Time.time - lastStepTime < stepCooldown) return;
        
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.magnitude < 0.1f) return;
        
        Vector3 moveDirection = transform.TransformDirection(new Vector3(input.x, 0, input.y).normalized);
        
        // Check for step in front
        Vector3 rayOrigin = transform.position + Vector3.up * stepHeight;
        RaycastHit hit;
        
        if (Physics.Raycast(rayOrigin, moveDirection, out hit, 0.5f, groundMask))
        {
            // Check if there's a walkable surface above the obstacle
            Vector3 stepCheckOrigin = hit.point + Vector3.up * (stepHeight + 0.1f);
            
            if (!Physics.Raycast(stepCheckOrigin, Vector3.down, stepHeight + 0.2f, groundMask))
            {
                // Step up smoothly
                Vector3 stepUpForce = Vector3.up * stepSmoothness;
                rb.AddForce(stepUpForce, ForceMode.Impulse);
                lastStepTime = Time.time;
            }
        }
    }
    
    private void UpdateCurrentSpeed()
    {
        float newSpeed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
        if (Mathf.Abs(newSpeed - CurrentSpeed) > 0.1f)
        {
            CurrentSpeed = newSpeed;
            OnSpeedChanged?.Invoke(CurrentSpeed);
        }
    }
    
    // Public methods for external control
    public Vector2 GetMovementInput()
    {
        return moveAction.ReadValue<Vector2>();
    }
    
    public bool IsMoving()
    {
        return GetMovementInput().magnitude > 0.1f && CurrentSpeed > 0.1f;
    }
    
    public bool IsSprinting()
    {
        return sprintAction.IsPressed();
    }
    
    public void AddForce(Vector3 force, ForceMode forceMode = ForceMode.Force)
    {
        rb.AddForce(force, forceMode);
    }
}