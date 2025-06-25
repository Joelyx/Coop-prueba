using UnityEngine;

/// <summary>
/// Configuración principal del sistema de terreno procedural
/// </summary>
[CreateAssetMenu(fileName = "TerrainSettings", menuName = "Procedural Terrain/Terrain Settings")]
public class TerrainSettings : ScriptableObject
{
    [Header("Dimensiones del Mundo")]
    [Range(100, 2000)]
    public int worldWidth = 1000;
    
    [Range(100, 2000)]
    public int worldLength = 1000;
    
    [Range(1f, 100f)]
    public float heightScale = 50f;
    
    [Header("Configuración de Chunks")]
    [Range(32, 128)]
    public int chunkSize = 64;
    
    [Range(1, 10)]
    public int renderDistance = 3; // Chunks desde el jugador
    
    [Header("Navegabilidad del Submarino")]
    [Range(0f, 1f)]
    public float navigableHeightThreshold = 0.3f; // Por debajo de este valor es navegable
    
    [Range(5f, 50f)]
    public float minPassageWidth = 15f; // Anchura mínima de pasillos navegables
    
    [Range(5f, 30f)]
    public float submarineHeight = 8f; // Altura del submarino para clearance
    
    [Header("Configuración de Ruido")]
    public NoiseSettings terrainNoise;
    public NoiseSettings detailNoise;
    
    [Header("Materiales")]
    public Material seafloorMaterial;
    public Material mountainMaterial;
    public Material transitionMaterial;
    
    [Header("Optimización")]
    public bool useJobSystem = true;
    public bool useBurstCompiler = true;
    public bool useGPUInstancing = true;
}