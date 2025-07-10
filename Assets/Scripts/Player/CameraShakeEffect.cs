using UnityEngine;

/// <summary>
/// Efecto de sacudida de cámara para feedback de impactos
/// </summary>
public class CameraShakeEffect : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float maxShakeIntensity = 1f;
    [SerializeField] private float shakeSpeed = 50f;
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Shake Types")]
    [SerializeField] private bool enablePositionShake = true;
    [SerializeField] private bool enableRotationShake = true;
    [SerializeField] private float rotationShakeMultiplier = 0.5f;
    
    [Header("Smoothing")]
    [SerializeField] private float smoothing = 1f;
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float currentShakeTime = 0f;
    private float currentShakeDuration = 0f;
    private float currentShakeIntensity = 0f;
    private bool isShaking = false;
    
    private Vector3 shakePosition;
    private Vector3 shakeRotation;
    
    private void Start()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }
    
    private void LateUpdate()
    {
        if (isShaking)
        {
            UpdateShake();
        }
        else if (transform.localPosition != originalPosition || transform.localRotation != originalRotation)
        {
            // Suavemente volver a la posición original
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * smoothing * 2f);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, originalRotation, Time.deltaTime * smoothing * 2f);
        }
    }
    
    private void UpdateShake()
    {
        currentShakeTime += Time.deltaTime;
        
        if (currentShakeTime >= currentShakeDuration)
        {
            isShaking = false;
            return;
        }
        
        // Calcular intensidad actual basada en la curva
        float progress = currentShakeTime / currentShakeDuration;
        float curveValue = shakeCurve.Evaluate(progress);
        float intensity = currentShakeIntensity * curveValue;
        
        // Generar valores de shake usando Perlin noise para movimiento más suave
        float noiseX = Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f;
        float noiseY = Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f;
        float noiseZ = Mathf.PerlinNoise(Time.time * shakeSpeed, Time.time * shakeSpeed) - 0.5f;
        
        // Aplicar shake de posición
        if (enablePositionShake)
        {
            Vector3 targetShakePosition = new Vector3(noiseX, noiseY, noiseZ * 0.5f) * intensity * maxShakeIntensity;
            shakePosition = Vector3.Lerp(shakePosition, targetShakePosition, Time.deltaTime * smoothing * 10f);
            transform.localPosition = originalPosition + shakePosition;
        }
        
        // Aplicar shake de rotación
        if (enableRotationShake)
        {
            Vector3 targetShakeRotation = new Vector3(
                noiseY * rotationShakeMultiplier,
                noiseX * rotationShakeMultiplier,
                noiseZ * rotationShakeMultiplier * 0.5f
            ) * intensity * maxShakeIntensity * 10f; // Multiplicar por 10 para convertir a grados
            
            shakeRotation = Vector3.Lerp(shakeRotation, targetShakeRotation, Time.deltaTime * smoothing * 10f);
            transform.localRotation = originalRotation * Quaternion.Euler(shakeRotation);
        }
    }
    
    /// <summary>
    /// Inicia el efecto de sacudida
    /// </summary>
    /// <param name="intensity">Intensidad del shake (0-1)</param>
    /// <param name="duration">Duración del shake en segundos</param>
    public void TriggerShake(float intensity, float duration)
    {
        // Si ya hay un shake activo, tomar el mayor
        if (isShaking)
        {
            currentShakeIntensity = Mathf.Max(currentShakeIntensity, intensity);
            currentShakeDuration = Mathf.Max(currentShakeDuration, duration);
            if (currentShakeTime > currentShakeDuration * 0.5f)
            {
                currentShakeTime = 0f; // Reiniciar si estamos en la segunda mitad
            }
        }
        else
        {
            isShaking = true;
            currentShakeTime = 0f;
            currentShakeIntensity = Mathf.Clamp01(intensity);
            currentShakeDuration = duration;
            shakePosition = Vector3.zero;
            shakeRotation = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Detiene inmediatamente el shake
    /// </summary>
    public void StopShake()
    {
        isShaking = false;
        currentShakeTime = 0f;
        shakePosition = Vector3.zero;
        shakeRotation = Vector3.zero;
    }
    
    /// <summary>
    /// Configura la intensidad máxima del shake
    /// </summary>
    public void SetMaxIntensity(float intensity)
    {
        maxShakeIntensity = Mathf.Max(0f, intensity);
    }
    
    /// <summary>
    /// Obtiene si la cámara está actualmente sacudiéndose
    /// </summary>
    public bool IsShaking => isShaking;
    
    /// <summary>
    /// Obtiene el progreso actual del shake (0-1)
    /// </summary>
    public float GetShakeProgress()
    {
        if (!isShaking) return 0f;
        return currentShakeTime / currentShakeDuration;
    }
    
    private void OnDisable()
    {
        StopShake();
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
    }
}