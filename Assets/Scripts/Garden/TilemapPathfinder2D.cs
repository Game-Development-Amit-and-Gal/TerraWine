using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 2D A* pathfinder that builds a grid directly from a Tilemap.
/// The Tilemap defines the walkable area (e.g. Grass).
/// Cells that overlap an obstacle collider are marked as not walkable.
/// 
/// Editor support:
/// - Builds the grid in Edit Mode (so you can see debug gizmos without Play)
/// - Rebuilds when values change in the Inspector (OnValidate)
/// 
/// Obstacle filtering:
/// - Can ignore Trigger colliders so triggers do NOT block walking.
/// </summary>
[ExecuteAlways]
public class TilemapPathfinder2D : MonoBehaviour
{
    // ------------------------------
    // Tilemap & obstacle settings
    // ------------------------------

    [Header("Tilemap & Layers")]
    [Tooltip("Tilemap that defines the walkable area (e.g. Grass).")]
    [SerializeField] private Tilemap groundTilemap;


    [Tooltip("LayerMask for obstacles (house, fence, truck, etc.).")]
    [SerializeField] private LayerMask obstacleMask;

    // ------------------------------
    // Obstacle filtering
    // ------------------------------

    [Header("Obstacle Filtering")]
    [Tooltip("If true, colliders marked as Trigger will NOT block the path.")]
    [SerializeField] private bool ignoreTriggerObstacles = true;

    [Tooltip("If true, checks all colliders at point and blocks only if a SOLID collider exists. " +
             "Recommended when you may have both trigger + solid colliders overlapping.")]
    [SerializeField] private bool checkAllCollidersAtPoint = true;

    // ------------------------------
    // Debug
    // ------------------------------

    [Header("Debug")]
    [Tooltip("If true, draw the debug grid gizmos in the Scene view.")]
    [SerializeField] private bool drawDebugGrid = false;

    [Tooltip("If true, draw gizmos even when the object is NOT selected.")]
    [SerializeField] private bool drawWhenNotSelected = false;

    [Tooltip("Debug wire cube size relative to tile cell size (1 = full cell).")]
    [SerializeField] private float debugCubeScale = 0.9f;

    [Tooltip("Log grid build info to Console.")]
    [SerializeField] private bool logBuildInfo = true;

    // ------------------------------
    // Internal grid data
    // ------------------------------

    private Vector3Int cellOrigin;   // bottom-left cell index in the tilemap
    private Vector2Int gridSize;     // number of cells in X/Y
    private Node[,] grid;

    private class Node
    {
        public int x;
        public int y;
        public bool walkable;
        public Vector2 worldPos;

        public float gCost; //accumulating cost from the start node to the current node
        public float hCost; //huristic cost: any approach to problem solving that employs a pragmatic method that is not fully optimized,
        public float fCost => gCost + hCost; //Goal: minimize fCost.
        public Node parent;

        public Node(int x, int y, bool walkable, Vector2 worldPos)
        {
            this.x = x;
            this.y = y;
            this.walkable = walkable;
            this.worldPos = worldPos;
        }
    }

    // ------------------------------
    // Unity lifecycle
    // ------------------------------

    private void Awake()
    {
        // Awake runs only in Play Mode. In Edit Mode we rebuild via OnEnable/OnValidate.
        if (Application.isPlaying)
            BuildGridFromTilemap();
    }

    private void OnEnable()
    {
        // ExecuteAlways makes this run in Edit Mode too.
        // Build only if debug is enabled (avoids editor slowdown).
        TryBuildGridIfNeeded();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Called in editor when inspector values change.
        // Build only if debug is enabled (avoids editor slowdown).
        TryBuildGridIfNeeded();
    }
#endif

    private void TryBuildGridIfNeeded()
    {
        if (!groundTilemap) return;
        if (!drawDebugGrid) return;

        BuildGridFromTilemap();
    }

    // ------------------------------
    // Gizmos
    // ------------------------------

    private void OnDrawGizmos()
    {
        if (!drawWhenNotSelected) return;
        DrawGridGizmosIfReady();
    }

    private void OnDrawGizmosSelected()
    {
        if (drawWhenNotSelected) return;
        DrawGridGizmosIfReady();
    }

    private void DrawGridGizmosIfReady()
    {
        if (!drawDebugGrid || grid == null || groundTilemap == null)
            return;

        Vector3 cellWorldSize = groundTilemap.cellSize;
        Vector3 cubeSize = cellWorldSize * debugCubeScale;

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Node n = grid[x, y];
                Gizmos.color = n.walkable ? Color.green : Color.red;
                Gizmos.DrawWireCube(n.worldPos, cubeSize);
            }
        }
    }

    // ------------------------------
    // Grid building
    // ------------------------------

    /// <summary>
    /// Builds a grid over the ground tilemap.
    /// Every cell inside the tilemap bounds is walkable by default,
    /// unless it has no tile or overlaps a SOLID obstacle collider.
    /// Triggers can be ignored based on ignoreTriggerObstacles.
    /// </summary>
    public void BuildGridFromTilemap()
    {
        if (groundTilemap == null)
        {
            Debug.LogError("[TilemapPathfinder2D] Ground tilemap not assigned.");
            return;
        }

        BoundsInt bounds = groundTilemap.cellBounds;
        cellOrigin = bounds.min;
        gridSize = new Vector2Int(bounds.size.x, bounds.size.y);

        grid = new Node[gridSize.x, gridSize.y];

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3Int cell = new Vector3Int(cellOrigin.x + x, cellOrigin.y + y, 0);

                // center of this tile in world space
                Vector3 worldCenter = groundTilemap.GetCellCenterWorld(cell); 
                Vector2 worldPos2D = new Vector2(worldCenter.x, worldCenter.y);

                bool hasTile = groundTilemap.HasTile(cell);

                bool hasObstacle = HasObstacleAt(worldPos2D);

                bool walkable =  (hasTile) && (!hasObstacle);

                grid[x, y] = new Node(x, y, walkable, worldPos2D);
            }
        }

        if (logBuildInfo)
            Debug.Log($"[TilemapPathfinder2D] Grid built. Size: {gridSize.x} x {gridSize.y}");
    }

    // ------------------------------
    // Pathfinding API
    // ------------------------------

    /// <summary>
    /// Finds a path from startWorld to targetWorld.
    /// Returns a list of world positions (including start & end) or null if no path exists.
    /// </summary>
    public List<Vector3> FindPath(Vector2 startWorld, Vector2 targetWorld)
    {
        if (grid == null)
        {
            if (logBuildInfo)
                Debug.LogWarning("[TilemapPathfinder2D] Grid not built yet. Call BuildGridFromTilemap().");
            return null;
        }

        Node startNode = WorldToNode(startWorld);
        Node targetNode = WorldToNode(targetWorld);

        if (startNode == null || targetNode == null || !targetNode.walkable)
        {
            if (logBuildInfo)
                Debug.Log("[TilemapPathfinder2D] Start or target invalid / not walkable.");
            return null;
        }

        // Reset node costs
        foreach (Node n in grid)
        {
            n.gCost = float.MaxValue;
            n.hCost = 0f;
            n.parent = null;
        }

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.gCost = 0f;
        startNode.hCost = Heuristic(startNode, targetNode);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node current = GetLowestCostNode(openSet); // get the lowest weight since Csharp doesnt have a min heap

            if (current == targetNode) // found the target then return the path
                return RetracePath(startNode, targetNode);

            openSet.Remove(current); // mark as visited
            closedSet.Add(current); 

            foreach (Node neighbor in GetNeighbours(current))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))//Non walkable or already visited then continue on
                    continue;

                float tentativeG = current.gCost + 1f; // 

                if (tentativeG < neighbor.gCost)
                {
                    neighbor.gCost = tentativeG;
                    neighbor.hCost = Heuristic(neighbor, targetNode);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        if (logBuildInfo)
            Debug.Log("[TilemapPathfinder2D] No path found.");

        return null;
    }

    // ------------------------------
    // A* helpers
    // ------------------------------

    private static Node GetLowestCostNode(List<Node> openSet)
    {
        Node best = openSet[0];

        for (int i = 1; i < openSet.Count; i++)
        {
            Node candidate = openSet[i];

            bool lowerF = candidate.fCost < best.fCost;
            bool equalFLowerH = Mathf.Approximately(candidate.fCost, best.fCost) &&
                                candidate.hCost < best.hCost;

            if (lowerF || equalFLowerH)
                best = candidate;
        }

        return best;
    }

    private static float Heuristic(Node a, Node b)
    {
        // Manhattan distance (4-directional movement)
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Node> nodes = new List<Node>();
        Node current = endNode;

        while (current != startNode)
        {
            nodes.Add(current);
            current = current.parent;
            if (current == null) return null; // safety (should not happen)
        }

        nodes.Add(startNode);
        nodes.Reverse();

        List<Vector3> worldPath = new List<Vector3>(nodes.Count);
        foreach (Node n in nodes)
            worldPath.Add(new Vector3(n.worldPos.x, n.worldPos.y, 0f));

        return worldPath;
    }

    private IEnumerable<Node> GetNeighbours(Node node)
    {
        // 4-neighbour grid (no diagonals)
        Vector2Int[] offsets =
        {
            new Vector2Int( 1,  0),
            new Vector2Int(-1,  0),
            new Vector2Int( 0,  1),
            new Vector2Int( 0, -1),
        };

        foreach (var o in offsets)
        {
            int nx = node.x + o.x;
            int ny = node.y + o.y;

            if (nx >= 0 && nx < gridSize.x && ny >= 0 && ny < gridSize.y)
                yield return grid[nx, ny];
        }
    }

    private Node WorldToNode(Vector2 worldPos)
    {
        Vector3Int cell = groundTilemap.WorldToCell(worldPos);

        int gx = cell.x - cellOrigin.x;
        int gy = cell.y - cellOrigin.y;

        if (gx < 0 || gx >= gridSize.x || gy < 0 || gy >= gridSize.y)
            return null;

        return grid[gx, gy];
    }

    // ------------------------------
    // Obstacle filtering (Triggers)
    // ------------------------------

    private bool HasObstacleAt(Vector2 worldPos)
    {
        return checkAllCollidersAtPoint ? HasObstacleAt_All(worldPos) : HasObstacleAt_Single(worldPos);
    }

    private bool HasObstacleAt_Single(Vector2 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos, obstacleMask);
        if (hit == null)
            return false;

        if (ignoreTriggerObstacles && hit.isTrigger)
            return false;

        return true;
    }

    private bool HasObstacleAt_All(Vector2 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos, obstacleMask);
        if (hits == null || hits.Length == 0)
            return false;

        foreach (Collider2D h in hits)
        {
            if (h == null) continue;

            if (ignoreTriggerObstacles && h.isTrigger)
                continue;

            return true; // found a solid obstacle
        }

        return false;
    }

    public Vector2 SnapToNearestWalkable(Vector2 worldPos, int maxRadius = 30) // From an Illegal Tile position find the nearest valid tile to start and trigger A* for the enemy.
    {
        if (grid == null) BuildGridFromTilemap();

        Vector3Int c = groundTilemap.WorldToCell(worldPos); //WorldMap{float} -> tileMap{int}
        int cx = c.x - cellOrigin.x;
        int cy = c.y - cellOrigin.y;

        bool In(int x, int y) => x >= 0 && x < gridSize.x && y >= 0 && y < gridSize.y; //lambda function in order to check if a position [i,j] is in range.

        // If already valid
        if (In(cx, cy) && grid[cx, cy].walkable)
            return grid[cx, cy].worldPos;

        // Search around
        for (int r = 1; r <= maxRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (!In(x, y)) continue;
                    if (grid[x, y].walkable)
                        return grid[x, y].worldPos;
                }
        }

        Debug.LogWarning("No walkable tile found near " + worldPos);
        return worldPos; // fallback,instead of returning null.
    }

    public bool TryGetApproachTile(Vector2 obstacleWorld, Vector2 fromWorld, out Vector2 approachWorld) // this function choose a sutiable tile near the Bed Garden to steal the crop
    {
        approachWorld = obstacleWorld;
        if (grid == null) BuildGridFromTilemap();

        Vector3Int bedCell = groundTilemap.WorldToCell(obstacleWorld);

        // direction from thief -> bed
        Vector2 d = obstacleWorld - fromWorld;

        // preferred side to stand on (approach from the side the thief comes from)
        // if bed is to the right of thief => thief comes from left => stand LEFT of bed => (-1,0)
        Vector3Int[] preferred =
            Mathf.Abs(d.x) >= Mathf.Abs(d.y)
            ? (d.x >= 0 ? new[] { new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 1, 0), new Vector3Int(1, 0, 0) }
                        : new[] { new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 1, 0), new Vector3Int(-1, 0, 0) })
            : (d.y >= 0 ? new[] { new Vector3Int(0, -1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0) }
                        : new[] { new Vector3Int(0, 1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0) });

        foreach (var off in preferred)
        {
            Vector3Int c = bedCell + off;
            Vector2 w = (Vector2)groundTilemap.GetCellCenterWorld(c);

            Node n = WorldToNode(w);
            if (n != null && n.walkable)
            {
                approachWorld = n.worldPos;
                return true;
            }
        }

        return false;
    }



}
