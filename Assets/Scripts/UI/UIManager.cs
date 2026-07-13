using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Singleton instance
    public static UIManager Instance { get; private set; }

    [Header("Speed")]
    public TMP_Text speedText;

    [Header("Gear")]
    public TMP_Text gearText;
    public TMP_Text transmissionModeText;

    [Header("RPM")]
    public TMP_Text rpmText;
    public Image rpmBar;           // Fill image for RPM gauge (Image type = Filled)

    [Header("Clutch (Manual Mode Only)")]
    public TMP_Text clutchText;
    public Image clutchBar;        // Fill image for clutch indicator

    // ─── Race Panel (Accept/Reject Prompt) ────────────────────────
    [Header("Race Panel (Accept/Reject)")]
    [SerializeField] private GameObject racePanel;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private TMP_Text racePanelStatusText;

    // ─── Race UI (In-Race HUD) ────────────────────────────────────
    [Header("Race UI (In-Race HUD)")]
    [SerializeField] private GameObject raceUI;
    [SerializeField] private TMP_Text racePositionText;
    [SerializeField] private TMP_Text raceTimeText;
    [SerializeField] private TMP_Text raceProgressText;
    [SerializeField] private TMP_Text countDownText;

    // ─── Leaderboard UI ───────────────────────────────────────────
    [Header("Leaderboard UI")]
    [SerializeField] private GameObject leaderBoardUI;
    [SerializeField] private Transform leaderBoardPanel; // Parent for leaderboard entries
    [SerializeField] private Button continueButton;
    [SerializeField] private Button challengeAgainButton;

    // ─── Leaderboard Entry References (for up to 4 players) ──────
    [Header("Leaderboard Entry")]
    [SerializeField] private GameObject leaderboardEntryPrefab;

    [Header("Input System (Assign in Inspector)")]
    public UnityEngine.InputSystem.InputActionReference acceptAction;
    public UnityEngine.InputSystem.InputActionReference rejectAction;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        if (acceptAction != null)
        {
            acceptAction.action.performed += OnAcceptPerformed;
            acceptAction.action.Enable();
        }
        
        if (rejectAction != null)
        {
            rejectAction.action.performed += OnRejectPerformed;
            rejectAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (acceptAction != null)
        {
            acceptAction.action.performed -= OnAcceptPerformed;
            acceptAction.action.Disable();
        }
        
        if (rejectAction != null)
        {
            rejectAction.action.performed -= OnRejectPerformed;
            rejectAction.action.Disable();
        }
    }

    private void OnAcceptPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        OnAcceptClicked();
    }

    private void OnRejectPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        OnRejectClicked();
    }

    void Start()
    {
        // Wire up race button click handlers
        if (acceptButton != null)
            acceptButton.onClick.AddListener(OnAcceptClicked);

        if (rejectButton != null)
            rejectButton.onClick.AddListener(OnRejectClicked);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        if (challengeAgainButton != null)
            challengeAgainButton.onClick.AddListener(OnChallengeAgainClicked);

        // Ensure all race panels start hidden
        HideAllRacePanels();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CAR HUD
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Called every frame by PrometeoCarController to update the HUD.
    /// </summary>
    public void UpdateCarUI(float speed, int gear, float rpm, float maxRPM,
                            bool clutchIsEngaged,
                            PrometeoCarController.TransmissionMode mode)
    {
        // Speed
        if (speedText != null)
        {
            int displaySpeed = Mathf.RoundToInt(Mathf.Abs(speed));
            speedText.text = displaySpeed.ToString();
        }

        // Gear display
        if (gearText != null)
        {
            string gearDisplay;
            if (gear == -1)
                gearDisplay = "R";
            else if (gear == 0)
                gearDisplay = "N";
            else
                gearDisplay = gear.ToString();

            gearText.text = gearDisplay;
        }

        // Transmission mode
        if (transmissionModeText != null)
        {
            switch (mode)
            {
                case PrometeoCarController.TransmissionMode.Automatic:
                    transmissionModeText.text = "AUTO";
                    break;
                case PrometeoCarController.TransmissionMode.SequentialGear:
                    transmissionModeText.text = "SEQ";
                    break;
                case PrometeoCarController.TransmissionMode.ManualClutch:
                    transmissionModeText.text = "MANUAL";
                    break;
            }
        }

        // RPM
        if (rpmText != null)
        {
            rpmText.text = Mathf.RoundToInt(rpm).ToString();
        }

        if (rpmBar != null)
        {
            rpmBar.fillAmount = Mathf.Clamp01(rpm / maxRPM);
        }

        // Clutch (only relevant in ManualClutch mode)
        if (mode == PrometeoCarController.TransmissionMode.ManualClutch)
        {
            if (clutchText != null)
            {
                clutchText.gameObject.SetActive(true);
                clutchText.text = clutchIsEngaged ? "CLUTCH: ON" : "CLUTCH: OFF";
            }
            if (clutchBar != null)
            {
                clutchBar.gameObject.SetActive(true);
                clutchBar.fillAmount = clutchIsEngaged ? 1f : 0f;
            }
        }
        else
        {
            // Hide clutch indicators in non-manual modes
            if (clutchText != null)
                clutchText.gameObject.SetActive(false);
            if (clutchBar != null)
                clutchBar.gameObject.SetActive(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  RACE PANEL (Accept/Reject)
    // ═══════════════════════════════════════════════════════════════

    public void ShowRacePanel()
    {
        if (racePanel != null) racePanel.SetActive(true);
        if (raceUI != null) raceUI.SetActive(false);
        if (leaderBoardUI != null) leaderBoardUI.SetActive(false);

        // Re-enable buttons (they may have been disabled after a previous race)
        if (acceptButton != null) acceptButton.interactable = true;
        if (rejectButton != null) rejectButton.interactable = true;
    }

    public void HideRacePanel()
    {
        if (racePanel != null) racePanel.SetActive(false);
    }

    public void UpdateRacePanelStatus(string status)
    {
        if (racePanelStatusText != null)
            racePanelStatusText.text = status;
    }

    // ═══════════════════════════════════════════════════════════════
    //  RACE UI (In-Race HUD)
    // ═══════════════════════════════════════════════════════════════

    public void ShowRaceUI()
    {
        if (raceUI != null) raceUI.SetActive(true);
        if (racePanel != null) racePanel.SetActive(false);
        if (leaderBoardUI != null) leaderBoardUI.SetActive(false);
    }

    public void HideRaceUI()
    {
        if (raceUI != null) raceUI.SetActive(false);
    }

    public void UpdateCountdown(string text)
    {
        if (countDownText != null)
            countDownText.text = text;
    }

    /// <summary>
    /// Updates the race HUD with current position, time, and progress.
    /// </summary>
    public void UpdateRaceHUD(int position, float time, float progress)
    {
        if (racePositionText != null)
        {
            string suffix = GetPositionSuffix(position);
            racePositionText.text = position + suffix;
        }

        if (raceTimeText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);
            raceTimeText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }

        if (raceProgressText != null)
        {
            int percent = Mathf.RoundToInt(progress * 100f);
            raceProgressText.text = percent + "%";
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  LEADERBOARD UI
    // ═══════════════════════════════════════════════════════════════

    public void ShowLeaderboard(List<PlayerWaypointTracker> trackers)
    {
        if (leaderBoardUI != null) leaderBoardUI.SetActive(true);
        if (raceUI != null) raceUI.SetActive(false);
        if (racePanel != null) racePanel.SetActive(false);

        PopulateLeaderboard(trackers);
    }

    public void HideLeaderboard()
    {
        if (leaderBoardUI != null) leaderBoardUI.SetActive(false);
    }

    private void PopulateLeaderboard(List<PlayerWaypointTracker> trackers)
    {
        // Clear existing entries
        if (leaderBoardPanel != null)
        {
            foreach (Transform child in leaderBoardPanel)
            {
                Destroy(child.gameObject);
            }
        }

        // Sort by finish position (1st, 2nd, 3rd, 4th)
        List<PlayerWaypointTracker> sorted = new List<PlayerWaypointTracker>(trackers);
        sorted.Sort((a, b) => a.FinishPosition.CompareTo(b.FinishPosition));

        // Spawn a prefab for each player that finished
        foreach (var tracker in sorted)
        {
            if (tracker.HasFinished && leaderboardEntryPrefab != null && leaderBoardPanel != null)
            {
                GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderBoardPanel);
                LeaderboardEntryUI entryUI = entryObj.GetComponent<LeaderboardEntryUI>();
                
                if (entryUI != null)
                {
                    string suffix = GetPositionSuffix(tracker.FinishPosition);
                    string resultStr = tracker.FinishPosition == 1 ? "WINNER" : "Finished";
                    
                    entryUI.SetData(
                        tracker.FinishPosition, 
                        suffix, 
                        tracker.PlayerDisplayName.ToString(), 
                        tracker.RaceTime, 
                        resultStr
                    );
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  PANEL MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    public void HideAllRacePanels()
    {
        if (racePanel != null) racePanel.SetActive(false);
        if (raceUI != null) raceUI.SetActive(false);
        if (leaderBoardUI != null) leaderBoardUI.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════
    //  BUTTON HANDLERS
    // ═══════════════════════════════════════════════════════════════

    private void OnAcceptClicked()
    {

        Debug.Log("Accept clicked");
        // Only register if the Race Panel is actually visible
        if (racePanel == null || !racePanel.activeSelf) return;

        Debug.Log("[UIManager] Accept button clicked.");

        // Find the local player's tracker and accept
        PlayerWaypointTracker[] trackers = FindObjectsOfType<PlayerWaypointTracker>();
        foreach (var tracker in trackers)
        {
            if (tracker.HasStateAuthority)
            {
                tracker.AcceptRace();
                break;
            }
        }

        // Disable buttons so they can't be pressed again
        if (acceptButton != null) acceptButton.interactable = false;
        if (rejectButton != null) rejectButton.interactable = false;
    }

    private void OnRejectClicked()
    {
        Debug.Log("Reject clicked");
        // Only register if the Race Panel is actually visible
        if (racePanel == null || !racePanel.activeSelf) return;

        Debug.Log("[UIManager] Reject button clicked.");

        // Find the local player's tracker and reject
        PlayerWaypointTracker[] trackers = FindObjectsOfType<PlayerWaypointTracker>();
        foreach (var tracker in trackers)
        {
            if (tracker.HasStateAuthority)
            {
                tracker.RejectRace();
                break;
            }
        }

        // Disable buttons
        if (acceptButton != null) acceptButton.interactable = false;
        if (rejectButton != null) rejectButton.interactable = false;
    }

    private void OnContinueClicked()
    {
        Debug.Log("[UIManager] Continue button clicked.");
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnContinuePressed();
        }
    }

    private void OnChallengeAgainClicked()
    {
        Debug.Log("[UIManager] Challenge Again button clicked.");
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnChallengeAgainPressed();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════

    private string GetPositionSuffix(int position)
    {
        switch (position)
        {
            case 1: return "st";
            case 2: return "nd";
            case 3: return "rd";
            default: return "th";
        }
    }
}
