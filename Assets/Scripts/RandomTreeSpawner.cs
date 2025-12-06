using UnityEngine;
using UnityEngine.Tilemaps;

public class RandomTreeSpawner : MonoBehaviour
{
    [System.Serializable]

    public class TreeConfig
    {
        public GameObject prefab;  
        public int count;           
    }

    [SerializeField] private Tilemap groundTilemap;  
    [SerializeField] private TreeConfig[] trees;     

    [Header("התנגשות (לא חובה)")]
    [SerializeField] private LayerMask obstacleMask;  
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private int maxTriesPerTree = 30;

    [SerializeField] private int edgeMargin = 1;

    private void Start()
    {
        SpawnAllTrees();
    }

    private void SpawnAllTrees()
    {
        BoundsInt bounds = groundTilemap.cellBounds;

        foreach (var tree in trees)
        {
            for (int i = 0; i < tree.count; i++)
            {
                bool placed = false;

                for (int tries = 0; tries < maxTriesPerTree && !placed; tries++)
                {
                    int x = Random.Range(bounds.xMin + edgeMargin, bounds.xMax - edgeMargin);
                    int y = Random.Range(bounds.yMin + edgeMargin, bounds.yMax - edgeMargin);
                    Vector3Int cellPos = new Vector3Int(x, y, 0);


                    if (!groundTilemap.HasTile(cellPos))
                        continue;

                    Vector3 worldPos = groundTilemap.GetCellCenterWorld(cellPos);

                
                    if (Physics2D.OverlapCircle(worldPos, collisionRadius, obstacleMask) != null)
                        continue;

                    Instantiate(tree.prefab, worldPos, Quaternion.identity, transform);
                    placed = true;
                }
            }
        }
    }
}
