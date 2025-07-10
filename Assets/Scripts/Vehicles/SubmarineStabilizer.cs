using UnityEngine;

/// <summary>
/// Sistema de estabilización que permite al submarino tumbarse con impactos
/// pero gradualmente vuelve a la posición horizontal
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SubmarineStabilizer : MonoBehaviour
{
    [Header("Stabilization Settings")]
    [SerializeField] private float stabilizationStrength = 2f;
    [SerializeField] private float maxTiltAngle = 45f;
    [SerializeField] private float stabilizationDelay = 0.5f;
    
    [Header("Collision Response")]
    [SerializeField] private float collisionTiltMultiplier = 1f;
    [SerializeField] private float collisionSpinMultiplier = 0.5f;
    
    [Header("Buoyancy Simulation")]
    [SerializeField] private bool simulateBuoyancy = true;
    [SerializeField] private float buoyancyStrength = 10f;
    [SerializeField] private float centerOfBuoyancy = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Rigidbody rb;
    private float timeSinceLastCollision = 0f;
    private Vector3 targetRotation = Vector3.zero;
    private bool isStabilizing = false;
    
    // Eventos para notificar cambios de estado
    public System.Action<float> OnTiltChanged;
    public System.Action<float> OnImpact;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    private void Start()
    {
        // Configurar el Rigidbody para permitir rotaciones
        rb.constraints = RigidbodyConstraints.None;
        
        // Ajustar el centro de masa para mejor estabilidad
        rb.centerOfMass = new Vector3(0, -centerOfBuoyancy, 0);
        
        // Aumentar el drag angular para movimientos más suaves
        rb.angularDamping = 2f;
    }
    
    private void FixedUpdate()
    {
        timeSinceLastCollision += Time.fixedDeltaTime;
        
        // Solo estabilizar después del delay y si no estamos en medio de una colisión
        if (timeSinceLastCollision > stabilizationDelay)
        {
            if (!isStabilizing) 
            {
                isStabilizing = true;
                targetRotation = new Vector3(0, transform.eulerAngles.y, 0);
            }
            
            ApplyStabilization();
        }
        else
        {
            isStabilizing = false;
        }
        
        if (simulateBuoyancy)
        {
            ApplyBuoyancy();
        }
        
        // Limitar el ángulo máximo de inclinación
        LimitTilt();
        
        // Notificar el ángulo de inclinación actual
        float currentTilt = Vector3.Angle(transform.up, Vector3.up);
        OnTiltChanged?.Invoke(currentTilt);
    }
    
    private void ApplyStabilization()
    {
        // Calcular la rotación actual sin el componente Y (yaw)
        Vector3 currentRotation = transform.eulerAngles;
        float yaw = currentRotation.y;
        
        // Calcular qué tan lejos estamos de la horizontal
        float pitchError = Mathf.DeltaAngle(currentRotation.x, 0);
        float rollError = Mathf.DeltaAngle(currentRotation.z, 0);
        
        // Aplicar torque correctivo proporcional al error
        Vector3 correctiveTorque = new Vector3(
            -pitchError * stabilizationStrength,
            0, // No corregir el yaw
            -rollError * stabilizationStrength
        );
        
        // Convertir a espacio local y aplicar
        correctiveTorque = transform.TransformDirection(correctiveTorque);
        rb.AddTorque(correctiveTorque, ForceMode.Force);
        
        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[STABILIZER] Pitch error: {pitchError:F1}°, Roll error: {rollError:F1}°");
        }
    }
    
    private void ApplyBuoyancy()
    {
        // Simular fuerzas de flotación que ayudan a enderezar el submarino
        Vector3 buoyancyForce = Vector3.up * buoyancyStrength;
        
        // Aplicar la fuerza en un punto por encima del centro de masa
        Vector3 buoyancyPoint = transform.position + transform.up * centerOfBuoyancy;
        rb.AddForceAtPosition(buoyancyForce, buoyancyPoint, ForceMode.Force);
        
        // Fuerza opuesta en la parte inferior para crear torque estabilizador
        Vector3 gravityPoint = transform.position - transform.up * centerOfBuoyancy;
        rb.AddForceAtPosition(-buoyancyForce * 0.5f, gravityPoint, ForceMode.Force);
    }
    
    private void LimitTilt()
    {
        Vector3 currentRotation = transform.eulerAngles;
        
        // Convertir ángulos a rango -180 a 180
        float pitch = currentRotation.x > 180 ? currentRotation.x - 360 : currentRotation.x;
        float roll = currentRotation.z > 180 ? currentRotation.z - 360 : currentRotation.z;
        
        // Limitar los ángulos
        bool limited = false;
        if (Mathf.Abs(pitch) > maxTiltAngle)
        {
            pitch = Mathf.Sign(pitch) * maxTiltAngle;
            limited = true;
        }
        
        if (Mathf.Abs(roll) > maxTiltAngle)
        {
            roll = Mathf.Sign(roll) * maxTiltAngle;
            limited = true;
        }
        
        if (limited)
        {
            // Aplicar la rotación limitada
            transform.rotation = Quaternion.Euler(pitch, currentRotation.y, roll);
            
            // Reducir velocidad angular para evitar rebotes
            rb.angularVelocity *= 0.8f;
        }
    }
    
    public void OnCollisionEnter(Collision collision)
    {
        // Resetear el timer de estabilización
        timeSinceLastCollision = 0f;
        
        // Calcular la fuerza del impacto
        float impactForce = collision.relativeVelocity.magnitude;
        
        if (impactForce > 2f)
        {
            // Calcular dirección del impacto
            Vector3 impactDirection = collision.contacts[0].normal;
            Vector3 impactPoint = collision.contacts[0].point;
            
            // Calcular el torque basado en el punto de impacto
            Vector3 leverArm = impactPoint - rb.worldCenterOfMass;
            Vector3 impactTorque = Vector3.Cross(leverArm, -impactDirection) * impactForce * collisionTiltMultiplier;
            
            // Añadir algo de giro aleatorio para hacer más dramático
            Vector3 randomSpin = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ) * impactForce * collisionSpinMultiplier;
            
            // Aplicar el torque
            rb.AddTorque(impactTorque + randomSpin, ForceMode.Impulse);
            
            // Notificar el impacto
            OnImpact?.Invoke(impactForce);
            
            if (showDebugInfo)
            {
                Debug.Log($"[STABILIZER] Impact! Force: {impactForce:F1}, Torque: {impactTorque.magnitude:F1}");
            }
        }
    }
    
    // Método público para aplicar fuerzas externas (explosiones, etc)
    public void ApplyExternalForce(Vector3 force, Vector3 position)
    {
        timeSinceLastCollision = 0f;
        rb.AddForceAtPosition(force, position, ForceMode.Impulse);
    }
    
    // Método para obtener el ángulo de inclinación actual
    public float GetCurrentTilt()
    {
        return Vector3.Angle(transform.up, Vector3.up);
    }
    
    // Método para obtener si el submarino está estable
    public bool IsStable()
    {
        return isStabilizing && GetCurrentTilt() < 5f;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // Mostrar centro de masa
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.TransformDirection(rb.centerOfMass), 0.2f);
        
        // Mostrar punto de flotación
        Gizmos.color = Color.cyan;
        Vector3 buoyancyPoint = transform.position + transform.up * centerOfBuoyancy;
        Gizmos.DrawWireSphere(buoyancyPoint, 0.15f);
        
        // Mostrar dirección "arriba" actual vs objetivo
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.up * 2f);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Vector3.up * 2f);
    }
}