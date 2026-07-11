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

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotTo, false);
    }

    public void SetGlobalWaypoint(Vector3 position)
    {
        if (HasStateAuthority || Runner.Topology == Topologies.Shared)
        {
            GlobalWaypoint = position;
            IsGlobalWaypointActive = true;
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
