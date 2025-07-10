using UnityEngine;

/// <summary>
/// Aísla la física del submarino para evitar que sea afectado por fuerzas externas del jugador
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SubmarinePhysicsIsolator : MonoBehaviour
{
    [Header("Physics Isolation Settings")]
    [SerializeField] private float massMultiplier = 1000f;
    [SerializeField] private bool freezeRotationX = true;
    [SerializeField] private bool freezeRotationZ = true;
    [SerializeField] private bool freezePositionY = true;
    [SerializeField] private float maxAngularVelocity = 5f;
    
    [Header("Collision Response")]
    [SerializeField] private LayerMask playerLayer = -1;
    [SerializeField] private float collisionDampingFactor = 0.95f;
    
    private Rigidbody rb;
    private float originalMass;
    private Vector3 lastValidVelocity;
    private Vector3 lastValidAngularVelocity;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalMass = rb.mass;
    }
    
    private void Start()
    {
        ConfigurePhysics();
    }
    
    private void ConfigurePhysics()
    {
        // Aumentar significativamente la masa para resistir fuerzas externas
        rb.mass = originalMass * massMultiplier;
        
        // Configurar restricciones
        RigidbodyConstraints constraints = RigidbodyConstraints.None;
        
        if (freezePositionY)
            constraints |= RigidbodyConstraints.FreezePositionY;
            
        if (freezeRotationX)
            constraints |= RigidbodyConstraints.FreezeRotationX;
            
        if (freezeRotationZ)
            constraints |= RigidbodyConstraints.FreezeRotationZ;
            
        rb.constraints = constraints;
        
        // Configurar física adicional
        rb.maxAngularVelocity = maxAngularVelocity;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        Debug.Log($"[SUBMARINE PHYSICS ISOLATOR] Configured - Mass: {rb.mass}, Constraints: {rb.constraints}");
    }
    
    private void FixedUpdate()
    {
        // Guardar velocidades válidas
        lastValidVelocity = rb.linearVelocity;
        lastValidAngularVelocity = rb.angularVelocity;
        
        // Limitar velocidad angular si es necesario
        if (rb.angularVelocity.magnitude > maxAngularVelocity)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        HandlePlayerCollision(collision);
    }
    
    private void OnCollisionStay(Collision collision)
    {
        HandlePlayerCollision(collision);
    }
    
    private void HandlePlayerCollision(Collision collision)
    {
        // Verificar si es el jugador
        if (IsPlayer(collision.gameObject))
        {
            // Aplicar amortiguación para reducir cualquier cambio brusco en la velocidad
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, lastValidVelocity, collisionDampingFactor);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, lastValidAngularVelocity, collisionDampingFactor);
            
            // Opcionalmente, aplicar una pequeña fuerza de separación al jugador
            Rigidbody playerRb = collision.rigidbody;
            if (playerRb != null)
            {
                Vector3 separationDirection = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(separationDirection * 2f, ForceMode.Impulse);
            }
        }
    }
    
    private bool IsPlayer(GameObject obj)
    {
        // Verificar por layer
        if ((playerLayer.value & (1 << obj.layer)) != 0)
            return true;
            
        // Verificar por tag
        if (obj.CompareTag("Player"))
            return true;
            
        // Verificar por componente
        if (obj.GetComponent<PlayerMovement>() != null)
            return true;
            
        return false;
    }
    
    // Método público para ajustar la masa en runtime
    public void SetMassMultiplier(float multiplier)
    {
        massMultiplier = Mathf.Max(1f, multiplier);
        rb.mass = originalMass * massMultiplier;
    }
    
    // Método para restaurar la configuración original
    public void RestoreOriginalPhysics()
    {
        rb.mass = originalMass;
        rb.constraints = RigidbodyConstraints.None;
    }
}