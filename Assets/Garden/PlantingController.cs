using UnityEngine;
using UnityEngine.InputSystem;   // חשוב – Input System חדש

public class PlantingController : MonoBehaviour
{
    public static PlantingController Instance { get; private set; }

    [Header("Cursor Seed")]
    [SerializeField] private SpriteRenderer cursorSprite;  // אייקון של זרע שזז עם העכבר

    private string currentSeedId;

    public bool HasSeed => !string.IsNullOrEmpty(currentSeedId);


    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (cursorSprite != null)
            cursorSprite.enabled = false;
    }

    // נקרא מהאינבנטורי כשעושים קליק על סלוט
    public void SelectSeed(string id, Sprite icon)
    {
        currentSeedId = id;
        if (cursorSprite != null)
        {
            cursorSprite.sprite = icon;
            cursorSprite.enabled = true;
        }
    }

    public void ClearSelection()
    {
        currentSeedId = null;
        if (cursorSprite != null)
            cursorSprite.enabled = false;
    }

    void Update()
    {
        // אם יש זרע נבחר – קליק ימני מבטל את הבחירה
        if (HasSeed && Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            ClearSelection();
        }

        // אם אין זרע נבחר – אין מה להזיז אייקון
        if (!HasSeed || cursorSprite == null || Camera.main == null) return;

        // להזיז את אייקון הזרע עם העכבר
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(mousePos);
        world.z = 0f;
        cursorSprite.transform.position = world;
    }


    // נקרא מתוך ה-PlantPlot כשמשתילים
    public bool TryPlantOn(PlantPlot plot)
    {
        if (!HasSeed || plot == null) return false;
        if (InventoryManager.Instance == null) return false;

        int countBefore = InventoryManager.Instance.CountOf(currentSeedId);

        // אין זרעים? מנקים בחירה
        if (countBefore <= 0)
        {
            ClearSelection();
            return false;
        }

        if (!plot.CanPlant) return false;

        // מורידים זרע אחד מהתיק
        InventoryManager.Instance.Remove(currentSeedId, 1);

        // מתחילים גדילה בערוגה
        plot.StartGrowth(currentSeedId);

        // אם אחרי ההורדה כבר אין זרעים – עכשיו לנקות את הבחירה
        int countAfter = InventoryManager.Instance.CountOf(currentSeedId);
        if (countAfter <= 0)
        {
            ClearSelection();
        }

        return true;
    }

}
