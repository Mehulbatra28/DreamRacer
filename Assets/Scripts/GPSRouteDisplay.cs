using UnityEngine;
using SAP2D;
using System.Collections.Generic;

[RequireComponent(typeof(UILineRenderer))]
public class GPSRouteDisplay : MonoBehaviour
{
    public Transform playerCar;
    public Transform finishLineTarget;
    public SAP2DPathfindingConfig pathfindingConfig;
    public Minimap minimapReference; 
    
    private UILineRenderer uiLineRenderer;
    private float updateTimer = 0f;
    private float startDelay = 1.0f; // Wait 1 second before first path
    private bool gridInitialized = false;
    private Vector2[] currentPath;

    private bool isCalculatingPath = false;
    private bool pathNeedsUIUpdate = false;

    void Awake()
    {
        uiLineRenderer = GetComponent<UILineRenderer>();
    }

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

        if (playerCar == null || finishLineTarget == null || minimapReference == null || pathfindingConfig == null)
            return;

        // Delay start by 1 second to ensure all Road meshes and colliders have spawned via Network/Physics
        startDelay -= Time.deltaTime;
        if (startDelay > 0) return;

        // OPTIMIZATION: Only calculate path once every 1 second (instead of every 0.1s) to save FPS!
        updateTimer += Time.deltaTime;
        if (updateTimer > 1.0f && !isCalculatingPath) 
        {
            updateTimer = 0f;
            SAP_GridSource grid = SAP2DPathfinder.singleton.GetGrid(pathfindingConfig.GridIndex);
            
            // Force recalculate colliders ONCE after the game has fully started
            if (!gridInitialized)
            {
                grid.CalculateColliders();
                gridInitialized = true;
            }

            // Capture positions on Main Thread
            Vector2 startPos = GetNearestWalkablePosition(grid, new Vector2(playerCar.position.x, playerCar.position.z));
            Vector2 endPos = GetNearestWalkablePosition(grid, new Vector2(finishLineTarget.position.x, finishLineTarget.position.z));
            
            isCalculatingPath = true;

            // OPTIMIZATION: Run massive A* algorithm on a background thread so it doesn't freeze the game!
            System.Threading.Tasks.Task.Run(() => {
                Vector2[] newPath = SAP2DPathfinder.singleton.FindPath(startPos, endPos, pathfindingConfig);
                currentPath = newPath;
                pathNeedsUIUpdate = true; // Tell main thread to update UI
                isCalculatingPath = false;
            });
        }

        // OPTIMIZATION: Only rebuild the UI Line Renderer when a new path is actually ready!
        // Actually, we want to update the UI line EVERY FRAME so that the line smoothly "disappears" 
        // as the car drives over it! (Just like a real GPS).
        if (currentPath == null || currentPath.Length == 0)
        {
            if (uiLineRenderer.points.Count > 0)
            {
                uiLineRenderer.points.Clear();
                uiLineRenderer.SetVerticesDirty();
            }
            return;
        }

        // 1. Find the point on the path closest to the car's current position
        Vector2 carPos2D = new Vector2(playerCar.position.x, playerCar.position.z);
        int closestIndex = 0;
        float minDst = float.MaxValue;
        
        // We only check the first 50 points to save CPU, since the car can't teleport!
        int searchLimit = Mathf.Min(50, currentPath.Length);
        for (int i = 0; i < searchLimit; i++)
        {
            float sqrDst = (currentPath[i] - carPos2D).sqrMagnitude;
            if (sqrDst < minDst)
            {
                minDst = sqrDst;
                closestIndex = i;
            }
        }

        List<Vector2> uiPoints = new List<Vector2>();

        // 2. Add the car's EXACT current position as the starting point so it looks buttery smooth!
        float startUiX = (carPos2D.x - minimapReference.mapWorldCenter.x) * minimapReference.mapScale;
        float startUiY = (carPos2D.y - minimapReference.mapWorldCenter.y) * minimapReference.mapScale;
        uiPoints.Add(new Vector2(startUiX, startUiY));

        // 3. Add the rest of the path, but ONLY the points ahead of the car!
        // We start from closestIndex to drop the points behind us.
        for (int i = closestIndex; i < currentPath.Length; i += 10)
        {
            Vector2 worldPosXZ = currentPath[i];
            float uiX = (worldPosXZ.x - minimapReference.mapWorldCenter.x) * minimapReference.mapScale;
            float uiY = (worldPosXZ.y - minimapReference.mapWorldCenter.y) * minimapReference.mapScale;
            uiPoints.Add(new Vector2(uiX, uiY));
        }
        
        // Always add the very last point so it connects exactly to the finish line
        if (currentPath.Length > 0)
        {
            Vector2 lastPos = currentPath[currentPath.Length - 1];
            float uiX = (lastPos.x - minimapReference.mapWorldCenter.x) * minimapReference.mapScale;
            float uiY = (lastPos.y - minimapReference.mapWorldCenter.y) * minimapReference.mapScale;
            uiPoints.Add(new Vector2(uiX, uiY));
        }

        uiLineRenderer.points = uiPoints;
        uiLineRenderer.SetVerticesDirty();
    }

    private Vector2 GetNearestWalkablePosition(SAP_GridSource grid, Vector2 pos)
    {
        SAP_TileData tile = grid.GetTileDataAtWorldPosition(pos);
        if (tile != null && tile.isWalkable) return pos; // Already walkable

        // Spiral search for nearest walkable tile within 5 tiles distance
        for (int radius = 1; radius <= 5; radius++)
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
                            return testTile.WorldPosition;
                        }
                    }
                }
            }
        }
        return pos; // Fallback
    }
}
