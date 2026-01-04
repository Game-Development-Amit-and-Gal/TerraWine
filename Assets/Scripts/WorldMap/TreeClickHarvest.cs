using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(TreeYield))]
public class TreeClickHarvest : MonoBehaviour
{
    [SerializeField] private string woodItemId = "WOOD";
    [SerializeField] private bool destroyAfterHarvest = true;
    [SerializeField] private Camera cam;                 // אפשר להשאיר ריק
    [SerializeField] private LayerMask clickableMask = ~0; // כל השכבות

    private bool harvested;
    private TreeYield treeYield;
    private Collider2D myCol;

    private void Awake()
    {
        treeYield = GetComponent<TreeYield>();
        myCol = GetComponent<Collider2D>();
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (harvested) return;
        if (cam == null) return;
        if (Mouse.current == null) return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 worldPos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        Collider2D hit = Physics2D.OverlapPoint(worldPos, clickableMask);
        if (hit == null) return;

        // חשוב: רק אם הלחיצה פגעה בקוליידר של העץ הזה
        if (hit != myCol) return;

        TryHarvest();
    }

    private void TryHarvest()
    {
        // לשלם פעולה
        var aq = ActionQuotaManager.Instance;
        if (aq != null && !aq.TrySpend(1))
            return;

        // להוסיף WOOD לפי ה-yield של העץ
        int amount = treeYield != null ? treeYield.YieldAmount : 1;
        if (amount <= 0) amount = 1;

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        if (!inv.Add(woodItemId, amount))
            return;

        harvested = true;

        // למחוק את העץ
        if (destroyAfterHarvest) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
