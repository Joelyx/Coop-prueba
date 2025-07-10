using UnityEngine;

public class PhysicsConfigurationHelper : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float playerBounciness = 0f;
    [SerializeField] private float playerDynamicFriction = 0.6f;
    [SerializeField] private float playerStaticFriction = 0.6f;
    [SerializeField] private PhysicsMaterialCombine frictionCombine = PhysicsMaterialCombine.Average;
    [SerializeField] private PhysicsMaterialCombine bounceCombine = PhysicsMaterialCombine.Average;
    
    void Start()
    {
        ConfigurePlayerPhysics();
        ConfigureSubmarinePhysics();
    }
    
    void ConfigurePlayerPhysics()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider != null)
            {
                // Crear un nuevo material físico para el jugador
                PhysicsMaterial playerPhysMat = new PhysicsMaterial("PlayerPhysicMaterial");
                playerPhysMat.bounciness = playerBounciness;
                playerPhysMat.dynamicFriction = playerDynamicFriction;
                playerPhysMat.staticFriction = playerStaticFriction;
                playerPhysMat.frictionCombine = frictionCombine;
                playerPhysMat.bounceCombine = bounceCombine;
                
                playerCollider.material = playerPhysMat;
                
                Debug.Log("Player physics material configured");
            }
        }
    }
    
    void ConfigureSubmarinePhysics()
    {
        GameObject submarine = GameObject.FindGameObjectWithTag("Submarine");
        if (submarine != null)
        {
            Collider submarineCollider = submarine.GetComponent<Collider>();
            if (submarineCollider != null)
            {
                // Crear un nuevo material físico para el submarino
                PhysicsMaterial submarinePhysMat = new PhysicsMaterial("SubmarinePhysicMaterial");
                submarinePhysMat.bounciness = 0.2f; // Un poco de rebote
                submarinePhysMat.dynamicFriction = 0.4f;
                submarinePhysMat.staticFriction = 0.5f;
                submarinePhysMat.frictionCombine = PhysicsMaterialCombine.Average;
                submarinePhysMat.bounceCombine = PhysicsMaterialCombine.Maximum;
                
                submarineCollider.material = submarinePhysMat;
                
                Debug.Log("Submarine physics material configured");
            }
        }
    }
}
