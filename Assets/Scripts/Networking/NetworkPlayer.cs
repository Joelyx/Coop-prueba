using UnityEngine;

// Placeholder para sistema de networking
public class NetworkPlayer : MonoBehaviour
{
    [Header("Network Settings")]
    public bool isLocalPlayer = true;
    public int playerId = 0;
    
    [Header("Sync Settings")]
    public float sendRate = 30f;
    public float interpolationTime = 0.1f;
    
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    
    private void Start()
    {
        if (isLocalPlayer)
        {
            // Enable local player controls
            GetComponent<FirstPersonController>().enabled = true;
        }
        else
        {
            // Disable controls for remote players
            GetComponent<FirstPersonController>().enabled = false;
        }
    }
    
    private void Update()
    {
        if (!isLocalPlayer)
        {
            // Interpolate position for remote players
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * sendRate);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * sendRate);
        }
    }
    
    public void SetNetworkData(Vector3 position, Quaternion rotation)
    {
        networkPosition = position;
        networkRotation = rotation;
    }
}