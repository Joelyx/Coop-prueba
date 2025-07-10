using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class SubmarineInterior : MonoBehaviour
{
    [Header("Interior Settings")]
    public Transform submarineTransform;
    public bool debugMode = false;
    
    [Header("Movement Sync")]
    public bool syncVelocity = true;
    public float velocitySyncStrength = 1f;
    
    private HashSet<PlayerMovement> playersInside = new HashSet<PlayerMovement>();
    private Vector3 lastSubmarinePosition;
    private Quaternion lastSubmarineRotation;
    private Vector3 submarineVelocity;
    private Rigidbody submarineRigidbody;
    
    private void Start()
    {
        if (submarineTransform == null)
            submarineTransform = transform.parent;
            
        if (submarineTransform != null)
        {
            lastSubmarinePosition = submarineTransform.position;
            lastSubmarineRotation = submarineTransform.rotation;
            submarineRigidbody = submarineTransform.GetComponent<Rigidbody>();
        }
        
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;
        
        if (debugMode)
            Debug.Log("[SUBMARINE INTERIOR] Initialized");
    }
    
    private void FixedUpdate()
    {
        if (submarineTransform != null && playersInside.Count > 0)
        {
            UpdateSubmarineVelocity();
            SyncPlayersWithSubmarine();
        }
    }
    
    private void UpdateSubmarineVelocity()
    {
        if (submarineRigidbody != null)
        {
            submarineVelocity = submarineRigidbody.linearVelocity;
        }
        else
        {
            // Calcular velocidad basada en cambio de posición
            Vector3 positionDelta = submarineTransform.position - lastSubmarinePosition;
            submarineVelocity = positionDelta / Time.fixedDeltaTime;
        }
    }
    
    private void SyncPlayersWithSubmarine()
    {
        Vector3 positionDelta = submarineTransform.position - lastSubmarinePosition;
        Quaternion rotationDelta = submarineTransform.rotation * Quaternion.Inverse(lastSubmarineRotation);
        
        foreach (var player in playersInside)
        {
            if (player != null && player.gameObject.activeInHierarchy)
            {
                Rigidbody playerRb = player.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    // Método 1: Sincronizar velocidad (permite movimiento relativo)
                    if (syncVelocity)
                    {
                        SyncPlayerVelocity(playerRb, player);
                    }
                    // Método 2: Mover directamente (más rígido)
                    else
                    {
                        MovePlayerDirectly(playerRb, player, positionDelta, rotationDelta);
                    }
                }
            }
        }
        
        lastSubmarinePosition = submarineTransform.position;
        lastSubmarineRotation = submarineTransform.rotation;
    }
    
    private void SyncPlayerVelocity(Rigidbody playerRb, PlayerMovement player)
    {
        // Obtener la velocidad actual del jugador en espacio local del submarino
        Vector3 playerLocalVelocity = submarineTransform.InverseTransformDirection(playerRb.linearVelocity);
        Vector3 submarineLocalVelocity = submarineTransform.InverseTransformDirection(submarineVelocity);
        
        // La velocidad del jugador debería ser: velocidad del submarino + movimiento relativo
        Vector3 playerRelativeVelocity = playerLocalVelocity - submarineLocalVelocity;
        
        // Aplicar la velocidad del submarino como base
        Vector3 targetVelocity = submarineVelocity + submarineTransform.TransformDirection(playerRelativeVelocity);
        
        // Aplicar con suavizado
        Vector3 velocityCorrection = (targetVelocity - playerRb.linearVelocity) * velocitySyncStrength;
        playerRb.AddForce(velocityCorrection, ForceMode.Acceleration);
        
        if (debugMode && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[SUBMARINE INTERIOR] Player {player.name} - Sub vel: {submarineVelocity.magnitude:F2}, Player vel: {playerRb.linearVelocity.magnitude:F2}");
        }
    }
    
    private void MovePlayerDirectly(Rigidbody playerRb, PlayerMovement player, Vector3 positionDelta, Quaternion rotationDelta)
    {
        Vector3 relativePosition = player.transform.position - lastSubmarinePosition;
        Vector3 rotatedPosition = rotationDelta * relativePosition;
        Vector3 newPosition = submarineTransform.position + rotatedPosition;
        
        Vector3 movementVector = newPosition - player.transform.position;
        
        if (movementVector.magnitude > 0.001f)
        {
            playerRb.MovePosition(player.transform.position + movementVector);
            player.transform.rotation = rotationDelta * player.transform.rotation;
            
            if (debugMode)
                Debug.Log($"[SUBMARINE INTERIOR] Moving player {player.name} by {movementVector}");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            playersInside.Add(player);
            
            // Notificar al sistema de sincronización del jugador
            var playerSync = player.GetComponent<PlayerSubmarineSync>();
            if (playerSync != null)
            {
                playerSync.EnterSubmarine(this);
            }
            
            if (debugMode)
                Debug.Log($"[SUBMARINE INTERIOR] Player {player.name} entered submarine");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            playersInside.Remove(player);
            
            // Notificar al sistema de sincronización del jugador
            var playerSync = player.GetComponent<PlayerSubmarineSync>();
            if (playerSync != null)
            {
                playerSync.ExitSubmarine();
            }
            
            if (debugMode)
                Debug.Log($"[SUBMARINE INTERIOR] Player {player.name} exited submarine");
        }
    }
    
    public bool IsPlayerInside(PlayerMovement player)
    {
        return playersInside.Contains(player);
    }
    
    public int GetPlayerCount()
    {
        return playersInside.Count;
    }
    
    public HashSet<PlayerMovement> GetPlayersInside()
    {
        return new HashSet<PlayerMovement>(playersInside);
    }
}