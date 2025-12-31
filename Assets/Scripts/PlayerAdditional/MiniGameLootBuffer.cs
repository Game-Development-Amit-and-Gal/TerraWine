using System.Collections.Generic;
using UnityEngine;

public class MiniGameLootBuffer : MonoBehaviour
{
    public static MiniGameLootBuffer Instance { get; private set; }

    private readonly Dictionary<string, int> pending = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void Add(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) return;

        if (pending.TryGetValue(itemId, out int cur)) pending[itemId] = cur + amount;
        else pending[itemId] = amount;
    }

    public void Clear() => pending.Clear();

    public void CommitToInventory()
    {
        if (InventoryManager.Instance == null) return;

        foreach (var kv in pending)
            InventoryManager.Instance.Add(kv.Key, kv.Value);

        pending.Clear();
    }
}
