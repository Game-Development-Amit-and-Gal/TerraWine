using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Randomly spawns a set amount of tree prefabs on valid tiles of a Tilemap.
/// Useful for procedural decoration (e.g., forests on the world map).
/// </summary>
public class RandomTreeSpawner : MonoBehaviour
{
    /// <summary>
    /// Configuration for a specific type of tree:
    /// which prefab to spawn and how many copies.
    /// </summary>
    [System.Serializable]
    public class TreeConfig
    {
        public GameObject prefab;  // Prefab of the tree to spawn
        public int count;          // How many trees of this type to spawn
    }

    [Header("Tilemap Source")]
    [SerializeField] private Tilemap groundTilemap;  // Tilemap to sample for spawn positions

    [Header("Tree Types")]
    [SerializeField] private TreeConfig[] trees;     // All tree types and their spawn counts

    [Header("Collision (Optional)")]
    [SerializeField] private LayerMask obstacleMask; // Trees won't spawn if object exists in this layer
    [SerializeField] private float collisionRadius = 0.2f;   // Radius for checking collisions before spawning
    [SerializeField] private int maxTriesPerTree = 30;       // Attempts before giving up on a spawn

    [Header("Spawn Area Margin")]
    [SerializeField] private int edgeMargin = 1; // Prevent trees spawning at extreme edge of tilemap

    private void Start()
    {
        SpawnAllTrees();
    }

    /// <summary>
    /// Spawns all tree types according to their specified counts and settings.
    /// </summary>
    private void SpawnAllTrees()
    {
        int zero = 0; //avoid magic numbers
        // Tilemap area boundaries
        BoundsInt bounds = groundTilemap.cellBounds;

        foreach (var tree in trees)
        {
            for (int i = 0; i < tree.count; i++)
            {
                bool placed = false;

                // Try random positions multiple times to avoid overlaps or invalid tiles
                for (int tries = 0; tries < maxTriesPerTree && !placed; tries++)
                {
                    int x = Random.Range(bounds.xMin + edgeMargin, bounds.xMax - edgeMargin);
                    int y = Random.Range(bounds.yMin + edgeMargin, bounds.yMax - edgeMargin);
                    Vector3Int cellPos = new Vector3Int(x, y, zero);

                    // Only spawn if tile exists here
                    if (!groundTilemap.HasTile(cellPos))
                        continue;

                    Vector3 worldPos = groundTilemap.GetCellCenterWorld(cellPos);

                    // Check for collisions (avoid putting a tree where something already exists)
                    if (Physics2D.OverlapCircle(worldPos, collisionRadius, obstacleMask) != null)
                        continue;

                    // Spawn tree and parent it to this spawner
                    Instantiate(tree.prefab, worldPos, Quaternion.identity, transform);
                    placed = true;
                }
            }
        }
    }
}
