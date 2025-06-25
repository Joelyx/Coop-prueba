using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Herramienta para crear ScriptableObjects de ejemplo para testing
/// </summary>
public static class ScriptableObjectCreator
{
    #if UNITY_EDITOR
    
    [MenuItem("Procedural Terrain/Create Example Assets/Basic Terrain Settings")]
    public static void CreateBasicTerrainSettings()
    {
        // Crear configuración básica de terreno
        TerrainSettings terrainSettings = ScriptableObject.CreateInstance<TerrainSettings>();
        
        terrainSettings.worldWidth = 512;
        terrainSettings.worldLength = 512;
        terrainSettings.heightScale = 30f;
        terrainSettings.chunkSize = 64;
        terrainSettings.renderDistance = 3;
        terrainSettings.navigableHeightThreshold = 0.3f;
        terrainSettings.minPassageWidth = 15f;
        terrainSettings.submarineHeight = 8f;
        terrainSettings.useJobSystem = true;
        terrainSettings.useBurstCompiler = true;
        terrainSettings.useGPUInstancing = true;
        
        // Crear y asignar ruido de terreno
        NoiseSettings terrainNoise = CreateBasicNoiseSettings("TerrainNoise", 50f, 4, 0.5f, 2f);
        terrainSettings.terrainNoise = terrainNoise;
        
        // Crear y asignar ruido de detalle
        NoiseSettings detailNoise = CreateBasicNoiseSettings("DetailNoise", 20f, 3, 0.3f, 2.2f);
        terrainSettings.detailNoise = detailNoise;
        
        // Guardar assets
        AssetDatabase.CreateAsset(terrainNoise, "Assets/ScriptableObjects/Terrain/BasicTerrainNoise.asset");
        AssetDatabase.CreateAsset(detailNoise, "Assets/ScriptableObjects/Terrain/BasicDetailNoise.asset");
        AssetDatabase.CreateAsset(terrainSettings, "Assets/ScriptableObjects/Terrain/BasicTerrainSettings.asset");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[ScriptableObjectCreator] Configuración básica de terreno creada");
        Selection.activeObject = terrainSettings;
    }
    
    [MenuItem("Procedural Terrain/Create Example Assets/Ocean Floor Settings")]
    public static void CreateOceanFloorSettings()
    {
        TerrainSettings oceanSettings = ScriptableObject.CreateInstance<TerrainSettings>();
        
        oceanSettings.worldWidth = 1000;
        oceanSettings.worldLength = 1000;
        oceanSettings.heightScale = 50f;
        oceanSettings.chunkSize = 64;
        oceanSettings.renderDistance = 4;
        oceanSettings.navigableHeightThreshold = 0.4f;
        oceanSettings.minPassageWidth = 20f;
        oceanSettings.submarineHeight = 10f;
        oceanSettings.useJobSystem = true;
        oceanSettings.useBurstCompiler = true;
        oceanSettings.useGPUInstancing = true;
        
        // Ruido para fondo oceánico (más suave)
        NoiseSettings oceanNoise = CreateBasicNoiseSettings("OceanFloorNoise", 80f, 5, 0.4f, 1.8f);
        oceanNoise.heightMultiplier = 0.8f;
        oceanSettings.terrainNoise = oceanNoise;
        
        // Ruido de corrientes submarinas
        NoiseSettings currentNoise = CreateBasicNoiseSettings("CurrentNoise", 30f, 2, 0.2f, 2.5f);
        currentNoise.heightMultiplier = 0.3f;
        oceanSettings.detailNoise = currentNoise;
        
        AssetDatabase.CreateAsset(oceanNoise, "Assets/ScriptableObjects/Terrain/OceanFloorNoise.asset");
        AssetDatabase.CreateAsset(currentNoise, "Assets/ScriptableObjects/Terrain/CurrentNoise.asset");
        AssetDatabase.CreateAsset(oceanSettings, "Assets/ScriptableObjects/Terrain/OceanFloorSettings.asset");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[ScriptableObjectCreator] Configuración de fondo oceánico creada");
        Selection.activeObject = oceanSettings;
    }
    
    [MenuItem("Procedural Terrain/Create Example Assets/Complete Biome Settings")]
    public static void CreateCompleteBiomeSettings()
    {
        BiomeSettings biomeSettings = ScriptableObject.CreateInstance<BiomeSettings>();
        
        // Configurar biomas submarinos
        biomeSettings.biomes = new BiomeSettings.BiomeData[4];
        
        // Llanura Abisal
        biomeSettings.biomes[0] = new BiomeSettings.BiomeData
        {
            biomeName = "Llanura Abisal",
            biomeColor = new Color(0.1f, 0.2f, 0.8f, 1f), // Azul profundo
            minHeight = 0f,
            maxHeight = 0.3f,
            temperature = 0.2f,
            moisture = 0.7f,
            isNavigable = true,
            roughnessFactor = 1f,
            objectDensity = 0.05f,
            objectScale = 1f
        };
        
        // Formación Rocosa
        biomeSettings.biomes[1] = new BiomeSettings.BiomeData
        {
            biomeName = "Formación Rocosa",
            biomeColor = new Color(0.4f, 0.3f, 0.2f, 1f), // Marrón rocoso
            minHeight = 0.3f,
            maxHeight = 0.8f,
            temperature = 0.3f,
            moisture = 0.2f,
            isNavigable = false,
            roughnessFactor = 2.5f,
            objectDensity = 0.2f,
            objectScale = 1.5f
        };
        
        // Cañón Submarino
        biomeSettings.biomes[2] = new BiomeSettings.BiomeData
        {
            biomeName = "Cañón Submarino",
            biomeColor = new Color(0.1f, 0.1f, 0.3f, 1f), // Azul muy oscuro
            minHeight = 0f,
            maxHeight = 0.5f,
            temperature = 0.1f,
            moisture = 0.9f,
            isNavigable = true,
            roughnessFactor = 1.3f,
            objectDensity = 0.1f,
            objectScale = 0.8f
        };
        
        // Arrecife Coralino
        biomeSettings.biomes[3] = new BiomeSettings.BiomeData
        {
            biomeName = "Arrecife Coralino",
            biomeColor = new Color(0.8f, 0.3f, 0.5f, 1f), // Rosa coralino
            minHeight = 0.2f,
            maxHeight = 0.6f,
            temperature = 0.6f,
            moisture = 0.8f,
            isNavigable = true,
            roughnessFactor = 1.8f,
            objectDensity = 0.3f,
            objectScale = 1.2f
        };
        
        biomeSettings.transitionSmoothness = 3f;
        
        // Crear ruido para biomas
        NoiseSettings biomeNoise = CreateBasicNoiseSettings("BiomeNoise", 120f, 2, 0.6f, 1.5f);
        biomeSettings.biomeNoise = biomeNoise;
        
        AssetDatabase.CreateAsset(biomeNoise, "Assets/ScriptableObjects/Biomes/BiomeNoise.asset");
        AssetDatabase.CreateAsset(biomeSettings, "Assets/ScriptableObjects/Biomes/CompleteBiomeSettings.asset");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[ScriptableObjectCreator] Configuración completa de biomas creada");
        Selection.activeObject = biomeSettings;
    }
    
    [MenuItem("Procedural Terrain/Create Example Assets/Performance Test Settings")]
    public static void CreatePerformanceTestSettings()
    {
        // Configuración optimizada para testing de rendimiento
        TerrainSettings perfSettings = ScriptableObject.CreateInstance<TerrainSettings>();
        
        perfSettings.worldWidth = 2048; // Mundo grande
        perfSettings.worldLength = 2048;
        perfSettings.heightScale = 80f;
        perfSettings.chunkSize = 128; // Chunks más grandes
        perfSettings.renderDistance = 2; // Menos chunks activos
        perfSettings.navigableHeightThreshold = 0.35f;
        perfSettings.minPassageWidth = 25f;
        perfSettings.submarineHeight = 12f;
        perfSettings.useJobSystem = true;
        perfSettings.useBurstCompiler = true;
        perfSettings.useGPUInstancing = true;
        
        // Ruido optimizado para rendimiento
        NoiseSettings perfNoise = CreateBasicNoiseSettings("PerformanceNoise", 100f, 3, 0.45f, 2f);
        perfNoise.heightMultiplier = 1f;
        perfSettings.terrainNoise = perfNoise;
        
        AssetDatabase.CreateAsset(perfNoise, "Assets/ScriptableObjects/Terrain/PerformanceNoise.asset");
        AssetDatabase.CreateAsset(perfSettings, "Assets/ScriptableObjects/Terrain/PerformanceTestSettings.asset");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[ScriptableObjectCreator] Configuración de testing de rendimiento creada");
        Selection.activeObject = perfSettings;
    }
    
    [MenuItem("Procedural Terrain/Create Example Assets/All Example Assets")]
    public static void CreateAllExampleAssets()
    {
        CreateBasicTerrainSettings();
        CreateOceanFloorSettings();
        CreateCompleteBiomeSettings();
        CreatePerformanceTestSettings();
        CreateSpecializedNoiseSettings();
        
        Debug.Log("[ScriptableObjectCreator] Todos los assets de ejemplo creados!");
    }
    
    [MenuItem("Procedural Terrain/Create Example Assets/Specialized Noise Settings")]
    public static void CreateSpecializedNoiseSettings()
    {
        // Ruido para montañas (ridged)
        NoiseSettings mountainNoise = ScriptableObject.CreateInstance<NoiseSettings>();
        mountainNoise.scale = 60f;
        mountainNoise.octaves = 6;
        mountainNoise.persistence = 0.6f;
        mountainNoise.lacunarity = 2.2f;
        mountainNoise.heightMultiplier = 1.2f;
        mountainNoise.heightCurve = CreateMountainCurve();
        
        // Ruido para valles
        NoiseSettings valleyNoise = ScriptableObject.CreateInstance<NoiseSettings>();
        valleyNoise.scale = 40f;
        valleyNoise.octaves = 4;
        valleyNoise.persistence = 0.3f;
        valleyNoise.lacunarity = 1.8f;
        valleyNoise.heightMultiplier = 0.6f;
        valleyNoise.heightCurve = CreateValleyCurve();
        
        // Ruido para islas
        NoiseSettings islandNoise = ScriptableObject.CreateInstance<NoiseSettings>();
        islandNoise.scale = 80f;
        islandNoise.octaves = 5;
        islandNoise.persistence = 0.5f;
        islandNoise.lacunarity = 2f;
        islandNoise.heightMultiplier = 0.9f;
        islandNoise.heightCurve = CreateIslandCurve();
        
        AssetDatabase.CreateAsset(mountainNoise, "Assets/ScriptableObjects/Terrain/MountainNoise.asset");
        AssetDatabase.CreateAsset(valleyNoise, "Assets/ScriptableObjects/Terrain/ValleyNoise.asset");
        AssetDatabase.CreateAsset(islandNoise, "Assets/ScriptableObjects/Terrain/IslandNoise.asset");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[ScriptableObjectCreator] Configuraciones especializadas de ruido creadas");
    }
    
    private static NoiseSettings CreateBasicNoiseSettings(string name, float scale, int octaves, float persistence, float lacunarity)
    {
        NoiseSettings noise = ScriptableObject.CreateInstance<NoiseSettings>();
        noise.scale = scale;
        noise.octaves = octaves;
        noise.persistence = persistence;
        noise.lacunarity = lacunarity;
        noise.seed = Random.Range(0, 10000);
        noise.offset = Vector2.zero;
        noise.heightMultiplier = 1f;
        noise.heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        return noise;
    }
    
    private static AnimationCurve CreateMountainCurve()
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0f);
        curve.AddKey(0.3f, 0.1f);
        curve.AddKey(0.6f, 0.4f);
        curve.AddKey(0.8f, 0.7f);
        curve.AddKey(1f, 1f);
        
        // Hacer la curva más pronunciada para montañas
        for (int i = 0; i < curve.length; i++)
        {
            curve.SmoothTangents(i, 0.3f);
        }
        
        return curve;
    }
    
    private static AnimationCurve CreateValleyCurve()
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0f);
        curve.AddKey(0.2f, 0.3f);
        curve.AddKey(0.5f, 0.2f);
        curve.AddKey(0.8f, 0.4f);
        curve.AddKey(1f, 0.6f);
        
        return curve;
    }
    
    private static AnimationCurve CreateIslandCurve()
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0f);
        curve.AddKey(0.1f, 0.05f);
        curve.AddKey(0.4f, 0.8f);
        curve.AddKey(0.7f, 0.9f);
        curve.AddKey(1f, 0.3f);
        
        return curve;
    }
    
    [MenuItem("Procedural Terrain/Create Example Assets/Clear All Example Assets")]
    public static void ClearAllExampleAssets()
    {
        string[] assetPaths = {
            "Assets/ScriptableObjects/Terrain/BasicTerrainNoise.asset",
            "Assets/ScriptableObjects/Terrain/BasicDetailNoise.asset",
            "Assets/ScriptableObjects/Terrain/BasicTerrainSettings.asset",
            "Assets/ScriptableObjects/Terrain/OceanFloorNoise.asset",
            "Assets/ScriptableObjects/Terrain/CurrentNoise.asset",
            "Assets/ScriptableObjects/Terrain/OceanFloorSettings.asset",
            "Assets/ScriptableObjects/Biomes/BiomeNoise.asset",
            "Assets/ScriptableObjects/Biomes/CompleteBiomeSettings.asset",
            "Assets/ScriptableObjects/Terrain/PerformanceNoise.asset",
            "Assets/ScriptableObjects/Terrain/PerformanceTestSettings.asset",
            "Assets/ScriptableObjects/Terrain/MountainNoise.asset",
            "Assets/ScriptableObjects/Terrain/ValleyNoise.asset",
            "Assets/ScriptableObjects/Terrain/IslandNoise.asset"
        };
        
        foreach (string path in assetPaths)
        {
            AssetDatabase.DeleteAsset(path);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[ScriptableObjectCreator] Todos los assets de ejemplo eliminados");
    }
    
    #endif
}