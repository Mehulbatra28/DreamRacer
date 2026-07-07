using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WorldMapController : MonoBehaviour
{
    [Header("Map References")]
    public GameObject mapCanvas; // The main UI Canvas or Panel for the map
    public RectTransform mapImageRect; // The map image itself
    
    [Header("Coordinate Mapping")]
    public Vector2 mapWorldCenter;
    public float mapScale;

    [Header("Markers")]
    public GameObject localWaypointPrefab;
    public GameObject globalWaypointPrefab;
    private GameObject currentLocalMarker;
    private GameObject currentGlobalMarker;

    [Header("Player Tracking")]
    public GameObject localPlayerIconPrefab;
    public GameObject otherPlayerIconPrefab;
    private System.Collections.Generic.Dictionary<Transform, RectTransform> activePlayerIcons = new System.Collections.Generic.Dictionary<Transform, RectTransform>();

    [Header("Dependencies")]
    public GPSRouteDisplay gpsDisplay; // Reference to update GPS
    
    // We will find this at runtime once the player spawns
    private PlayerWaypointTracker localPlayerTracker;
    
    private RectTransform currentLocalMarkerRect;
    private RectTransform currentGlobalMarkerRect;
    
    // Unity Input System Action Reference
    public InputActionReference openMapAction;

    private void Awake()
    {
        if (openMapAction != null)
        {
            openMapAction.action.performed += ToggleMap;
            openMapAction.action.Enable();
        }
        
        if (mapCanvas != null)
        {
            mapCanvas.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (openMapAction != null)
        {
            openMapAction.action.Disable();
            openMapAction.action.performed -= ToggleMap;
        }
    }

    private void Update()
    {
        // Try to find the local player's network tracker if we haven't already
        if (localPlayerTracker == null)
        {
            PlayerWaypointTracker[] trackers = FindObjectsOfType<PlayerWaypointTracker>();
            foreach (var tracker in trackers)
            {
                if (tracker.HasInputAuthority) // This is the local player's object in Fusion
                {
                    localPlayerTracker = tracker;
                    break;
                }
            }
        }

        // Manual Click Detection (Foolproof against UI EventSystem issues)
        if (mapCanvas != null && mapCanvas.activeInHierarchy)
        {
            UpdatePlayerMarkers();

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Debug.Log("[WorldMapController] Left Click Detected via Input System!");
                    HandleMapClick(Mouse.current.position.ReadValue(), isLocal: true);
                }
                else if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    Debug.Log("[WorldMapController] Right Click Detected via Input System!");
                    HandleMapClick(Mouse.current.position.ReadValue(), isLocal: false);
                }
            }
        }
    }

    private void ToggleMap(InputAction.CallbackContext context)
    {
        if (mapCanvas != null)
        {
            bool isActive = !mapCanvas.activeSelf;
            mapCanvas.SetActive(isActive);
            
            // Show and unlock cursor when map is open, hide/lock when closed
            if (isActive)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    public Vector2 GetEffectiveScale()
    {
        if (mapImageRect == null) return new Vector2(mapScale, mapScale);
        Image img = mapImageRect.GetComponent<Image>();
        if (img != null && img.sprite != null)
        {
            float nativeWidth = img.sprite.rect.width;
            float nativeHeight = img.sprite.rect.height;
            
            // If native width/height are 0 for some reason, fallback to mapScale
            if (nativeWidth <= 0 || nativeHeight <= 0) return new Vector2(mapScale, mapScale);
            
            float scaleX = mapScale * (mapImageRect.rect.width / nativeWidth);
            float scaleY = mapScale * (mapImageRect.rect.height / nativeHeight);
            return new Vector2(scaleX, scaleY);
        }
        return new Vector2(mapScale, mapScale);
    }

    private void HandleMapClick(Vector2 screenPos, bool isLocal)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapImageRect, screenPos, mapCanvas.GetComponent<Canvas>().worldCamera, out Vector2 localCursor);
        Debug.Log($"[WorldMapController] Clicked Local UI Coordinate: {localCursor}");

        Vector2 effectiveScale = GetEffectiveScale();
        float worldX = (localCursor.x / effectiveScale.x) + mapWorldCenter.x;
        float worldZ = (localCursor.y / effectiveScale.y) + mapWorldCenter.y;
        
        Vector3 worldPos = new Vector3(worldX, 0, worldZ);
        Debug.Log($"[WorldMapController] Converted to World Coordinate: {worldPos}");

        if (isLocal)
        {
            SetLocalWaypoint(worldPos, localCursor);
        }
        else
        {
            SetGlobalWaypoint(worldPos, localCursor);
        }
    }

    private void SetLocalWaypoint(Vector3 worldPos, Vector2 uiPos)
    {
        if (currentLocalMarker == null && localWaypointPrefab != null)
        {
            currentLocalMarker = Instantiate(localWaypointPrefab, mapImageRect);
            currentLocalMarkerRect = currentLocalMarker.transform as RectTransform;
            if (currentLocalMarkerRect != null)
            {
                currentLocalMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
                currentLocalMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
                currentLocalMarkerRect.pivot = new Vector2(0.5f, 0.5f);
                currentLocalMarkerRect.sizeDelta = new Vector2(30f, 30f); // Guarantee a visible size
                currentLocalMarkerRect.localScale = Vector3.one;
            }
        }
        
        if (currentLocalMarkerRect != null)
        {
            currentLocalMarkerRect.anchoredPosition = uiPos;
            currentLocalMarker.SetActive(true);
        }

        if (gpsDisplay != null)
        {
            gpsDisplay.SetLocalDestination(worldPos);
        }
    }

    private void SetGlobalWaypoint(Vector3 worldPos, Vector2 uiPos)
    {
        if (currentGlobalMarker == null && globalWaypointPrefab != null)
        {
            currentGlobalMarker = Instantiate(globalWaypointPrefab, mapImageRect);
            currentGlobalMarkerRect = currentGlobalMarker.transform as RectTransform;
            if (currentGlobalMarkerRect != null)
            {
                currentGlobalMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
                currentGlobalMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
                currentGlobalMarkerRect.pivot = new Vector2(0.5f, 0.5f);
                currentGlobalMarkerRect.sizeDelta = new Vector2(30f, 30f); // Guarantee a visible size
                currentGlobalMarkerRect.localScale = Vector3.one;
            }
        }
        
        if (currentGlobalMarkerRect != null)
        {
            currentGlobalMarkerRect.anchoredPosition = uiPos;
            currentGlobalMarker.SetActive(true);
        }

        if (localPlayerTracker != null)
        {
            localPlayerTracker.SetGlobalWaypoint(worldPos);
        }
        
        if (gpsDisplay != null)
        {
            gpsDisplay.SetGlobalDestination(worldPos);
        }
    }

    private void UpdatePlayerMarkers()
    {
        if (mapImageRect == null) return;

        // Find all cars in the scene
        PrometeoCarController[] allCars = FindObjectsOfType<PrometeoCarController>();
        
        // Track which cars we've seen this frame to remove old ones
        System.Collections.Generic.HashSet<Transform> currentFramesCars = new System.Collections.Generic.HashSet<Transform>();

        foreach (var car in allCars)
        {
            Transform carTransform = car.transform;
            currentFramesCars.Add(carTransform);

            // If we don't have an icon for this car yet, spawn one!
            if (!activePlayerIcons.ContainsKey(carTransform))
            {
                // Determine if this car is the local player by checking Fusion authority (ONLY DO THIS ONCE ON SPAWN)
                bool isLocalPlayer = false;
                Fusion.NetworkObject networkObject = car.GetComponent<Fusion.NetworkObject>();
                if (networkObject != null && networkObject.HasInputAuthority)
                {
                    isLocalPlayer = true;
                }

                GameObject prefabToUse = isLocalPlayer ? localPlayerIconPrefab : otherPlayerIconPrefab;
                if (prefabToUse != null)
                {
                    GameObject newIcon = Instantiate(prefabToUse, mapImageRect);
                    RectTransform iconRectNew = newIcon.transform as RectTransform; // Direct cast instead of GetComponent
                    if (iconRectNew != null)
                    {
                        iconRectNew.gameObject.SetActive(true); // Fix invisible icons
                        iconRectNew.anchorMin = new Vector2(0.5f, 0.5f);
                        iconRectNew.anchorMax = new Vector2(0.5f, 0.5f);
                        iconRectNew.pivot = new Vector2(0.5f, 0.5f);
                        iconRectNew.sizeDelta = new Vector2(30f, 30f); // Guarantee a visible size
                        iconRectNew.localScale = Vector3.one; // Fix any weird scaling
                    }
                    activePlayerIcons.Add(carTransform, iconRectNew);
                }
            }

            if (activePlayerIcons.TryGetValue(carTransform, out RectTransform iconRect))
            {
                if (iconRect != null)
                {
                    Vector2 effectiveScale = GetEffectiveScale();
                    // Update position on the map using effective squashed scale
                    float uiX = (carTransform.position.x - mapWorldCenter.x) * effectiveScale.x;
                    float uiY = (carTransform.position.z - mapWorldCenter.y) * effectiveScale.y;
                    
                    iconRect.anchoredPosition = new Vector2(uiX, uiY);
                    
                    // Rotate icon to match car's rotation (assuming top-down view)
                    iconRect.localEulerAngles = new Vector3(0, 0, -carTransform.eulerAngles.y + 90f);
                }
            }
        }

        // Cleanup cars that disconnected or were destroyed
        System.Collections.Generic.List<Transform> keysToRemove = new System.Collections.Generic.List<Transform>();
        foreach (var key in activePlayerIcons.Keys)
        {
            if (key == null || !currentFramesCars.Contains(key))
            {
                if (activePlayerIcons[key] != null) Destroy(activePlayerIcons[key].gameObject);
                keysToRemove.Add(key);
            }
        }
        
        foreach(var key in keysToRemove)
        {
            activePlayerIcons.Remove(key);
        }
    }

    public void DisplayOtherPlayerGlobalWaypoint(Vector3 worldPos, string playerName)
    {
        float uiX = (worldPos.x - mapWorldCenter.x) * mapScale;
        float uiY = (worldPos.z - mapWorldCenter.y) * mapScale;
        Vector2 uiPos = new Vector2(uiX, uiY);
        
        // Placeholder for instantiating markers for other players.
    }
}
