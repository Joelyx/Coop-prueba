using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlayerCollisionHandler : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private float submarineRepelForce = 8f;
    [SerializeField] private float minSeparationDistance = 1f;
    
    private Rigidbody playerRb;
    private Collider playerCollider;
    
    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        
        // Asegurar configuración correcta del Rigidbody
        if (playerRb != null)
        {
            playerRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            playerRb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        
        // Asegurar que el collider no sea trigger
        if (playerCollider != null)
        {
            playerCollider.isTrigger = false;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Manejar colisión con submarino
        if (collision.gameObject.CompareTag("Submarine"))
        {
            HandleSubmarineCollision(collision);
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        // Continuar manejando la colisión mientras esté en contacto
        if (collision.gameObject.CompareTag("Submarine"))
        {
            HandleSubmarineCollision(collision);
        }
    }
    
    private void HandleSubmarineCollision(Collision collision)
    {
        // Calcular dirección de separación
        Vector3 separationDirection = Vector3.zero;
        
        // Usar los puntos de contacto para determinar la mejor dirección de separación
        foreach (ContactPoint contact in collision.contacts)
        {
            separationDirection += contact.normal;
        }
        
        if (separationDirection != Vector3.zero)
        {
            separationDirection.Normalize();
            
            // Asegurar que haya un componente vertical para evitar que el jugador se quede atascado
            if (Mathf.Abs(separationDirection.y) < 0.3f)
            {
                separationDirection.y = 0.3f;
                separationDirection.Normalize();
            }
            
            // Aplicar fuerza de separación
            playerRb.AddForce(separationDirection * submarineRepelForce, ForceMode.Impulse);
            
            // Opcional: Reducir temporalmente la velocidad del jugador hacia el submarino
            Vector3 velocityTowardsSubmarine = Vector3.Project(playerRb.linearVelocity, -separationDirection);
            if (Vector3.Dot(playerRb.linearVelocity, -separationDirection) > 0)
            {
                playerRb.linearVelocity -= velocityTowardsSubmarine * 0.8f;
            }
            
            Debug.Log($"Player repelled from submarine. Separation direction: {separationDirection}");
        }
    }
    
    void FixedUpdate()
    {
        // Verificar proximidad al submarino y aplicar fuerza preventiva si está muy cerca
        GameObject submarine = GameObject.FindGameObjectWithTag("Submarine");
        if (submarine != null)
        {
            float distance = Vector3.Distance(transform.position, submarine.transform.position);
            BoxCollider submarineCollider = submarine.GetComponent<BoxCollider>();
            
            if (submarineCollider != null)
            {
                // Calcular la distancia al borde del collider del submarino
                Vector3 closestPoint = submarineCollider.ClosestPoint(transform.position);
                float edgeDistance = Vector3.Distance(transform.position, closestPoint);
                
                // Si está muy cerca del borde, aplicar una fuerza preventiva suave
                if (edgeDistance < minSeparationDistance)
                {
                    Vector3 awayDirection = (transform.position - closestPoint).normalized;
                    float forceMagnitude = (minSeparationDistance - edgeDistance) / minSeparationDistance * submarineRepelForce * 0.3f;
                    playerRb.AddForce(awayDirection * forceMagnitude, ForceMode.Force);
                }
            }
        }
    }
}
