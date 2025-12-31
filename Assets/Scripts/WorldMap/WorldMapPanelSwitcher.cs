using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class WorldMapPanelSwitcher : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject actionsPanel; // הפאנל עם Steal a recipe וכו'
    [SerializeField] private GameObject gamePanel;    // הפאנל של GAME

    [Header("Optional UI")]
    [SerializeField] private TMP_Text errorText;      // הודעה לשחקן (לא חובה)

    [Header("Steal Settings")]
    [SerializeField] private int maxBottleSteal = 4;        // 0-4
    [SerializeField] private int maxGrapeSeedSteal = 20;    // 0-20

    // נטען לפי תיקיות בתוך Resources/Items
    private ItemSO[] bottleItems;
    private ItemSO[] seedItems;
    private ItemSO[] grapItems;

    private void Awake()
    {
        // לפי המבנה שלך:
        // Resources/Items/Bottle
        // Resources/Items/Grap
        // Resources/Items/Seed
        bottleItems = Resources.LoadAll<ItemSO>("Items/Bottle");
        grapItems = Resources.LoadAll<ItemSO>("Items/Grap");
        seedItems = Resources.LoadAll<ItemSO>("Items/Seed");
    }

    public void OnClick_StealRecipe()
    {
        if (!TrySpendOneAction()) return;

        if (actionsPanel != null) actionsPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);
    }

    // ✅ Steal Bottle: גונב 0-4 מאותו בקבוק אחד
    public void OnClick_StealBottle()
    {
        if (!TrySpendOneAction()) return;

        var inv = InventoryManager.Instance;
        if (inv == null) { SetMsg("אין InventoryManager בסצנה."); return; }

        int count = Random.Range(0, maxBottleSteal + 1); // 0..4
        if (count == 0) { SetMsg("לא נגנב כלום הפעם."); return; }

        if (bottleItems == null || bottleItems.Length == 0)
        {
            SetMsg("לא נמצאו Items בתיקייה Resources/Items/Bottle.");
            return;
        }

        var chosen = bottleItems[Random.Range(0, bottleItems.Length)];
        bool ok = inv.Add(chosen.id, count);

        SetMsg(ok
            ? $"נגנבו {count} בקבוקים ({chosen.id})."
            : "התיק מלא, לא הצלחתי להוסיף את כל הבקבוקים.");
    }

    // ✅ Steal Grapes: בוחר Seeds או Grapes, ואז 0-20 מחולק בין 1/2/3 סוגים (שווה הסתברות)
    public void OnClick_StealGrapes()
    {
        if (!TrySpendOneAction()) return;

        var inv = InventoryManager.Instance;
        if (inv == null) { SetMsg("אין InventoryManager בסצנה."); return; }

        int total = Random.Range(0, maxGrapeSeedSteal + 1); // 0..20
        if (total == 0) { SetMsg("לא נגנב כלום הפעם."); return; }

        bool pickSeeds = Random.value < 0.5f;
        var pool = pickSeeds ? seedItems : grapItems;

        if (pool == null || pool.Length == 0)
        {
            SetMsg(pickSeeds
                ? "לא נמצאו Items בתיקייה Resources/Items/Seed."
                : "לא נמצאו Items בתיקייה Resources/Items/Grap.");
            return;
        }

        // ✅ k = 1/2/3 בהסתברות שווה (1/3)
        int k = Random.Range(1, 4); // 1..3
        k = Mathf.Min(k, pool.Length);

        // לבחור k שונים
        var available = new List<ItemSO>(pool);
        var chosen = new List<ItemSO>();
        for (int i = 0; i < k; i++)
        {
            int idx = Random.Range(0, available.Count);
            chosen.Add(available[idx]);
            available.RemoveAt(idx);
        }

        // לחלק את total בין k (כל אחד לפחות 1)
        int[] amounts = new int[k];
        int remaining = total;

        for (int i = 0; i < k; i++)
        {
            amounts[i] = 1;
            remaining--;
            if (remaining <= 0) { remaining = 0; break; }
        }

        while (remaining > 0)
        {
            amounts[Random.Range(0, k)]++;
            remaining--;
        }

        // להוסיף לאינבנטורי + לבנות פירוט
        int addedTotal = 0;
        var parts = new List<string>();

        for (int i = 0; i < k; i++)
        {
            if (amounts[i] <= 0) continue;

            bool ok = inv.Add(chosen[i].id, amounts[i]);
            if (!ok) break;

            addedTotal += amounts[i];
            parts.Add($"{chosen[i].id} x{amounts[i]}");
        }

        if (addedTotal <= 0)
        {
            SetMsg("התיק מלא, לא הצלחתי להוסיף את השלל.");
            return;
        }

        string typeName = pickSeeds ? "זרעים" : "ענבים";
        SetMsg($"נגנבו {addedTotal} {typeName} מתוך {k} סוגים: {string.Join(", ", parts)}");
    }

    public void OpenActionsPanel()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (actionsPanel != null) actionsPanel.SetActive(true);
    }

    // ---------------- Helpers ----------------

    private bool TrySpendOneAction()
    {
        if (ActionQuotaManager.Instance != null)
        {
            bool ok = ActionQuotaManager.Instance.TrySpend(1);
            if (!ok)
            {
                SetMsg("אין לך מספיק פעולות להיום.");
                return false;
            }
        }
        return true;
    }

    private void SetMsg(string msg)
    {
        if (errorText != null) errorText.text = msg;
        Debug.Log("[WorldMapPanelSwitcher] " + msg);
    }

    
}
