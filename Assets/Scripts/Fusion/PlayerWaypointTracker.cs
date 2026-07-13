using UnityEngine;
using Fusion;

public class PlayerWaypointTracker : NetworkBehaviour
{
    [Networked]
    public Vector3 GlobalWaypoint { get; set; }

    [Networked]
    public NetworkBool IsGlobalWaypointActive { get; set; }
    // Optional: a 3D marker in the game world
    public GameObject globalWaypoint3DPrefab;
    private GameObject current3DMarker;

    private ChangeDetector _changeDetector;

    // ─── Race State (Networked) ───────────────────────────────────
    [Networked] public NetworkBool HasAcceptedRace { get; set; }
    [Networked] public NetworkBool HasRejectedRace { get; set; }
    [Networked] public NetworkBool IsRacing { get; set; }
    [Networked] public NetworkBool HasFinished { get; set; }
    [Networked] public float RaceTime { get; set; }
    [Networked] public float RaceProgress { get; set; }
    [Networked] public int FinishPosition { get; set; }
    [Networked] public int NextCheckpointIndex { get; set; }
    [Networked] public NetworkString<_32> PlayerDisplayName { get; set; }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotTo, false);

        // Set a default display name based on PlayerRef
        if (HasStateAuthority)
        {
            PlayerDisplayName = "Player " + Object.InputAuthority.PlayerId;
        }
    }

    public void SetGlobalWaypoint(Vector3 position)
    {
        if (HasStateAuthority || Runner.Topology == Topologies.Shared)
        {
            GlobalWaypoint = position;
            IsGlobalWaypointActive = true;
            RpcProposeRace(position);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcProposeRace(Vector3 destination)
    {
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceProposed(destination, this);
        }
    }
    
    public void ClearGlobalWaypoint()
    {
         if (HasStateAuthority || Runner.Topology == Topologies.Shared)
         {
             IsGlobalWaypointActive = false;
         }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcClearWaypoint()
    {
        IsGlobalWaypointActive = false;
    }

    // ─── Race RPCs ────────────────────────────────────────────────

    /// <summary>
    /// Called by the local player to accept the race invitation.
    /// In Shared Mode, only the state authority (the owner) can write to networked props.
    /// </summary>
    public void AcceptRace()
    {
        if (HasStateAuthority)
        {
            HasAcceptedRace = true;
            HasRejectedRace = false;
            Debug.Log($"[PlayerWaypointTracker] Player {Object.InputAuthority.PlayerId} ACCEPTED race.");
        }
    }

    /// <summary>
    /// Called by the local player to reject the race invitation.
    /// </summary>
    public void RejectRace()
    {
        if (HasStateAuthority)
        {
            HasRejectedRace = true;
            HasAcceptedRace = false;
            Debug.Log($"[PlayerWaypointTracker] Player {Object.InputAuthority.PlayerId} REJECTED race.");
        }
    }

    /// <summary>
    /// Resets all race-related networked state for a new race.
    /// </summary>
    public void ResetRaceState()
    {
        if (HasStateAuthority)
        {
            HasAcceptedRace = false;
            HasRejectedRace = false;
            IsRacing = false;
            HasFinished = false;
            RaceTime = 0f;
            RaceProgress = 0f;
            FinishPosition = 0;
            NextCheckpointIndex = 0;
            Debug.Log($"[PlayerWaypointTracker] Player {Object.InputAuthority.PlayerId} race state RESET.");
        }
    }

    /// <summary>
    /// Updates the local player's race progress and time each frame during a race.
    /// Called by RaceManager on the local player's tracker only.
    /// </summary>
    public void UpdateRaceProgress(float progress, float time)
    {
        if (HasStateAuthority)
        {
            RaceProgress = progress;
            RaceTime = time;
        }
    }

    /// <summary>
    /// Marks this player as finished the race at a given position.
    /// </summary>
    public void FinishRace(int position, float finalTime)
    {
        if (HasStateAuthority)
        {
            HasFinished = true;
            IsRacing = false;
            FinishPosition = position;
            RaceTime = finalTime;
            Debug.Log($"[PlayerWaypointTracker] Player {Object.InputAuthority.PlayerId} FINISHED at position {position}, time: {finalTime:F2}s");
        }
    }

    /// <summary>
    /// Marks this player as actively racing (called after countdown).
    /// </summary>
    public void StartRacing()
    {
        if (HasStateAuthority)
        {
            IsRacing = true;
            RaceTime = 0f;
            RaceProgress = 0f;
            NextCheckpointIndex = 0;
            Debug.Log($"[PlayerWaypointTracker] Player {Object.InputAuthority.PlayerId} STARTED racing.");
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(GlobalWaypoint) || change == nameof(IsGlobalWaypointActive))
            {
                UpdateGlobalWaypointMarker();
            }
        }
    }

    private void UpdateGlobalWaypointMarker()
    {
        if (IsGlobalWaypointActive)
        {
            // 3. Spawn/Move 3D World Marker
            if (globalWaypoint3DPrefab != null)
            {
                if (current3DMarker == null)
                {
                    current3DMarker = Instantiate(globalWaypoint3DPrefab, GlobalWaypoint, Quaternion.identity);
                }
                else
                {
                    current3DMarker.transform.position = GlobalWaypoint;
                    current3DMarker.SetActive(true);
                }
            }
        }
        else
        {
            // Hide marker
            if (current3DMarker != null)
            {
                current3DMarker.SetActive(false);
            }
        }
    }
}
