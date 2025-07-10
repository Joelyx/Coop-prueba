using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema que transmite el feedback de colisiones del submarino a los jugadores
/// </summary>
[RequireComponent(typeof(SubmarineController))]
[RequireComponent(typeof(SubmarineStabilizer))]
public class SubmarineCollisionFeedback : MonoBehaviour
{
    [Header("Impact Settings")]
    [SerializeField] private float minImpactForce = 5f;
    [SerializeField] private float maxImpactForce = 50f;
    [SerializeField] private AnimationCurve impactCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Player Feedback")]
    [SerializeField] private float playerForceMultiplier = 10f;
    [SerializeField] private float playerVerticalForce = 5f;
    [SerializeField] private float disorientationDuration = 2f;
    
    [Header("Camera Shake")]
    [SerializeField] private float shakeIntensity = 0.5f;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject sparksPrefab;
    [SerializeField] private GameObject emergencyLightsPrefab;
    [SerializeField] private float lightFlashDuration = 3f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip[] impactSounds;
    [SerializeField] private AudioClip alarmSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] [Range(0, 1)] private float impactVolume = 0.8f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private SubmarineController submarineController;
    private SubmarineStabilizer stabilizer;
    private SubmarineInterior interior;
    
    // Estado del sistema
    private float currentShakeTime = 0f;
    private bool isShaking = false;
    private List<Light> emergencyLights = new List<Light>();
    private float emergencyLightTimer = 0f;
    
    // Eventos
    public System.Action<float> OnSubmarineImpact;
    public System.Action<Vector3> OnImpactDirection;
    
    private void Awake()
    {
        submarineController = GetComponent<SubmarineController>();
        stabilizer = GetComponent<SubmarineStabilizer>();
        interior = GetComponentInChildren<SubmarineInterior>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    private void Start()
    {
        // Suscribirse a eventos del stabilizer
        if (stabilizer != null)
        {
            stabilizer.OnImpact += HandleImpact;
            stabilizer.OnTiltChanged += HandleTiltChange;
        }
        
        // Crear luces de emergencia si no existen
        if (emergencyLightsPrefab == null)
        {
            CreateDefaultEmergencyLights();
        }
    }
    
    private void Update()
    {
        // Actualizar shake de cámara
        if (isShaking)
        {
            UpdateCameraShake();
        }
        
        // Actualizar luces de emergencia
        if (emergencyLightTimer > 0)
        {
            UpdateEmergencyLights();
        }
    }
    
    private void HandleImpact(float impactForce)
    {
        if (impactForce < minImpactForce) return;
        
        // Normalizar la fuerza del impacto
        float normalizedForce = Mathf.Clamp01((impactForce - minImpactForce) / (maxImpactForce - minImpactForce));
        float adjustedForce = impactCurve.Evaluate(normalizedForce);
        
        if (debugMode)
            Debug.Log($"[COLLISION FEEDBACK] Impact detected! Raw: {impactForce:F1}, Normalized: {normalizedForce:F2}, Adjusted: {adjustedForce:F2}");
        
        // Aplicar efectos a los jugadores
        ApplyPlayerFeedback(adjustedForce);
        
        // Efectos visuales
        TriggerVisualEffects(adjustedForce);
        
        // Efectos de audio
        PlayImpactSound(adjustedForce);
        
        // Iniciar shake de cámara
        StartCameraShake(adjustedForce);
        
        // Notificar el evento
        OnSubmarineImpact?.Invoke(adjustedForce);
    }
    
    private void HandleTiltChange(float tiltAngle)
    {
        // Aplicar efectos sutiles basados en la inclinación
        if (interior != null && tiltAngle > 10f)
        {
            // Los jugadores podrían deslizarse ligeramente cuando el submarino está inclinado
            foreach (var player in interior.GetPlayersInside())
            {
                if (player != null)
                {
                    Vector3 slideDirection = Vector3.ProjectOnPlane(-transform.up, Vector3.up).normalized;
                    float slideForce = Mathf.Sin(tiltAngle * Mathf.Deg2Rad) * 2f;
                    
                    Rigidbody playerRb = player.GetComponent<Rigidbody>();
                    if (playerRb != null)
                    {
                        playerRb.AddForce(slideDirection * slideForce, ForceMode.Force);
                    }
                }
            }
        }
    }
    
    private void ApplyPlayerFeedback(float impactStrength)
    {
        if (interior == null) return;
        
        foreach (var player in interior.GetPlayersInside())
        {
            if (player == null) continue;
            
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb == null) continue;
            
            // Calcular dirección del impacto
            Vector3 impactDirection = Random.insideUnitSphere;
            impactDirection.y = Mathf.Abs(impactDirection.y); // Siempre algo de fuerza hacia arriba
            impactDirection.Normalize();
            
            // Aplicar fuerza al jugador
            float playerForce = impactStrength * playerForceMultiplier;
            Vector3 force = impactDirection * playerForce;
            force.y += playerVerticalForce * impactStrength; // Fuerza vertical adicional
            
            playerRb.AddForce(force, ForceMode.Impulse);
            
            // Aplicar desorientación (rotación aleatoria)
            if (impactStrength > 0.5f)
            {
                Vector3 randomTorque = Random.insideUnitSphere * impactStrength * 100f;
                playerRb.AddTorque(randomTorque, ForceMode.Impulse);
            }
            
            // Notificar al sistema de cámara del jugador si existe
            var cameraShake = player.GetComponentInChildren<CameraShakeEffect>();
            if (cameraShake != null)
            {
                cameraShake.TriggerShake(impactStrength * shakeIntensity, shakeDuration);
            }
            
            if (debugMode)
                Debug.Log($"[COLLISION FEEDBACK] Applied force to player: {force.magnitude:F1}");
        }
    }
    
    private void TriggerVisualEffects(float impactStrength)
    {
        // Generar chispas en puntos aleatorios del interior
        if (sparksPrefab != null && impactStrength > 0.3f)
        {
            int sparkCount = Mathf.RoundToInt(impactStrength * 3);
            for (int i = 0; i < sparkCount; i++)
            {
                Vector3 sparkPosition = transform.position + Random.insideUnitSphere * 3f;
                sparkPosition.y = transform.position.y + Random.Range(0f, 2f);
                
                GameObject sparks = Instantiate(sparksPrefab, sparkPosition, Random.rotation);
                Destroy(sparks, 3f);
            }
        }
        
        // Activar luces de emergencia
        if (impactStrength > 0.5f)
        {
            emergencyLightTimer = lightFlashDuration * impactStrength;
        }
    }
    
    private void PlayImpactSound(float impactStrength)
    {
        if (audioSource == null || impactSounds == null || impactSounds.Length == 0) return;
        
        // Seleccionar sonido basado en la intensidad
        int soundIndex = Mathf.Min(Mathf.FloorToInt(impactStrength * impactSounds.Length), impactSounds.Length - 1);
        AudioClip impactClip = impactSounds[soundIndex];
        
        if (impactClip != null)
        {
            audioSource.PlayOneShot(impactClip, impactVolume * impactStrength);
        }
        
        // Reproducir alarma si el impacto es muy fuerte
        if (impactStrength > 0.7f && alarmSound != null)
        {
            audioSource.PlayOneShot(alarmSound, impactVolume * 0.5f);
        }
    }
    
    private void StartCameraShake(float intensity)
    {
        isShaking = true;
        currentShakeTime = 0f;
    }
    
    private void UpdateCameraShake()
    {
        currentShakeTime += Time.deltaTime;
        
        if (currentShakeTime >= shakeDuration)
        {
            isShaking = false;
            return;
        }
        
        float shakeProgress = currentShakeTime / shakeDuration;
        float currentIntensity = shakeCurve.Evaluate(shakeProgress) * shakeIntensity;
        
        // Aplicar shake al submarino completo para que afecte a todos los jugadores
        Vector3 shakeOffset = Random.insideUnitSphere * currentIntensity;
        transform.position += shakeOffset * Time.deltaTime;
    }
    
    private void UpdateEmergencyLights()
    {
        emergencyLightTimer -= Time.deltaTime;
        
        // Parpadeo de luces
        float flashSpeed = 8f;
        bool lightsOn = Mathf.Sin(Time.time * flashSpeed) > 0;
        
        foreach (var light in emergencyLights)
        {
            if (light != null)
                light.enabled = lightsOn;
        }
        
        if (emergencyLightTimer <= 0)
        {
            // Apagar todas las luces de emergencia
            foreach (var light in emergencyLights)
            {
                if (light != null)
                    light.enabled = false;
            }
        }
    }
    
    private void CreateDefaultEmergencyLights()
    {
        // Crear luces de emergencia rojas básicas
        for (int i = 0; i < 4; i++)
        {
            GameObject lightObj = new GameObject($"EmergencyLight_{i}");
            lightObj.transform.parent = transform;
            
            float angle = i * 90f * Mathf.Deg2Rad;
            lightObj.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * 2f,
                1.5f,
                Mathf.Sin(angle) * 4f
            );
            
            Light emergencyLight = lightObj.AddComponent<Light>();
            emergencyLight.color = Color.red;
            emergencyLight.intensity = 2f;
            emergencyLight.range = 5f;
            emergencyLight.enabled = false;
            
            emergencyLights.Add(emergencyLight);
        }
    }
    
    private void OnDestroy()
    {
        // Desuscribirse de eventos
        if (stabilizer != null)
        {
            stabilizer.OnImpact -= HandleImpact;
            stabilizer.OnTiltChanged -= HandleTiltChange;
        }
    }
    
    // Método público para simular impacto (útil para testing)
    public void SimulateImpact(float force)
    {
        HandleImpact(force);
    }
}