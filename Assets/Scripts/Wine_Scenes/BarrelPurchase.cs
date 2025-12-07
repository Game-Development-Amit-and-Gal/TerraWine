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

    // ---------------------- Runtime State ----------------------
    [Header("State (runtime only)")]
    [SerializeField] private bool owned = false;        // Has the barrel been purchased?
    [SerializeField] private bool premiumOwned = false; // Was it purchased as premium?

    // Transparency value used to visually mark barrels purchasable (≈150 / 255)
    private readonly float purchasableAlpha = 150f / 255f;

    private void Reset()
    {
        // Auto-assign SpriteRenderer if it was forgotten in the Inspector
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Automatically assigns a unique ID while editing if none exists.
    /// Prevents accidental duplication when copying objects in the editor.
    /// </summary>
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
                {
                    c = premiumColor; // Use premium look
                }
                c.a = 1f; // Fully visible
                spriteRenderer.color = c;
            }
        }
    }

    /// <summary>
    /// A barrel is purchasable only if:
    /// - It is not already owned
    /// - Its sprite is set with the semi-transparent alpha
    /// </summary>
    private bool IsPurchasable()
    {
        if (owned) return false;
        if (spriteRenderer == null) return false;

        Color c = spriteRenderer.color;
        return Mathf.Approximately(c.a, purchasableAlpha);
    }

    /// <summary>
    /// Saves purchase ownership into GameData + writes to disk.
    /// </summary>
    private void SaveOwned(bool isPremium)
    {
        if (GameManager.Instance == null || GameManager.Instance.Data == null)
            return;

        // Ensure list exists
        if (GameManager.Instance.Data.ownedBarrels == null)
            GameManager.Instance.Data.ownedBarrels = new List<OwnedBarrelData>();

        var list = GameManager.Instance.Data.ownedBarrels;
        var existing = list.Find(b => b.id == barrelId);

        // Create or update entry
        if (existing == null)
        {
            list.Add(new OwnedBarrelData { id = barrelId, isPremium = isPremium });
        }
        else
        {
            existing.isPremium = isPremium;
        }

        // Persist to save file
        SaveSystem.Save(GameManager.Instance.Data);
    }

    /// <summary>
    /// Attempts to purchase a normal barrel.
    /// </summary>
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

        // Deduct cost; if failed, cancel purchase
        if (!GameManager.Instance.TrySpendMoney(normalPrice))
        {
            Debug.Log("[Barrel] Not enough money for normal barrel.");
            return;
        }

        owned = true;
        premiumOwned = false;

        // Fully visible normal sprite
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        SaveOwned(false);
        Debug.Log("[Barrel] Bought NORMAL barrel.");
    }

    /// <summary>
    /// Attempts to purchase a premium barrel.
    /// </summary>
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

        // Deduct cost; if failed, cancel purchase
        if (!GameManager.Instance.TrySpendMoney(premiumPrice))
        {
            Debug.Log("[Barrel] Not enough money for premium barrel.");
            return;
        }

        owned = true;
        premiumOwned = true;

        // Fully visible premium color
        if (spriteRenderer != null)
        {
            Color c = premiumColor;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        SaveOwned(true);
        Debug.Log("[Barrel] Bought PREMIUM barrel.");
    }

    /// <summary>
    /// Detects when the barrel is clicked and opens the purchase UI.
    /// Only responds to left mouse button.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Open purchase UI based on this barrel
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
