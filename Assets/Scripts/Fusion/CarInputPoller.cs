using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarInputPoller : MonoBehaviour, INetworkRunnerCallbacks
{
    private CarControls controls;

    private void Awake()
    {
        controls = new CarControls();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            runner.AddCallbacks(this);
        }
    }

    private void OnDestroy()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            runner.RemoveCallbacks(this);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var myInput = new NetworkCarInput();

        myInput.Steering = controls.Driving.Steer.ReadValue<float>();
        
        myInput.Buttons.Set(NetworkCarInput.ACCELERATE, controls.Driving.Accelerate.IsPressed());
        myInput.Buttons.Set(NetworkCarInput.REVERSE, controls.Driving.Reverse.IsPressed());
        myInput.Buttons.Set(NetworkCarInput.BRAKE, controls.Driving.HandBrake.IsPressed());
        myInput.Buttons.Set(NetworkCarInput.SHIFT_UP, controls.Driving.ShiftUp.IsPressed());
        myInput.Buttons.Set(NetworkCarInput.SHIFT_DOWN, controls.Driving.ShiftDown.IsPressed());
        myInput.Buttons.Set(NetworkCarInput.CLUTCH, controls.Driving.Clutch.IsPressed());

        input.Set(myInput);
    }

    #region Unused Callbacks
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
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
