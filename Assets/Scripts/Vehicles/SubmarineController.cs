using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class SubmarineController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 10f;
    public float backwardSpeed = 7f;
    public float turnSpeed = 45f;
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
    
    private Rigidbody rb;
    private Vector3 _currentForce = Vector3.zero;
    private float currentTorque = 0f;
    
    // Current velocity for smooth movement
    private float _targetForwardVelocity = 0f;
    private float _currentForwardVelocity = 0f;
    private float _targetAngularVelocity = 0f;
    private float _currentAngularVelocity = 0f;
    
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
    }
    
    private void ConfigureRigidbody()
    {
        // Configure submarine physics
        rb.mass = mass;
        rb.linearDamping = waterDrag;
        rb.angularDamping = angularDrag;
        rb.useGravity = false;
        
        // Freeze Y movement and X/Z rotation (submarine stays horizontal)
        rb.constraints = RigidbodyConstraints.FreezePositionY | 
                        RigidbodyConstraints.FreezeRotationX | 
                        RigidbodyConstraints.FreezeRotationZ;
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
        if (sonarSystem != null)
        {
            sonarSystem.TriggerSonarPing();
        }
    }
    
    // Public method for external sonar control
    public void TriggerSonar()
    {
        ActivateSonar();
    }
    
    public bool CanUseSonar()
    {
        return sonarSystem != null && sonarSystem.CanPing;
    }
}