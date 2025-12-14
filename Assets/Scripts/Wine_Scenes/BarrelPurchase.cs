using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;   // Needed for IPointerClickHandler interaction

/// <summary>
/// Represents a purchaseable barrel in the scene.
/// Handles visual feedback (color/transparency), purchase logic,
/// saving ownership status, and detecting user clicks.
/// </summary>
public class BarrelPurchase : MonoBehaviour, IPointerClickHandler
{
    // ---------------------- Barrel Identity ----------------------
    [Header("Identity")]
    [SerializeField] private string barrelId;   // Unique ID used to save/load barrel ownership

    // ---------------------- Prices ----------------------
    [Header("Prices")]
    [SerializeField] private int normalPrice = 100;
    [SerializeField] private int premiumPrice = 200;

    // ---------------------- Visuals ----------------------
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer; // Barrel sprite renderer
    [SerializeField] private Color normalColor = new Color(0.6f, 0.4f, 0.2f, 1f);   // Not actively used anymore
    [SerializeField] private Color premiumColor = new Color(0.35f, 0.2f, 0.08f, 1f); // Premium/expensive look

    // ---------------------- Link to Production Script ----------------------
    [Header("Production (Barrel script)")]
    [Tooltip("Drag the Barrel component here (usually same GameObject). Disabled until purchased.")]
    [SerializeField] private Barrel barrel; // זה הסקריפט של הייצור (Aging/Recipes)

    // ---------------------- Runtime State ----------------------
    [Header("State (runtime only)")]
    [SerializeField] private bool owned = false;        // Has the barrel been purchased?
    [SerializeField] private bool premiumOwned = false; // Was it purchased as premium?

    // Transparency value used to visually mark barrels purchasable (≈150 / 255)
    private readonly float purchasableAlpha = 150f / 255f;

    private void Reset()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (barrel == null)
            barrel = GetComponent<Barrel>(); // אם Barrel על אותו אובייקט
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(barrelId))
        {
            barrelId = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    private void Awake()
    {
        // לפני טעינה, נבטיח שברירת המחדל היא "נעול" כדי שלא יקרה מצב ש-Barrel מגיב לפני שקנינו
        if (barrel == null) barrel = GetComponent<Barrel>();
        if (barrel != null) barrel.enabled = false;
    }

    private void Start()
    {
        // Restore ownership from saved GameData
        if (GameManager.Instance == null || GameManager.Instance.Data == null)
            return;

        List<OwnedBarrelData> list = GameManager.Instance.Data.ownedBarrels;
        if (list == null) return;

        // Find this barrel by ID
        OwnedBarrelData data = list.Find(b => b.id == barrelId);
        if (data != null)
        {
            owned = true;
            premiumOwned = data.isPremium;

            // Update visual to match owned status (full alpha, premium tint if needed)
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                if (data.isPremium)
                    c = premiumColor;

                c.a = 1f;
                spriteRenderer.color = c;
            }
        }

        // הכי חשוב: להדליק/לכבות את Barrel לפי owned
        if (barrel == null) barrel = GetComponent<Barrel>();
        if (barrel != null) barrel.enabled = owned;
    }

    private bool IsPurchasable()
    {
        if (owned) return false;
        if (spriteRenderer == null) return false;

        Color c = spriteRenderer.color;
        return Mathf.Approximately(c.a, purchasableAlpha);
    }

    private void SaveOwned(bool isPremium)
    {
        if (GameManager.Instance == null || GameManager.Instance.Data == null)
            return;

        if (GameManager.Instance.Data.ownedBarrels == null)
            GameManager.Instance.Data.ownedBarrels = new List<OwnedBarrelData>();

        var list = GameManager.Instance.Data.ownedBarrels;
        var existing = list.Find(b => b.id == barrelId);

        if (existing == null)
            list.Add(new OwnedBarrelData { id = barrelId, isPremium = isPremium });
        else
            existing.isPremium = isPremium;

        SaveSystem.Save(GameManager.Instance.Data);
    }

    public void BuyNormal()
    {
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
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        SaveOwned(false);

        // עכשיו הייצור מותר
        if (barrel == null) barrel = GetComponent<Barrel>();
        if (barrel != null) barrel.enabled = true;

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

        // עכשיו הייצור מותר
        if (barrel == null) barrel = GetComponent<Barrel>();
        if (barrel != null) barrel.enabled = true;

        Debug.Log("[Barrel] Bought PREMIUM barrel.");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // אם כבר קנוי — לא פותחים חנות.
        // Barrel (Script הייצור) יקבל את הלחיצה ויפתח מתכונים.
        if (owned) return;

        if (BarrelShopUI.Instance != null)
            BarrelShopUI.Instance.Open(this);
        else
            Debug.LogWarning("[Barrel] No BarrelShopUI in scene.");
    }
}
