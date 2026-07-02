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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

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
}
