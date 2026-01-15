using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class MiniGamePickup : MonoBehaviour
{
    [SerializeField] private string itemId;
    public string ItemId => itemId;

    public const int Amount = 1; // תמיד 1

    private SpriteRenderer sr;

    // ✅ ADDED: גובה קבוע בעולם לכל Pickup (תכווני במספרים)
    [SerializeField] private float targetWorldHeight = 0.45f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public void Init(string id, int _ignoredAmount)
    {
        itemId = id;

        if (InventoryManager.Instance != null)
        {
            var def = InventoryManager.Instance.GetDefinition(itemId);
            if (def != null && def.icon != null)
                sr.sprite = def.icon;
        }

        // ✅ ADDED: אחרי שקבענו sprite -> נרמול גודל
        NormalizeSpriteToFixedHeight();
    }

    // ✅ ADDED: הופך את כל הספראייטים לאותו גובה בעולם, בלי קשר לגודל התמונה
    private void NormalizeSpriteToFixedHeight()
    {
        if (sr == null || sr.sprite == null) return;

        float currentHeight = sr.bounds.size.y;   // גובה אמיתי בעולם
        if (currentHeight <= 0.0001f) return;

        float k = targetWorldHeight / currentHeight;
        transform.localScale = transform.localScale * k;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        MiniGameLootBuffer.Instance?.Add(itemId, Amount); // תמיד 1
        Destroy(gameObject);
    }
}
