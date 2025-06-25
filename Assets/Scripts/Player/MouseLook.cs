using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles first-person mouse look functionality
/// Manages camera rotation and cursor locking
/// </summary>
public class MouseLook : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;
    public bool lockCursor = true;
    
    [Header("Look Constraints")]
    public float minLookAngle = -90f;
    public float maxLookAngle = 90f;
    
    [Header("References")]
    public Transform playerBody;
    public Transform cameraHolder;
    
    // Private variables
    private float xRotation = 0f;
    private float yRotation = 0f;
    private InputAction lookAction;
    private InputAction toggleCursorAction;
    
    private void Awake()
    {
        // Auto-find references if not assigned
        if (playerBody == null)
            playerBody = transform.parent;
            
        if (cameraHolder == null)
            cameraHolder = transform.parent;
            
        SetupInputActions();
        InitializeRotations();
    }
    
    private void Start()
    {
        SetCursorState(lockCursor);
    }
    
    private void OnEnable()
    {
        lookAction?.Enable();
        toggleCursorAction?.Enable();
    }
    
    private void OnDisable()
    {
        lookAction?.Disable();
        toggleCursorAction?.Disable();
    }
    
    private void Update()
    {
        HandleCursorToggle();
        HandleMouseLook();
    }
    
    private void SetupInputActions()
    {
        // Look: Mouse Delta / Right Stick
        lookAction = new InputAction("Look");
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");
        
        // Tab key to toggle cursor
        toggleCursorAction = new InputAction("ToggleCursor", InputActionType.Button);
        toggleCursorAction.AddBinding("<Keyboard>/tab");
    }
    
    private void InitializeRotations()
    {
        if (playerBody != null)
            yRotation = playerBody.eulerAngles.y;
            
        if (cameraHolder != null)
            xRotation = cameraHolder.localEulerAngles.x;
    }
    
    private void HandleCursorToggle()
    {
        if (toggleCursorAction.triggered)
        {
            bool newLockState = Cursor.lockState != CursorLockMode.Locked;
            SetCursorState(newLockState);
        }
    }
    
    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
        
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;
        
        // Accumulate rotations
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minLookAngle, maxLookAngle);
        
        // Apply rotations
        ApplyRotations();
    }
    
    private void ApplyRotations()
    {
        if (playerBody != null)
            playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);
            
        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
    
    private void SetCursorState(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    // Public methods for external control
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
    
    public void SetLockState(bool locked)
    {
        SetCursorState(locked);
    }
    
    public Vector2 GetLookAngles()
    {
        return new Vector2(xRotation, yRotation);
    }
}