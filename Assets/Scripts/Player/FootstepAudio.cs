using UnityEngine;

/// <summary>
/// Manages footstep audio synchronized with head bob and movement
/// Provides realistic audio feedback for player movement
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class FootstepAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip[] footstepClips;
    public float volumeRange = 0.1f;
    public float pitchRange = 0.1f;
    
    [Header("Step Timing")]
    [Range(0f, 1f)]
    public float stepTriggerPoint = 0.3f; // When in bob cycle to trigger step
    [Range(0f, 1f)] 
    public float stepResetPoint = 0.6f;   // When to reset step cycle
    
    [Header("References")]
    public PlayerMovement playerMovement;
    public HeadBobController headBobController;
    
    // Properties
    public bool IsEnabled { get; set; } = true;
    
    // Private variables
    private AudioSource audioSource;
    private float lastBobValue;
    private bool hasPlayedStepThisCycle;
    private float stepTriggerThreshold;
    private float stepResetThreshold;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Auto-find references if not assigned
        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
            
        if (headBobController == null)
            headBobController = GetComponentInParent<HeadBobController>();
    }
    
    private void Start()
    {
        ConfigureAudioSource();
        SubscribeToEvents();
        CalculateThresholds();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void ConfigureAudioSource()
    {
        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D audio for first person
        }
    }
    
    private void SubscribeToEvents()
    {
        if (headBobController != null)
            headBobController.OnBobStep += HandleBobStep;
    }
    
    private void UnsubscribeFromEvents()
    {
        if (headBobController != null)
            headBobController.OnBobStep -= HandleBobStep;
    }
    
    private void CalculateThresholds()
    {
        if (headBobController != null)
        {
            stepTriggerThreshold = headBobController.bobVerticalAmplitude * stepTriggerPoint;
            stepResetThreshold = headBobController.bobVerticalAmplitude * stepResetPoint;
        }
    }
    
    private void HandleBobStep(float currentBobValue)
    {
        if (!IsEnabled || !ShouldPlayFootstep()) return;
        
        // Detect when we cross the trigger point going down
        if (lastBobValue > stepTriggerThreshold && 
            currentBobValue <= stepTriggerThreshold && 
            !hasPlayedStepThisCycle)
        {
            PlayFootstep();
            hasPlayedStepThisCycle = true;
        }
        
        // Reset the cycle when we go back up past reset point
        if (currentBobValue > stepResetThreshold)
        {
            hasPlayedStepThisCycle = false;
        }
        
        lastBobValue = currentBobValue;
    }
    
    private bool ShouldPlayFootstep()
    {
        return playerMovement != null && 
               playerMovement.IsGrounded && 
               playerMovement.IsMoving() &&
               footstepClips.Length > 0;
    }
    
    private void PlayFootstep()
    {
        if (audioSource == null || footstepClips.Length == 0) return;
        
        // Select random footstep clip
        int clipIndex = Random.Range(0, footstepClips.Length);
        AudioClip clip = footstepClips[clipIndex];
        
        // Add variation to volume and pitch
        float volume = 1f + Random.Range(-volumeRange, volumeRange);
        float pitch = 1f + Random.Range(-pitchRange, pitchRange);
        
        // Apply speed-based volume modification
        if (playerMovement != null)
        {
            float speedMultiplier = Mathf.Clamp(playerMovement.CurrentSpeed / 6f, 0.5f, 1.2f);
            volume *= speedMultiplier;
        }
        
        // Play the footstep with variations
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume);
    }
    
    // Public methods for external control
    public void SetFootstepClips(AudioClip[] clips)
    {
        footstepClips = clips;
    }
    
    public void SetVolumeRange(float range)
    {
        volumeRange = Mathf.Clamp01(range);
    }
    
    public void SetPitchRange(float range)
    {
        pitchRange = Mathf.Clamp01(range);
    }
    
    public void SetStepTiming(float triggerPoint, float resetPoint)
    {
        stepTriggerPoint = Mathf.Clamp01(triggerPoint);
        stepResetPoint = Mathf.Clamp01(resetPoint);
        CalculateThresholds();
    }
    
    public void PlayManualFootstep()
    {
        if (IsEnabled)
            PlayFootstep();
    }
    
    public void ResetStepCycle()
    {
        hasPlayedStepThisCycle = false;
        lastBobValue = 0f;
    }
}