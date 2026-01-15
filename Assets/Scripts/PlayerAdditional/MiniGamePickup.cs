using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MiniGamePickup : MonoBehaviour
{
    [SerializeField] private string itemId;
    public string ItemId => itemId;

    public const int Amount = 1;

    private SpriteRenderer sr;

    [Header("Normalize Size")]
    [SerializeField] private bool normalizeSize = true;

    // זה ה"מספר הקבוע" שאת מחליטה עליו
    [SerializeField] private float targetWorldHeight = 0.8f;

    // אם את רוצה לשמור על סקייל בסיסי כלשהו מה-Prefab (אופציונלי)
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // ✅ חשוב: למצוא SpriteRenderer גם בילדים
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>(true);

        baseScale = transform.localScale;
    }

    public void Init(string id, int _ignoredAmount)
    {
        itemId = id;

        if (InventoryManager.Instance != null)
        {
            var def = InventoryManager.Instance.GetDefinition(itemId);
            if (def != null && def.icon != null)
            {
                if (sr == null) sr = GetComponentInChildren<SpriteRenderer>(true);
                sr.sprite = def.icon;
            }
        }

        if (normalizeSize)
            StartCoroutine(NormalizeWhenReady());
    }

    private System.Collections.IEnumerator NormalizeWhenReady()
    {
        // נחכה פריים כדי לוודא שהספרייט יושב ומעודכן
        yield return null;

        TryNormalize();
    }

    private void TryNormalize()
    {
        if (!normalizeSize) return;

        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr == null || sr.sprite == null) return;

        float currentHeight = sr.bounds.size.y; 
        if (currentHeight <= 0.0001f) return;

        float k = targetWorldHeight / currentHeight;

       
        transform.localScale = baseScale * k;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        MiniGameLootBuffer.Instance?.Add(itemId, Amount);
        Destroy(gameObject);
    }
}
