using System.Collections.Generic;
using UnityEngine;
using Fusion;

/// <summary>
/// Orchestrates the full race lifecycle: prompt → accept/reject → countdown → racing → finish → leaderboard.
/// Attach to a persistent "Manager" GameObject in the scene.
/// 
/// In Fusion Shared Mode, each client runs this MonoBehaviour locally.
/// Race state is decentralized: each player's PlayerWaypointTracker owns their own networked state.
/// RaceManager reads all trackers to determine the current phase.
/// </summary>
public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    public enum RacePhase
    {
        None,               // No race active
        WaitingForAccept,   // Global waypoint set, showing accept/reject prompt
        Countdown,          // All accepted, 3-2-1-GO countdown
        Racing,             // Race in progress
        Finished            // All players finished, showing leaderboard
    }

    [Header("Race Settings")]
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private float checkpointSpacing = 50f; // Distance between checkpoints in meters
    [SerializeField] private float finishRadius = 15f; // How close to the destination = finished

    [Header("Checkpoint")]
    [SerializeField] private GameObject checkpointPrefab; // Assign your checkpoint prefab in Inspector

    [Header("Spawn Positioning")]
    [SerializeField] private float spawnLateralOffset = 4f; // Offset between cars side-by-side

    // Current state
    public RacePhase CurrentPhase { get; private set; } = RacePhase.None;

    // Race data
    private Vector3 raceDestination;
    private float countdownTimeRemaining;
    private float raceElapsedTime;
    private int finishedPlayerCount;
    private List<Vector3> startPositions = new List<Vector3>();
    private List<Quaternion> startRotations = new List<Quaternion>();
    private bool carsAreFrozen = false;
    private bool pendingRacePrompt = false;

    // Spawned checkpoint GameObjects (so we can clean them up)
    private List<GameObject> spawnedCheckpoints = new List<GameObject>();

    // Total checkpoint count for the current race
    private int totalCheckpoints;

    // Cached references
    private PlayerWaypointTracker localTracker;
    private List<PlayerWaypointTracker> allTrackers = new List<PlayerWaypointTracker>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        RefreshTrackers();

        // If a waypoint was set, wait until the player actually closes the map to show the prompt
        if (pendingRacePrompt)
        {
            if (WorldMapController.Instance == null || !WorldMapController.Instance.mapCanvas.activeInHierarchy)
            {
                pendingRacePrompt = false;
                StartWaitingPhase();
            }
        }

        switch (CurrentPhase)
        {
            case RacePhase.None:
                // We rely strictly on OnGlobalWaypointSet now so the map delay works
                break;

            case RacePhase.WaitingForAccept:
                UpdateWaitingPhase();
                break;

            case RacePhase.Countdown:
                UpdateCountdownPhase();
                break;

            case RacePhase.Racing:
                UpdateRacingPhase();
                break;

            case RacePhase.Finished:
                // Leaderboard is shown, waiting for UI button presses
                break;
        }
    }

    // ─── Public API ───────────────────────────────────────────────

    /// <summary>
    /// Called via RPC when any player sets a global waypoint.
    /// </summary>
    public void OnRaceProposed(Vector3 destination, PlayerWaypointTracker proposer)
    {
        if (CurrentPhase != RacePhase.None) return; // Don't interrupt an active race

        raceDestination = destination;
        
        if (proposer.HasStateAuthority)
        {
            // We set the waypoint, wait for our map to close
            pendingRacePrompt = true; 
        }
        else
        {
            // Someone else set the waypoint, start immediately
            StartWaitingPhase();
        }
    }

    /// <summary>
    /// Called by RaceCheckpoint trigger when a player reaches a checkpoint.
    /// </summary>
    public void OnCheckpointReached(PlayerWaypointTracker tracker, int checkpointIndex, bool isFinishLine)
    {
        if (CurrentPhase != RacePhase.Racing) return;
        if (!tracker.HasStateAuthority) return; // Only process for local player

        // Check that the checkpoint is the next expected one (prevent skipping)
        if (checkpointIndex != tracker.NextCheckpointIndex) return;

        Debug.Log($"[RaceManager] Player {tracker.Object.InputAuthority.PlayerId} reached checkpoint {checkpointIndex}/{totalCheckpoints}");

        // Advance to next checkpoint
        tracker.NextCheckpointIndex = checkpointIndex + 1;

        // Update progress based on checkpoints passed
        float progress = (float)(checkpointIndex + 1) / totalCheckpoints;
        tracker.UpdateRaceProgress(Mathf.Clamp01(progress), raceElapsedTime);

        // Check if this was the finish line
        if (isFinishLine)
        {
            finishedPlayerCount++;
            tracker.FinishRace(finishedPlayerCount, raceElapsedTime);

            Debug.Log($"[RaceManager] Player finished! Total finished: {finishedPlayerCount}/{allTrackers.Count}");

            // Check if all players have finished
            CheckAllFinished();
        }
    }

    /// <summary>
    /// Called by UIManager when "Continue" is pressed.
    /// </summary>
    public void OnContinuePressed()
    {
        CleanupRace();
        CurrentPhase = RacePhase.None;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideAllRacePanels();
        }
    }

    /// <summary>
    /// Called by UIManager when "Challenge Again" is pressed.
    /// Uses the same destination as the previous race.
    /// </summary>
    public void OnChallengeAgainPressed()
    {
        Vector3 savedDestination = raceDestination;
        CleanupRace();
        CurrentPhase = RacePhase.None;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideAllRacePanels();
        }

        // Restart the race flow with the same destination
        raceDestination = savedDestination;
        StartWaitingPhase();
    }

    // ─── Phase Management ─────────────────────────────────────────

    private void StartWaitingPhase()
    {
        CurrentPhase = RacePhase.WaitingForAccept;

        // Reset all players' race state
        if (localTracker != null)
        {
            localTracker.ResetRaceState();
        }

        Debug.Log("[RaceManager] Phase → WaitingForAccept");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowRacePanel();
            UIManager.Instance.UpdateRacePanelStatus("Waiting for players...");
        }
    }

    private void UpdateWaitingPhase()
    {
        if (allTrackers.Count == 0) return;

        int acceptedCount = 0;
        bool anyRejected = false;

        foreach (var tracker in allTrackers)
        {
            if (tracker.HasAcceptedRace) acceptedCount++;
            if (tracker.HasRejectedRace) anyRejected = true;
        }

        // Update status text
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRacePanelStatus($"{acceptedCount}/{allTrackers.Count} players accepted");
        }

        // If anyone rejected, cancel the race
        if (anyRejected)
        {
            Debug.Log("[RaceManager] A player rejected. Cancelling race.");
            CleanupRace();
            CurrentPhase = RacePhase.None;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateRacePanelStatus("Race cancelled!");
                // Hide panel after a short delay
                Invoke(nameof(HideRacePanelDelayed), 2f);
            }
            return;
        }

        // If all players accepted (minimum 1 for testing, normally 2), start countdown
        if (acceptedCount >= allTrackers.Count && allTrackers.Count >= 1)
        {
            StartCountdownPhase();
        }
    }

    private void HideRacePanelDelayed()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideAllRacePanels();
        }
    }

    private void StartCountdownPhase()
    {
        CurrentPhase = RacePhase.Countdown;
        countdownTimeRemaining = countdownDuration;

        Debug.Log("[RaceManager] Phase → Countdown");

        // Immediately update UI to prevent it from getting stuck if an error occurs below
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideRacePanel();
            UIManager.Instance.ShowRaceUI();
        }

        try
        {
            // Calculate start positions near the race destination
            CalculateStartPositions();

            // Spawn checkpoints along the route
            SpawnCheckpoints();

            // Freeze all cars and teleport them to start positions
            FreezeAndPositionCars();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RaceManager] Error during StartCountdownPhase setup: {e}");
        }
    }

    private void UpdateCountdownPhase()
    {
        countdownTimeRemaining -= Time.deltaTime;

        int displayCount = Mathf.CeilToInt(countdownTimeRemaining);

        if (UIManager.Instance != null)
        {
            if (countdownTimeRemaining > 0)
            {
                UIManager.Instance.UpdateCountdown(displayCount.ToString());
            }
            else
            {
                UIManager.Instance.UpdateCountdown("GO!");
            }
        }

        if (countdownTimeRemaining <= 0)
        {
            StartRacingPhase();
        }
    }

    private void StartRacingPhase()
    {
        CurrentPhase = RacePhase.Racing;
        raceElapsedTime = 0f;
        finishedPlayerCount = 0;

        // Unfreeze all cars
        UnfreezeCars();

        // Mark local player as racing
        if (localTracker != null)
        {
            localTracker.StartRacing();
        }

        Debug.Log("[RaceManager] Phase → Racing");

        // Hide countdown text after a short delay
        Invoke(nameof(ClearCountdownText), 1f);
    }

    private void ClearCountdownText()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCountdown("");
        }
    }

    private void UpdateRacingPhase()
    {
        raceElapsedTime += Time.deltaTime;

        // Update local player's race data
        if (localTracker != null && localTracker.IsRacing)
        {
            // Progress is based on checkpoints — updated via OnCheckpointReached
            // But we still update time every frame
            localTracker.UpdateRaceProgress(localTracker.RaceProgress, raceElapsedTime);
        }

        // Calculate position for all players based on progress
        UpdatePlayerPositions();

        // Update race UI
        if (UIManager.Instance != null && localTracker != null)
        {
            int position = GetLocalPlayerPosition();
            UIManager.Instance.UpdateRaceHUD(
                position,
                raceElapsedTime,
                localTracker.RaceProgress
            );
        }

        // Check if all finished
        CheckAllFinished();
    }

    private void CheckAllFinished()
    {
        int finishedCount = 0;
        foreach (var tracker in allTrackers)
        {
            if (tracker.HasFinished) finishedCount++;
        }

        if (finishedCount >= allTrackers.Count && allTrackers.Count >= 1)
        {
            CurrentPhase = RacePhase.Finished;
            Debug.Log("[RaceManager] Phase → Finished (all players done)");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideRaceUI();
                UIManager.Instance.ShowLeaderboard(allTrackers);
            }
        }
    }

    // ─── Car Positioning & Freezing ───────────────────────────────

    private void CalculateStartPositions()
    {
        startPositions.Clear();
        startRotations.Clear();

        if (allTrackers.Count == 0) return;

        // Calculate a start line perpendicular to the direction of the destination
        // Use the average position of all players as the start area
        Vector3 avgPosition = Vector3.zero;
        foreach (var tracker in allTrackers)
        {
            avgPosition += tracker.transform.position;
        }
        avgPosition /= allTrackers.Count;

        // Direction from avg position to destination
        Vector3 toDestination = (raceDestination - avgPosition).normalized;
        toDestination.y = 0; // Keep on ground plane
        if (toDestination.sqrMagnitude < 0.01f) toDestination = Vector3.forward;
        toDestination.Normalize();

        // Perpendicular direction for side-by-side positioning
        Vector3 rightDir = Vector3.Cross(Vector3.up, toDestination).normalized;

        // Center the cars around the average position
        float totalWidth = (allTrackers.Count - 1) * spawnLateralOffset;
        Vector3 startOffset = -rightDir * (totalWidth * 0.5f);

        for (int i = 0; i < allTrackers.Count; i++)
        {
            Vector3 pos = avgPosition + startOffset + rightDir * (i * spawnLateralOffset);
            pos.y = allTrackers[i].transform.position.y; // Keep original height
            startPositions.Add(pos);

            Quaternion rot = Quaternion.LookRotation(toDestination, Vector3.up);
            startRotations.Add(rot);
        }
    }

    private void FreezeAndPositionCars()
    {
        carsAreFrozen = true;

        for (int i = 0; i < allTrackers.Count; i++)
        {
            var tracker = allTrackers[i];

            // Only teleport the local player's car (Shared Mode — each client controls their own)
            if (tracker.HasStateAuthority && i < startPositions.Count)
            {
                // Teleport car to start position
                Rigidbody rb = tracker.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                tracker.transform.position = startPositions[i];
                tracker.transform.rotation = startRotations[i];

                // Freeze the car controller
                PrometeoCarController carController = tracker.GetComponent<PrometeoCarController>();
                if (carController != null)
                {
                    carController.SetFrozen(true);
                }
            }
        }
    }

    private void UnfreezeCars()
    {
        carsAreFrozen = false;

        foreach (var tracker in allTrackers)
        {
            if (tracker.HasStateAuthority)
            {
                PrometeoCarController carController = tracker.GetComponent<PrometeoCarController>();
                if (carController != null)
                {
                    carController.SetFrozen(false);
                }
            }
        }
    }

    // ─── Checkpoint Spawning ──────────────────────────────────────

    private void SpawnCheckpoints()
    {
        ClearCheckpoints();

        if (checkpointPrefab == null)
        {
            Debug.LogWarning("[RaceManager] No checkpoint prefab assigned! Using distance-based finish only.");
            totalCheckpoints = 1; // Just a finish line
            return;
        }

        // Calculate the average start position
        Vector3 avgStart = Vector3.zero;
        foreach (var pos in startPositions)
        {
            avgStart += pos;
        }
        if (startPositions.Count > 0) avgStart /= startPositions.Count;

        // Spawn checkpoints between start and destination
        Vector3 direction = raceDestination - avgStart;
        float totalDistance = direction.magnitude;
        direction.Normalize();

        // How many checkpoints to place
        int checkpointCount = Mathf.Max(1, Mathf.FloorToInt(totalDistance / checkpointSpacing));
        totalCheckpoints = checkpointCount;

        for (int i = 0; i < checkpointCount; i++)
        {
            float t = (float)(i + 1) / checkpointCount; // 0 to 1, where 1 = destination
            Vector3 cpPosition = avgStart + direction * (totalDistance * t);
            cpPosition.y = avgStart.y; // Keep checkpoint at ground level

            GameObject cp = Instantiate(checkpointPrefab, cpPosition, Quaternion.LookRotation(direction));
            
            RaceCheckpoint raceCP = cp.GetComponent<RaceCheckpoint>();
            if (raceCP == null) raceCP = cp.AddComponent<RaceCheckpoint>();

            raceCP.checkpointIndex = i;
            raceCP.isFinishLine = (i == checkpointCount - 1);

            // Ensure the checkpoint has a trigger collider
            Collider col = cp.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            spawnedCheckpoints.Add(cp);

            Debug.Log($"[RaceManager] Spawned checkpoint {i} at {cpPosition}" + (raceCP.isFinishLine ? " (FINISH LINE)" : ""));
        }
    }

    private void ClearCheckpoints()
    {
        foreach (var cp in spawnedCheckpoints)
        {
            if (cp != null) Destroy(cp);
        }
        spawnedCheckpoints.Clear();
    }

    // ─── Position Calculation ─────────────────────────────────────

    private int GetLocalPlayerPosition()
    {
        if (localTracker == null) return 1;

        int position = 1;
        float localProgress = localTracker.RaceProgress;
        int localCheckpoint = localTracker.NextCheckpointIndex;

        foreach (var tracker in allTrackers)
        {
            if (tracker == localTracker) continue;

            // Higher checkpoint = further ahead
            // If same checkpoint, compare distance to next checkpoint
            if (tracker.NextCheckpointIndex > localCheckpoint)
            {
                position++;
            }
            else if (tracker.NextCheckpointIndex == localCheckpoint && tracker.RaceProgress > localProgress)
            {
                position++;
            }
        }

        return position;
    }

    private void UpdatePlayerPositions()
    {
        // Sort trackers by progress to determine positions
        // This is used for the race UI "Position" display
        // Each frame, update the local player's position based on comparative progress
    }

    // ─── Tracker Management ───────────────────────────────────────

    private void RefreshTrackers()
    {
        // Periodically refresh the list of all PlayerWaypointTrackers in the scene
        // This handles late joiners and disconnects
        allTrackers.Clear();
        localTracker = null;

        PlayerWaypointTracker[] trackers = FindObjectsOfType<PlayerWaypointTracker>();
        foreach (var tracker in trackers)
        {
            if (tracker.Object == null || !tracker.Object.IsValid) continue;
            allTrackers.Add(tracker);

            if (tracker.HasStateAuthority)
            {
                localTracker = tracker;
            }
        }
    }

    // ─── Cleanup ──────────────────────────────────────────────────

    private void CleanupRace()
    {
        ClearCheckpoints();

        // Reset local player's race state
        if (localTracker != null)
        {
            localTracker.ResetRaceState();
        }

        // Ensure cars are unfrozen
        if (carsAreFrozen)
        {
            UnfreezeCars();
        }

        finishedPlayerCount = 0;
        raceElapsedTime = 0f;
    }
}
