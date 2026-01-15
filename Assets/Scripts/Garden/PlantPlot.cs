using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem; // ✅ ADDED

/// <summary>
/// Serializable save data for one plant plot.
/// Stores state, remaining time, and harvest info.
/// </summary>
[System.Serializable]
public class PlantPlotSave
{
    public string id;              // Unique plot ID (e.g., PLOT_1)
    public bool isGrowing;         // Currently growing?
    public bool isReady;           // Ready to harvest?
    public float remainingTime;    // Seconds left to grow

    public string seedId;          // ID of planted seed
    public string harvestItemId;   // Item produced from harvest
    public int harvestAmount;      // Amount produced


}

/// <summary>
/// A clickable plant plot that can grow a seed,
/// show a timer, and harvest when done.
/// </summary>
public class PlantPlot : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Identity")]
    [SerializeField] private string plotId; // Unique ID per plot

    [Header("Renderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite plantedSprite;
    [SerializeField] private Sprite readySprite;

    [Header("VFX")]
    [SerializeField] private GameObject readyVfx;     // Glow when ready

    [Header("UI (optional)")]
    [SerializeField] private TMP_Text timerText;      // Timer above plot

    // Runtime state
    private bool isGrowing;
    private bool isReady;
    private float remainingTime;
    // Per-seed visuals (runtime)
    private Sprite plantedOverrideSprite;
    private Sprite readyOverrideSprite;
    public bool IsGrowing => isGrowing;
    public bool IsReady => isReady;

    // Harvest info (set when planting)
    private string seedId;
    private string harvestItemId;
    private int harvestAmount;

    /// <summary>True if nothing is planted or finished.</summary>
    public bool CanPlant => !isGrowing && !isReady;

    /// <summary>Unique plot ID.</summary>
    public string PlotId => plotId;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ResetToEmpty();
    }

    /// <summary>
    /// Clears the plot to empty state (no crop).
    /// </summary>
    public void ResetToEmpty()
    {
        isGrowing = false;
        isReady = false;
        remainingTime = 0f;

        seedId = null;
        harvestItemId = null;
        harvestAmount = 0;

        plantedOverrideSprite = null;
        readyOverrideSprite = null;

        if (spriteRenderer != null) spriteRenderer.sprite = emptySprite;
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (readyVfx != null) readyVfx.SetActive(false);

        HarvestIconUI.Instance?.Hide(); // ✅ ADDED
    }


    /// <summary>
    /// Handles click: harvest if ready, or plant if allowed.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (isReady)
        {
            Harvest();
            return;
        }

        if (CanPlant &&
            PlantingController.Instance != null &&
            PlantingController.Instance.HasSeed)
        {
            PlantingController.Instance.TryPlantOn(this);
        }
    }

    /// <summary>
    /// Starts growth using a seed configuration (ItemSO).
    /// </summary>
    public void StartGrowth(ItemSO seed)
    {
        if (!CanPlant || seed == null || !seed.isSeed) return;
        TutorialManager.Instance?.SetFlag("Press vineyard");

        seedId = seed.id;
        remainingTime = seed.growTimeSeconds;
        harvestItemId = seed.harvestItem != null ? seed.harvestItem.id : null;
        harvestAmount = seed.harvestAmount;
        plantedOverrideSprite = seed.plantedPlotSprite;
        readyOverrideSprite = seed.readyPlotSprite;

        isGrowing = true;
        isReady = false;

        // Change sprite upon planting
        if (spriteRenderer != null)
            spriteRenderer.sprite = (plantedOverrideSprite != null) ? plantedOverrideSprite : plantedSprite;

        if (readyVfx != null) readyVfx.SetActive(false);

        HarvestIconUI.Instance?.Hide(); // ✅ ADDED

        StopAllCoroutines();
        StartCoroutine(GrowRoutine());
    }

    /// <summary>
    /// Coroutine that counts down while growing.
    /// </summary>
    private System.Collections.IEnumerator GrowRoutine()
    {
        while (remainingTime > 0f && isGrowing)
        {
            remainingTime -= Time.deltaTime;
            UpdateTimerUI();
            yield return null;
        }

        if (!isGrowing) yield break;

        isGrowing = false;
        isReady = true;
        TutorialManager.Instance?.SetFlag("Vineyard Redy");
        remainingTime = 0f;

        if (spriteRenderer != null)
            spriteRenderer.sprite = (readyOverrideSprite != null) ? readyOverrideSprite : readySprite;

        if (timerText != null) timerText.gameObject.SetActive(false);
        if (readyVfx != null) readyVfx.SetActive(true);

        // ✅ ADDED: אם העכבר כבר מעל בזמן שזה נהיה Ready
        if (IsPointerOverThisPlotNow())
            HarvestIconUI.Instance?.Show();
    }

    /// <summary>
    /// Converts seconds to text like "1:23".
    /// </summary>
    private void UpdateTimerUI()
    {
        float minutesInSeconds = 60f;
        int zero = 0;
        if (timerText == null) return;

        if (remainingTime < zero) remainingTime = zero;

        int minutes = Mathf.FloorToInt(remainingTime / minutesInSeconds);
        int seconds = Mathf.FloorToInt(remainingTime % minutesInSeconds);
        timerText.text = $"{minutes:0}:{seconds:00}";
    }

    /// <summary>
    /// Harvests the ready crop into inventory.
    /// </summary>
    private void Harvest()
    {
        if (!isReady) return;
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[PlantPlot] Tried to harvest but no InventoryManager in scene");
            return;
        }

        bool ok = InventoryManager.Instance.Add(harvestItemId, harvestAmount);
        TutorialManager.Instance?.SetFlag("Is Ready");
        Debug.Log($"[PlantPlot] Harvested {harvestAmount}x {harvestItemId}, success={ok}");

        HarvestIconUI.Instance?.Hide(); // ✅ ADDED

        ResetToEmpty();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ✅ ADDED: להדליק רק כשהוא Ready
        if (isReady) HarvestIconUI.Instance?.Show();
        else HarvestIconUI.Instance?.Hide();

        if (timerText == null) return;

        if (isGrowing)
        {
            timerText.gameObject.SetActive(true);
            UpdateTimerUI();
        }
        else if (isReady)
        {
            timerText.text = "Ready!";
            timerText.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HarvestIconUI.Instance?.Hide(); // ✅ ADDED

        if (timerText == null) return;
        timerText.gameObject.SetActive(false);
    }

    // ---------------- Save/Load ----------------

    /// <summary>Returns the current plot state for saving.</summary>
    public PlantPlotSave GetSave()
    {
        return new PlantPlotSave
        {
            id = plotId,
            isGrowing = isGrowing,
            isReady = isReady,
            remainingTime = remainingTime,
            seedId = seedId,
            harvestItemId = harvestItemId,
            harvestAmount = harvestAmount
        };
    }

    /// <summary>
    /// Restores the plot state from save data.
    /// deltaSeconds: how long the player was offline.
    /// </summary>
    public void LoadFrom(PlantPlotSave s, float deltaSeconds)
    {
        if (s == null) return;

        seedId = s.seedId;

        isGrowing = s.isGrowing;
        isReady = s.isReady;
        remainingTime = s.remainingTime;

        // אם היה seedId – נשחזר ממנו harvest + sprites
        // (ואז אין צורך לסמוך על מה ששמור ב-save)
        TryRestoreFromSeedId();

        // אם משום מה לא הצלחנו לשחזר (למשל InventoryManager לא קיים עדיין),
        // נשמור לפחות את מה שהיה בקובץ (fallback)
        if (string.IsNullOrEmpty(harvestItemId))
            harvestItemId = s.harvestItemId;

        if (harvestAmount <= 0)
            harvestAmount = s.harvestAmount;

        // Offline progression
        if (isGrowing)
        {
            remainingTime -= deltaSeconds;
            if (remainingTime <= 0f)
            {
                isGrowing = false;
                isReady = true;
                remainingTime = 0f;
            }
        }

        // Render state
        if (!isGrowing && !isReady)
        {
            ResetToEmpty();
            return;
        }

        if (isReady)
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = (readyOverrideSprite != null) ? readyOverrideSprite : readySprite;

            if (timerText != null) timerText.gameObject.SetActive(false);
            if (readyVfx != null) readyVfx.SetActive(true);

            // ✅ ADDED
            if (IsPointerOverThisPlotNow()) HarvestIconUI.Instance?.Show();
            else HarvestIconUI.Instance?.Hide();

            return;
        }

        // isGrowing
        if (spriteRenderer != null)
            spriteRenderer.sprite = (plantedOverrideSprite != null) ? plantedOverrideSprite : plantedSprite;

        if (timerText != null) timerText.gameObject.SetActive(false);
        if (readyVfx != null) readyVfx.SetActive(false);

        HarvestIconUI.Instance?.Hide(); // ✅ ADDED

        StopAllCoroutines();
        StartCoroutine(GrowRoutine());
    }

    public bool EnemyRaid_TryStealHarvest(out string stolenInfo)
    {
        stolenInfo = null;

        if (!isReady) return false;

        // מה נגנב (מידע ל-Log)
        stolenInfo = $"{harvestItemId} x{harvestAmount} (Plot={plotId})";

        // הגנב לקח -> מאפסים את החלקה בלי לתת לשחקן
        ResetToEmpty();
        return true;
    }
    private bool TryRestoreFromSeedId()
    {
        // חייבים קטלוג כדי להביא ItemSO לפי id
        if (InventoryManager.Instance == null) return false;
        if (string.IsNullOrEmpty(seedId)) return false;

        var seed = InventoryManager.Instance.GetDefinition(seedId);
        if (seed == null) return false;

        // שחזור harvest מהזרע
        harvestItemId = seed.harvestItem != null ? seed.harvestItem.id : null;
        harvestAmount = seed.harvestAmount;

        // שחזור sprites מהזרע
        plantedOverrideSprite = seed.plantedPlotSprite;
        readyOverrideSprite = seed.readyPlotSprite;

        return true;
    }

    // ✅ ADDED: Input System mouse position
    private Vector2 GetMouseScreenPos()
    {
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Vector2.zero;
    }

    // ✅ ADDED: בדיקה אם העכבר עכשיו מעל החלקה (עובד עם Input System)
    private bool IsPointerOverThisPlotNow()
    {
        if (EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current)
        {
            position = GetMouseScreenPos()
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject == gameObject)
                return true;

            // אם יש לך ילדים של האובייקט שמקבלים את ה-raycast:
            // if (results[i].gameObject.transform.IsChildOf(transform)) return true;
        }

        return false;
    }

}
