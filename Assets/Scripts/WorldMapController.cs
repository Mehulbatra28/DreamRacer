
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WorldMapController : MonoBehaviour
{
    // Singleton instance so other scripts (e.g. Minimap) can access without Inspector wiring
    public static WorldMapController Instance { get; private set; }
    [Header("Map References")]
    public GameObject mapCanvas; // The main UI Canvas or Panel for the map
    public Canvas rootCanvas; // Assign the parent Canvas here directly
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

    // We will find this at runtime once the player spawns
    private PlayerWaypointTracker localPlayerTracker;
    
    private RectTransform currentLocalMarkerRect;
    private RectTransform currentGlobalMarkerRect;
    
    // Unity Input System Action References (assign in Inspector)
    public InputActionReference openMapAction;
    public InputActionReference mapLeftClickAction;
    public InputActionReference mapRightClickAction;
    public InputActionReference mapDeleteWaypointAction;

    // Track which waypoint was last set so middle-click deletes the correct one
    private enum LastWaypointType { None, Local, Global }
    private LastWaypointType lastSetWaypoint = LastWaypointType.None;

    private void Awake()
    {
        Instance = this;

        if (openMapAction != null)
        {
            openMapAction.action.performed += ToggleMap;
            openMapAction.action.Enable();
        }
        
        if (mapLeftClickAction != null)
        {
            mapLeftClickAction.action.performed += OnMapLeftClick;
            mapLeftClickAction.action.Enable();
        }
        
        if (mapRightClickAction != null)
        {
            mapRightClickAction.action.performed += OnMapRightClick;
            mapRightClickAction.action.Enable();
        }
        
        if (mapDeleteWaypointAction != null)
        {
            mapDeleteWaypointAction.action.performed += OnMapDeleteWaypoint;
            mapDeleteWaypointAction.action.Enable();
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
        
        if (mapLeftClickAction != null)
        {
            mapLeftClickAction.action.Disable();
            mapLeftClickAction.action.performed -= OnMapLeftClick;
        }
        
        if (mapRightClickAction != null)
        {
            mapRightClickAction.action.Disable();
            mapRightClickAction.action.performed -= OnMapRightClick;
        }
        
        if (mapDeleteWaypointAction != null)
        {
            mapDeleteWaypointAction.action.Disable();
            mapDeleteWaypointAction.action.performed -= OnMapDeleteWaypoint;
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

        // Update player markers every frame when map is visible
        if (mapCanvas != null && mapCanvas.activeInHierarchy)
        {
            UpdatePlayerMarkers();
        }

        // Auto-delete waypoints if the player reaches them
        CheckWaypointArrival();
    }

    private void CheckWaypointArrival()
    {
        if (Minimap.LocalPlayer == null) return;
        
        Vector3 playerPos = Minimap.LocalPlayer.position;
        float arrivalDistance = 15f; // within 15 meters clears the waypoint

        if (HasLocalWaypoint())
        {
            float dist = Vector3.Distance(playerPos, GetLocalWaypointWorldPos());
            if (dist < arrivalDistance)
            {
                Debug.Log("[WorldMapController] Arrived at LOCAL destination. Clearing...");
                ClearLocalWaypointOnly();
            }
        }

        if (HasGlobalWaypoint())
        {
            float dist = Vector3.Distance(playerPos, GetGlobalWaypointWorldPos());
            if (dist < arrivalDistance)
            {
                Debug.Log("[WorldMapController] Arrived at GLOBAL destination. Clearing...");
                ClearGlobalWaypointOnly();
            }
        }
    }

    private void ClearLocalWaypointOnly()
    {
        if (currentLocalMarker != null)
        {
            Destroy(currentLocalMarker);
            currentLocalMarker = null;
            currentLocalMarkerRect = null;
        }
        
        if (GPSRouteDisplay.Instance != null)
        {
            GPSRouteDisplay.Instance.ClearLocalDestination();
        }

        if (lastSetWaypoint == LastWaypointType.Local)
        {
            if (currentGlobalMarker != null && currentGlobalMarker.activeSelf)
                lastSetWaypoint = LastWaypointType.Global;
            else
                lastSetWaypoint = LastWaypointType.None;
        }
    }

    private void ClearGlobalWaypointOnly()
    {
        if (currentGlobalMarker != null)
        {
            Destroy(currentGlobalMarker);
            currentGlobalMarker = null;
            currentGlobalMarkerRect = null;
        }
        
        if (localPlayerTracker != null)
        {
            localPlayerTracker.ClearGlobalWaypoint();
        }
        
        if (GPSRouteDisplay.Instance != null)
        {
            GPSRouteDisplay.Instance.ClearGlobalDestination();
        }

        if (lastSetWaypoint == LastWaypointType.Global)
        {
            if (currentLocalMarker != null && currentLocalMarker.activeSelf)
                lastSetWaypoint = LastWaypointType.Local;
            else
                lastSetWaypoint = LastWaypointType.None;
        }
    }

    // --- Input System Callbacks --- //

    private void OnMapLeftClick(InputAction.CallbackContext context)
    {
        if (mapCanvas == null || !mapCanvas.activeInHierarchy) return;
        if (Mouse.current == null) return;
        
        Debug.Log("[WorldMapController] Left Click Detected via Input System!");
        HandleMapClick(Mouse.current.position.ReadValue(), isLocal: true);
    }

    private void OnMapRightClick(InputAction.CallbackContext context)
    {
        if (mapCanvas == null || !mapCanvas.activeInHierarchy) return;
        if (Mouse.current == null) return;
        
        Debug.Log("[WorldMapController] Right Click Detected via Input System!");
        HandleMapClick(Mouse.current.position.ReadValue(), isLocal: false);
    }

    private void OnMapDeleteWaypoint(InputAction.CallbackContext context)
    {
        if (mapCanvas == null || !mapCanvas.activeInHierarchy) return;
        
        Debug.Log("[WorldMapController] Middle Click Detected — Deleting last waypoint!");
        DeleteLastWaypoint();
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

    /// <summary>
    /// Returns the correct scale for the big map UI.
    /// Returns the correct X/Y scale for the big map UI.
    /// The big map might be stretched differently than the minimap.
    /// </summary>
    public Vector2 GetBigMapScale()
    {
        if (mapImageRect == null || Minimap.Instance == null || Minimap.Instance.mapImage == null) 
            return new Vector2(mapScale, mapScale);
            
        float minimapWidth = Minimap.Instance.mapImage.rect.width * Minimap.Instance.mapImage.localScale.x;
        float minimapHeight = Minimap.Instance.mapImage.rect.height * Minimap.Instance.mapImage.localScale.y;
        
        if (minimapWidth <= 0 || minimapHeight <= 0) 
            return new Vector2(mapScale, mapScale);
            
        float bigMapWidth = mapImageRect.rect.width * mapImageRect.localScale.x;
        float bigMapHeight = mapImageRect.rect.height * mapImageRect.localScale.y;
        
        return new Vector2(
            mapScale * (bigMapWidth / minimapWidth),
            mapScale * (bigMapHeight / minimapHeight)
        );
    }

    private void HandleMapClick(Vector2 screenPos, bool isLocal)
    {
        Vector2 bigMapScale = GetBigMapScale();
        
        if (rootCanvas == null)
        {
            Debug.LogError("[WorldMapController] rootCanvas is not assigned in the Inspector!");
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapImageRect, screenPos, rootCanvas.worldCamera, out Vector2 localCursor);
        Debug.Log($"[WorldMapController] Clicked Local UI Coordinate: {localCursor}");

        float worldX = (localCursor.x / bigMapScale.x) + mapWorldCenter.x;
        float worldZ = (localCursor.y / bigMapScale.y) + mapWorldCenter.y;
        
        Vector3 worldPos = new Vector3(worldX, 0, worldZ);
        Debug.Log($"[WorldMapController] Converted to World Coordinate: {worldPos}");

        // Snap the world position to nearest road via GPSRouteDisplay's pathfinding grid
        Vector3 snappedWorldPos = worldPos;
        Vector2 snappedUIPos = localCursor;
        
        if (GPSRouteDisplay.Instance != null)
        {
            Vector2 snapped2D = GPSRouteDisplay.Instance.SnapToNearestRoad(new Vector2(worldPos.x, worldPos.z));
            snappedWorldPos = new Vector3(snapped2D.x, 0, snapped2D.y);
            
            // Recalculate UI position from the snapped world position
            float snappedUiX = (snappedWorldPos.x - mapWorldCenter.x) * bigMapScale.x;
            float snappedUiZ = (snappedWorldPos.z - mapWorldCenter.y) * bigMapScale.y;
            snappedUIPos = new Vector2(snappedUiX, snappedUiZ);
            
            Debug.Log($"[WorldMapController] Snapped to road: {snappedWorldPos}, UI: {snappedUIPos}");
        }

        if (isLocal)
        {
            SetLocalWaypoint(snappedWorldPos, snappedUIPos);
        }
        else
        {
            SetGlobalWaypoint(snappedWorldPos, snappedUIPos);
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

        if (GPSRouteDisplay.Instance != null)
        {
            GPSRouteDisplay.Instance.SetLocalDestination(worldPos);
        }
        
        lastSetWaypoint = LastWaypointType.Local;
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
        
        if (GPSRouteDisplay.Instance != null)
        {
            GPSRouteDisplay.Instance.SetGlobalDestination(worldPos);
        }
        
        lastSetWaypoint = LastWaypointType.Global;
    }

    /// <summary>
    /// Deletes the last set waypoint (local or global). Middle-click calls this.
    /// If the last one was local, delete local. If global, delete global.
    /// </summary>
    private void DeleteLastWaypoint()
    {
        if (lastSetWaypoint == LastWaypointType.Local)
        {
            Debug.Log("[WorldMapController] Deleting LOCAL waypoint.");
            ClearLocalWaypointOnly();
        }
        else if (lastSetWaypoint == LastWaypointType.Global)
        {
            Debug.Log("[WorldMapController] Deleting GLOBAL waypoint.");
            ClearGlobalWaypointOnly();
        }
        else
        {
            Debug.Log("[WorldMapController] No waypoint to delete.");
        }
    }

    private void UpdatePlayerMarkers()
    {
        if (mapImageRect == null) return;

        Vector2 bigMapScale = GetBigMapScale();

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
                    // Use the correct big map scale (not raw mapScale which is calibrated for minimap)
                    float uiX = (carTransform.position.x - mapWorldCenter.x) * bigMapScale.x;
                    float uiY = (carTransform.position.z - mapWorldCenter.y) * bigMapScale.y;
                    
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
        Vector2 bigMapScale = GetBigMapScale();
        float uiX = (worldPos.x - mapWorldCenter.x) * bigMapScale.x;
        float uiY = (worldPos.z - mapWorldCenter.y) * bigMapScale.y;
        Vector2 uiPos = new Vector2(uiX, uiY);
        
        // Placeholder for instantiating markers for other players.
    }
    
    // --- Public accessors for Minimap to show waypoint icons --- //
    
    public bool HasLocalWaypoint()
    {
        return GPSRouteDisplay.Instance != null && GPSRouteDisplay.Instance.HasLocalDestination();
    }
    
    public bool HasGlobalWaypoint()
    {
        return GPSRouteDisplay.Instance != null && GPSRouteDisplay.Instance.HasGlobalDestination();
    }
    
    public Vector3 GetLocalWaypointWorldPos()
    {
        if (GPSRouteDisplay.Instance != null && GPSRouteDisplay.Instance.HasLocalDestination())
            return GPSRouteDisplay.Instance.GetLocalDestinationPos();
        return Vector3.zero;
    }
    
    public Vector3 GetGlobalWaypointWorldPos()
    {
        if (GPSRouteDisplay.Instance != null && GPSRouteDisplay.Instance.HasGlobalDestination())
            return GPSRouteDisplay.Instance.GetGlobalDestinationPos();
        return Vector3.zero;
    }
}
