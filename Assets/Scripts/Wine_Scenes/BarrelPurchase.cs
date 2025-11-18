using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;   // חשוב בשביל IPointerClickHandler

public class BarrelPurchase : MonoBehaviour, IPointerClickHandler
{
    [Header("Identity")]
    [SerializeField] private string barrelId;   // מזהה ייחודי לחבית הזו (נוצר אוטומטית)

    [Header("Prices")]
    [SerializeField] private int normalPrice = 100;
    [SerializeField] private int premiumPrice = 200;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = new Color(0.6f, 0.4f, 0.2f, 1f);   // לא באמת משתמשים בגוון הזה, רק נשאר
    [SerializeField] private Color premiumColor = new Color(0.35f, 0.2f, 0.08f, 1f); // חבית יוקרתית

    [Header("State (runtime only)")]
    [SerializeField] private bool owned = false;        // האם החבית נקנתה
    [SerializeField] private bool premiumOwned = false; // האם נקנתה כיוקרתית

    // שקיפות של חבית שניתנת לקנייה (150 מתוך 255)
    private readonly float purchasableAlpha = 150f / 255f;

    private void Reset()
    {
        // אם שכחנו לגרור SpriteRenderer – הוא ינסה למצוא אחד באותו אובייקט
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

#if UNITY_EDITOR
    // יצירת ID אוטומטי בזמן עריכה אם שדה barrelId ריק
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(barrelId))
        {
            barrelId = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    private void Start()
    {
        // משחזרים מצב מ-GameData (אם החבית כבר נקנתה בעבר)
        if (GameManager.Instance == null || GameManager.Instance.Data == null)
            return;

        List<OwnedBarrelData> list = GameManager.Instance.Data.ownedBarrels;
        if (list == null) return;

        OwnedBarrelData data = list.Find(b => b.id == barrelId);
        if (data != null)
        {
            owned = true;
            premiumOwned = data.isPremium;

            if (spriteRenderer != null)
            {
                // רגילה: נשאר הצבע המקורי, רק Alpha = 1
                // יוקרתית: מחליפים לצבע הכהה
                Color c = spriteRenderer.color;
                if (data.isPremium)
                {
                    c = premiumColor;
                }
                c.a = 1f; // שקיפות מלאה
                spriteRenderer.color = c;
            }
        }
    }

    private bool IsPurchasable()
    {
        if (owned) return false;
        if (spriteRenderer == null) return false;

        Color c = spriteRenderer.color;
        // נחשבת למכירה רק אם ה-alpha בערך 150/255
        return Mathf.Approximately(c.a, purchasableAlpha);
    }

    private void SaveOwned(bool isPremium)
    {
        // עדכון ב-GameData
        if (GameManager.Instance == null || GameManager.Instance.Data == null)
            return;

        if (GameManager.Instance.Data.ownedBarrels == null)
            GameManager.Instance.Data.ownedBarrels = new List<OwnedBarrelData>();

        var list = GameManager.Instance.Data.ownedBarrels;
        var existing = list.Find(b => b.id == barrelId);

        if (existing == null)
        {
            list.Add(new OwnedBarrelData
            {
                id = barrelId,
                isPremium = isPremium
            });
        }
        else
        {
            existing.isPremium = isPremium;
        }

        // שומרים לדיסק
        SaveSystem.Save(GameManager.Instance.Data);
    }

    public void BuyNormal()
    {
        // כאן בודקים אם בכלל אפשר לקנות
        if (!IsPurchasable())
        {
            Debug.Log("[Barrel] Cannot buy normal (not purchasable or already owned).");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Barrel] No GameManager.");
            return;
        }

        if (!GameManager.Instance.TrySpendMoney(normalPrice))
        {
            Debug.Log("[Barrel] Not enough money for normal barrel.");
            return;
        }

        owned = true;
        premiumOwned = false;

        if (spriteRenderer != null)
        {
            // לוקחים את הצבע הנוכחי (עם הגוון המקורי), רק מעלים Alpha ל-255
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        SaveOwned(false);
        Debug.Log("[Barrel] Bought NORMAL barrel.");
    }

    public void BuyPremium()
    {
        if (!IsPurchasable())
        {
            Debug.Log("[Barrel] Cannot buy premium (not purchasable or already owned).");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Barrel] No GameManager.");
            return;
        }

        if (!GameManager.Instance.TrySpendMoney(premiumPrice))
        {
            Debug.Log("[Barrel] Not enough money for premium barrel.");
            return;
        }

        owned = true;
        premiumOwned = true;

        if (spriteRenderer != null)
        {
            Color c = premiumColor;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        SaveOwned(true);
        Debug.Log("[Barrel] Bought PREMIUM barrel.");
    }

   
    public void OnPointerClick(PointerEventData eventData)
    {
        // נוודא שזה לחיצה עם כפתור שמאלי
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (BarrelShopUI.Instance != null)
        {
            BarrelShopUI.Instance.Open(this);
        }
        else
        {
            Debug.LogWarning("[Barrel] No BarrelShopUI in scene.");
        }
    }
}
