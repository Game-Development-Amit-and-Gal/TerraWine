using UnityEngine;
using UnityEngine.InputSystem;   // Input System החדש

public class PlantingController : MonoBehaviour
{
    public static PlantingController Instance { get; private set; }

    [SerializeField] private float cursorWorldHeight = 1f;

    [Header("Cursor Seed")]
    [SerializeField] private SpriteRenderer cursorSprite;  // האייקון שמזיזים עם העכבר

    // הזרע שנבחר כרגע (ItemSO)
    private ItemSO currentSeedItem;

    public bool HasSeed => currentSeedItem != null;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (cursorSprite != null)
            cursorSprite.enabled = false;

        // בהתחלה – להראות את סמן העכבר הרגיל
        Cursor.visible = true;
    }

    /// <summary>
    /// נבחר זרע מהאינבנטורי.
    /// </summary>
    public void SelectSeed(ItemSO item)
    {
        // אם קיבלת פריט שהוא לא זרע – מנקים בחירה
        if (item == null || !item.isSeed)
        {
            ClearSelection();
            return;
        }

        currentSeedItem = item;

        if (cursorSprite != null)
        {
            cursorSprite.sprite = item.icon;
            cursorSprite.enabled = true;
            SetCursorSpriteSize(item.icon);
        }

        // מסתיר את סמן העכבר הרגיל – עכשיו רואים רק את האייקון
        Cursor.visible = false;
    }

    /// <summary>
    /// ביטול בחירת זרע.
    /// </summary>
    public void ClearSelection()
    {
        currentSeedItem = null;

        if (cursorSprite != null)
            cursorSprite.enabled = false;

        // מחזיר את סמן העכבר הרגיל
        Cursor.visible = true;
    }

    void OnDisable()
    {
        // ביטוח: אם האובייקט נכבה – לא להשאיר את העכבר מוסתר
        Cursor.visible = true;
    }

    void Update()
    {
        // קליק ימני מבטל בחירה של זרע
        if (HasSeed && Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            ClearSelection();
        }

        // אם אין זרע נבחר – לא צריך להזיז את האייקון
        if (!HasSeed || cursorSprite == null || Camera.main == null) return;

        // להזיז את אייקון הזרע עם העכבר
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(mousePos);
        world.z = 0f;
        cursorSprite.transform.position = world;
    }

    /// <summary>
    /// ניסיון לשתול בערוגה מסוימת.
    /// נקרא מתוך PlantPlot כשעושים עליו קליק.
    /// </summary>
    public bool TryPlantOn(PlantPlot plot)
    {
        if (!HasSeed || plot == null) return false;
        if (InventoryManager.Instance == null) return false;

        string seedId = currentSeedItem.id;

        // כמה זרעים יש לפני השתילה
        int countBefore = InventoryManager.Instance.CountOf(seedId);

        // אין זרעים? ננקה בחירה
        if (countBefore <= 0)
        {
            ClearSelection();
            return false;
        }

        if (!plot.CanPlant) return false;

        // מורידים זרע אחד מהאינבנטורי
        InventoryManager.Instance.Remove(seedId, 1);

        // מתחילים גדילה בערוגה – לפי ה-ItemSO (שם הזן, זמן גדילה, harvest וכו')
        plot.StartGrowth(currentSeedItem);

        // אם אחרי ההורדה לא נשארו זרעים – לנקות בחירה
        int countAfter = InventoryManager.Instance.CountOf(seedId);
        if (countAfter <= 0)
        {
            ClearSelection();
        }

        return true;
    }

    /// <summary>
    /// התאמת גודל הספייט של הזרע לגובה בעולם.
    /// </summary>
    private void SetCursorSpriteSize(Sprite sprite)
    {
        if (sprite == null || cursorSprite == null)
            return;

        float unitsPerPixel = 1f / sprite.pixelsPerUnit;
        float spriteHeightInWorld = sprite.rect.height * unitsPerPixel;

        // כמה צריך למתוח/לכווץ כדי שהגובה יהיה cursorWorldHeight
        float scale = cursorWorldHeight / spriteHeightInWorld;

        cursorSprite.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
