using UnityEngine;

/// <summary>
/// Gestiona los colliders del submarino para separar colisiones físicas de triggers del interior
/// </summary>
public class SubmarineCollisionManager : MonoBehaviour
{
    [Header("Collision Setup")]
    public bool autoSetupColliders = true;
    public PhysicsMaterial submarinePhysicsMaterial;
    
    [Header("Hull Colliders (Physical)")]
    public Transform hullCollidersParent;
    
    [Header("Interior Trigger")]
    public Transform interiorTriggerParent;
    
    [Header("Debug")]
    public bool debugMode = false;
    
    private SubmarineController submarineController;
    
    private void Awake()
    {
        submarineController = GetComponent<SubmarineController>();
        
        if (autoSetupColliders)
        {
            SetupColliders();
        }
    }
    
    private void SetupColliders()
    {
        // Crear padres para organizar colliders
        if (hullCollidersParent == null)
        {
            hullCollidersParent = new GameObject("Hull_Colliders").transform;
            hullCollidersParent.SetParent(transform);
            hullCollidersParent.localPosition = Vector3.zero;
            hullCollidersParent.localRotation = Quaternion.identity;
        }
        
        if (interiorTriggerParent == null)
        {
            interiorTriggerParent = new GameObject("Interior_Trigger").transform;  
            interiorTriggerParent.SetParent(transform);
            interiorTriggerParent.localPosition = Vector3.zero;
            interiorTriggerParent.localRotation = Quaternion.identity;
        }
        
        CreateHullColliders();
        CreateInteriorTrigger();
        
        if (debugMode)
            Debug.Log("[SUBMARINE COLLISION] Colliders setup completed");
    }
    
    private void CreateHullColliders()
    {
        // Collider principal del casco (cápsula alargada)
        GameObject mainHull = new GameObject("MainHull_Collider");
        mainHull.transform.SetParent(hullCollidersParent);
        mainHull.transform.localPosition = Vector3.zero;
        mainHull.transform.localRotation = Quaternion.identity;
        
        CapsuleCollider mainCollider = mainHull.AddComponent<CapsuleCollider>();
        mainCollider.direction = 2; // Z-axis (forward/backward)
        mainCollider.height = 8f; // Largo del submarino
        mainCollider.radius = 1.2f; // Radio del casco
        mainCollider.center = Vector3.zero;
        
        if (submarinePhysicsMaterial != null)
            mainCollider.material = submarinePhysicsMaterial;
        
        // Collider de la torre/conning tower
        GameObject tower = new GameObject("Tower_Collider");
        tower.transform.SetParent(hullCollidersParent);
        tower.transform.localPosition = new Vector3(0, 1.5f, 0);
        tower.transform.localRotation = Quaternion.identity;
        
        BoxCollider towerCollider = tower.AddComponent<BoxCollider>();
        towerCollider.size = new Vector3(1.5f, 2f, 3f);
        
        if (submarinePhysicsMaterial != null)
            towerCollider.material = submarinePhysicsMaterial;
        
        // Colliders de las aletas/fins
        CreateFinCollider("LeftFin_Collider", new Vector3(-1.8f, 0, -2f), new Vector3(1f, 0.3f, 2f));
        CreateFinCollider("RightFin_Collider", new Vector3(1.8f, 0, -2f), new Vector3(1f, 0.3f, 2f));
        
        if (debugMode)
            Debug.Log("[SUBMARINE COLLISION] Hull colliders created");
    }
    
    private void CreateFinCollider(string name, Vector3 position, Vector3 size)
    {
        GameObject fin = new GameObject(name);
        fin.transform.SetParent(hullCollidersParent);
        fin.transform.localPosition = position;
        fin.transform.localRotation = Quaternion.identity;
        
        BoxCollider finCollider = fin.AddComponent<BoxCollider>();
        finCollider.size = size;
        
        if (submarinePhysicsMaterial != null)
            finCollider.material = submarinePhysicsMaterial;
    }
    
    private void CreateInteriorTrigger()
    {
        GameObject interior = new GameObject("Interior_Zone");
        interior.transform.SetParent(interiorTriggerParent);
        interior.transform.localPosition = Vector3.zero;
        interior.transform.localRotation = Quaternion.identity;
        
        // Añadir el componente SubmarineInterior
        SubmarineInterior interiorScript = interior.AddComponent<SubmarineInterior>();
        interiorScript.submarineTransform = transform;
        interiorScript.debugMode = debugMode;
        
        // Crear trigger collider para el interior
        CapsuleCollider interiorCollider = interior.AddComponent<CapsuleCollider>();
        interiorCollider.isTrigger = true;
        interiorCollider.direction = 2; // Z-axis
        interiorCollider.height = 6f; // Más pequeño que el casco físico
        interiorCollider.radius = 0.8f; // Más pequeño que el casco físico
        interiorCollider.center = Vector3.zero;
        
        if (debugMode)
            Debug.Log("[SUBMARINE COLLISION] Interior trigger created");
    }
    
    // Método para crear material físico por defecto si no se asigna uno
    private void CreateDefaultPhysicsMaterial()
    {
        if (submarinePhysicsMaterial == null)
        {
            submarinePhysicsMaterial = new PhysicsMaterial("SubmarineHull");
            submarinePhysicsMaterial.dynamicFriction = 0.3f;
            submarinePhysicsMaterial.staticFriction = 0.4f;
            submarinePhysicsMaterial.bounciness = 0.1f;
            submarinePhysicsMaterial.frictionCombine = PhysicsMaterialCombine.Average;
            submarinePhysicsMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
            
            if (debugMode)
                Debug.Log("[SUBMARINE COLLISION] Default physics material created");
        }
    }
    
    private void Start()
    {
        CreateDefaultPhysicsMaterial();
    }
    
    // Método público para reconfigurar colliders en runtime
    public void ReconfigureColliders()
    {
        // Limpiar colliders existentes
        if (hullCollidersParent != null)
        {
            for (int i = hullCollidersParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(hullCollidersParent.GetChild(i).gameObject);
            }
        }
        
        if (interiorTriggerParent != null)
        {
            for (int i = interiorTriggerParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(interiorTriggerParent.GetChild(i).gameObject);
            }
        }
        
        // Recrear colliders
        SetupColliders();
        
        if (debugMode)
            Debug.Log("[SUBMARINE COLLISION] Colliders reconfigured");
    }
}