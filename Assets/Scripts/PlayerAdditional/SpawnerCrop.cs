// Assets/Scripts/MiniGames/ClosingWall/MiniGameCropSpawner.cs
using UnityEngine;

public class MiniGameCropSpawner : MonoBehaviour
{
    // ---- constants (no magic numbers in logic) ----
    private const float ZERO_F = 0f;
    private const float ONE_F = 1f;
    private const float NEG_ONE_F = -1f;

    private const int FIRST_INDEX = 0;
    private const int ONE_I = 1;

    // Defaults (can be overridden in Inspector)
    private const float DEFAULT_LAUNCH_SPEED = 8f;
    private const float DEFAULT_LAUNCH_ANGLE_DEG = 65f;
    private const float DEFAULT_MIN_COOLDOWN_SECONDS = 0.15f;

    private const float DEFAULT_ENTRY_WEIGHT = 1f;
    private const int DEFAULT_ENTRY_AMOUNT = 1;

    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject prefab;     // grapePrefab / seedPrefab
        public string itemId;         // MUST match your ItemSO.id
        public int amount = DEFAULT_ENTRY_AMOUNT;
        public float weight = DEFAULT_ENTRY_WEIGHT;
    }

    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private SpawnEntry[] entries;

    [Header("Launch")]
    [SerializeField] private float launchSpeed = DEFAULT_LAUNCH_SPEED;
    [SerializeField] private float launchAngleDeg = DEFAULT_LAUNCH_ANGLE_DEG;
    [SerializeField] private float minCooldownSeconds = DEFAULT_MIN_COOLDOWN_SECONDS;

    private float lastSpawnTime = float.NegativeInfinity;

    private const int ZERO_I = 0;

    public void SpawnAndLaunch(Transform player)
    {
        if (spawnPoint == null) return;
        if (entries == null || entries.Length == ZERO_I) return;

        if (Time.time - lastSpawnTime < minCooldownSeconds) return;
        lastSpawnTime = Time.time;

        SpawnEntry e = PickWeighted(entries);
        if (e == null || e.prefab == null) return;

        GameObject go = Instantiate(e.prefab, spawnPoint.position, Quaternion.identity);
        Debug.Log("Instantiate: " + e.prefab.name);

        // ✅ CHILDSAFE: look on root OR children
        MiniGamePickup pickup =
            go.GetComponent<MiniGamePickup>() ?? go.GetComponentInChildren<MiniGamePickup>(true);

        if (pickup != null)
            pickup.Configure(e.itemId, e.amount);

        Rigidbody2D body =
            go.GetComponent<Rigidbody2D>() ?? go.GetComponentInChildren<Rigidbody2D>(true);

        if (body == null)
        {
            Debug.LogWarning("[Spawner] No Rigidbody2D on prefab root/children: " + go.name);
            return;
        }

        float dir = FacingSign(player);
        float angleRad = launchAngleDeg * Mathf.Deg2Rad;

        float vx = Mathf.Cos(angleRad) * launchSpeed * dir;
        float vy = Mathf.Sin(angleRad) * launchSpeed;

        body.linearVelocity = new Vector2(vx, vy);
    }

    private static float FacingSign(Transform player)
    {
        if (player == null) return ONE_F;
        return (player.localScale.x < ZERO_F) ? NEG_ONE_F : ONE_F;
    }

    private static SpawnEntry PickWeighted(SpawnEntry[] arr)
    {
        float total = ZERO_F;

        for (int i = FIRST_INDEX; i < arr.Length; i++)
            total += Mathf.Max(ZERO_F, arr[i].weight);

        // if all weights are 0 (or negative), pick first non-null prefab entry
        if (total <= ZERO_F)
        {
            for (int i = FIRST_INDEX; i < arr.Length; i++)
                if (arr[i] != null && arr[i].prefab != null)
                    return arr[i];

            return null;
        }

        float r = Random.value * total;

        for (int i = FIRST_INDEX; i < arr.Length; i++)
        {
            r -= Mathf.Max(ZERO_F, arr[i].weight);
            if (r <= ZERO_F) return arr[i];
        }

        return arr[arr.Length - ONE_I];
    }
}
