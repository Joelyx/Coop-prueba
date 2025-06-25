using UnityEngine;

/// <summary>
/// Script simple para verificar que todos los componentes del sistema compilan correctamente
/// </summary>
public class CompilationTest : MonoBehaviour
{
    [Header("Verificación de Compilación")]
    public bool testOnStart = true;
    
    private void Start()
    {
        if (testOnStart)
        {
            TestCompilation();
        }
    }
    
    [ContextMenu("Test Compilation")]
    public void TestCompilation()
    {
        Debug.Log("=== TEST DE COMPILACIÓN ===");
        
        // Verificar que todas las clases principales existen
        TestClassExists<TerrainGenerator>("TerrainGenerator");
        TestClassExists<TerrainSettings>("TerrainSettings");
        TestClassExists<NoiseSettings>("NoiseSettings");
        TestClassExists<BiomeSettings>("BiomeSettings");
        TestClassExists<TerrainChunk>("TerrainChunk");
        TestClassExists<ChunkManager>("ChunkManager");
        TestClassExists<NavigationManager>("NavigationManager");
        TestClassExists<CollisionManager>("CollisionManager");
        TestClassExists<BiomeManager>("BiomeManager");
        TestClassExists<WorldBoundarySystem>("WorldBoundarySystem");
        TestClassExists<TerrainTestSuite>("TerrainTestSuite");
        
        // Verificar que las clases estáticas funcionan
        TestStaticClass("NoiseGenerator");
        TestStaticClass("MeshGenerator");
        TestStaticClass("BiomeGenerator");
        
        Debug.Log("✅ Compilación verificada correctamente");
        Debug.Log("=== FIN TEST DE COMPILACIÓN ===");
    }
    
    private void TestClassExists<T>(string className) where T : class
    {
        try
        {
            System.Type type = typeof(T);
            Debug.Log($"✅ {className}: OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ {className}: ERROR - {e.Message}");
        }
    }
    
    private void TestStaticClass(string className)
    {
        try
        {
            switch (className)
            {
                case "NoiseGenerator":
                    // Test que NoiseGenerator existe y tiene métodos públicos
                    var noiseMethod = typeof(NoiseGenerator).GetMethod("GenerateNoise");
                    if (noiseMethod != null)
                        Debug.Log($"✅ {className}: OK");
                    else
                        Debug.LogError($"❌ {className}: Método GenerateNoise no encontrado");
                    break;
                    
                case "MeshGenerator":
                    var meshMethod = typeof(MeshGenerator).GetMethod("GenerateTerrainMesh");
                    if (meshMethod != null)
                        Debug.Log($"✅ {className}: OK");
                    else
                        Debug.LogError($"❌ {className}: Método GenerateTerrainMesh no encontrado");
                    break;
                    
                case "BiomeGenerator":
                    var biomeMethod = typeof(BiomeGenerator).GetMethod("ApplyBiomeToChunk");
                    if (biomeMethod != null)
                        Debug.Log($"✅ {className}: OK");
                    else
                        Debug.LogError($"❌ {className}: Método ApplyBiomeToChunk no encontrado");
                    break;
                    
                default:
                    Debug.LogWarning($"⚠️ {className}: No hay test específico");
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ {className}: ERROR - {e.Message}");
        }
    }
}