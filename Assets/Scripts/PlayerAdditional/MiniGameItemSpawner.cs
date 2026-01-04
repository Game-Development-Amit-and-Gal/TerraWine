using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MiniGameItemSpawner : MonoBehaviour
{
    [Header("Ground")]
    public Tilemap groundTilemap;
    public float minWorldY = -1f;

    [Header("Pickup Prefab")]
    public MiniGamePickup pickupPrefab;
    public Transform spawnedParent;

    [Header("Only these ItemSO folders")]
    public string seedFolder = "Items/Seed";
    public string grapFolder = "Items/Grap";

    [Header("Random")]
    [Range(2, 5)] public int minDistinctItems = 2;
    [Range(2, 5)] public int maxDistinctItems = 5;

    [Range(3, 20)] public int minTotalSpawns = 3;
    [Range(3, 20)] public int maxTotalSpawns = 20;

    // Amount תמיד 1, אז השדות האלה לא באמת משנים כלום,
    // אבל השארתי כדי שלא ישבר לך ה-Inspector.
    [Header("Amount per pickup (ignored - always 1)")]
    [Range(1, 1)] public int minAmount = 1;
    [Range(1, 1)] public int maxAmount = 1;

    public Vector2 randomOffsetInCell = new Vector2(0.2f, 0f);
    public bool spawnOnStart = true;

    [Header("Debug")]
    public bool verboseLogs = true;

    [ContextMenu("Spawn Items")]
    public void Spawn()
    {
        Debug.Log("[MiniGameItemSpawner] Spawn called");

        if (!groundTilemap) { Debug.LogError("[MiniGameItemSpawner] No groundTilemap"); return; }
        if (!pickupPrefab) { Debug.LogError("[MiniGameItemSpawner] No pickupPrefab"); return; }

        // Load from Resources
        var seeds = Resources.LoadAll<ItemSO>(seedFolder);
        var grapes = Resources.LoadAll<ItemSO>(grapFolder);

        if (verboseLogs)
        {
            Debug.Log($"[MiniGameItemSpawner] LoadAll paths: seedFolder='{seedFolder}', grapFolder='{grapFolder}'");
            Debug.Log($"[MiniGameItemSpawner] Seeds loaded: {(seeds != null ? seeds.Length : 0)}");
            Debug.Log($"[MiniGameItemSpawner] Grapes loaded: {(grapes != null ? grapes.Length : 0)}");
        }

        // Build combined list
        var all = new List<ItemSO>();
        if (seeds != null) all.AddRange(seeds);
        if (grapes != null) all.AddRange(grapes);

        if (verboseLogs)
            Debug.Log($"[MiniGameItemSpawner] Total items before icon filter: {all.Count}");

        // Icon filter + counters
        int beforeFilter = all.Count;
        int seedsMissingIcon = 0;
        int grapesMissingIcon = 0;

        if (seeds != null)
            foreach (var s in seeds)
                if (s == null || s.icon == null) seedsMissingIcon++;

        if (grapes != null)
            foreach (var g in grapes)
                if (g == null || g.icon == null) grapesMissingIcon++;

        all.RemoveAll(x => x == null || x.icon == null);

        if (verboseLogs)
        {
            Debug.Log($"[MiniGameItemSpawner] Seeds missing icon: {seedsMissingIcon}");
            Debug.Log($"[MiniGameItemSpawner] Grapes missing icon: {grapesMissingIcon}");
            Debug.Log($"[MiniGameItemSpawner] Total items after icon filter: {all.Count} (removed {beforeFilter - all.Count})");
        }

        if (all.Count == 0)
        {
            Debug.LogError("[MiniGameItemSpawner] No ItemSO found in Seed/Grap folders (with icons).");
            return;
        }

        // Collect valid ground cells
        var validCells = CollectValidCells();

        if (verboseLogs)
        {
            Debug.Log($"[MiniGameItemSpawner] GroundTilemap name: {groundTilemap.name}");
            Debug.Log($"[MiniGameItemSpawner] Tilemap bounds: {groundTilemap.cellBounds}");
            Debug.Log($"[MiniGameItemSpawner] validCells count: {validCells.Count} (minWorldY={minWorldY})");
        }

        if (validCells.Count == 0)
        {
            Debug.LogError("[MiniGameItemSpawner] No valid ground cells (HasTile + y>=minWorldY).");
            return;
        }

        // Pick distinct
        int distinctCount = Random.Range(minDistinctItems, maxDistinctItems + 1);
        distinctCount = Mathf.Clamp(distinctCount, 1, all.Count);
        var chosen = PickRandomDistinct(all, distinctCount);

        // Total spawns
        int totalSpawns = Random.Range(minTotalSpawns, maxTotalSpawns + 1);

        if (verboseLogs)
        {
            Debug.Log($"[MiniGameItemSpawner] distinctCount chosen: {distinctCount}");
            Debug.Log($"[MiniGameItemSpawner] totalSpawns: {totalSpawns}");

            for (int i = 0; i < chosen.Count; i++)
            {
                var it = chosen[i];
                string guess = "(unknown)";
                if (it != null && it.isSeed) guess = "(Seed)";
                else guess = "(Grap?)";
                Debug.Log($"[MiniGameItemSpawner] Chosen[{i}] {guess} id='{it?.id}' name='{it?.name}' icon={(it?.icon != null)}");
            }
        }

        // New run
        MiniGameLootBuffer.Instance?.Clear();

        // Spawn loop
        int spawned = 0;
        for (int i = 0; i < totalSpawns; i++)
        {
            var item = chosen[Random.Range(0, chosen.Count)];

            int amount = 1; // ✅ תמיד 1 (לא רנדומלי)

            var cell = validCells[Random.Range(0, validCells.Count)];
            Vector3 pos = groundTilemap.GetCellCenterWorld(cell);
            pos += new Vector3(
                Random.Range(-randomOffsetInCell.x, randomOffsetInCell.x),
                Random.Range(-randomOffsetInCell.y, randomOffsetInCell.y) + 0.5f,
                0f
            );

            var pickup = Instantiate(pickupPrefab, pos, Quaternion.identity, spawnedParent);
            pickup.Init(item.id, amount);

            // ✅ סקייל לענבים: X=0.1 Y=0.2
            bool isGrap = item.id != null && item.id.Contains("_Grap"); // לפי השמות אצלך
            if (isGrap)
                pickup.transform.localScale = new Vector3(0.16f, 0.12f, 1f);

            spawned++;

            if (verboseLogs && i < 10)
                Debug.Log($"[MiniGameItemSpawner] Spawned #{i + 1}: id='{item.id}', amount={amount}, worldPos={pos}, cell={cell}, isGrap={isGrap}");
        }

        Debug.Log($"[MiniGameItemSpawner] DONE. Spawned objects: {spawned}");
    }

    private List<Vector3Int> CollectValidCells()
    {
        var valid = new List<Vector3Int>();
        var bounds = groundTilemap.cellBounds;

        int totalChecked = 0;
        int hasTileCount = 0;
        int yOkCount = 0;

        foreach (var c in bounds.allPositionsWithin)
        {
            totalChecked++;

            if (!groundTilemap.HasTile(c)) continue;
            hasTileCount++;

            float y = groundTilemap.GetCellCenterWorld(c).y;
            if (y < minWorldY) continue;
            yOkCount++;

            valid.Add(c);
        }

        if (verboseLogs)
            Debug.Log($"[MiniGameItemSpawner] Cells checked={totalChecked}, HasTile={hasTileCount}, y>=minWorldY={yOkCount}, valid={valid.Count}");

        return valid;
    }

    private List<ItemSO> PickRandomDistinct(List<ItemSO> source, int count)
    {
        var pool = new List<ItemSO>(source);
        var result = new List<ItemSO>(count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        return result;
    }

    private void Start()
    {
        Debug.Log($"[MiniGameItemSpawner] START on '{name}' instanceID={GetInstanceID()} at path={GetHierarchyPath(transform)} minDistinct={minDistinctItems} maxDistinct={maxDistinctItems}");
        if (spawnOnStart) Spawn();
    }

    private string GetHierarchyPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
