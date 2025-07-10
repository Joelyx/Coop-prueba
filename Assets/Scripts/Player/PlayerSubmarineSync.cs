using UnityEngine;

/// <summary>
/// Maneja la sincronización del jugador con el submarino cuando está dentro
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerSubmarineSync : MonoBehaviour
{
    [Header("Submarine Sync")]
    public bool isInsideSubmarine = false;
    public SubmarineInterior currentSubmarine;
    
    [Header("Sync Settings")]
    public float submarineInfluence = 0.8f;
    public bool maintainRelativePosition = true;
    public bool debugSync = false;
    
    private PlayerMovement playerMovement;
    private Rigidbody playerRb;
    private Vector3 submarineBaseVelocity = Vector3.zero;
    private Vector3 lastSubmarinePosition = Vector3.zero;
    
    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerRb = GetComponent<Rigidbody>();
    }
    
    private void Start()
    {
        // Suscribirse a eventos de PlayerMovement si existen
        if (playerMovement != null)
        {
            // Aquí podrías suscribirte a eventos de movimiento si los hay
        }
    }
    
    private void FixedUpdate()
    {
        if (isInsideSubmarine && currentSubmarine != null)
        {
            ApplySubmarineMovement();
        }
    }
    
    private void ApplySubmarineMovement()
    {
        if (currentSubmarine.submarineTransform == null) return;
        
        // Obtener la velocidad del submarino
        Rigidbody submarineRb = currentSubmarine.submarineTransform.GetComponent<Rigidbody>();
        Vector3 currentSubmarineVelocity = Vector3.zero;
        
        if (submarineRb != null)
        {
            currentSubmarineVelocity = submarineRb.linearVelocity;
        }
        else
        {
            // Calcular velocidad basada en cambio de posición
            Vector3 deltaPos = currentSubmarine.submarineTransform.position - lastSubmarinePosition;
            currentSubmarineVelocity = deltaPos / Time.fixedDeltaTime;
        }
        
        // Calcular la velocidad relativa del jugador
        Vector3 playerWorldVelocity = playerRb.linearVelocity;
        Vector3 playerRelativeVelocity = playerWorldVelocity - submarineBaseVelocity;
        
        // La nueva velocidad del jugador es: velocidad del submarino + movimiento relativo
        Vector3 targetVelocity = currentSubmarineVelocity + (playerRelativeVelocity * (1f - submarineInfluence));
        
        // Aplicar la corrección de velocidad gradualmente
        Vector3 velocityDifference = targetVelocity - playerWorldVelocity;
        playerRb.AddForce(velocityDifference * submarineInfluence, ForceMode.Acceleration);
        
        // Actualizar la velocidad base del submarino para el próximo frame
        submarineBaseVelocity = currentSubmarineVelocity;
        lastSubmarinePosition = currentSubmarine.submarineTransform.position;
        
        if (debugSync && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[PLAYER SUBMARINE SYNC] Sub vel: {currentSubmarineVelocity.magnitude:F2}, " +
                     $"Player vel: {playerWorldVelocity.magnitude:F2}, " +
                     $"Target vel: {targetVelocity.magnitude:F2}");
        }
    }
    
    public void EnterSubmarine(SubmarineInterior submarine)
    {
        if (submarine == null) return;
        
        isInsideSubmarine = true;
        currentSubmarine = submarine;
        
        // Inicializar velocidades
        if (submarine.submarineTransform != null)
        {
            Rigidbody submarineRb = submarine.submarineTransform.GetComponent<Rigidbody>();
            if (submarineRb != null)
            {
                submarineBaseVelocity = submarineRb.linearVelocity;
            }
            lastSubmarinePosition = submarine.submarineTransform.position;
        }
        
        if (debugSync)
            Debug.Log($"[PLAYER SUBMARINE SYNC] Player {name} entered submarine");
    }
    
    public void ExitSubmarine()
    {
        if (debugSync && isInsideSubmarine)
            Debug.Log($"[PLAYER SUBMARINE SYNC] Player {name} exited submarine");
            
        isInsideSubmarine = false;
        currentSubmarine = null;
        submarineBaseVelocity = Vector3.zero;
        lastSubmarinePosition = Vector3.zero;
    }
    
    // Método para ajustar la influencia del submarino en runtime
    public void SetSubmarineInfluence(float influence)
    {
        submarineInfluence = Mathf.Clamp01(influence);
    }
    
    // Método para obtener la velocidad relativa del jugador dentro del submarino
    public Vector3 GetRelativeVelocity()
    {
        if (!isInsideSubmarine || currentSubmarine == null)
            return playerRb.linearVelocity;
            
        return playerRb.linearVelocity - submarineBaseVelocity;
    }
}