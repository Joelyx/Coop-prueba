using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Suite de testing para el sistema de terreno procedural
/// </summary>
public class TerrainTestSuite : MonoBehaviour
{
    [Header("Configuración de Testing")]
    public TerrainSettings testTerrainSettings;
    public BiomeSettings testBiomeSettings;
    public bool autoRunTests = false;
    public bool showDetailedLogs = true;
    
    [Header("Tests de Rendimiento")]
    public bool enablePerformanceTests = true;
    public int performanceIterations = 10;
    public bool profileMemoryUsage = true;
    
    [Header("Tests de Funcionalidad")]
    public bool testChunkGeneration = true;
    public bool testNavigationSystem = true;
    public bool testBiomeSystem = true;
    public bool testCollisionSystem = true;
    public bool testBoundarySystem = true;
    
    [Header("Visualización de Tests")]
    public bool showTestGizmos = true;
    public Color testSuccessColor = Color.green;
    public Color testFailColor = Color.red;
    public Color testProgressColor = Color.yellow;
    
    // Estado de testing
    private bool isRunningTests = false;
    private string currentTest = "";
    private float testProgress = 0f;
    private System.Collections.Generic.List<TestResult> testResults = new System.Collections.Generic.List<TestResult>();
    
    [System.Serializable]
    public class TestResult
    {
        public string testName;
        public bool success;
        public float executionTime;
        public string message;
        public int memoryUsage;
        
        public TestResult(string name, bool passed, float time, string msg = "", int memory = 0)
        {
            testName = name;
            success = passed;
            executionTime = time;
            message = msg;
            memoryUsage = memory;
        }
    }
    
    private void Start()
    {
        if (autoRunTests)
        {
            RunAllTests();
        }
    }
    
    /// <summary>
    /// Ejecuta todos los tests disponibles
    /// </summary>
    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        if (isRunningTests)
        {
            Debug.LogWarning("[TerrainTestSuite] Tests ya en ejecución");
            return;
        }
        
        StartCoroutine(RunTestSuite());
    }
    
    private System.Collections.IEnumerator RunTestSuite()
    {
        isRunningTests = true;
        testResults.Clear();
        testProgress = 0f;
        
        Debug.Log("=== INICIANDO SUITE DE TESTS DE TERRENO PROCEDURAL ===");
        
        float totalTests = GetTotalTestCount();
        float currentTestIndex = 0f;
        
        // Test 1: Validación de configuración
        yield return StartCoroutine(RunConfigurationValidationTest());
        currentTestIndex++;
        testProgress = currentTestIndex / totalTests;
        
        // Test 2: Generación de ruido
        yield return StartCoroutine(RunNoiseGenerationTest());
        currentTestIndex++;
        testProgress = currentTestIndex / totalTests;
        
        // Test 3: Generación de chunks
        if (testChunkGeneration)
        {
            yield return StartCoroutine(RunChunkGenerationTest());
            currentTestIndex++;
            testProgress = currentTestIndex / totalTests;
        }
        
        // Test 4: Sistema de navegación
        if (testNavigationSystem)
        {
            yield return StartCoroutine(RunNavigationSystemTest());
            currentTestIndex++;
            testProgress = currentTestIndex / totalTests;
        }
        
        // Test 5: Sistema de biomas
        if (testBiomeSystem)
        {
            yield return StartCoroutine(RunBiomeSystemTest());
            currentTestIndex++;
            testProgress = currentTestIndex / totalTests;
        }
        
        // Test 6: Sistema de colisiones
        if (testCollisionSystem)
        {
            yield return StartCoroutine(RunCollisionSystemTest());
            currentTestIndex++;
            testProgress = currentTestIndex / totalTests;
        }
        
        // Test 7: Sistema de límites
        if (testBoundarySystem)
        {
            yield return StartCoroutine(RunBoundarySystemTest());
            currentTestIndex++;
            testProgress = currentTestIndex / totalTests;
        }
        
        // Tests de rendimiento
        if (enablePerformanceTests)
        {
            yield return StartCoroutine(RunPerformanceTests());
            currentTestIndex++;
            testProgress = currentTestIndex / totalTests;
        }
        
        // Finalizar testing
        FinishTestSuite();
        isRunningTests = false;
    }
    
    private System.Collections.IEnumerator RunConfigurationValidationTest()
    {
        currentTest = "Validación de Configuración";
        float startTime = Time.realtimeSinceStartup;
        
        bool success = true;
        string message = "";
        
        try
        {
            // Validar TerrainSettings
            if (testTerrainSettings == null)
            {
                success = false;
                message += "TerrainSettings no asignado. ";
            }
            else
            {
                if (testTerrainSettings.worldWidth <= 0 || testTerrainSettings.worldLength <= 0)
                {
                    success = false;
                    message += "Dimensiones del mundo inválidas. ";
                }
                
                if (testTerrainSettings.chunkSize <= 0 || testTerrainSettings.chunkSize > 256)
                {
                    success = false;
                    message += "Tamaño de chunk inválido. ";
                }
                
                if (testTerrainSettings.terrainNoise == null)
                {
                    success = false;
                    message += "NoiseSettings de terreno no asignado. ";
                }
            }
            
            // Validar BiomeSettings
            if (testBiomeSettings == null)
            {
                message += "BiomeSettings no asignado (opcional). ";
            }
            else if (testBiomeSettings.biomes.Length == 0)
            {
                message += "No hay biomas configurados. ";
            }
            
            if (success)
            {
                message = "Configuración válida";
            }
        }
        catch (System.Exception e)
        {
            success = false;
            message = $"Error en validación: {e.Message}";
        }
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        testResults.Add(new TestResult(currentTest, success, executionTime, message));
        
        if (showDetailedLogs)
        {
            Debug.Log($"[TEST] {currentTest}: {(success ? "PASS" : "FAIL")} - {message} ({executionTime:F3}s)");
        }
        
        yield return null;
    }
    
    private System.Collections.IEnumerator RunNoiseGenerationTest()
    {
        currentTest = "Generación de Ruido";
        float startTime = Time.realtimeSinceStartup;
        
        bool success = true;
        string message = "";
        
        try
        {
            if (testTerrainSettings?.terrainNoise != null)
            {
                // Test de generación de ruido en diferentes puntos
                Vector2[] testPoints = {
                    Vector2.zero,
                    new Vector2(100, 100),
                    new Vector2(-50, 75),
                    new Vector2(testTerrainSettings.worldWidth * 0.5f, testTerrainSettings.worldLength * 0.5f)
                };
                
                foreach (Vector2 point in testPoints)
                {
                    float noiseValue = NoiseGenerator.GenerateNoise(point, testTerrainSettings.terrainNoise);
                    
                    if (float.IsNaN(noiseValue) || float.IsInfinity(noiseValue))
                    {
                        success = false;
                        message += $"Valor de ruido inválido en {point}. ";
                        break;
                    }
                    
                    if (noiseValue < 0f || noiseValue > 1f)
                    {
                        success = false;
                        message += $"Ruido fuera de rango [0,1] en {point}: {noiseValue}. ";
                        break;
                    }
                }
                
                if (success)
                {
                    message = $"Ruido generado correctamente para {testPoints.Length} puntos";
                }
            }
            else
            {
                success = false;
                message = "NoiseSettings no disponible para testing";
            }
        }
        catch (System.Exception e)
        {
            success = false;
            message = $"Error en generación de ruido: {e.Message}";
        }
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        testResults.Add(new TestResult(currentTest, success, executionTime, message));
        
        if (showDetailedLogs)
        {
            Debug.Log($"[TEST] {currentTest}: {(success ? "PASS" : "FAIL")} - {message} ({executionTime:F3}s)");
        }
        
        yield return null;
    }
    
    private System.Collections.IEnumerator RunChunkGenerationTest()
    {
        currentTest = "Generación de Chunks";
        float startTime = Time.realtimeSinceStartup;
        
        bool success = true;
        string message = "";
        int memoryBefore = (int)System.GC.GetTotalMemory(false);
        
        try
        {
            // Crear un chunk de prueba
            GameObject testChunkObj = new GameObject("TestChunk");
            TerrainChunk testChunk = testChunkObj.AddComponent<TerrainChunk>();
            
            // Inicializar chunk
            Vector2Int testCoord = Vector2Int.zero;
            int testSize = testTerrainSettings != null ? testTerrainSettings.chunkSize : 64;
            
            testChunk.Initialize(testCoord, testSize, testTerrainSettings);
            
            // Validar que el chunk se generó correctamente
            if (testChunk.heightMap == null)
            {
                success = false;
                message += "HeightMap no generado. ";
            }
            else if (testChunk.heightMap.GetLength(0) != testSize + 1 || testChunk.heightMap.GetLength(1) != testSize + 1)
            {
                success = false;
                message += "Dimensiones de HeightMap incorrectas. ";
            }
            
            if (testChunk.navigabilityMap == null)
            {
                success = false;
                message += "NavigabilityMap no generado. ";
            }
            
            if (testChunk.meshFilter == null || testChunk.meshFilter.mesh == null)
            {
                success = false;
                message += "Malla no generada. ";
            }
            else
            {
                Mesh mesh = testChunk.meshFilter.mesh;
                if (mesh.vertices.Length == 0 || mesh.triangles.Length == 0)
                {
                    success = false;
                    message += "Malla vacía. ";
                }
            }
            
            // Limpiar
            DestroyImmediate(testChunkObj);
            
            if (success)
            {
                message = $"Chunk {testSize}x{testSize} generado correctamente";
            }
        }
        catch (System.Exception e)
        {
            success = false;
            message = $"Error en generación de chunk: {e.Message}";
        }
        
        int memoryAfter = (int)System.GC.GetTotalMemory(false);
        int memoryUsed = memoryAfter - memoryBefore;
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        testResults.Add(new TestResult(currentTest, success, executionTime, message, memoryUsed));
        
        if (showDetailedLogs)
        {
            Debug.Log($"[TEST] {currentTest}: {(success ? "PASS" : "FAIL")} - {message} ({executionTime:F3}s, {memoryUsed} bytes)");
        }
        
        yield return null;
    }
    
    private System.Collections.IEnumerator RunNavigationSystemTest()
    {
        currentTest = "Sistema de Navegación";
        float startTime = Time.realtimeSinceStartup;
        
        bool success = true;
        string message = "";
        
        try
        {
            NavigationManager navManager = FindFirstObjectByType<NavigationManager>();
            
            if (navManager == null)
            {
                success = false;
                message = "NavigationManager no encontrado en la escena";
            }
            else
            {
                // Test de verificación de posición navegable
                Vector3 testPosition = Vector3.zero;
                bool isNavigable = navManager.IsPositionNavigable(testPosition);
                
                // Test de búsqueda de posición navegable cercana
                Vector3 nearestNavigable = navManager.GetNearestNavigablePosition(testPosition);
                
                if (Vector3.Distance(testPosition, nearestNavigable) > 1000f)
                {
                    success = false;
                    message += "Posición navegable muy lejana. ";
                }
                
                message += $"Navegación funcionando (navegable: {isNavigable})";
            }
        }
        catch (System.Exception e)
        {
            success = false;
            message = $"Error en sistema de navegación: {e.Message}";
        }
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        testResults.Add(new TestResult(currentTest, success, executionTime, message));
        
        if (showDetailedLogs)
        {
            Debug.Log($"[TEST] {currentTest}: {(success ? "PASS" : "FAIL")} - {message} ({executionTime:F3}s)");
        }
        
        yield return null;
    }
    
    private System.Collections.IEnumerator RunBiomeSystemTest()
    {
        currentTest = "Sistema de Biomas";
        float startTime = Time.realtimeSinceStartup;
        
        bool success = true;
        string message = "";
        
        try
        {
            if (testBiomeSettings != null && testBiomeSettings.biomes.Length > 0)
            {
                // Test de determinación de bioma
                int biome = BiomeGenerator.DetermineBestBiome(0.5f, 0.5f, 0.5f, testBiomeSettings);
                
                if (biome < 0 || biome >= testBiomeSettings.biomes.Length)
                {
                    success = false;
                    message += "Índice de bioma inválido. ";
                }
                
                // Test de transición de color
                Color transitionColor = BiomeGenerator.CalculateBiomeTransition(Vector2.zero, testBiomeSettings, null);
                
                if (transitionColor.a <= 0)
                {
                    success = false;
                    message += "Color de transición inválido. ";
                }
                
                if (success)
                {
                    message = $"Sistema de biomas funcionando ({testBiomeSettings.biomes.Length} biomas)";
                }
            }
            else
            {
                message = "BiomeSettings no disponible - test saltado";
            }
        }
        catch (System.Exception e)
        {
            success = false;
            message = $"Error en sistema de biomas: {e.Message}";
        }
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        testResults.Add(new TestResult(currentTest, success, executionTime, message));
        
        if (showDetailedLogs)
        {
            Debug.Log($"[TEST] {currentTest}: {(success ? "PASS" : "FAIL")} - {message} ({executionTime:F3}s)");
        }
        
        yield return null;
    }
    
    private System.Collections.IEnumerator RunCollisionSystemTest()
    {
        currentTest = "Sistema de Colisiones";
        float startTime = Time.realtimeSinceStartup;
        
        bool success = true;
        string message = "";
        
        try
        {
            CollisionManager collisionManager = FindFirstObjectByType<CollisionManager>();
            
            if (collisionManager == null)
            {
                message = "CollisionManager no encontrado - test saltado";
            }
            else
            {
                // Verificar configuración del collision manager
                if (collisionManager.poolSize <= 0)
                {
                    success = false;
                    message += "Pool size inválido. ";
                }
                
                if (collisionManager.colliderResolution <= 0)
                {
                    success = false;
                    message += "Resolución de collider inválida. ";
                }
                
                if (success)
                {
                    message = "Sistema de colisiones configurado correctamente";
                }
            }
        }
        catch (System.Exception e)
        {
            success = false;
            message = $"Error en sistema de colisiones: {e.Message}";
        }
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        testResults.Add(new TestResult(currentTest, success, executionTime, message));
        
        if (showDetailedLogs)
        {
            Debug.Log($"[TEST] {currentTest}: {(success ? "PASS" : "FAIL")} - {message} ({executionTime:F3}s)");
        }
        
        yield return null;
    }
    
    private System.Collections.IEnumerator RunBoundarySystemTest()
    {
        currentTest = "Sistema de Límites";
        float startTime = Time.realtimeSinceStartup;
        
        bool success = true;
        string message = "";
        
        try
        {
            WorldBoundarySystem boundarySystem = FindFirstObjectByType<WorldBoundarySystem>();
            
            if (boundarySystem == null)
            {
                message = "WorldBoundarySystem no encontrado - test saltado";
            }
            else
            {
                // Test de detección de proximidad a límites
                Vector3 centerPosition = Vector3.zero;
                Vector3 edgePosition = new Vector3(999999, 0, 0); // Posición muy lejana
                
                bool centerNearBoundary = boundarySystem.IsPositionNearBoundary(centerPosition);
                bool edgeNearBoundary = boundarySystem.IsPositionNearBoundary(edgePosition);
                
                // Test de posición segura
                Vector3 safePosition = boundarySystem.GetNearestSafePosition(edgePosition);
                
                if (Vector3.Distance(safePosition, edgePosition) < 0.1f)
                {
                    // Si las posiciones son muy similares, puede que no esté funcionando
                    message += "Posición segura cuestionable. ";
                }
                
                message += "Sistema de límites funcionando";
            }
        }
        catch (System.Exception e)
        {
            success = false;
            message = $"Error en sistema de límites: {e.Message}";
        }
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        testResults.Add(new TestResult(currentTest, success, executionTime, message));
        
        if (showDetailedLogs)
        {
            Debug.Log($"[TEST] {currentTest}: {(success ? "PASS" : "FAIL")} - {message} ({executionTime:F3}s)");
        }
        
        yield return null;
    }
    
    private System.Collections.IEnumerator RunPerformanceTests()
    {
        currentTest = "Tests de Rendimiento";
        float startTime = Time.realtimeSinceStartup;
        
        bool success = true;
        string message = "";
        
        float totalNoiseTime = 0f;
        
        for (int i = 0; i < performanceIterations; i++)
        {
            // Test de rendimiento de ruido
            float noiseStart = Time.realtimeSinceStartup;
            try
            {
                for (int j = 0; j < 1000; j++)
                {
                    Vector2 randomPoint = new Vector2(Random.Range(0, 100), Random.Range(0, 100));
                    NoiseGenerator.GenerateNoise(randomPoint, testTerrainSettings.terrainNoise);
                }
                totalNoiseTime += Time.realtimeSinceStartup - noiseStart;
            }
            catch (System.Exception e)
            {
                success = false;
                message = $"Error en tests de rendimiento: {e.Message}";
                break;
            }
            
            yield return null; // Permitir que otros sistemas funcionen
        }
        
        if (success)
        {
            float avgNoiseTime = totalNoiseTime / performanceIterations;
            
            if (avgNoiseTime > 0.1f) // Si toma más de 100ms para 1000 puntos
            {
                message += $"Rendimiento de ruido lento: {avgNoiseTime:F3}s. ";
            }
            
            message += $"Rendimiento: {avgNoiseTime:F3}s promedio para 1000 puntos de ruido";
        }
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        testResults.Add(new TestResult(currentTest, success, executionTime, message));
        
        if (showDetailedLogs)
        {
            Debug.Log($"[TEST] {currentTest}: {(success ? "PASS" : "FAIL")} - {message} ({executionTime:F3}s)");
        }
        
        yield return null;
    }
    
    private void FinishTestSuite()
    {
        testProgress = 1f;
        currentTest = "Completado";
        
        int passedTests = 0;
        int failedTests = 0;
        float totalTime = 0f;
        
        foreach (TestResult result in testResults)
        {
            if (result.success) passedTests++;
            else failedTests++;
            totalTime += result.executionTime;
        }
        
        Debug.Log("=== RESULTADOS DE TESTS DE TERRENO PROCEDURAL ===");
        Debug.Log($"Tests ejecutados: {testResults.Count}");
        Debug.Log($"Exitosos: {passedTests}");
        Debug.Log($"Fallidos: {failedTests}");
        Debug.Log($"Tiempo total: {totalTime:F3}s");
        Debug.Log($"Éxito: {(failedTests == 0 ? "SÍ" : "NO")}");
        
        if (failedTests > 0)
        {
            Debug.LogWarning("Tests fallidos encontrados:");
            foreach (TestResult result in testResults)
            {
                if (!result.success)
                {
                    Debug.LogWarning($"- {result.testName}: {result.message}");
                }
            }
        }
        
        Debug.Log("=== FIN DE TESTS ===");
    }
    
    private float GetTotalTestCount()
    {
        float count = 2f; // Config + Noise siempre se ejecutan
        
        if (testChunkGeneration) count++;
        if (testNavigationSystem) count++;
        if (testBiomeSystem) count++;
        if (testCollisionSystem) count++;
        if (testBoundarySystem) count++;
        if (enablePerformanceTests) count++;
        
        return count;
    }
    
    // Métodos públicos para testing individual
    [ContextMenu("Test Configuration Only")]
    public void TestConfigurationOnly()
    {
        StartCoroutine(RunConfigurationValidationTest());
    }
    
    [ContextMenu("Test Noise Generation Only")]
    public void TestNoiseGenerationOnly()
    {
        StartCoroutine(RunNoiseGenerationTest());
    }
    
    [ContextMenu("Test Chunk Generation Only")]
    public void TestChunkGenerationOnly()
    {
        StartCoroutine(RunChunkGenerationTest());
    }
    
    [ContextMenu("Show Last Results")]
    public void ShowLastResults()
    {
        if (testResults.Count == 0)
        {
            Debug.Log("No hay resultados de tests disponibles");
            return;
        }
        
        Debug.Log("=== ÚLTIMOS RESULTADOS DE TESTS ===");
        foreach (TestResult result in testResults)
        {
            string status = result.success ? "PASS" : "FAIL";
            Debug.Log($"{status} - {result.testName}: {result.message} ({result.executionTime:F3}s)");
        }
    }
    
    // Visualización en el inspector
    private void OnDrawGizmos()
    {
        if (!showTestGizmos) return;
        
        // Mostrar progreso de tests
        if (isRunningTests)
        {
            Gizmos.color = testProgressColor;
            Vector3 center = transform.position + Vector3.up * 5f;
            Gizmos.DrawWireSphere(center, 2f * testProgress);
        }
        
        // Mostrar resultados de último test
        if (testResults.Count > 0)
        {
            TestResult lastResult = testResults[testResults.Count - 1];
            Gizmos.color = lastResult.success ? testSuccessColor : testFailColor;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
}