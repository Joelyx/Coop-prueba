using UnityEngine;

/// <summary>
/// Configuración de biomas para diferentes tipos de terreno submarino
/// </summary>
[CreateAssetMenu(fileName = "BiomeSettings", menuName = "Procedural Terrain/Biome Settings")]
public class BiomeSettings : ScriptableObject
{
    [System.Serializable]
    public class BiomeData
    {
        [Header("Identificación")]
        public string biomeName;
        public Color biomeColor = Color.white;
        
        [Header("Condiciones de Aparición")]
        [Range(0f, 1f)]
        public float minHeight = 0f;
        
        [Range(0f, 1f)]
        public float maxHeight = 1f;
        
        [Range(0f, 1f)]
        public float temperature = 0.5f;
        
        [Range(0f, 1f)]
        public float moisture = 0.5f;
        
        [Header("Características del Terreno")]
        public Material terrainMaterial;
        public Texture2D heightTexture;
        public Texture2D normalTexture;
        
        [Header("Vegetación/Características")]
        public GameObject[] decorativeObjects;
        
        [Range(0f, 1f)]
        public float objectDensity = 0.1f;
        
        [Range(0.5f, 5f)]
        public float objectScale = 1f;
        
        [Header("Navegabilidad")]
        public bool isNavigable = true;
        public float roughnessFactor = 1f;
    }
    
    [Header("Biomas Disponibles")]
    public BiomeData[] biomes = new BiomeData[]
    {
        new BiomeData { biomeName = "Llanura Abisal", biomeColor = Color.blue },
        new BiomeData { biomeName = "Formación Rocosa", biomeColor = Color.gray },
        new BiomeData { biomeName = "Cañón Submarino", biomeColor = Color.red },
        new BiomeData { biomeName = "Arrecife Coralino", biomeColor = Color.magenta }
    };
    
    [Header("Configuración de Transición")]
    [Range(0.1f, 10f)]
    public float transitionSmoothness = 2f;
    
    public NoiseSettings biomeNoise;
}