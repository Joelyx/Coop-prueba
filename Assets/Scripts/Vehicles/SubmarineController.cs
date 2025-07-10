using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class SubmarineController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 10f;
    public float backwardSpeed = 7f;
    public float turnSpeed = 120f;
    public float acceleration = 5f;
    public float deceleration = 8f;
    
    [Header("Physics Settings")]
    public float waterDrag = 2f;
    public float angularDrag = 5f;
    public float mass = 100f;
    
    [Header("Visual Feedback")]
    public ParticleSystem forwardThruster;
    public ParticleSystem backwardThruster;
    public ParticleSystem leftThruster;
    public ParticleSystem rightThruster;
    
    [Header("Audio")]
    public AudioSource engineSound;
    public AudioClip thrusterSound;
    
    [Header("Sonar System")]
    public SubmarineSonar sonarSystem;
    
    [Header("Collision Settings")]
    public bool enableCollisions = true;
    public float collisionDamping = 0.5f;
    
    [Header("Physics Isolation")]
    public bool usePhysicsIsolation = true;
    private SubmarinePhysicsIsolator physicsIsolator;
    
    [Header("Stabilization")]
    public bool useStabilization = true;
    private SubmarineStabilizer stabilizer;
    
    [Header("Collision Feedback")]
    public bool useCollisionFeedback = true;
    // private SubmarineCollisionFeedback collisionFeedback; // Se añadirá dinámicamente
    
    private Rigidbody rb;
    private Vector3 _currentForce = Vector3.zero;
    
    // Current velocity for smooth movement
    private float _targetForwardVelocity = 0f;
    private float _currentForwardVelocity = 0f;
    private float _targetAngularVelocity = 0f;
    private float _currentAngularVelocity = 0f;
    
    // Sonar command tracking to prevent spam
    private float lastSonarCommandTime = -999f;
    private const float SONAR_COMMAND_COOLDOWN = 0.5f; // Minimum time between sonar commands
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    private void Start()
    {
        ConfigureRigidbody();
        
        if (engineSound == null)
            engineSound = gameObject.AddComponent<AudioSource>();
            
        engineSound.loop = true;
        engineSound.volume = 0.3f;
        
        // Auto-find sonar system if not assigned
        if (sonarSystem == null)
            sonarSystem = GetComponent<SubmarineSonar>();
            
        // Configurar aislamiento de física si está habilitado
        if (usePhysicsIsolation)
        {
            physicsIsolator = GetComponent<SubmarinePhysicsIsolator>();
            if (physicsIsolator == null)
            {
                physicsIsolator = gameObject.AddComponent<SubmarinePhysicsIsolator>();
                Debug.Log("[SUBMARINE] Physics Isolator added to prevent player physics interference");
            }
        }
        
        // Configurar estabilización si está habilitada
        if (useStabilization)
        {
            stabilizer = GetComponent<SubmarineStabilizer>();
            if (stabilizer == null)
            {
                stabilizer = gameObject.AddComponent<SubmarineStabilizer>();
                Debug.Log("[SUBMARINE] Stabilizer added for realistic collision response");
            }
        }
        
        // El sistema de feedback se configurará automáticamente con SubmarineSetup
        if (useCollisionFeedback)
        {
            Debug.Log("[SUBMARINE] Collision feedback will be configured by SubmarineSetup");
        }
    }
    
    private void ConfigureRigidbody()
    {
        // Configure submarine physics
        rb.mass = mass;
        rb.linearDamping = waterDrag;
        rb.angularDamping = angularDrag;
        rb.useGravity = false;
        
        // MUY IMPORTANTE: No hacer kinematic para permitir colisiones
        rb.isKinematic = false;
        
        // Si usamos estabilización, permitir todas las rotaciones
        // Si no, mantener el submarino horizontal
        if (useStabilization)
        {
            rb.constraints = RigidbodyConstraints.None;
        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezePositionY | 
                            RigidbodyConstraints.FreezeRotationX | 
                            RigidbodyConstraints.FreezeRotationZ;
        }
        
        // Enable collision detection
        if (enableCollisions)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        Debug.Log($"[SUBMARINE] Rigidbody configurado - isKinematic: {rb.isKinematic}, CollisionMode: {rb.collisionDetectionMode}");
    }
    
    public void ProcessCommand(SubmarineInput input)
    {
        Debug.Log($"Submarine received command: {input.command} with intensity: {input.intensity}");
        
        switch (input.command)
        {
            case SubmarineCommand.Forward:
                ApplyForwardThrust(input.intensity);
                break;
                
            case SubmarineCommand.Backward:
                ApplyBackwardThrust(input.intensity);
                break;
                
            case SubmarineCommand.TurnLeft:
                ApplyLeftTurn(input.intensity);
                break;
                
            case SubmarineCommand.TurnRight:
                ApplyRightTurn(input.intensity);
                break;
                
            case SubmarineCommand.SonarPing:
                ActivateSonar();
                break;
        }
    }
    
    private void ApplyForwardThrust(float intensity)
    {
        _targetForwardVelocity = forwardSpeed * intensity;
        Debug.Log($"Forward thrust: intensity={intensity}, targetVel={_targetForwardVelocity}, forwardSpeed={forwardSpeed}");
        
        // Visual effects
        if (forwardThruster != null)
        {
            var emission = forwardThruster.emission;
            emission.rateOverTime = intensity * 50f;
            if (!forwardThruster.isPlaying) forwardThruster.Play();
        }
        
        PlayEngineSound(intensity);
    }
    
    private void ApplyBackwardThrust(float intensity)
    {
        _targetForwardVelocity = -backwardSpeed * intensity;
        
        // Visual effects
        if (backwardThruster != null)
        {
            var emission = backwardThruster.emission;
            emission.rateOverTime = intensity * 30f;
            if (!backwardThruster.isPlaying) backwardThruster.Play();
        }
        
        PlayEngineSound(intensity * 0.7f);
    }
    
    private void ApplyLeftTurn(float intensity)
    {
        _targetAngularVelocity = -turnSpeed * intensity;
        
        // Visual effects
        if (rightThruster != null)
        {
            var emission = rightThruster.emission;
            emission.rateOverTime = intensity * 20f;
            if (!rightThruster.isPlaying) rightThruster.Play();
        }
    }
    
    private void ApplyRightTurn(float intensity)
    {
        _targetAngularVelocity = turnSpeed * intensity;
        
        // Visual effects
        if (leftThruster != null)
        {
            var emission = leftThruster.emission;
            emission.rateOverTime = intensity * 20f;
            if (!leftThruster.isPlaying) leftThruster.Play();
        }
    }
    
    private void FixedUpdate()
    {
        HandleMovement();
        HandleEffects();
    }
    
    private void HandleMovement()
    {
        // Smooth forward/backward movement
        _currentForwardVelocity = Mathf.MoveTowards(_currentForwardVelocity, 
                                                  _targetForwardVelocity, 
                                                  acceleration * Time.fixedDeltaTime);
        
        // Smooth rotation
        _currentAngularVelocity = Mathf.MoveTowards(_currentAngularVelocity, 
                                                  _targetAngularVelocity, 
                                                  acceleration * 10f * Time.fixedDeltaTime);
        
        // Apply forces
        Vector3 forwardForce = transform.forward * _currentForwardVelocity;
        rb.AddForce(forwardForce, ForceMode.Acceleration);
        
        // Debug movement
        if (Mathf.Abs(_currentForwardVelocity) > 0.1f)
        {
            Debug.Log($"Submarine moving: currentVel={_currentForwardVelocity}, targetVel={_targetForwardVelocity}, force={forwardForce}, rbVel={rb.linearVelocity}");
        }
        
        // Apply torque for rotation
        rb.AddTorque(Vector3.up * _currentAngularVelocity, ForceMode.Acceleration);
        
        // Gradually reduce target velocities when no input
        _targetForwardVelocity = Mathf.MoveTowards(_targetForwardVelocity, 0f, deceleration * Time.fixedDeltaTime);
        _targetAngularVelocity = Mathf.MoveTowards(_targetAngularVelocity, 0f, deceleration * 10f * Time.fixedDeltaTime);
    }
    
    private void HandleEffects()
    {
        // Stop particle effects when not thrusting
        if (Mathf.Abs(_targetForwardVelocity) < 0.1f)
        {
            if (forwardThruster != null && forwardThruster.isPlaying) forwardThruster.Stop();
            if (backwardThruster != null && backwardThruster.isPlaying) backwardThruster.Stop();
        }
        
        if (Mathf.Abs(_targetAngularVelocity) < 0.1f)
        {
            if (leftThruster != null && leftThruster.isPlaying) leftThruster.Stop();
            if (rightThruster != null && rightThruster.isPlaying) rightThruster.Stop();
        }
    }
    
    private void PlayEngineSound(float intensity)
    {
        if (engineSound != null)
        {
            if (!engineSound.isPlaying && intensity > 0.1f)
            {
                engineSound.pitch = 0.8f + (intensity * 0.4f);
                engineSound.volume = 0.2f + (intensity * 0.3f);
                if (thrusterSound != null)
                    engineSound.clip = thrusterSound;
                engineSound.Play();
            }
            else if (engineSound.isPlaying)
            {
                engineSound.pitch = 0.8f + (intensity * 0.4f);
                engineSound.volume = 0.2f + (intensity * 0.3f);
            }
        }
    }
    
    public Vector3 GetVelocity()
    {
        return rb.linearVelocity;
    }
    
    public float GetSpeed()
    {
        return rb.linearVelocity.magnitude;
    }
    
    private void ActivateSonar()
    {
        // Check if enough time has passed since last sonar command
        if (Time.time - lastSonarCommandTime < SONAR_COMMAND_COOLDOWN)
        {
            return;
        }
        
        if (sonarSystem != null)
        {
            if (sonarSystem.CanPing)
            {
                Debug.Log("[SUBMARINE] Activating sonar...");
                sonarSystem.TriggerSonarPing();
                lastSonarCommandTime = Time.time;
            }
            else
            {
                Debug.Log($"[SUBMARINE] Sonar on cooldown. Remaining: {sonarSystem.GetCooldownRemaining():F1}s");
            }
        }
        else
        {
            Debug.LogError("[SUBMARINE] No sonar system assigned!");
        }
    }
    
    // Public method for external sonar control
    public void TriggerSonar()
    {
        ActivateSonar();
    }
    
    public bool CanUseSonar()
    {
        return sonarSystem != null && sonarSystem.CanPing && (Time.time - lastSonarCommandTime >= SONAR_COMMAND_COOLDOWN);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!enableCollisions) return;
        
        float collisionForce = collision.relativeVelocity.magnitude;
        
        if (collisionForce > 2f)
        {
            Debug.Log($"[SUBMARINE] Collision with {collision.gameObject.name}, force: {collisionForce}");
            
            // Apply damping to reduce bounce
            Vector3 dampingForce = -rb.linearVelocity * collisionDamping;
            rb.AddForce(dampingForce, ForceMode.VelocityChange);
            
            // El stabilizer manejará la respuesta de rotación
            if (stabilizer != null)
            {
                stabilizer.OnCollisionEnter(collision);
            }
        }
    }
    
    private void OnCollisionStay(Collision collision)
    {
        if (!enableCollisions) return;
        
        // Continuous damping while in contact
        Vector3 dampingForce = -rb.linearVelocity * collisionDamping * 0.1f;
        rb.AddForce(dampingForce, ForceMode.Force);
    }
}