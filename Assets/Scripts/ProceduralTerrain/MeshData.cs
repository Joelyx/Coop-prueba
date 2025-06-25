using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Estructura de datos para almacenar información de malla generada
/// </summary>
public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    public Vector3[] normals;
    public Color[] colors;
    
    public int vertexIndex;
    public int triangleIndex;
    
    public MeshData(int meshWidth, int meshHeight)
    {
        int vertexCount = meshWidth * meshHeight;
        int triangleCount = (meshWidth - 1) * (meshHeight - 1) * 6;
        
        vertices = new Vector3[vertexCount];
        triangles = new int[triangleCount];
        uvs = new Vector2[vertexCount];
        normals = new Vector3[vertexCount];
        colors = new Color[vertexCount];
        
        vertexIndex = 0;
        triangleIndex = 0;
    }
    
    public void AddVertex(Vector3 position, Vector2 uv, Color color)
    {
        vertices[vertexIndex] = position;
        uvs[vertexIndex] = uv;
        colors[vertexIndex] = color;
        vertexIndex++;
    }
    
    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }
    
    public void CalculateNormals()
    {
        // Inicializar normales
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.zero;
        }
        
        // Calcular normales por triángulo
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int vertexA = triangles[i];
            int vertexB = triangles[i + 1];
            int vertexC = triangles[i + 2];
            
            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexA, vertexB, vertexC);
            
            normals[vertexA] += triangleNormal;
            normals[vertexB] += triangleNormal;
            normals[vertexC] += triangleNormal;
        }
        
        // Normalizar
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i].Normalize();
        }
    }
    
    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = vertices[indexA];
        Vector3 pointB = vertices[indexB];
        Vector3 pointC = vertices[indexC];
        
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        
        return Vector3.Cross(sideAB, sideAC).normalized;
    }
    
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Para chunks grandes
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;
        
        CalculateNormals();
        mesh.normals = normals;
        
        return mesh;
    }
}