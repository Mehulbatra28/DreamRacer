using UnityEngine;

public class Minimap : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform mapImage;      // The giant map image inside the mask
    public RectTransform playerIcon;    // The arrow icon

    [Header("Settings")]
    public bool rotateWithPlayer = true;

    [Header("AAA Baked Data (DO NOT EDIT)")]
    [Tooltip("These values are injected automatically by the AAA Map Baker tool!")]
    public Vector2 mapWorldCenter;
    public float mapScale;

    // The transform of the local car
    public static Transform LocalPlayer;

    void Update()
    {
        if (LocalPlayer == null)
        {
            PrometeoCarController car = FindObjectOfType<PrometeoCarController>();
            if (car != null) 
            {
                LocalPlayer = car.transform;
                Debug.Log("[Minimap] Found Local Player Car: " + car.gameObject.name);
            }
        }

        if (LocalPlayer == null) return;

        // 1. Calculate player's offset from the center of the world map
        Vector3 playerPos = LocalPlayer.position;
        float offsetX = playerPos.x - mapWorldCenter.x;
        float offsetZ = playerPos.z - mapWorldCenter.y;

        Vector2 offsetPos = new Vector2(offsetX * mapScale, offsetZ * mapScale);

        if (rotateWithPlayer)
        {
            float angle = LocalPlayer.eulerAngles.y;
            float angleRad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            // Rotate the offset by the map's rotation angle (which is the car's Y rotation)
            Vector2 rotatedOffset = new Vector2(
                offsetPos.x * cos - offsetPos.y * sin,
                offsetPos.x * sin + offsetPos.y * cos
            );

            // Move the map image in the OPPOSITE direction so the player stays centered
            mapImage.anchoredPosition = -rotatedOffset;

            // Rotate the map image to match the car's rotation
            mapImage.localEulerAngles = new Vector3(0, 0, angle);

            // Keep the Player Icon pointing UP
            playerIcon.localEulerAngles = new Vector3(0, 0, 90f);
        }
        else
        {
            // Move the map image in the OPPOSITE direction so the player stays centered
            mapImage.anchoredPosition = -offsetPos;
            mapImage.localEulerAngles = Vector3.zero;

            // Rotate the Player Icon to match the car's rotation
            playerIcon.localEulerAngles = new Vector3(0, 0, -LocalPlayer.eulerAngles.y + 90f);
        }
    }
}
