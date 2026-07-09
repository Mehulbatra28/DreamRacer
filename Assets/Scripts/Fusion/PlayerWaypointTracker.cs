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
            // 1. Tell WorldMapController to show it on the map (for other players)
            if (!HasInputAuthority && WorldMapController.Instance != null)
            {
                WorldMapController.Instance.DisplayOtherPlayerGlobalWaypoint(GlobalWaypoint, Object.Id.ToString());
            }

            // 2. Spawn/Move 3D World Marker
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
            // Hide markers
            if (current3DMarker != null)
            {
                current3DMarker.SetActive(false);
            }
        }
    }
}
