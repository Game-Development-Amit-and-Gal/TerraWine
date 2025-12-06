// Assets/Scripts/Barrels/Barrel.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class Barrel : MonoBehaviour, IPointerClickHandler
{
    [Header("Identity")]
    [SerializeField] private string barrelId = "BARREL_1";   // אם יהיו כמה חביות, אפשר לתת לכל אחת id אחר (לעתיד לשמירה)

    [Header("Items")]
    [SerializeField] private string grapeItemId = "Cabernet_Sauvignon_Grap";
    [SerializeField] private string bottleItemId = "Cabernet_Sauvignon_Bottle";
    [SerializeField] private int grapesPerBottle = 5;

    [Header("Aging Times (seconds)")]
    [SerializeField] private float semiDrySeconds = 20f;   // 2 דקות
    [SerializeField] private float drySeconds = 300f;       // 5 דקות

    [Header("UI")]
    [SerializeField] private BarrelUI ui;   // נגרור את הפאנל של ה-UI לכאן

    // סטייט של החבית
    int grapesInside = 0;
    bool isAging = false;
    bool isReady = false;
    float remainingSeconds = 0f;
    float totalSeconds = 0f;
    bool isDry = false;   // true = יבש, false = חצי יבש

    public int GrapesPerBottle => grapesPerBottle;
    public float SemiDrySeconds => semiDrySeconds;
    public float DrySeconds => drySeconds;

    public void OnPointerClick(PointerEventData eventData)
    {
        // אם החבית פנויה – פותחים UI לבחור כמה ענבים וכמה זמן
        if (!isAging && !isReady)
        {
            if (InventoryManager.Instance == null || ui == null) return;

            int grapesInBag = InventoryManager.Instance.CountOf(grapeItemId);
            ui.OpenForBarrel(this, grapesInBag);
        }
        // אם היין מוכן – קוטפות בקבוקים
        else if (isReady)
        {
            HarvestBottles();
        }
        else
        {
            // עדיין מתיישן – אפשר להציג הודעה אם תרצי
            Debug.Log("[Barrel] Wine is still aging...");
        }
    }

    // קריאה מ-BarrelUI אחרי שהשחקן בחר כמות וזמן
    public void StartAging(int grapesToUse, bool makeDry)
    {
        if (isAging || isReady) return;
        if (InventoryManager.Instance == null) return;

        // משתמשים רק במכפלות של 5
        int usableGrapes = (grapesToUse / grapesPerBottle) * grapesPerBottle;
        if (usableGrapes <= 0) return;

        // מוודאים שיש מספיק ענבים
        int inBag = InventoryManager.Instance.CountOf(grapeItemId);
        if (inBag < usableGrapes)
        {
            Debug.LogWarning($"[Barrel] Not enough grapes. Have {inBag}, tried to use {usableGrapes}");
            return;
        }

        // מורידים ענבים מהתיק
        bool removed = InventoryManager.Instance.Remove(grapeItemId, usableGrapes);
        if (!removed)
        {
            Debug.LogWarning("[Barrel] Failed to remove grapes from inventory");
            return;
        }

        grapesInside = usableGrapes;
        isDry = makeDry;
        totalSeconds = makeDry ? drySeconds : semiDrySeconds;
        remainingSeconds = totalSeconds;
        isAging = true;
        isReady = false;

        StopAllCoroutines();
        StartCoroutine(AgingRoutine());

        Debug.Log($"[Barrel] Started aging {grapesInside} grapes for {(makeDry ? "dry" : "semi-dry")} wine. Time={totalSeconds}s");
    }

    System.Collections.IEnumerator AgingRoutine()
    {
        while (remainingSeconds > 0f && isAging)
        {
            remainingSeconds -= Time.deltaTime;
            // כאן אפשר לעדכן UI של טיימר אם תרצי
            yield return null;
        }

        if (!isAging) yield break;

        isAging = false;
        isReady = true;
        remainingSeconds = 0f;

        Debug.Log("[Barrel] Wine is ready!");
    }

    void HarvestBottles()
    {
        if (!isReady || grapesInside <= 0) return;
        if (InventoryManager.Instance == null) return;

        int bottles = grapesInside / grapesPerBottle;
        if (bottles <= 0)
        {
            Debug.LogWarning("[Barrel] No full bottles to harvest");
            return;
        }

        bool added = InventoryManager.Instance.Add(bottleItemId, bottles);
        Debug.Log($"[Barrel] Harvested {bottles} bottles of wine. success={added}");

        // מאפסים חבית
        grapesInside = 0;
        isReady = false;
        isAging = false;
        totalSeconds = 0f;
        remainingSeconds = 0f;
    }
}
