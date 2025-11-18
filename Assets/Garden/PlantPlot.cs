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
}

public class PlantPlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Identity")]
    [SerializeField] private string plotId;   // למשל: "PLOT_1", "PLOT_2" לכל ערוגה

    [Header("Renderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite plantedSprite;
    [SerializeField] private Sprite readySprite;

    [Header("Growth")]
    [SerializeField] private float growTimeSeconds = 180f;

    [Header("UI (לא חובה)")]
    [SerializeField] private TMP_Text timerText;

    [Header("Harvest")]
    [SerializeField] private string harvestItemId = "Cabernet_Sauvignon_Grap"; // לשנות ל-id של הענבים שלך
    [SerializeField] private int harvestAmount = 10;

    bool isGrowing;
    bool isReady;
    float remainingTime;

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
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // אם אפשר לשתול (ריק / לא גדל / לא מוכן) – ננסה לשתול
        if (CanPlant)
        {
            if (PlantingController.Instance != null)
                PlantingController.Instance.TryPlantOn(this);
        }
        // אחרת, אם הצמח מוכן – נקצור
        else if (isReady)
        {
            Harvest();
        }
    }


    public void StartGrowth(string seedId)
    {
        if (!CanPlant) return;

        isGrowing = true;
        isReady = false;
        remainingTime = growTimeSeconds;

        if (spriteRenderer != null) spriteRenderer.sprite = plantedSprite;
        if (timerText != null) timerText.gameObject.SetActive(true);

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
            remainingTime = remainingTime
        };
    }

    // ---- טעינה (עם דלתא זמן אמיתי) ----
    public void LoadFrom(PlantPlotSave s, float deltaSeconds)
    {
        if (s == null) return;

        isGrowing = s.isGrowing;
        isReady = s.isReady;
        remainingTime = s.remainingTime;

        // אם הייתה באמצע גדילה – מחסירים את הזמן שעבר
        if (isGrowing)
        {
            remainingTime -= deltaSeconds;
            if (remainingTime <= 0f)
            {
                // בזמן שהמשחק היה סגור – הצמח כבר הספיק לגדול
                isGrowing = false;
                isReady = true;
                remainingTime = 0f;
            }
        }

        // לעדכן ספרייטים ו-Timer
        if (!isGrowing && !isReady)
        {
            ResetToEmpty();
        }
        else if (isReady)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = readySprite;
            if (timerText != null) timerText.gameObject.SetActive(false);
        }
        else if (isGrowing)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = plantedSprite;
            if (timerText != null) timerText.gameObject.SetActive(true);
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

}
