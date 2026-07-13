using UnityEngine;

/// <summary>
/// Attach this to a checkpoint GameObject with a trigger collider.
/// The RaceManager spawns checkpoints along the route and assigns their index.
/// When a car drives through, RaceManager is notified.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RaceCheckpoint : MonoBehaviour
{
    /// <summary>
    /// The index of this checkpoint in the race route (0 = first, N-1 = finish line).
    /// Set by RaceManager when spawning.
    /// </summary>
    [HideInInspector] public int checkpointIndex;

    /// <summary>
    /// Whether this is the final checkpoint (finish line).
    /// Set by RaceManager when spawning.
    /// </summary>
    [HideInInspector] public bool isFinishLine;

    private void OnTriggerEnter(Collider other)
    {
        // Only process cars that have a PlayerWaypointTracker (networked players)
        PlayerWaypointTracker tracker = other.GetComponentInParent<PlayerWaypointTracker>();
        if (tracker == null) return;

        // Only the local player should report checkpoint hits
        if (!tracker.HasStateAuthority) return;

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnCheckpointReached(tracker, checkpointIndex, isFinishLine);
        }
    }
}
