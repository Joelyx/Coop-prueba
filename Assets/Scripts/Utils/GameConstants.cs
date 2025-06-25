using UnityEngine;

public static class GameConstants
{
    // Player Settings
    public const float DEFAULT_WALK_SPEED = 3f;
    public const float DEFAULT_RUN_SPEED = 5.5f;
    public const float DEFAULT_JUMP_FORCE = 8f;
    
    // Network Settings
    public const int MAX_PLAYERS = 4;
    public const float NETWORK_SEND_RATE = 30f;
    
    // Input Names
    public const string HORIZONTAL_INPUT = "Horizontal";
    public const string VERTICAL_INPUT = "Vertical";
    public const string JUMP_INPUT = "Jump";
    public const string RUN_INPUT = "Run";
    
    // Tags
    public const string PLAYER_TAG = "Player";
    public const string GROUND_TAG = "Ground";
    public const string ENEMY_TAG = "Enemy";
    
    // Layers
    public const int GROUND_LAYER = 0;
    public const int PLAYER_LAYER = 8;
    public const int ENEMY_LAYER = 9;
}