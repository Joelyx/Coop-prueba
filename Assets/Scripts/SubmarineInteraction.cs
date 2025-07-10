using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SubmarineInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private bool blockPlayerEntry = true;
    [SerializeField] private float pushForce = 10f;
    
    private Rigidbody submarineRb;
    private SubmarineController submarineController;
    private Collider submarineCollider;
    
    void Start()
    {
        submarineRb = GetComponent<Rigidbody>();
        submarineController = GetComponent<SubmarineController>();
        submarineCollider = GetComponent<Collider>();
        
        // Asegurarse de que el submarino tenga las propiedades correctas de física
        if (submarineRb != null)
        {
            submarineRb.constraints = RigidbodyConstraints.None;
            submarineRb.useGravity = true;
            submarineRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        // Asegurarse de que el collider no sea trigger
        if (submarineCollider != null)
        {
            submarineCollider.isTrigger = false;
        }
        
        // Configurar la capa del submarino si es necesario
        gameObject.layer = LayerMask.NameToLayer("Default");
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Detectar si es el jugador
        if (collision.gameObject.CompareTag("Player"))
        {
            if (blockPlayerEntry)
            {
                // Empujar al jugador lejos del submarino
                Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    // Calcular el punto de contacto promedio
                    Vector3 contactPoint = Vector3.zero;
                    foreach (ContactPoint contact in collision.contacts)
                    {
                        contactPoint += contact.point;
                    }
                    contactPoint /= collision.contacts.Length;
                    
                    // Calcular dirección de empuje desde el punto de contacto
                    Vector3 pushDirection = (collision.transform.position - contactPoint).normalized;
                    pushDirection.y = Mathf.Max(pushDirection.y, 0.3f); // Asegurar que empuje hacia arriba
                    
                    playerRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
                    
                    // También añadir velocidad directa para asegurar separación
                    playerRb.linearVelocity = pushDirection * pushForce * 0.5f;
                }
            }
            
            Debug.Log($"Player collided with submarine at {collision.contacts.Length} contact points!");
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        // Mantener al jugador fuera mientras esté en contacto
        if (collision.gameObject.CompareTag("Player") && blockPlayerEntry)
        {
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                // Calcular el punto de contacto promedio
                Vector3 contactPoint = Vector3.zero;
                foreach (ContactPoint contact in collision.contacts)
                {
                    contactPoint += contact.point;
                }
                contactPoint /= collision.contacts.Length;
                
                Vector3 pushDirection = (collision.transform.position - contactPoint).normalized;
                pushDirection.y = Mathf.Max(pushDirection.y, 0.2f);
                
                // Aplicar fuerza continua pero menor
                playerRb.AddForce(pushDirection * (pushForce * 0.5f), ForceMode.Force);
            }
        }
    }
    
    void FixedUpdate()
    {
        // Asegurar que el Rigidbody del submarino mantenga sus propiedades
        if (submarineRb != null)
        {
            // Prevenir que el submarino sea empujado fácilmente por el jugador
            if (submarineRb.mass < 5000f)
            {
                submarineRb.mass = 10000f;
            }
        }
    }
    
    // Método para verificar si las colisiones están funcionando
    void OnDrawGizmos()
    {
        if (Application.isPlaying && submarineCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(submarineCollider.bounds.center, submarineCollider.bounds.size);
        }
    }
}
