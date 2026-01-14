using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WorldMapPanelSwitcher : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject actionsPanel;
    [SerializeField] private GameObject gamePanel;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text errorText;

    [Header("Steal Settings")]
    [SerializeField] private int maxBottleSteal = 4;
    [SerializeField] private int maxGrapeSeedSteal = 20;

    [Header("MiniGame Scene - Grapes")]
    [SerializeField] private string closingWallSceneName = "ClosingWallMiniGame";
    [SerializeField] private Vector2 miniGamePlayerSpawnPos = Vector2.zero;

    [Header("MiniGame Scene - Bottles")]
    [SerializeField] private string bottlesMiniGameSceneName = "MiniGameBottles";
    [SerializeField] private Vector2 bottlesMiniGameSpawnPos = Vector2.zero;

    private ItemSO[] bottleItems;
    private ItemSO[] seedItems;
    private ItemSO[] grapItems;

    private void Awake()
    {
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

    // ✅ Steal Bottle -> מעביר למיני-גיים במקום לגנוב ישר
    public void OnClick_StealBottle()
    {
        if (!TrySpendOneAction()) return;

        SetMsg("מתחילים מיני-גיים לגניבת בקבוקים...");

        // ניסיון חדש נקי (לוט זמני)
        MiniGameLootBuffer.Instance?.Clear();

        // אם את רוצה שהמיני-גיים ידע מה מותר לגנוב (אופציונלי)
        // MiniGameLootBuffer.Instance?.SetBottleLimits(maxBottleSteal, bottleItems);

        // מעבר סצנה כמו ב-StealGrapes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeScene(bottlesMiniGameSceneName, bottlesMiniGameSpawnPos);
        }
        else
        {
            Debug.LogWarning("[WorldMapPanelSwitcher] GameManager.Instance is null — cannot change scene.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(bottlesMiniGameSceneName);
        }
    }

    public void OnClick_StealGrapes()
    {
        if (!TrySpendOneAction()) return;

        SetMsg("מתחילים מיני-גיים לגניבת ענבים/זרעים...");

        MiniGameLootBuffer.Instance?.Clear();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeScene(closingWallSceneName, miniGamePlayerSpawnPos);
        }
        else
        {
            Debug.LogWarning("[WorldMapPanelSwitcher] GameManager.Instance is null — cannot change scene.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(closingWallSceneName);
        }
    }

    public void OpenActionsPanel()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (actionsPanel != null) actionsPanel.SetActive(true);
    }

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
