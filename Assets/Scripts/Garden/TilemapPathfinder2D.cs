using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 2D A* pathfinder that builds a grid directly from a Tilemap.
/// The Tilemap defines the walkable area (e.g. Grass).
/// Cells that overlap an obstacle collider are marked as not walkable.
/// </summary>
public class TilemapPathfinder2D : MonoBehaviour
{
    [Header("Tilemap & Layers")]
    [Tooltip("Tilemap that defines the walkable area (e.g. Grass).")]
    [SerializeField] private Tilemap groundTilemap;

    [Tooltip("LayerMask for obstacles (house, fence, truck, etc.).")]
    [SerializeField] private LayerMask obstacleMask;

    [Tooltip("Size of each debug square in world units (visual only).")]
    [SerializeField] private float cellSize = 1f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGrid = false;

    // Internal grid data
    private Vector3Int cellOrigin;   // bottom-left cell index in the tilemap
    private Vector2Int gridSize;     // number of cells in X/Y
    private Node[,] grid;

    private class Node
    {
        public int x;
        public int y;
        public bool walkable;
        public Vector2 worldPos;

        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost;
        public Node parent;

        public Node(int x, int y, bool walkable, Vector2 worldPos)
        {
            this.x = x;
            this.y = y;
            this.walkable = walkable;
            this.worldPos = worldPos;
        }
    }

    private void Awake()
    {
        BuildGridFromTilemap();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGrid || grid == null)
            return;

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Node n = grid[x, y];
                Gizmos.color = n.walkable ? Color.green : Color.red;
                Gizmos.DrawWireCube(n.worldPos, Vector3.one * (cellSize * 0.9f));
            }
        }
    }

    /// <summary>
    /// Builds a grid over the Grass tilemap.
    /// Every cell inside the tilemap bounds is walkable by default,
    /// unless it has no tile or overlaps an obstacle collider.
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

                // walkable only if there is a tile AND no obstacle
                bool hasTile = groundTilemap.HasTile(cell);
                bool hasObstacle = Physics2D.OverlapPoint(worldPos2D, obstacleMask);

                bool walkable = hasTile && !hasObstacle;

                grid[x, y] = new Node(x, y, walkable, worldPos2D);
            }
        }

        Debug.Log($"[TilemapPathfinder2D] Grid built. Size: {gridSize.x} x {gridSize.y}");
    }

    /// <summary>
    /// Finds a path from startWorld to targetWorld.
    /// Returns a list of world positions (including start & end) or null if no path exists.
    /// </summary>
    public List<Vector3> FindPath(Vector2 startWorld, Vector2 targetWorld)
    {
        if (grid == null)
        {
            Debug.LogWarning("[TilemapPathfinder2D] Grid not built yet.");
            return null;
        }

        Node startNode = WorldToNode(startWorld);
        Node targetNode = WorldToNode(targetWorld);

        if (startNode == null || targetNode == null || !targetNode.walkable)
        {
            Debug.Log("[TilemapPathfinder2D] Start or target invalid/not walkable.");
            return null;
        }

        // Reset all nodes
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
            Node current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < current.fCost ||
                    (Mathf.Approximately(openSet[i].fCost, current.fCost) &&
                     openSet[i].hCost < current.hCost))
                {
                    current = openSet[i];
                }
            }

            if (current == targetNode)
                return RetracePath(startNode, targetNode);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in GetNeighbours(current))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                float tentativeG = current.gCost + 1f;

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

        Debug.Log("[TilemapPathfinder2D] No path found.");
        return null;
    }

    // ---------- helpers ----------

    private float Heuristic(Node a, Node b)
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
        }
        nodes.Add(startNode);
        nodes.Reverse();

        List<Vector3> worldPath = new List<Vector3>(nodes.Count);
        foreach (Node n in nodes)
        {
            worldPath.Add(new Vector3(n.worldPos.x, n.worldPos.y, 0f));
        }
        return worldPath;
    }

    private IEnumerable<Node> GetNeighbours(Node node)
    {
        // 4 neighbours (no diagonals) – good for your farm style
        int[,] offsets =
        {
            {  1,  0 },
            { -1,  0 },
            {  0,  1 },
            {  0, -1 }
        };

        for (int i = 0; i < offsets.GetLength(0); i++)
        {
            int nx = node.x + offsets[i, 0];
            int ny = node.y + offsets[i, 1];

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
}
