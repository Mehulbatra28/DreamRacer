using UnityEngine;
using System.Collections.Generic;

public class Minimap : MonoBehaviour
{
    public static Minimap Instance { get; private set; }
    [Header("UI References")]
    public RectTransform mapImage;      // The giant map image inside the mask
    public RectTransform playerIcon;    // The arrow icon (OUTSIDE the mask, always centered)
    public RectTransform minimapMask;   // The mask RectTransform (circular) — assign in Inspector

    [Header("Waypoint Icons (GTA 5 Style)")]
    public GameObject localWaypointIconPrefab;  // Blue waypoint icon prefab
    public GameObject globalWaypointIconPrefab; // Yellow waypoint icon prefab

    [Header("Other Players")]
    public GameObject otherPlayerIconPrefab;     // Icon prefab for other players on minimap
    public GameObject otherGlobalWaypointIconPrefab; // Icon prefab for other player's global waypoint

    [Header("Settings")]
    public bool rotateWithPlayer = true;

    [Header("AAA Baked Data (DO NOT EDIT)")]
    [Tooltip("These values are injected automatically by the AAA Map Baker tool!")]
    public Vector2 mapWorldCenter;
    public float mapScale;

    // The transform of the local car
    public static Transform LocalPlayer;

    // Runtime waypoint icon instances — these live OUTSIDE the mask (sibling of playerIcon)
    private GameObject localWaypointIcon;
    private RectTransform localWaypointIconRect;
    private GameObject globalWaypointIcon;
    private RectTransform globalWaypointIconRect;

    // Other player car icons on minimap — also OUTSIDE the mask
    private Dictionary<Transform, RectTransform> otherPlayerIcons = new Dictionary<Transform, RectTransform>();

    // Radius for circular edge-clamping (calculated from the mask size)
    private float clampRadius;

    // Dedicated overlay container that matches the mask's position/size but is NOT clipped
    private RectTransform overlayParent;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Calculate clamp radius from the minimap mask
        if (minimapMask != null)
        {
            // Use the smaller dimension / 2 for a circular minimap
            float maskWidth = minimapMask.rect.width;
            float maskHeight = minimapMask.rect.height;
            clampRadius = Mathf.Min(maskWidth, maskHeight) * 0.5f - 30f; // 25px padding to keep icons fully inside boundary

            // Create a dedicated overlay container that matches the mask exactly
            // but is NOT a mask — so its children won't be clipped.
            // Placed as a sibling AFTER the mask so it renders on top.
            GameObject overlayGo = new GameObject("MinimapIconOverlay");

            overlayParent = overlayGo.AddComponent<RectTransform>();
            overlayParent.SetParent(minimapMask.parent, false);
            overlayParent.anchorMin = minimapMask.anchorMin;
            overlayParent.anchorMax = minimapMask.anchorMax;
            overlayParent.anchoredPosition = minimapMask.anchoredPosition;
            overlayParent.sizeDelta = minimapMask.sizeDelta;
            overlayParent.pivot = minimapMask.pivot;
            overlayParent.localScale = minimapMask.localScale;
            overlayParent.SetAsLastSibling(); // Render on top of the mask + map

            Debug.Log($"[Minimap] Created icon overlay. Mask size: {maskWidth}x{maskHeight}, clampRadius: {clampRadius}");
        }
        else
        {
            clampRadius = 80f; // Fallback if mask not assigned
            Debug.LogWarning("[Minimap] minimapMask not assigned! Using fallback clampRadius of 80. Please assign the minimap mask RectTransform in the Inspector.");
        }
    }

    void Update()
    {
        if (LocalPlayer == null)
        {
            // Find the LOCAL player's car (the one with InputAuthority)
            PrometeoCarController[] allCars = FindObjectsOfType<PrometeoCarController>();
            foreach (var car in allCars)
            {
                Fusion.NetworkObject netObj = car.GetComponent<Fusion.NetworkObject>();
                if (netObj != null && netObj.HasInputAuthority)
                {
                    LocalPlayer = car.transform;
                    Debug.Log("[Minimap] Found Local Player Car: " + car.gameObject.name);
                    break;
                }
            }

            // Fallback: if no NetworkObject found (single player testing), use the first car
            if (LocalPlayer == null && allCars.Length > 0)
            {
                LocalPlayer = allCars[0].transform;
                Debug.Log("[Minimap] Fallback - Found Car: " + allCars[0].gameObject.name);
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

        // 3. Update other player car icons on the minimap
        UpdateOtherPlayerIcons();
    }

    /// <summary>
    /// Shows/hides and positions the blue (local) and yellow (global) waypoint icons on the minimap.
    /// Icons are parented OUTSIDE the mask (sibling of playerIcon) so they are never clipped.
    /// Positions are calculated in screen-space relative to the minimap center, then clamped to circle edge.
    /// </summary>
    private void UpdateWaypointIcons()
    {
        if (WorldMapController.Instance == null || overlayParent == null) return;

        // --- Local (Blue) Waypoint Icon ---
        if (WorldMapController.Instance.HasLocalWaypoint())
        {
            if (localWaypointIcon == null && localWaypointIconPrefab != null)
            {
                localWaypointIcon = Instantiate(localWaypointIconPrefab, overlayParent);
                localWaypointIconRect = localWaypointIcon.transform as RectTransform;
                CenterIcon(localWaypointIconRect);
                if (localWaypointIconRect != null)
                {
                    localWaypointIconRect.sizeDelta = new Vector2(30f, 30f);
                    localWaypointIconRect.localScale = Vector3.one;
                }
            }

            if (localWaypointIconRect != null)
            {
                Vector3 wpWorld = WorldMapController.Instance.GetLocalWaypointWorldPos();
                Vector2 screenOffset = WorldToMinimapScreenOffset(wpWorld);
                localWaypointIconRect.anchoredPosition = ClampToCircle(screenOffset);
                localWaypointIcon.SetActive(true);
                localWaypointIconRect.transform.SetAsLastSibling();
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
                globalWaypointIcon = Instantiate(globalWaypointIconPrefab, overlayParent);
                globalWaypointIconRect = globalWaypointIcon.transform as RectTransform;
                CenterIcon(globalWaypointIconRect);
                if (globalWaypointIconRect != null)
                {
                    globalWaypointIconRect.sizeDelta = new Vector2(30f, 30f);
                    globalWaypointIconRect.localScale = Vector3.one;
                }
            }

            if (globalWaypointIconRect != null)
            {
                Vector3 wpWorld = WorldMapController.Instance.GetGlobalWaypointWorldPos();
                Vector2 screenOffset = WorldToMinimapScreenOffset(wpWorld);
                globalWaypointIconRect.anchoredPosition = ClampToCircle(screenOffset);
                globalWaypointIcon.SetActive(true);
                globalWaypointIconRect.transform.SetAsLastSibling();
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

    /// <summary>
    /// Tracks and displays other players' car icons on the minimap.
    /// Icons are outside the mask so they are always visible.
    /// </summary>
    private void UpdateOtherPlayerIcons()
    {
        if (overlayParent == null) return;

        PrometeoCarController[] allCars = FindObjectsOfType<PrometeoCarController>();
        HashSet<Transform> currentFrameCars = new HashSet<Transform>();

        foreach (var car in allCars)
        {
            Transform carTransform = car.transform;

            // Skip the local player — they already have the playerIcon
            if (carTransform == LocalPlayer) continue;

            currentFrameCars.Add(carTransform);

            // Spawn icon if we don't have one yet
            if (!otherPlayerIcons.ContainsKey(carTransform))
            {
                if (otherPlayerIconPrefab != null)
                {
                    GameObject newIcon = Instantiate(otherPlayerIconPrefab, overlayParent);
                    RectTransform iconRect = newIcon.transform as RectTransform;
                    CenterIcon(iconRect);
                    if (iconRect != null)
                    {
                        iconRect.gameObject.SetActive(true);
                        iconRect.sizeDelta = new Vector2(24f, 24f);
                        iconRect.localScale = Vector3.one;
                    }
                    otherPlayerIcons.Add(carTransform, iconRect);
                }
            }

            // Position the icon
            if (otherPlayerIcons.TryGetValue(carTransform, out RectTransform otherIconRect))
            {
                if (otherIconRect != null)
                {
                    Vector2 screenOffset = WorldToMinimapScreenOffset(carTransform.position);
                    otherIconRect.anchoredPosition = ClampToCircle(screenOffset);

                    // Rotate icon to match the other car's heading in screen space
                    float carYaw = carTransform.eulerAngles.y;
                    if (rotateWithPlayer)
                    {
                        // When map rotates with player, subtract local player's heading
                        carYaw -= LocalPlayer.eulerAngles.y;
                    }
                    otherIconRect.localEulerAngles = new Vector3(0, 0, -carYaw + 90f);

                    otherIconRect.transform.SetAsLastSibling();
                }
            }
        }

        // Cleanup disconnected/destroyed cars
        List<Transform> keysToRemove = new List<Transform>();
        foreach (var kvp in otherPlayerIcons)
        {
            if (kvp.Key == null || !currentFrameCars.Contains(kvp.Key))
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            otherPlayerIcons.Remove(key);
        }
    }



    /// <summary>
    /// Converts a world position to a screen-space offset from the minimap center.
    /// The minimap center = the local player's position.
    /// When rotateWithPlayer is on, the offset is rotated so "up" on screen = the player's forward.
    /// The result is in the overlayParent's coordinate space (screen-aligned, not map-rotated).
    /// </summary>
    private Vector2 WorldToMinimapScreenOffset(Vector3 worldPos)
    {
        // World offset from player to target
        float dx = worldPos.x - LocalPlayer.position.x;
        float dz = worldPos.z - LocalPlayer.position.z;

        // Scale to minimap pixels
        float uiX = dx * mapScale;
        float uiY = dz * mapScale;

        if (rotateWithPlayer)
        {
            // Rotate so the player's forward direction points UP on screen
            float angle = LocalPlayer.eulerAngles.y;
            float angleRad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            float rotX = uiX * cos - uiY * sin;
            float rotY = uiX * sin + uiY * cos;
            return new Vector2(rotX, rotY);
        }
        else
        {
            return new Vector2(uiX, uiY);
        }
    }

    /// <summary>
    /// Forces the icon to be perfectly centered on its own anchored position.
    /// This fixes bugs where prefabs with top-left anchors appear visually outside the mask!
    /// </summary>
    private void CenterIcon(RectTransform rect)
    {
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    /// <summary>
    /// Shows or hides the minimap overlay (player icon, waypoint icons, other player icons).
    /// Called by WorldMapController when toggling the big map to prevent duplicate icons.
    /// </summary>
    public void SetOverlayVisible(bool visible)
    {
        if (overlayParent != null)
        {
            overlayParent.gameObject.SetActive(visible);
        }
        if (playerIcon != null)
        {
            playerIcon.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Clamps a screen-space offset to the circular minimap edge.
    /// If within the visible circle, returns the offset unchanged.
    /// If outside, clamps to the circle edge (GTA-style).
    /// </summary>
    private Vector2 ClampToCircle(Vector2 screenOffset)
    {
        float dist = screenOffset.magnitude;
        if (dist > clampRadius && dist > 0.001f)
        {
            return screenOffset.normalized * clampRadius;
        }
        return screenOffset;
    }
}
