using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Gestiona los biomas activos y sus efectos en el juego
/// </summary>
public class BiomeManager : MonoBehaviour
{
    [Header("Configuración")]
    public BiomeSettings biomeSettings;
    public TerrainGenerator terrainGenerator;
    
    [Header("Efectos Globales")]
    public Light globalLight;
    public Camera mainCamera;
    public AudioSource ambientAudioSource;
    
    [Header("Transiciones")]
    public float transitionSpeed = 2f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    public bool showCurrentBiome = true;
    public bool enableBiomeEffects = true;
    
    // Estado actual
    private BiomeSettings.BiomeData currentBiome;
    private BiomeSettings.BiomeData targetBiome;
    private float transitionProgress = 0f;
    private bool isTransitioning = false;
    
    // Cache de efectos por bioma
    private Dictionary<string, BiomeEffects> biomeEffectsCache = new Dictionary<string, BiomeEffects>();
    
    // Seguimiento de posición
    private Transform player;
    private Vector3 lastPlayerPosition;
    private float biomeCheckInterval = 1f;
    private float lastBiomeCheck = 0f;
    
    // Eventos
    public System.Action<BiomeSettings.BiomeData> OnBiomeChanged;
    public System.Action<BiomeSettings.BiomeData, BiomeSettings.BiomeData> OnBiomeTransitionStarted;
    public System.Action OnBiomeTransitionCompleted;
    
    [System.Serializable]
    private class BiomeEffects
    {
        public Color lightColor = Color.white;
        public float lightIntensity = 1f;
        public Color fogColor = Color.gray;
        public float fogDensity = 0.01f;
        public AudioClip ambientSound;
        public Color cameraFilter = Color.white;
        public float cameraFilterStrength = 0f;
    }
    
    private void Start()
    {
        // Encontrar el jugador
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        
        // Inicializar efectos de biomas
        InitializeBiomeEffects();
        
        // Establecer bioma inicial
        if (player != null)
        {
            UpdateCurrentBiome(player.position);
        }
        
        Debug.Log("[BiomeManager] Inicializado");
    }
    
    private void Update()
    {
        if (player == null) return;
        
        // Verificar cambio de bioma periódicamente
        if (Time.time - lastBiomeCheck >= biomeCheckInterval)
        {
            CheckForBiomeChange();
            lastBiomeCheck = Time.time;
        }
        
        // Actualizar transición si está activa
        if (isTransitioning)
        {
            UpdateBiomeTransition();
        }
    }
    
    private void InitializeBiomeEffects()
    {
        if (biomeSettings == null) return;
        
        foreach (var biome in biomeSettings.biomes)
        {
            BiomeEffects effects = new BiomeEffects();
            
            // Configurar efectos basados en el tipo de bioma
            switch (biome.biomeName.ToLower())
            {
                case "llanura abisal":
                    effects.lightColor = new Color(0.2f, 0.3f, 0.8f); // Azul profundo
                    effects.lightIntensity = 0.4f;
                    effects.fogColor = new Color(0.1f, 0.2f, 0.4f);
                    effects.fogDensity = 0.02f;
                    effects.cameraFilter = new Color(0.8f, 0.9f, 1f);
                    effects.cameraFilterStrength = 0.1f;
                    break;
                    
                case "formación rocosa":
                    effects.lightColor = new Color(0.6f, 0.5f, 0.4f); // Marrón rocoso
                    effects.lightIntensity = 0.6f;
                    effects.fogColor = new Color(0.3f, 0.2f, 0.1f);
                    effects.fogDensity = 0.03f;
                    effects.cameraFilter = new Color(1f, 0.9f, 0.8f);
                    effects.cameraFilterStrength = 0.15f;
                    break;
                    
                case "cañón submarino":
                    effects.lightColor = new Color(0.1f, 0.1f, 0.2f); // Azul muy oscuro
                    effects.lightIntensity = 0.2f;
                    effects.fogColor = new Color(0.05f, 0.05f, 0.1f);
                    effects.fogDensity = 0.05f;
                    effects.cameraFilter = new Color(0.7f, 0.7f, 0.9f);
                    effects.cameraFilterStrength = 0.25f;
                    break;
                    
                case "arrecife coralino":
                    effects.lightColor = new Color(0.9f, 0.4f, 0.6f); // Rosa coralino
                    effects.lightIntensity = 0.8f;
                    effects.fogColor = new Color(0.4f, 0.2f, 0.3f);
                    effects.fogDensity = 0.015f;
                    effects.cameraFilter = new Color(1f, 0.8f, 0.9f);
                    effects.cameraFilterStrength = 0.1f;
                    break;
                    
                default:
                    effects.lightColor = Color.white;
                    effects.lightIntensity = 1f;
                    effects.fogColor = Color.gray;
                    effects.fogDensity = 0.01f;
                    effects.cameraFilter = Color.white;
                    effects.cameraFilterStrength = 0f;
                    break;
            }
            
            biomeEffectsCache[biome.biomeName] = effects;
        }
        
        Debug.Log($"[BiomeManager] Inicializados efectos para {biomeEffectsCache.Count} biomas");
    }
    
    private void CheckForBiomeChange()
    {
        if (Vector3.Distance(player.position, lastPlayerPosition) < 5f)
            return; // No se ha movido lo suficiente
        
        lastPlayerPosition = player.position;
        
        // Obtener bioma en la posición actual
        int biomeIndex = GetBiomeAtPosition(player.position);
        
        if (biomeIndex >= 0 && biomeIndex < biomeSettings.biomes.Length)
        {
            BiomeSettings.BiomeData newBiome = biomeSettings.biomes[biomeIndex];
            
            // Verificar si es diferente al bioma actual
            if (currentBiome.biomeName != newBiome.biomeName)
            {
                StartBiomeTransition(newBiome);
            }
        }
    }
    
    private int GetBiomeAtPosition(Vector3 worldPosition)
    {
        if (terrainGenerator == null) return 0;
        
        int x = Mathf.RoundToInt(worldPosition.x);
        int z = Mathf.RoundToInt(worldPosition.z);
        
        return terrainGenerator.GetBiomeAtWorldPosition(x, z);
    }
    
    private void UpdateCurrentBiome(Vector3 position)
    {
        int biomeIndex = GetBiomeAtPosition(position);
        
        if (biomeIndex >= 0 && biomeIndex < biomeSettings.biomes.Length)
        {
            currentBiome = biomeSettings.biomes[biomeIndex];
            ApplyBiomeEffects(currentBiome, 1f);
            
            if (showCurrentBiome)
            {
                Debug.Log($"[BiomeManager] Bioma actual: {currentBiome.biomeName}");
            }
        }
    }
    
    private void StartBiomeTransition(BiomeSettings.BiomeData newBiome)
    {
        if (isTransitioning) return;
        
        targetBiome = newBiome;
        transitionProgress = 0f;
        isTransitioning = true;
        
        OnBiomeTransitionStarted?.Invoke(currentBiome, targetBiome);
        
        if (showCurrentBiome)
        {
            Debug.Log($"[BiomeManager] Transición: {currentBiome.biomeName} -> {targetBiome.biomeName}");
        }
    }
    
    private void UpdateBiomeTransition()
    {
        transitionProgress += transitionSpeed * Time.deltaTime;
        
        if (transitionProgress >= 1f)
        {
            // Transición completada
            transitionProgress = 1f;
            currentBiome = targetBiome;
            isTransitioning = false;
            
            OnBiomeChanged?.Invoke(currentBiome);
            OnBiomeTransitionCompleted?.Invoke();
            
            if (showCurrentBiome)
            {
                Debug.Log($"[BiomeManager] Transición completada a: {currentBiome.biomeName}");
            }
        }
        
        // Aplicar efectos interpolados
        float curvedProgress = transitionCurve.Evaluate(transitionProgress);
        ApplyBiomeTransition(currentBiome, targetBiome, curvedProgress);
    }
    
    private void ApplyBiomeTransition(BiomeSettings.BiomeData fromBiome, BiomeSettings.BiomeData toBiome, float t)
    {
        if (!enableBiomeEffects) return;
        
        // Obtener efectos de ambos biomas
        BiomeEffects fromEffects = GetBiomeEffects(fromBiome.biomeName);
        BiomeEffects toEffects = GetBiomeEffects(toBiome.biomeName);
        
        // Interpolar iluminación
        if (globalLight != null)
        {
            globalLight.color = Color.Lerp(fromEffects.lightColor, toEffects.lightColor, t);
            globalLight.intensity = Mathf.Lerp(fromEffects.lightIntensity, toEffects.lightIntensity, t);
        }
        
        // Interpolar niebla
        RenderSettings.fogColor = Color.Lerp(fromEffects.fogColor, toEffects.fogColor, t);
        RenderSettings.fogDensity = Mathf.Lerp(fromEffects.fogDensity, toEffects.fogDensity, t);
        
        // Aplicar filtro de cámara
        ApplyCameraFilter(
            Color.Lerp(fromEffects.cameraFilter, toEffects.cameraFilter, t),
            Mathf.Lerp(fromEffects.cameraFilterStrength, toEffects.cameraFilterStrength, t)
        );
    }
    
    private void ApplyBiomeEffects(BiomeSettings.BiomeData biome, float intensity = 1f)
    {
        if (!enableBiomeEffects) return;
        
        BiomeEffects effects = GetBiomeEffects(biome.biomeName);
        
        // Aplicar iluminación
        if (globalLight != null)
        {
            globalLight.color = Color.Lerp(Color.white, effects.lightColor, intensity);
            globalLight.intensity = Mathf.Lerp(1f, effects.lightIntensity, intensity);
        }
        
        // Aplicar niebla
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.Lerp(Color.gray, effects.fogColor, intensity);
        RenderSettings.fogDensity = Mathf.Lerp(0.01f, effects.fogDensity, intensity);
        
        // Aplicar filtro de cámara
        ApplyCameraFilter(effects.cameraFilter, effects.cameraFilterStrength * intensity);
        
        // Reproducir sonido ambiental
        if (effects.ambientSound != null && ambientAudioSource != null)
        {
            if (ambientAudioSource.clip != effects.ambientSound)
            {
                ambientAudioSource.clip = effects.ambientSound;
                ambientAudioSource.loop = true;
                ambientAudioSource.volume = 0.3f * intensity;
                ambientAudioSource.Play();
            }
        }
    }
    
    private BiomeEffects GetBiomeEffects(string biomeName)
    {
        if (biomeEffectsCache.TryGetValue(biomeName, out BiomeEffects effects))
        {
            return effects;
        }
        
        // Efectos por defecto si no se encuentran
        return new BiomeEffects();
    }
    
    private void ApplyCameraFilter(Color filterColor, float strength)
    {
        if (mainCamera == null) return;
        
        // Aplicar tinte de color a la cámara usando un componente de post-procesamiento
        // Esto requeriría un shader o componente de post-procesamiento específico
        // Por ahora, aplicamos el color al fondo de la cámara
        Color backgroundColor = mainCamera.backgroundColor;
        mainCamera.backgroundColor = Color.Lerp(backgroundColor, filterColor, strength);
    }
    
    // Método público para cambio manual de bioma
    public void OnBiomeEntered(BiomeSettings.BiomeData biomeData)
    {
        if (currentBiome.biomeName != biomeData.biomeName)
        {
            StartBiomeTransition(biomeData);
        }
    }
    
    // Métodos públicos de utilidad
    public BiomeSettings.BiomeData GetCurrentBiome()
    {
        return currentBiome;
    }
    
    public bool IsInBiome(string biomeName)
    {
        return currentBiome.biomeName.Equals(biomeName, System.StringComparison.OrdinalIgnoreCase);
    }
    
    public float GetBiomeRoughness()
    {
        return currentBiome.roughnessFactor;
    }
    
    public bool IsCurrentBiomeNavigable()
    {
        return currentBiome.isNavigable;
    }
    
    // Métodos de configuración
    public void SetBiomeEffectsEnabled(bool enabled)
    {
        enableBiomeEffects = enabled;
        
        if (!enabled)
        {
            // Restaurar configuración por defecto
            if (globalLight != null)
            {
                globalLight.color = Color.white;
                globalLight.intensity = 1f;
            }
            
            RenderSettings.fog = false;
            
            if (ambientAudioSource != null)
            {
                ambientAudioSource.Stop();
            }
        }
    }
    
    public void SetTransitionSpeed(float speed)
    {
        transitionSpeed = Mathf.Max(0.1f, speed);
    }
    
    // Debug y herramientas
    [ContextMenu("Force Biome Check")]
    private void ForceBiomeCheck()
    {
        if (player != null)
        {
            CheckForBiomeChange();
        }
    }
    
    [ContextMenu("Print Current Biome")]
    private void PrintCurrentBiome()
    {
        Debug.Log($"[BiomeManager] Bioma actual: {currentBiome.biomeName}");
        Debug.Log($"- Navegable: {currentBiome.isNavigable}");
        Debug.Log($"- Rugosidad: {currentBiome.roughnessFactor}");
        Debug.Log($"- Color: {currentBiome.biomeColor}");
    }
    
    private void OnDrawGizmos()
    {
        if (!showCurrentBiome || player == null) return;
        
        // Verificar que currentBiome está inicializado
        if (string.IsNullOrEmpty(currentBiome.biomeName)) return;
        
        // Dibujar esfera del bioma actual
        Gizmos.color = currentBiome.biomeColor;
        Gizmos.DrawWireSphere(player.position, 10f);
        
        // Mostrar información del bioma
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(player.position + Vector3.up * 15f, 
            $"Bioma: {currentBiome.biomeName}\nNavegable: {currentBiome.isNavigable}");
        #endif
    }
}