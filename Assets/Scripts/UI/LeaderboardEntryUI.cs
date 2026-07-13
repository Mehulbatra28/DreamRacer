using UnityEngine;
using TMPro;

/// <summary>
/// Attach this to your PlayerLeaderboard prefab.
/// It holds references to the UI text elements for a single leaderboard row.
/// </summary>
public class LeaderboardEntryUI : MonoBehaviour
{
    public TMP_Text positionText;
    public TMP_Text nameText;
    public TMP_Text timeText;
    public TMP_Text resultText;
    
    public void SetData(int position, string suffix, string playerName, float time, string result)
    {
        if (positionText != null) positionText.text = position + suffix;
        if (nameText != null) nameText.text = playerName;
        
        if (timeText != null)
        {
            int min = Mathf.FloorToInt(time / 60f);
            int sec = Mathf.FloorToInt(time % 60f);
            int ms = Mathf.FloorToInt((time * 100f) % 100f);
            timeText.text = string.Format("{0:00}:{1:00}.{2:00}", min, sec, ms);
        }
        
        if (resultText != null) resultText.text = result;
    }
}
