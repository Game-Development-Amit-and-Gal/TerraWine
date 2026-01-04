using System.Collections.Generic;
using TMPro;
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

    [Header("MiniGame Scene")]
    [SerializeField] private string closingWallSceneName = "ClosingWallMiniGame";
    [SerializeField] private Vector2 miniGamePlayerSpawnPos = Vector2.zero;


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

        // אופציונלי: הודעה / סגירת UI
        SetMsg("מתחילים מיני-גיים לגניבת ענבים/זרעים...");

        // חשוב: מתחילים ניסיון חדש נקי (לוט זמני)
        MiniGameLootBuffer.Instance?.Clear();

        // מעבר סצנה "כמו WorldMapZone"
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeScene(closingWallSceneName, miniGamePlayerSpawnPos);
        }
        else
        {
            Debug.LogWarning("[WorldMapPanelSwitcher] GameManager.Instance is null — cannot change scene.");
            // fallback אם אין GameManager
            UnityEngine.SceneManagement.SceneManager.LoadScene(closingWallSceneName);
        }
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
