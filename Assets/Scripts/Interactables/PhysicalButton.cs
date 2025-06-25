using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PhysicalButton : MonoBehaviour
{
    [Header("Button Settings")]
    public SubmarineCommand buttonCommand = SubmarineCommand.Forward;
    public float pressDepth = 0.1f;
    public float springForce = 50f;
    public float damping = 5f;
    public bool useClickInteraction = true;
    
    [Header("Visual Feedback")]
    public Material pressedMaterial;
    public Material normalMaterial;
    public AudioClip pressSound;
    public AudioClip releaseSound;
    
    [Header("Events")]
    public UnityEvent<SubmarineInput> OnButtonPressed;
    public UnityEvent OnButtonReleased;
    
    private Vector3 originalPosition;
    private Vector3 pressedPosition;
    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private AudioSource audioSource;
    private bool isPressed = false;
    private float pressStartTime;
    private bool isMouseOver = false;
    private bool isPlayerOnTop = false;
    
    // New Input System
    private Mouse mouse;
    private Camera playerCamera;
    
    // Reference to submarine controller
    private SubmarineController submarineController;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Find submarine in scene
        submarineController = FindFirstObjectByType<SubmarineController>();
        
        // Initialize Input System
        mouse = Mouse.current;
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();
    }
    
    private void Start()
    {
        originalPosition = transform.localPosition;
        pressedPosition = originalPosition + Vector3.down * pressDepth;
        
        // Configure rigidbody
        rb.mass = 1f;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | 
                        RigidbodyConstraints.FreezePositionX | 
                        RigidbodyConstraints.FreezePositionZ;
        
        // Set initial material
        if (normalMaterial != null)
            meshRenderer.material = normalMaterial;
    }
    
    private void Update()
    {
        HandleClickInteraction();
    }
    
    private void FixedUpdate()
    {
        if (useClickInteraction)
            HandleButtonPhysicsClick();
        else
            HandleButtonPhysics();
    }
    
    private void HandleButtonPhysics()
    {
        float currentY = transform.localPosition.y;
        float targetY = originalPosition.y;
        
        // Apply spring force back to original position
        Vector3 springDirection = Vector3.up;
        float displacement = targetY - currentY;
        Vector3 springForceVector = springDirection * (displacement * springForce);
        
        // Apply damping
        Vector3 dampingForce = -rb.linearVelocity * damping;
        
        rb.AddForce(springForceVector + dampingForce);
        
        // Limit movement range
        Vector3 clampedPosition = transform.localPosition;
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, 
                                       pressedPosition.y, 
                                       originalPosition.y + 0.01f);
        transform.localPosition = clampedPosition;
        
        // Check if button is pressed (50% of press depth)
        float pressThreshold = originalPosition.y - (pressDepth * 0.5f);
        bool wasPressed = isPressed;
        isPressed = currentY < pressThreshold;
        
        // Handle press/release events
        if (isPressed && !wasPressed)
        {
            OnPress();
        }
        else if (!isPressed && wasPressed)
        {
            OnRelease();
        }
        
        // Send continuous command while pressed
        if (isPressed && submarineController != null)
        {
            float intensity = Mathf.InverseLerp(originalPosition.y, pressedPosition.y, currentY);
            SubmarineInput input = new SubmarineInput(buttonCommand, intensity, Time.fixedDeltaTime);
            submarineController.ProcessCommand(input);
        }
    }
    
    private void OnPress()
    {
        pressStartTime = Time.time;
        
        // Visual feedback
        if (pressedMaterial != null)
            meshRenderer.material = pressedMaterial;
            
        // Audio feedback
        if (pressSound != null && audioSource != null)
            audioSource.PlayOneShot(pressSound);
            
        // Create submarine input
        SubmarineInput input = new SubmarineInput(buttonCommand, 1f, 0.1f);
        OnButtonPressed?.Invoke(input);
        
        Debug.Log($"Button {buttonCommand} pressed!");
    }
    
    private void OnRelease()
    {
        // Visual feedback
        if (normalMaterial != null)
            meshRenderer.material = normalMaterial;
            
        // Audio feedback
        if (releaseSound != null && audioSource != null)
            audioSource.PlayOneShot(releaseSound);
            
        OnButtonReleased?.Invoke();
        
        Debug.Log($"Button {buttonCommand} released!");
    }
    
    private void HandleClickInteraction()
    {
        if (!useClickInteraction || mouse == null || playerCamera == null) return;
        
        // Raycast from camera to detect button hover
        CheckMouseHover();
        
        // Check for mouse click on this button
        if (mouse.leftButton.wasPressedThisFrame && isMouseOver)
        {
            ActivateButton();
        }
        
        if (mouse.leftButton.wasReleasedThisFrame && isPressed)
        {
            DeactivateButton();
        }
    }
    
    private void CheckMouseHover()
    {
        Vector2 mousePosition = mouse.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                if (!isMouseOver)
                {
                    isMouseOver = true;
                    OnMouseEnterNew();
                }
            }
            else
            {
                if (isMouseOver)
                {
                    isMouseOver = false;
                    OnMouseExitNew();
                }
            }
        }
        else
        {
            if (isMouseOver)
            {
                isMouseOver = false;
                OnMouseExitNew();
            }
        }
    }
    
    private void HandleButtonPhysicsClick()
    {
        // Smooth animation to pressed/unpressed position
        Vector3 targetPosition = (isPressed || isPlayerOnTop) ? pressedPosition : originalPosition;
        float speed = 10f;
        
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, speed * Time.fixedDeltaTime);
        
        // Send continuous command while pressed
        if (isPressed && submarineController != null)
        {
            SubmarineInput input = new SubmarineInput(buttonCommand, 1f, Time.fixedDeltaTime);
            submarineController.ProcessCommand(input);
        }
    }
    
    private void ActivateButton()
    {
        if (isPressed) return;
        
        isPressed = true;
        pressStartTime = Time.time;
        
        // Visual feedback
        if (pressedMaterial != null)
            meshRenderer.material = pressedMaterial;
            
        // Audio feedback
        if (pressSound != null && audioSource != null)
            audioSource.PlayOneShot(pressSound);
            
        // Create submarine input
        SubmarineInput input = new SubmarineInput(buttonCommand, 1f, 0.1f);
        OnButtonPressed?.Invoke(input);
        
        Debug.Log($"Button {buttonCommand} clicked!");
    }
    
    private void DeactivateButton()
    {
        if (!isPressed) return;
        
        isPressed = false;
        
        // Visual feedback
        if (normalMaterial != null)
            meshRenderer.material = normalMaterial;
            
        // Audio feedback
        if (releaseSound != null && audioSource != null)
            audioSource.PlayOneShot(releaseSound);
            
        OnButtonReleased?.Invoke();
        
        Debug.Log($"Button {buttonCommand} released!");
    }
    
    private void OnMouseEnterNew()
    {
        // Optional: Add hover effect here
        Debug.Log($"Mouse entered button {buttonCommand}");
    }
    
    private void OnMouseExitNew()
    {
        // Optional: Remove hover effect here
        Debug.Log($"Mouse exited button {buttonCommand}");
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Check if player is stepping on the button
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerOnTop = true;
            if (!isPressed)
            {
                ActivateButtonByContact();
            }
        }
        
        if (useClickInteraction) return;
        
        // Add impact force when hit by player or objects (only for physical mode)
        if (collision.relativeVelocity.magnitude > 1f)
        {
            Vector3 impactForce = collision.relativeVelocity.normalized * collision.relativeVelocity.magnitude;
            rb.AddForce(-impactForce, ForceMode.Impulse);
        }
    }
    
    private void OnCollisionExit(Collision collision)
    {
        // Check if player stopped stepping on the button
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerOnTop = false;
            if (isPressed)
            {
                DeactivateButtonByContact();
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Alternative method using trigger colliders
        if (other.CompareTag("Player"))
        {
            isPlayerOnTop = true;
            if (!isPressed)
            {
                ActivateButtonByContact();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Alternative method using trigger colliders
        if (other.CompareTag("Player"))
        {
            isPlayerOnTop = false;
            if (isPressed)
            {
                DeactivateButtonByContact();
            }
        }
    }
    
    private void ActivateButtonByContact()
    {
        // Don't activate by contact if already pressed
        if (isPressed) return;
        
        isPressed = true;
        pressStartTime = Time.time;
        
        // Visual feedback
        if (pressedMaterial != null)
            meshRenderer.material = pressedMaterial;
            
        // Audio feedback
        if (pressSound != null && audioSource != null)
            audioSource.PlayOneShot(pressSound);
            
        // Create submarine input
        SubmarineInput input = new SubmarineInput(buttonCommand, 1f, 0.1f);
        OnButtonPressed?.Invoke(input);
        
        Debug.Log($"Button {buttonCommand} activated by player contact!");
    }
    
    private void DeactivateButtonByContact()
    {
        // Only deactivate by contact if currently pressed
        if (!isPressed) return;
        
        isPressed = false;
        
        // Visual feedback
        if (normalMaterial != null)
            meshRenderer.material = normalMaterial;
            
        // Audio feedback
        if (releaseSound != null && audioSource != null)
            audioSource.PlayOneShot(releaseSound);
            
        OnButtonReleased?.Invoke();
        
        Debug.Log($"Button {buttonCommand} deactivated by player leaving!");
    }
}