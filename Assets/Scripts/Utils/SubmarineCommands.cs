using UnityEngine;

public enum SubmarineCommand
{
    None,
    Forward,
    Backward, 
    TurnLeft,
    TurnRight,
    SonarPing
}

[System.Serializable]
public struct SubmarineInput
{
    public SubmarineCommand command;
    public float intensity; // 0-1 range for variable input strength
    public float duration;  // How long the command should be applied
    
    public SubmarineInput(SubmarineCommand cmd, float force = 1f, float time = 0.1f)
    {
        command = cmd;
        intensity = Mathf.Clamp01(force);
        duration = time;
    }
}