using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

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

        if (spriteRenderer != null) spriteRenderer.sprite = emptySprite;
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (readyVfx != null) readyVfx.SetActive(false);
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

        isGrowing = true;
        isReady = false;

        // Change sprite upon planting
        if (spriteRenderer != null)
        {
            if (seed.plantedPlotSprite != null)
                spriteRenderer.sprite = seed.plantedPlotSprite;
            else
                spriteRenderer.sprite = plantedSprite;
        }

        if (readyVfx != null) readyVfx.SetActive(false);

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

        if (spriteRenderer != null) spriteRenderer.sprite = readySprite;
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (readyVfx != null) readyVfx.SetActive(true);
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

        ResetToEmpty();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
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
        harvestItemId = s.harvestItemId;
        harvestAmount = s.harvestAmount;

        isGrowing = s.isGrowing;
        isReady = s.isReady;
        remainingTime = s.remainingTime;

        // Apply offline progression
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

        // Restore correct visuals
        if (!isGrowing && !isReady)
        {
            ResetToEmpty();
        }
        else if (isReady)
        {
            
            if (spriteRenderer != null) spriteRenderer.sprite = readySprite;
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (readyVfx != null) readyVfx.SetActive(true);
        }
        else if (isGrowing)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = plantedSprite;
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (readyVfx != null) readyVfx.SetActive(false);

            StopAllCoroutines();
            StartCoroutine(GrowRoutine());
        }
    }
}
