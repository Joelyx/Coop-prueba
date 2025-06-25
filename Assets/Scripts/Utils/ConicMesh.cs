using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Custom UI component that renders a cone shape for the sonar display
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ConicMesh : Graphic
{
    [Header("Cone Settings")]
    public float coneAngle = 90f;
    public float coneRadius = 150f;
    public Color coneColor = Color.green;
    public bool isRingOnly = false;
    public float ringWidth = 2f;
    
    [Header("Mesh Quality")]
    public int segments = 32;
    
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        
        if (isRingOnly)
        {
            CreateConeRing(vh);
        }
        else
        {
            CreateConeFill(vh);
        }
    }
    
    private void CreateConeFill(VertexHelper vh)
    {
        // Create a filled cone shape
        
        // Center vertex (tip of cone at origin)
        vh.AddVert(Vector3.zero, coneColor, Vector2.zero);
        
        // Create vertices around the arc
        float halfAngle = coneAngle * 0.5f;
        float angleStep = coneAngle / segments;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = -halfAngle + (angleStep * i);
            float angleRad = angle * Mathf.Deg2Rad;
            
            Vector3 position = new Vector3(
                Mathf.Sin(angleRad) * coneRadius,
                Mathf.Cos(angleRad) * coneRadius,
                0f
            );
            
            vh.AddVert(position, coneColor, Vector2.zero);
        }
        
        // Create triangles
        for (int i = 0; i < segments; i++)
        {
            vh.AddTriangle(0, i + 1, i + 2);
        }
    }
    
    private void CreateConeRing(VertexHelper vh)
    {
        // Create a ring/outline of the cone
        
        float halfAngle = coneAngle * 0.5f;
        float angleStep = coneAngle / segments;
        float innerRadius = coneRadius - ringWidth;
        
        // Create vertices for outer and inner arc
        for (int i = 0; i <= segments; i++)
        {
            float angle = -halfAngle + (angleStep * i);
            float angleRad = angle * Mathf.Deg2Rad;
            
            // Outer vertex
            Vector3 outerPos = new Vector3(
                Mathf.Sin(angleRad) * coneRadius,
                Mathf.Cos(angleRad) * coneRadius,
                0f
            );
            vh.AddVert(outerPos, coneColor, Vector2.zero);
            
            // Inner vertex
            Vector3 innerPos = new Vector3(
                Mathf.Sin(angleRad) * innerRadius,
                Mathf.Cos(angleRad) * innerRadius,
                0f
            );
            vh.AddVert(innerPos, coneColor, Vector2.zero);
        }
        
        // Create quads between outer and inner vertices
        for (int i = 0; i < segments; i++)
        {
            int outerCurrent = i * 2;
            int innerCurrent = i * 2 + 1;
            int outerNext = (i + 1) * 2;
            int innerNext = (i + 1) * 2 + 1;
            
            // First triangle
            vh.AddTriangle(outerCurrent, outerNext, innerCurrent);
            // Second triangle
            vh.AddTriangle(innerCurrent, outerNext, innerNext);
        }
        
        // Add side lines from center to arc endpoints if this is an outline
        if (ringWidth < coneRadius)
        {
            // Add center point
            int centerIndex = vh.currentVertCount;
            vh.AddVert(Vector3.zero, coneColor, Vector2.zero);
            
            // Add a small width point near center for the side lines
            int centerOffset = vh.currentVertCount;
            vh.AddVert(new Vector3(0, ringWidth, 0), coneColor, Vector2.zero);
            
            // Left side line
            float leftAngleRad = -halfAngle * Mathf.Deg2Rad;
            Vector3 leftOuter = new Vector3(
                Mathf.Sin(leftAngleRad) * coneRadius,
                Mathf.Cos(leftAngleRad) * coneRadius,
                0f
            );
            Vector3 leftInner = new Vector3(
                Mathf.Sin(leftAngleRad) * ringWidth,
                Mathf.Cos(leftAngleRad) * ringWidth,
                0f
            );
            
            int leftOuterIdx = vh.currentVertCount;
            vh.AddVert(leftOuter, coneColor, Vector2.zero);
            int leftInnerIdx = vh.currentVertCount;
            vh.AddVert(leftInner, coneColor, Vector2.zero);
            
            // Right side line
            float rightAngleRad = halfAngle * Mathf.Deg2Rad;
            Vector3 rightOuter = new Vector3(
                Mathf.Sin(rightAngleRad) * coneRadius,
                Mathf.Cos(rightAngleRad) * coneRadius,
                0f
            );
            Vector3 rightInner = new Vector3(
                Mathf.Sin(rightAngleRad) * ringWidth,
                Mathf.Cos(rightAngleRad) * ringWidth,
                0f
            );
            
            int rightOuterIdx = vh.currentVertCount;
            vh.AddVert(rightOuter, coneColor, Vector2.zero);
            int rightInnerIdx = vh.currentVertCount;
            vh.AddVert(rightInner, coneColor, Vector2.zero);
            
            // Create side line quads
            vh.AddTriangle(centerIndex, leftOuterIdx, leftInnerIdx);
            vh.AddTriangle(centerIndex, leftInnerIdx, centerOffset);
            
            vh.AddTriangle(centerIndex, rightInnerIdx, rightOuterIdx);
            vh.AddTriangle(centerIndex, centerOffset, rightInnerIdx);
        }
    }
    
    public override Color color
    {
        get { return coneColor; }
        set
        {
            if (coneColor != value)
            {
                coneColor = value;
                SetVerticesDirty();
            }
        }
    }
    
    protected override void OnValidate()
    {
        base.OnValidate();
        
        // Clamp values
        coneAngle = Mathf.Clamp(coneAngle, 1f, 179f);
        coneRadius = Mathf.Max(coneRadius, 1f);
        segments = Mathf.Max(segments, 3);
        ringWidth = Mathf.Clamp(ringWidth, 0.1f, coneRadius);
        
        SetVerticesDirty();
    }
    
#if UNITY_EDITOR
    protected override void Reset()
    {
        base.Reset();
        
        coneAngle = 90f;
        coneRadius = 150f;
        coneColor = new Color(0f, 1f, 0f, 0.3f);
        segments = 32;
        
        SetVerticesDirty();
    }
#endif
}