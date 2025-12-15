using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WineScoreSystem : MonoBehaviour
{
    private readonly Dictionary<string, int> lastBottleCounts = new();
    private bool initialized = false;

    private void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        while (InventoryManager.Instance == null)
            yield return null;

        InventoryManager.Instance.onChanged.AddListener(OnInventoryChanged);

        SnapshotCurrentBottles(); // חשוב: לא נותן ניקוד על הטעינה הראשונית
        initialized = true;
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.onChanged.RemoveListener(OnInventoryChanged);
    }

    private void SnapshotCurrentBottles()
    {
        lastBottleCounts.Clear();
        var current = BuildBottleCounts();
        foreach (var kvp in current)
            lastBottleCounts[kvp.Key] = kvp.Value;
    }

    private Dictionary<string, int> BuildBottleCounts()
    {
        var result = new Dictionary<string, int>();
        var inv = InventoryManager.Instance;
        if (inv == null) return result;

        foreach (var s in inv.Slots)
        {
            if (string.IsNullOrEmpty(s.id) || s.amount <= 0) continue;

            var item = inv.GetDefinition(s.id);
            if (item == null || !item.isWineBottle) continue;

            if (!result.ContainsKey(s.id)) result[s.id] = 0;
            result[s.id] += s.amount;
        }

        return result;
    }

    private void OnInventoryChanged()
    {
        if (!initialized) return;
        if (GameManager.Instance == null || GameManager.Instance.Data == null) return;

        var current = BuildBottleCounts();
        int deltaScore = 0;

        // בקבוקים שקיימים עכשיו
        foreach (var kvp in current)
        {
            string bottleId = kvp.Key;
            int now = kvp.Value;
            lastBottleCounts.TryGetValue(bottleId, out int prev);

            int delta = now - prev;
            if (delta != 0)
            {
                int rating = WineRecipeLookup.GetRatingForBottle(bottleId);
                deltaScore += delta * rating;
            }
        }

        // בקבוקים שנעלמו לגמרי
        foreach (var kvp in lastBottleCounts)
        {
            string bottleId = kvp.Key;
            if (current.ContainsKey(bottleId)) continue;

            int prev = kvp.Value;
            int delta = 0 - prev;

            int rating = WineRecipeLookup.GetRatingForBottle(bottleId);
            deltaScore += delta * rating;
        }

        // עדכון snapshot
        lastBottleCounts.Clear();
        foreach (var kvp in current)
            lastBottleCounts[kvp.Key] = kvp.Value;

        if (deltaScore == 0) return;

        // Apply
        GameManager.Instance.Data.wineScore += deltaScore;
        if (GameManager.Instance.Data.wineScore < 0)
            GameManager.Instance.Data.wineScore = 0;

        // שמירה פשוטה (בלי להזיז שחקן וכו')
        SaveSystem.Save(GameManager.Instance.Data);
    }
}
