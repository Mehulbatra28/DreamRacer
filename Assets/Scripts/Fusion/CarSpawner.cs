using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class CarSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef carPrefab;
    [SerializeField] private Transform[] spawnPoints; // Assign custom spawn locations in the Inspector
    private Dictionary<PlayerRef, NetworkObject> spawnedCars = new Dictionary<PlayerRef, NetworkObject>();
    
    private Transform localCarTransform;

    private void Start()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            runner.AddCallbacks(this);
            if (runner.IsServer)
            {
                foreach (var player in runner.ActivePlayers)
                {
                    if (!spawnedCars.ContainsKey(player))
                    {
                        OnPlayerJoined(runner, player);
                    }
                }
            }
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            // Default fallback position
            Vector3 spawnPosition = new Vector3(player.PlayerId * 5f, 2f, 0f); 
            Quaternion spawnRotation = Quaternion.identity;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // ALWAYS use assigned spawn points first (ensures unique positions in multiplayer)
                int spawnIndex = player.PlayerId % spawnPoints.Length;
                spawnPosition = spawnPoints[spawnIndex].position;
                spawnRotation = spawnPoints[spawnIndex].rotation;
                Debug.Log($"[CarSpawner] Using spawn point {spawnIndex} for player {player.PlayerId}");
            }
            else if (PlayerPrefs.HasKey("LastPosX"))
            {
                // No spawn points assigned — try to restore last saved position (single-player fallback)
                Vector3 savedPos = new Vector3(
                    PlayerPrefs.GetFloat("LastPosX"),
                    PlayerPrefs.GetFloat("LastPosY"),
                    PlayerPrefs.GetFloat("LastPosZ")
                );
                
                Quaternion savedRot = new Quaternion(
                    PlayerPrefs.GetFloat("LastRotX"),
                    PlayerPrefs.GetFloat("LastRotY"),
                    PlayerPrefs.GetFloat("LastRotZ"),
                    PlayerPrefs.GetFloat("LastRotW")
                );

                // Check if the car is upside down. 
                // We calculate the car's "Up" direction based on its rotation and compare it to the world's "Up".
                // If the dot product is greater than 0, it means it's mostly upright.
                if (Vector3.Dot(savedRot * Vector3.up, Vector3.up) > 0f)
                {
                    spawnPosition = savedPos;
                    spawnRotation = savedRot;
                    Debug.Log("[CarSpawner] Using saved position (no spawn points assigned).");
                }
                else
                {
                    Debug.Log("[CarSpawner] Saved position was upside down! Using default spawn.");
                }
            }

            NetworkObject networkCar = runner.Spawn(carPrefab, spawnPosition, spawnRotation, player);
            spawnedCars.Add(player, networkCar);
            localCarTransform = networkCar.transform;
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            SaveLocalCarPosition();
        }
        
        if (spawnedCars.TryGetValue(player, out NetworkObject networkCar))
        {
            if (networkCar != null && networkCar.HasStateAuthority)
            {
                runner.Despawn(networkCar);
            }
            spawnedCars.Remove(player);
        }
    }

    private void OnApplicationQuit()
    {
        SaveLocalCarPosition();
    }

    private void SaveLocalCarPosition()
    {
        if (localCarTransform != null)
        {
            PlayerPrefs.SetFloat("LastPosX", localCarTransform.position.x);
            PlayerPrefs.SetFloat("LastPosY", localCarTransform.position.y);
            PlayerPrefs.SetFloat("LastPosZ", localCarTransform.position.z);

            PlayerPrefs.SetFloat("LastRotX", localCarTransform.rotation.x);
            PlayerPrefs.SetFloat("LastRotY", localCarTransform.rotation.y);
            PlayerPrefs.SetFloat("LastRotZ", localCarTransform.rotation.z);
            PlayerPrefs.SetFloat("LastRotW", localCarTransform.rotation.w);
            PlayerPrefs.Save();
        }
    }

    #region Unused Callbacks
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) 
    { 
        SaveLocalCarPosition();
    }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) 
    { 
        SaveLocalCarPosition();
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    #endregion
}
