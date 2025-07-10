using UnityEngine;

/// <summary>
/// Script que configura automáticamente todos los componentes necesarios del submarino
/// Debe ejecutarse después de que todos los scripts se hayan compilado
/// </summary>
[System.Serializable]
public class SubmarineConfiguration
{
    [Header("Systems")]
    public bool enableStabilization = true;
    public bool enableCollisionFeedback = true;
    public bool enablePhysicsIsolation = true;
    
    [Header("Stabilization Settings")]
    public float stabilizationStrength = 2f;
    public float maxTiltAngle = 45f;
    
    [Header("Feedback Settings")]
    public float impactSensitivity = 1f;
    public float shakeIntensity = 0.5f;
}

[RequireComponent(typeof(SubmarineController))]
public class SubmarineSetup : MonoBehaviour
{
    [Header("Configuration")]
    public SubmarineConfiguration config = new SubmarineConfiguration();
    
    [Header("Auto Setup")]
    public bool setupOnStart = true;
    public bool debugSetup = true;
    
    private void Start()
    {
        if (setupOnStart)
        {
            SetupSubmarine();
        }
    }
    
    [ContextMenu("Setup Submarine")]
    public void SetupSubmarine()
    {
        if (debugSetup)
            Debug.Log("[SUBMARINE SETUP] Starting submarine configuration...");
        
        // 1. Configurar SubmarineStabilizer
        if (config.enableStabilization)
        {
            SetupStabilizer();
        }
        
        // 2. Configurar SubmarineCollisionFeedback
        if (config.enableCollisionFeedback)
        {
            SetupCollisionFeedback();
        }
        
        // 3. Configurar otros sistemas
        if (config.enablePhysicsIsolation)
        {
            SetupPhysicsIsolation();
        }
        
        if (debugSetup)
            Debug.Log("[SUBMARINE SETUP] Submarine configuration completed!");
    }
    
    private void SetupStabilizer()
    {
        var stabilizer = GetComponent<SubmarineStabilizer>();
        if (stabilizer == null)
        {
            stabilizer = gameObject.AddComponent<SubmarineStabilizer>();
        }
        
        // Configurar usando reflexión para evitar problemas de dependencia
        var stabilizerType = stabilizer.GetType();
        
        var stabilizationField = stabilizerType.GetField("stabilizationStrength", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (stabilizationField != null)
        {
            stabilizationField.SetValue(stabilizer, config.stabilizationStrength);
        }
        
        var maxTiltField = stabilizerType.GetField("maxTiltAngle", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (maxTiltField != null)
        {
            maxTiltField.SetValue(stabilizer, config.maxTiltAngle);
        }
        
        if (debugSetup)
            Debug.Log("[SUBMARINE SETUP] Stabilizer configured");
    }
    
    private void SetupCollisionFeedback()
    {
        // Buscar el tipo por nombre para evitar dependencias en tiempo de compilación
        var feedbackType = System.Type.GetType("SubmarineCollisionFeedback");
        if (feedbackType == null)
        {
            if (debugSetup)
                Debug.LogWarning("[SUBMARINE SETUP] SubmarineCollisionFeedback type not found");
            return;
        }
        
        var feedback = GetComponent(feedbackType);
        if (feedback == null)
        {
            feedback = gameObject.AddComponent(feedbackType);
        }
        
        if (debugSetup)
            Debug.Log("[SUBMARINE SETUP] Collision Feedback configured");
    }
    
    private void SetupPhysicsIsolation()
    {
        var isolatorType = System.Type.GetType("SubmarinePhysicsIsolator");
        if (isolatorType != null)
        {
            var isolator = GetComponent(isolatorType);
            if (isolator == null)
            {
                gameObject.AddComponent(isolatorType);
            }
            
            if (debugSetup)
                Debug.Log("[SUBMARINE SETUP] Physics Isolator configured");
        }
    }
    
    [ContextMenu("Remove All Submarine Components")]
    public void RemoveAllComponents()
    {
        // Remover componentes opcionales (mantener SubmarineController)
        var stabilizer = GetComponent<SubmarineStabilizer>();
        if (stabilizer != null)
        {
            if (Application.isPlaying)
                Destroy(stabilizer);
            else
                DestroyImmediate(stabilizer);
        }
        
        var feedbackType = System.Type.GetType("SubmarineCollisionFeedback");
        if (feedbackType != null)
        {
            var feedback = GetComponent(feedbackType);
            if (feedback != null)
            {
                if (Application.isPlaying)
                    Destroy(feedback);
                else
                    DestroyImmediate(feedback);
            }
        }
        
        if (debugSetup)
            Debug.Log("[SUBMARINE SETUP] All optional components removed");
    }
    
    [ContextMenu("Verify Configuration")]
    public void VerifyConfiguration()
    {
        Debug.Log("=== SUBMARINE CONFIGURATION VERIFICATION ===");
        
        var controller = GetComponent<SubmarineController>();
        Debug.Log($"SubmarineController: {(controller != null ? "✓" : "✗")}");
        
        var stabilizer = GetComponent<SubmarineStabilizer>();
        Debug.Log($"SubmarineStabilizer: {(stabilizer != null ? "✓" : "✗")}");
        
        var feedbackType = System.Type.GetType("SubmarineCollisionFeedback");
        var feedback = feedbackType != null ? GetComponent(feedbackType) : null;
        Debug.Log($"SubmarineCollisionFeedback: {(feedback != null ? "✓" : "✗")}");
        
        var rb = GetComponent<Rigidbody>();
        Debug.Log($"Rigidbody: {(rb != null ? "✓" : "✗")}");
        if (rb != null)
        {
            Debug.Log($"  - isKinematic: {rb.isKinematic}");
            Debug.Log($"  - constraints: {rb.constraints}");
        }
        
        Debug.Log("============================================");
    }
}