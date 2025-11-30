using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[System.Serializable]
public class PlantPlotSave
{
    public string id;
    public bool isGrowing;
    public bool isReady;
    public float remainingTime;
    public string seedId;
    public string harvestItemId;
    public int harvestAmount;
}

public class PlantPlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Identity")]
    [SerializeField] private string plotId;   // למשל: "PLOT_1", "PLOT_2" לכל ערוגה

    [Header("Renderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite plantedSprite;
    [SerializeField] private Sprite readySprite;

    [Header("VFX")]
    [SerializeField] private GameObject readyVfx;

    [Header("UI (לא חובה)")]
    [SerializeField] private TMP_Text timerText;

    [Header("Harvest")]
    string seedId;


    bool isGrowing;
    bool isReady;
    float remainingTime;
    private string harvestItemId;
    private int harvestAmount;

    // אופציונלי – אם תרצי לשמור איזה seed נשתל כאן


    public bool CanPlant => !isGrowing && !isReady;
    public string PlotId => plotId;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        ResetToEmpty();
    }

    public void ResetToEmpty()
    {
        isGrowing = false;
        isReady = false;
        remainingTime = 0f;
        if (spriteRenderer != null) spriteRenderer.sprite = emptySprite;
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (readyVfx != null) readyVfx.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // רק קליק שמאלי
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // אם הצמח מוכן – קודם כל לקצור
        if (isReady)
        {
            Harvest();
            return;
        }

        // אם אפשר לשתול וגם יש Seed נבחר
        if (CanPlant &&
            PlantingController.Instance != null &&
            PlantingController.Instance.HasSeed)
        {
            PlantingController.Instance.TryPlantOn(this);
        }
    }

    // 👇 פה השינוי – מקבלת growSeconds מבחוץ
    public void StartGrowth(ItemSO seed)
    {
        if (!CanPlant || seed == null || !seed.isSeed) return;

        seedId = seed.id;

        isGrowing = true;
        isReady = false;
        remainingTime = seed.growTimeSeconds;

        // להגדיר מה נקצור:
        harvestItemId = seed.harvestItem != null ? seed.harvestItem.id : null;
        harvestAmount = seed.harvestAmount;

        // ספרייט בזמן גדילה:
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



    System.Collections.IEnumerator GrowRoutine()
    {
        while (remainingTime > 0f && isGrowing)
        {
            remainingTime -= Time.deltaTime;
            UpdateTimerUI();
            yield return null;
        }

        if (!isGrowing) yield break;  // במקרה שעצרו ידנית

        isGrowing = false;
        isReady = true;
        remainingTime = 0f;

        if (spriteRenderer != null) spriteRenderer.sprite = readySprite;
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (readyVfx != null) readyVfx.SetActive(true);
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        if (remainingTime < 0) remainingTime = 0;
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"{minutes:0}:{seconds:00}";
    }

    // ---- שמירה ----
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

    public void LoadFrom(PlantPlotSave s, float deltaSeconds)
    {
        if (s == null) return;

        seedId = s.seedId;
        harvestItemId = s.harvestItemId;
        harvestAmount = s.harvestAmount;

        isGrowing = s.isGrowing;
        isReady = s.isReady;
        remainingTime = s.remainingTime;

        // כמו שהיה אצלך – חישוב הזמן שעבר:
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

        // לעדכן ספרייטים:
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

    private void Harvest()
    {
        if (!isReady) return;

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[PlantPlot] Tried to harvest but no InventoryManager in scene");
            return;
        }

        // מוסיפים את הענבים לתיק
        bool ok = InventoryManager.Instance.Add(harvestItemId, harvestAmount);
        Debug.Log($"[PlantPlot] Harvested {harvestAmount}x {harvestItemId}, success={ok}");

        // מאפסים את הערוגה לריקה
        ResetToEmpty();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (timerText == null) return;

        if (isGrowing)
        {
            timerText.gameObject.SetActive(true);
            UpdateTimerUI(); // לוודא שהטקסט מעודכן לזמן הנוכחי
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
}
