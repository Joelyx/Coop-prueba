using UnityEngine;

/// <summary>
/// Solución simple para añadir colisiones al submarino sin complicaciones
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SimpleSubmarineCollider : MonoBehaviour
{
    [Header("Basic Collision Setup")]
    public bool addCollidersOnStart = true;
    public bool debugCollisions = true;
    
    [Header("Collider Settings")]
    public Vector3 mainColliderSize = new Vector3(2f, 1.5f, 8f);
    public Vector3 mainColliderCenter = Vector3.zero;
    
    private Rigidbody rb;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (addCollidersOnStart)
        {
            SetupBasicCollider();
        }
        
        // Asegurar que el Rigidbody esté configurado correctamente para colisiones
        ConfigureRigidbodyForCollisions();
    }
    
    private void SetupBasicCollider()
    {
        // Verificar si ya hay un collider principal
        Collider existingCollider = GetComponent<Collider>();
        
        if (existingCollider == null)
        {
            // Crear un BoxCollider simple y efectivo
            BoxCollider mainCollider = gameObject.AddComponent<BoxCollider>();
            mainCollider.size = mainColliderSize;
            mainCollider.center = mainColliderCenter;
            mainCollider.isTrigger = false; // MUY IMPORTANTE: NO es trigger
            
            if (debugCollisions)
                Debug.Log("[SIMPLE SUBMARINE] BoxCollider añadido al submarino");
        }
        else
        {
            // Si existe, asegurarse de que NO sea trigger
            existingCollider.isTrigger = false;
            
            if (debugCollisions)
                Debug.Log("[SIMPLE SUBMARINE] Collider existente configurado para colisiones físicas");
        }
    }
    
    private void ConfigureRigidbodyForCollisions()
    {
        if (rb != null)
        {
            // Configuración básica para que las colisiones funcionen
            rb.isKinematic = false;
            
            // Asegurar que la detección de colisiones esté activa
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            if (debugCollisions)
                Debug.Log($"[SIMPLE SUBMARINE] Rigidbody configurado - isKinematic: {rb.isKinematic}, CollisionMode: {rb.collisionDetectionMode}");
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (debugCollisions)
        {
            Debug.Log($"[SIMPLE SUBMARINE] COLISIÓN DETECTADA con: {collision.gameObject.name}");
            Debug.Log($"[SIMPLE SUBMARINE] Punto de contacto: {collision.contacts[0].point}");
            Debug.Log($"[SIMPLE SUBMARINE] Fuerza relativa: {collision.relativeVelocity.magnitude}");
        }
    }
    
    private void OnCollisionStay(Collision collision)
    {
        if (debugCollisions && Time.frameCount % 60 == 0) // Log cada segundo aprox
        {
            Debug.Log($"[SIMPLE SUBMARINE] Manteniendo colisión con: {collision.gameObject.name}");
        }
    }
    
    private void OnCollisionExit(Collision collision)
    {
        if (debugCollisions)
        {
            Debug.Log($"[SIMPLE SUBMARINE] Fin de colisión con: {collision.gameObject.name}");
        }
    }
    
    // Método para verificar configuración
    [ContextMenu("Verificar Configuración")]
    public void VerifyConfiguration()
    {
        rb = GetComponent<Rigidbody>();
        Collider col = GetComponent<Collider>();
        
        Debug.Log("=== VERIFICACIÓN DE CONFIGURACIÓN DEL SUBMARINO ===");
        Debug.Log($"Rigidbody presente: {rb != null}");
        if (rb != null)
        {
            Debug.Log($"  - isKinematic: {rb.isKinematic}");
            Debug.Log($"  - mass: {rb.mass}");
            Debug.Log($"  - useGravity: {rb.useGravity}");
            Debug.Log($"  - CollisionDetectionMode: {rb.collisionDetectionMode}");
        }
        
        Debug.Log($"Collider presente: {col != null}");
        if (col != null)
        {
            Debug.Log($"  - isTrigger: {col.isTrigger}");
            Debug.Log($"  - enabled: {col.enabled}");
            Debug.Log($"  - Tipo: {col.GetType().Name}");
        }
        
        Debug.Log($"GameObject activo: {gameObject.activeInHierarchy}");
        Debug.Log($"Layer: {LayerMask.LayerToName(gameObject.layer)}");
        Debug.Log("================================================");
    }
}