using UnityEngine;

/// <summary>
/// Crea y configura materiales físicos para el submarino
/// </summary>
[CreateAssetMenu(fileName = "SubmarinePhysicsMaterial", menuName = "Submarine/Physics Material")]
public class SubmarinePhysicsMaterial : ScriptableObject
{
    [Header("Friction Settings")]
    [Range(0f, 1f)]
    public float dynamicFriction = 0.3f;
    [Range(0f, 1f)]
    public float staticFriction = 0.4f;
    
    [Header("Bounce Settings")]
    [Range(0f, 1f)]
    public float bounciness = 0.1f;
    
    [Header("Combine Modes")]
    public PhysicsMaterialCombine frictionCombine = PhysicsMaterialCombine.Average;
    public PhysicsMaterialCombine bounceCombine = PhysicsMaterialCombine.Minimum;
    
    public PhysicsMaterial CreatePhysicMaterial()
    {
        PhysicsMaterial material = new PhysicsMaterial("SubmarineHull");
        material.dynamicFriction = dynamicFriction;
        material.staticFriction = staticFriction;
        material.bounciness = bounciness;
        material.frictionCombine = frictionCombine;
        material.bounceCombine = bounceCombine;
        
        return material;
    }
}