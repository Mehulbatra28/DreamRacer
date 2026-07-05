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

            // If you have assigned spawn points in the inspector, use them!
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // Wrap around safely if there are more players than spawn points
                int spawnIndex = player.PlayerId % spawnPoints.Length;
                spawnPosition = spawnPoints[spawnIndex].position;
                spawnRotation = spawnPoints[spawnIndex].rotation;
            }

            NetworkObject networkCar = runner.Spawn(carPrefab, spawnPosition, spawnRotation, player);
            spawnedCars.Add(player, networkCar);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedCars.TryGetValue(player, out NetworkObject networkCar))
        {
            if (networkCar != null && networkCar.HasStateAuthority)
            {
                runner.Despawn(networkCar);
            }
            spawnedCars.Remove(player);
        }
    }

    #region Unused Callbacks
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
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
