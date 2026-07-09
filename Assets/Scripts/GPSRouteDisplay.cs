using UnityEngine;
using SAP2D;
using System.Collections.Generic;

public class GPSRouteDisplay : MonoBehaviour
{
    public static GPSRouteDisplay Instance { get; private set; }
    public Transform playerCar;
    
    [Header("Line Renderers (Minimap)")]
    public UILineRenderer localLineRenderer; // Should be set to Blue
    public UILineRenderer globalLineRenderer; // Should be set to Yellow
    
    [Header("Line Renderers (Big Map)")]
    public UILineRenderer bigMapLocalLineRenderer;
    public UILineRenderer bigMapGlobalLineRenderer;

    [Header("Settings")]
    public SAP2DPathfindingConfig pathfindingConfig;

    void Awake()
    {
        Instance = this;
    }
    
    private float updateTimer = 0f;
    private float startDelay = 1.0f; // Wait 1 second before first path
    private bool gridInitialized = false;
    
    // Dynamic targets set by WorldMapController
    private bool hasLocalDestination = false;
    private Vector3 localDestinationPos;
    private Vector2[] currentLocalPath;
    
    private bool hasGlobalDestination = false;
    private Vector3 globalDestinationPos;
    private Vector2[] currentGlobalPath;

    private bool isCalculatingPath = false;

    void Update()
    {
        if (playerCar == null)
        {
            // Try to find the PrometeoCarController in the scene
            PrometeoCarController car = FindObjectOfType<PrometeoCarController>();
            if (car != null)
            {
                playerCar = car.transform;
            }
        }

        if (playerCar == null || Minimap.Instance == null || pathfindingConfig == null)
            return;

        // Delay start by 1 second to ensure all Road meshes and colliders have spawned via Network/Physics
        startDelay -= Time.deltaTime;
        if (startDelay > 0) return;

        // Only calculate path once every 1 second (instead of every 0.1s) to save FPS!
        updateTimer += Time.deltaTime;
        if (updateTimer > 1.0f && !isCalculatingPath) 
        {
            updateTimer = 0f;
            
            if (hasLocalDestination || hasGlobalDestination)
            {
                SAP_GridSource grid = SAP2DPathfinder.singleton.GetGrid(pathfindingConfig.GridIndex);
                
                if (!gridInitialized)
                {
                    grid.CalculateColliders();
                    gridInitialized = true;
                    
                    // Diagnostic: Check if any tiles are walkable
                    int walkableCount = 0;
                    for (int x = 0; x < grid.Width; x++)
                    {
                        for (int y = 0; y < grid.Height; y++)
                        {
                            if (grid.GetTileDataAt(x, y).isWalkable) walkableCount++;
                        }
                    }
                    Debug.Log($"[GPSRouteDisplay] Grid Initialized. Walkable Tiles found: {walkableCount} out of {grid.Width * grid.Height}. Grid Y (Position.z) used for raycasts: {grid.Position.z}");
                }

                Vector2 startPos = GetNearestWalkablePosition(grid, new Vector2(playerCar.position.x, playerCar.position.z));
                Vector2 localEnd = hasLocalDestination ? GetNearestWalkablePosition(grid, new Vector2(localDestinationPos.x, localDestinationPos.z)) : Vector2.zero;
                Vector2 globalEnd = hasGlobalDestination ? GetNearestWalkablePosition(grid, new Vector2(globalDestinationPos.x, globalDestinationPos.z)) : Vector2.zero;
                
                isCalculatingPath = true;

                Debug.Log($"[GPSRouteDisplay] Pathfinding... Start: {startPos}, LocalEnd: {localEnd}, GlobalEnd: {globalEnd}");

                // Run A* on background thread
                System.Threading.Tasks.Task.Run(() => {
                    if (hasLocalDestination)
                    {
                        currentLocalPath = SAP2DPathfinder.singleton.FindPath(startPos, localEnd, pathfindingConfig);
                        Debug.Log("[GPSRouteDisplay] Local Path calculated. Points: " + (currentLocalPath != null ? currentLocalPath.Length.ToString() : "NULL"));
                    }
                        
                    if (hasGlobalDestination)
                    {
                        currentGlobalPath = SAP2DPathfinder.singleton.FindPath(startPos, globalEnd, pathfindingConfig);
                        Debug.Log("[GPSRouteDisplay] Global Path calculated. Points: " + (currentGlobalPath != null ? currentGlobalPath.Length.ToString() : "NULL"));
                    }
                        
                    isCalculatingPath = false;
                });
            }
        }

        // Draw Lines every frame for smoothness
        // Minimap uses mapScale directly (calibrated by the baker for the minimap rect)
        DrawLine(localLineRenderer, currentLocalPath, hasLocalDestination, "Minimap Local", Minimap.Instance.mapWorldCenter, Minimap.Instance.mapScale);
        DrawLine(globalLineRenderer, currentGlobalPath, hasGlobalDestination, "Minimap Global", Minimap.Instance.mapWorldCenter, Minimap.Instance.mapScale);

        // Big map uses GetBigMapScale() which accounts for different rect size
        if (WorldMapController.Instance != null)
        {
            Vector2 bigMapScale = WorldMapController.Instance.GetBigMapScale();
            DrawLine(bigMapLocalLineRenderer, currentLocalPath, hasLocalDestination, "BigMap Local", WorldMapController.Instance.mapWorldCenter, bigMapScale.x, bigMapScale.y);
            DrawLine(bigMapGlobalLineRenderer, currentGlobalPath, hasGlobalDestination, "BigMap Global", WorldMapController.Instance.mapWorldCenter, bigMapScale.x, bigMapScale.y);
        }
    }

    private void DrawLine(UILineRenderer lineRenderer, Vector2[] path, bool isActive, string debugName, Vector2 mapCenter, float mapScale)
    {
        DrawLine(lineRenderer, path, isActive, debugName, mapCenter, mapScale, mapScale);
    }

    private void DrawLine(UILineRenderer lineRenderer, Vector2[] path, bool isActive, string debugName, Vector2 mapCenter, float scaleX, float scaleY)
    {
        if (lineRenderer == null) 
        {
            if (isActive) Debug.LogWarning($"[GPSRouteDisplay] {debugName} Line Renderer is NULL! Please assign it in the Inspector.");
            return;
        }
        
        if (!isActive || path == null || path.Length == 0)
        {
            if (lineRenderer.points.Count > 0)
            {
                lineRenderer.points.Clear();
                lineRenderer.SetVerticesDirty();
            }
            return;
        }

        Vector2 carPos2D = new Vector2(playerCar.position.x, playerCar.position.z);
        int closestIndex = 0;
        float minDst = float.MaxValue;
        
        int searchLimit = Mathf.Min(50, path.Length);
        for (int i = 0; i < searchLimit; i++)
        {
            float sqrDst = (path[i] - carPos2D).sqrMagnitude;
            if (sqrDst < minDst)
            {
                minDst = sqrDst;
                closestIndex = i;
            }
        }

        List<Vector2> uiPoints = new List<Vector2>();

        float startUiX = (carPos2D.x - mapCenter.x) * scaleX;
        float startUiY = (carPos2D.y - mapCenter.y) * scaleY;
        uiPoints.Add(new Vector2(startUiX, startUiY));

        for (int i = closestIndex; i < path.Length; i += 10)
        {
            Vector2 worldPosXZ = path[i];
            float uiX = (worldPosXZ.x - mapCenter.x) * scaleX;
            float uiY = (worldPosXZ.y - mapCenter.y) * scaleY;
            uiPoints.Add(new Vector2(uiX, uiY));
        }
        
        if (path.Length > 0)
        {
            Vector2 lastPos = path[path.Length - 1];
            float uiX = (lastPos.x - mapCenter.x) * scaleX;
            float uiY = (lastPos.y - mapCenter.y) * scaleY;
            uiPoints.Add(new Vector2(uiX, uiY));
        }

        lineRenderer.points = uiPoints;
        lineRenderer.SetVerticesDirty();
    }

    private Vector2 GetNearestWalkablePosition(SAP_GridSource grid, Vector2 pos)
    {
        SAP_TileData tile = grid.GetTileDataAtWorldPosition(pos);
        if (tile != null && tile.isWalkable) return pos;

        // Increased radius from 5 to 30 to catch clicks that are further off-road
        for (int radius = 1; radius <= 30; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius)
                    {
                        Vector2 testPos = new Vector2(pos.x + x * grid.TileDiameter, pos.y + y * grid.TileDiameter);
                        SAP_TileData testTile = grid.GetTileDataAtWorldPosition(testPos);
                        if (testTile != null && testTile.isWalkable)
                        {
                            Debug.Log($"[GPSRouteDisplay] Snapped {pos} to nearest walkable road at {testTile.WorldPosition} (Distance: {Vector2.Distance(pos, testTile.WorldPosition)})");
                            return testTile.WorldPosition;
                        }
                    }
                }
            }
        }
        
        Debug.LogWarning($"[GPSRouteDisplay] FAILED to find any walkable road within 30 tiles of {pos}!");
        return pos;
    }

    // --- Public API for snapping clicks to road (used by WorldMapController) --- //

    /// <summary>
    /// Snaps a world XZ position to the nearest walkable road tile.
    /// Called by WorldMapController when placing waypoint markers.
    /// </summary>
    public Vector2 SnapToNearestRoad(Vector2 worldPosXZ)
    {
        if (pathfindingConfig == null || SAP2DPathfinder.singleton == null)
            return worldPosXZ;
            
        SAP_GridSource grid = SAP2DPathfinder.singleton.GetGrid(pathfindingConfig.GridIndex);
        if (grid == null) return worldPosXZ;
        
        if (!gridInitialized)
        {
            grid.CalculateColliders();
            gridInitialized = true;
        }
        
        return GetNearestWalkablePosition(grid, worldPosXZ);
    }

    // --- API FOR WORLD MAP CONTROLLER --- //

    public void SetLocalDestination(Vector3 worldPos)
    {
        Debug.Log("[GPSRouteDisplay] SetLocalDestination called with worldPos: " + worldPos);
        localDestinationPos = worldPos;
        hasLocalDestination = true;
        
        // Force recalculation next frame
        updateTimer = 2.0f; 
    }
    
    public void ClearLocalDestination()
    {
        hasLocalDestination = false;
        currentLocalPath = null;
        
        if (localLineRenderer != null)
        {
            localLineRenderer.points.Clear();
            localLineRenderer.SetVerticesDirty();
        }
        
        // Also clear big map local line
        if (bigMapLocalLineRenderer != null)
        {
            bigMapLocalLineRenderer.points.Clear();
            bigMapLocalLineRenderer.SetVerticesDirty();
        }
    }

    public void SetGlobalDestination(Vector3 worldPos)
    {
        Debug.Log("[GPSRouteDisplay] SetGlobalDestination called with worldPos: " + worldPos);
        globalDestinationPos = worldPos;
        hasGlobalDestination = true;
        
        updateTimer = 2.0f;
    }
    
    public void ClearGlobalDestination()
    {
        hasGlobalDestination = false;
        currentGlobalPath = null;
        
        if (globalLineRenderer != null)
        {
            globalLineRenderer.points.Clear();
            globalLineRenderer.SetVerticesDirty();
        }
        
        // Also clear big map global line
        if (bigMapGlobalLineRenderer != null)
        {
            bigMapGlobalLineRenderer.points.Clear();
            bigMapGlobalLineRenderer.SetVerticesDirty();
        }
    }
    
    // --- Accessors for WorldMapController / Minimap to query waypoint state --- //
    
    public bool HasLocalDestination() { return hasLocalDestination; }
    public bool HasGlobalDestination() { return hasGlobalDestination; }
    public Vector3 GetLocalDestinationPos() { return localDestinationPos; }
    public Vector3 GetGlobalDestinationPos() { return globalDestinationPos; }
}
