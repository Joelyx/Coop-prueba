using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Physical sonar activation button for submarine
/// Triggers sonar ping when pressed manually
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SonarButton : MonoBehaviour
{
    [Header("Button Settings")]
    public float pressDepth = 0.1f;
    public float springForce = 50f;
    public float damping = 5f;
    public bool useClickInteraction = true;
    
    [Header("Visual Feedback")]
    public Material pressedMaterial;
    public Material normalMaterial;
    public AudioClip pressSound;
    public AudioClip releaseSound;
    public Light buttonLight; // Optional LED indicator
    public Color normalLightColor = Color.green;
    public Color pressedLightColor = Color.red;
    public Color cooldownLightColor = Color.orange;
    
    [Header("Sonar Integration")]
    public SubmarineSonar sonarSystem;
    public bool showCooldownIndicator = true;
    
    [Header("Button Label")]
    public TextMesh buttonLabel;
    public string normalText = "SONAR\nREADY";
    public string cooldownText = "SONAR\nCOOLDOWN";
    public string pressedText = "SONAR\nPING!";
    
    [Header("Events")]
    public UnityEvent OnSonarActivated;
    public UnityEvent OnButtonPressed;
    public UnityEvent OnButtonReleased;
    
    private Vector3 originalPosition;
    private Vector3 pressedPosition;
    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private AudioSource audioSource;
    private bool isPressed = false;
    private bool isMouseOver = false;
    
    // Input System
    private Mouse mouse;
    private Camera playerCamera;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Find sonar system if not assigned
        if (sonarSystem == null)
            sonarSystem = FindFirstObjectByType<SubmarineSonar>();
            
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
            
        // Setup button light
        SetupButtonLight();
        
        // Update button state
        UpdateButtonState();
    }
    
    private void Update()
    {
        HandleClickInteraction();
        UpdateButtonState();
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
    }
    
    private void HandleButtonPhysicsClick()
    {
        // Smooth animation to pressed/unpressed position
        Vector3 targetPosition = isPressed ? pressedPosition : originalPosition;
        float speed = 10f;
        
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, speed * Time.fixedDeltaTime);
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
    
    private void ActivateButton()
    {
        if (isPressed) return;
        
        // Check if sonar can ping
        if (sonarSystem != null && !sonarSystem.CanPing)
        {
            // Play error sound or visual feedback
            PlayErrorFeedback();
            return;
        }
        
        isPressed = true;
        
        // Visual feedback
        if (pressedMaterial != null)
            meshRenderer.material = pressedMaterial;
            
        // Audio feedback
        if (pressSound != null && audioSource != null)
            audioSource.PlayOneShot(pressSound);
            
        // Trigger sonar ping
        if (sonarSystem != null)
        {
            sonarSystem.TriggerSonarPing();
            OnSonarActivated?.Invoke();
        }
        
        OnButtonPressed?.Invoke();
        
        Debug.Log("Sonar button activated - Ping sent!");
        
        // Auto-release after short delay
        Invoke(nameof(DeactivateButton), 0.2f);
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
        
        Debug.Log("Sonar button released!");
    }
    
    private void OnPress()
    {
        // Check if sonar can ping
        if (sonarSystem != null && !sonarSystem.CanPing)
        {
            PlayErrorFeedback();
            return;
        }
        
        // Visual feedback
        if (pressedMaterial != null)
            meshRenderer.material = pressedMaterial;
            
        // Audio feedback
        if (pressSound != null && audioSource != null)
            audioSource.PlayOneShot(pressSound);
            
        // Trigger sonar ping
        if (sonarSystem != null)
        {
            sonarSystem.TriggerSonarPing();
            OnSonarActivated?.Invoke();
        }
        
        OnButtonPressed?.Invoke();
        
        Debug.Log("Sonar button pressed - Ping sent!");
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
        
        Debug.Log("Sonar button released!");
    }
    
    private void UpdateButtonState()
    {
        if (sonarSystem == null) return;
        
        bool canPing = sonarSystem.CanPing;
        float cooldownRemaining = sonarSystem.GetCooldownRemaining();
        
        // Update button label
        if (buttonLabel != null)
        {
            if (isPressed)
            {
                buttonLabel.text = pressedText;
            }
            else if (!canPing)
            {
                buttonLabel.text = cooldownText + $"\n{cooldownRemaining:F1}s";
            }
            else
            {
                buttonLabel.text = normalText;
            }
        }
        
        // Update button light
        if (buttonLight != null)
        {
            if (isPressed)
            {
                buttonLight.color = pressedLightColor;
                buttonLight.intensity = 2f;
            }
            else if (!canPing && showCooldownIndicator)
            {
                buttonLight.color = cooldownLightColor;
                buttonLight.intensity = 0.5f;
            }
            else
            {
                buttonLight.color = normalLightColor;
                buttonLight.intensity = 1f;
            }
        }
    }
    
    private void SetupButtonLight()
    {
        if (buttonLight == null)
        {
            // Try to find a light as child
            buttonLight = GetComponentInChildren<Light>();
        }
        
        if (buttonLight != null)
        {
            buttonLight.type = LightType.Point;
            buttonLight.range = 3f;
            buttonLight.intensity = 1f;
            buttonLight.color = normalLightColor;
        }
    }
    
    private void PlayErrorFeedback()
    {
        // Flash red or play error sound when button can't be used
        if (buttonLight != null)
        {
            StartCoroutine(FlashErrorLight());
        }
        
        // Could add error sound here
        Debug.Log("Sonar is on cooldown!");
    }
    
    private System.Collections.IEnumerator FlashErrorLight()
    {
        if (buttonLight == null) yield break;
        
        Color originalColor = buttonLight.color;
        float originalIntensity = buttonLight.intensity;
        
        // Flash red 3 times
        for (int i = 0; i < 3; i++)
        {
            buttonLight.color = Color.red;
            buttonLight.intensity = 2f;
            yield return new WaitForSeconds(0.1f);
            
            buttonLight.color = originalColor;
            buttonLight.intensity = originalIntensity;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void OnMouseEnterNew()
    {
        // Optional: Add hover effect here
        Debug.Log("Mouse entered sonar button");
    }
    
    private void OnMouseExitNew()
    {
        // Optional: Remove hover effect here
        Debug.Log("Mouse exited sonar button");
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isPressed)
            {
                ActivateButton();
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
        // Player left collision area
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!isPressed)
            {
                ActivateButton();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Player left trigger area
    }
}