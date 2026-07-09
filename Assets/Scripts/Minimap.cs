using UnityEngine;

public class Minimap : MonoBehaviour
{
    public static Minimap Instance { get; private set; }
    [Header("UI References")]
    public RectTransform mapImage;      // The giant map image inside the mask
    public RectTransform playerIcon;    // The arrow icon

    [Header("Waypoint Icons (GTA 5 Style)")]
    public GameObject localWaypointIconPrefab;  // Blue waypoint icon prefab
    public GameObject globalWaypointIconPrefab; // Yellow waypoint icon prefab

    [Header("Settings")]
    public bool rotateWithPlayer = true;

    [Header("AAA Baked Data (DO NOT EDIT)")]
    [Tooltip("These values are injected automatically by the AAA Map Baker tool!")]
    public Vector2 mapWorldCenter;
    public float mapScale;

    // The transform of the local car
    public static Transform LocalPlayer;

    // Runtime waypoint icon instances
    private GameObject localWaypointIcon;
    private RectTransform localWaypointIconRect;
    private GameObject globalWaypointIcon;
    private RectTransform globalWaypointIconRect;

    void Awake()
    {
        Instance = this;
    }

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

        // 2. Update waypoint icons on the minimap (GTA 5 style)
        UpdateWaypointIcons();
    }

    /// <summary>
    /// Shows/hides and positions the blue (local) and yellow (global) waypoint icons on the minimap.
    /// These icons are children of the mapImage so they rotate/move with the minimap correctly.
    /// </summary>
    private void UpdateWaypointIcons()
    {
        if (WorldMapController.Instance == null) return;

        // --- Local (Blue) Waypoint Icon ---
        if (WorldMapController.Instance.HasLocalWaypoint())
        {
            if (localWaypointIcon == null && localWaypointIconPrefab != null)
            {
                localWaypointIcon = Instantiate(localWaypointIconPrefab, mapImage);
                localWaypointIconRect = localWaypointIcon.transform as RectTransform;
                if (localWaypointIconRect != null)
                {
                    localWaypointIconRect.anchorMin = new Vector2(0.5f, 0.5f);
                    localWaypointIconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    localWaypointIconRect.pivot = new Vector2(0.5f, 0.5f);
                    localWaypointIconRect.sizeDelta = new Vector2(20f, 20f);
                    localWaypointIconRect.localScale = Vector3.one;
                }
            }

            if (localWaypointIconRect != null)
            {
                Vector3 wpWorld = WorldMapController.Instance.GetLocalWaypointWorldPos();
                float uiX = (wpWorld.x - mapWorldCenter.x) * mapScale;
                float uiZ = (wpWorld.z - mapWorldCenter.y) * mapScale;
                localWaypointIconRect.anchoredPosition = new Vector2(uiX, uiZ);
                localWaypointIcon.SetActive(true);
            }
        }
        else
        {
            if (localWaypointIcon != null)
            {
                localWaypointIcon.SetActive(false);
            }
        }

        // --- Global (Yellow) Waypoint Icon ---
        if (WorldMapController.Instance.HasGlobalWaypoint())
        {
            if (globalWaypointIcon == null && globalWaypointIconPrefab != null)
            {
                globalWaypointIcon = Instantiate(globalWaypointIconPrefab, mapImage);
                globalWaypointIconRect = globalWaypointIcon.transform as RectTransform;
                if (globalWaypointIconRect != null)
                {
                    globalWaypointIconRect.anchorMin = new Vector2(0.5f, 0.5f);
                    globalWaypointIconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    globalWaypointIconRect.pivot = new Vector2(0.5f, 0.5f);
                    globalWaypointIconRect.sizeDelta = new Vector2(20f, 20f);
                    globalWaypointIconRect.localScale = Vector3.one;
                }
            }

            if (globalWaypointIconRect != null)
            {
                Vector3 wpWorld = WorldMapController.Instance.GetGlobalWaypointWorldPos();
                float uiX = (wpWorld.x - mapWorldCenter.x) * mapScale;
                float uiZ = (wpWorld.z - mapWorldCenter.y) * mapScale;
                globalWaypointIconRect.anchoredPosition = new Vector2(uiX, uiZ);
                globalWaypointIcon.SetActive(true);
            }
        }
        else
        {
            if (globalWaypointIcon != null)
            {
                globalWaypointIcon.SetActive(false);
            }
        }
    }
}
