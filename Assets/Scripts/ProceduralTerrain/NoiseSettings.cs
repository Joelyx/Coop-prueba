using UnityEngine;

/// <summary>
/// Configuración para generación de ruido Perlin/Simplex
/// </summary>
[CreateAssetMenu(fileName = "NoiseSettings", menuName = "Procedural Terrain/Noise Settings")]
public class NoiseSettings : ScriptableObject
{
    [Header("Ruido Base")]
    [Range(0.1f, 100f)]
    public float scale = 50f;
    
    [Range(1, 8)]
    public int octaves = 4;
    
    [Range(0f, 1f)]
    public float persistence = 0.5f;
    
    [Range(1f, 4f)]
    public float lacunarity = 2f;
    
    [Header("Configuración de Mundo")]
    public int seed = 0;
    public Vector2 offset = Vector2.zero;
    
    [Header("Normalización de Altura")]
    [Range(0f, 1f)]
    public float heightMultiplier = 1f;
    
    public AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
}