using System;
using UnityEngine;

public class WaterManager : MonoBehaviour
{
    public static WaterManager Instance { get; private set; }

    [Header("Drain rates")]
    [Tooltip("כמה מים יורדים בשעה, גם בלי עציצים")]
    [SerializeField] private float baseDrainPerHour = 0.25f;

    [Tooltip("כמה מים יורדים בשעה לכל עציץ שנמצא בגידול")]
    [SerializeField] private float drainPerGrowingPlotPerHour = 0.15f;

    [Header("Refill")]
    [SerializeField] private int refillAmountOnWell = 5;

    [Header("Runtime")]
    [SerializeField] private bool debugLogs = false;

    private int current;
    private int max;
    private bool readyForDrain = false;
    private float tickTimer = 0f;
    private const float TICK_EVERY_SECONDS = 1f;

    public int Current => current;
    public int Max => max;

    public event Action onChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        LoadFromGameData();
        RaiseChanged();
    }


    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!readyForDrain) return;

        tickTimer += Time.unscaledDeltaTime;
        if (tickTimer < TICK_EVERY_SECONDS) return;

        float dt = tickTimer;
        tickTimer = 0f;

        ApplyOnlineDrainStep(dt);
    }

    // ---------- Public API ----------
    public void RefillFromWell()
    {
        Refill(refillAmountOnWell);
    }

    public void Refill(int amount)
    {
        if (amount <= 0) return;
        current = Mathf.Clamp(current + amount, 0, max);
        SaveToGameData(saveNow: true);
        RaiseChanged();
    }

    public bool TryUse(int amount)
    {
        if (amount <= 0) return true;
        if (current < amount) return false;
        current -= amount;
        SaveToGameData(saveNow: true);
        RaiseChanged();
        return true;
    }
    private System.Collections.IEnumerator Start()
    {
        // מחכים ש-PlantManager יווצר
        while (PlantManager.Instance == null)
            yield return null;

        // אם הוספת HasLoaded ל-PlantManager:
        while (!PlantManager.Instance.HasLoaded)
            yield return null;

        // רק עכשיו עושים offline drain (כשאנחנו בטוחים שהצמחים נטענו)
        ApplyOfflineDrainIfNeeded();

        readyForDrain = true;
        RaiseChanged();
    }

    public void SetMax(int newMax)
    {
        max = Mathf.Max(0, newMax);
        current = Mathf.Clamp(current, 0, max);
        SaveToGameData(saveNow: true);
        RaiseChanged();
    }

    // ---------- Online (while in Garden) ----------
    private void ApplyOnlineDrainStep(float dtSeconds)
    {
        var d = GameManager.Instance?.Data;
        if (d == null) return;

        int growingNow = GetGrowingPlotsCountSafe();

        float drainPerSecond =
            (baseDrainPerHour / 3600f) +
            (growingNow * (drainPerGrowingPlotPerHour / 3600f));

        float drainFloat = dtSeconds * drainPerSecond;

        d.waterDrainRemainder += drainFloat;

        int drainInt = Mathf.FloorToInt(d.waterDrainRemainder);
        if (drainInt > 0)
        {
            current = Mathf.Max(0, current - drainInt);
            d.waterDrainRemainder -= drainInt;

            if (debugLogs)
                Debug.Log($"[Water] Online drain -{drainInt} (grow={growingNow}). Now {current}/{max}");

            RaiseChanged();
            SaveToGameData(saveNow: true); // שומרים רק כשירד שלם
        }

        d.waterLastUpdateTicks = DateTime.UtcNow.Ticks;
        d.waterGrowingCountSnapshot = growingNow;
    }

    // ---------- Offline (when returning to Garden) ----------
    private void ApplyOfflineDrainIfNeeded()
    {
        var d = GameManager.Instance?.Data;
        if (d == null) return;

        long now = DateTime.UtcNow.Ticks;

        if (d.waterLastUpdateTicks == 0)
        {
            d.waterLastUpdateTicks = now;
            d.waterGrowingCountSnapshot = GetGrowingPlotsCountSafe();
            return;
        }

        long dtTicks = now - d.waterLastUpdateTicks;
        if (dtTicks <= 0) return;

        double dtSeconds = dtTicks / (double)TimeSpan.TicksPerSecond;

        int growingSnapshot = Mathf.Max(0, d.waterGrowingCountSnapshot);

        float drainPerSecond =
            (baseDrainPerHour / 3600f) +
            (growingSnapshot * (drainPerGrowingPlotPerHour / 3600f));

        float drainFloat = (float)(dtSeconds * drainPerSecond);

        d.waterDrainRemainder += drainFloat;

        int drainInt = Mathf.FloorToInt(d.waterDrainRemainder);
        if (drainInt > 0)
        {
            current = Mathf.Max(0, current - drainInt);
            d.waterDrainRemainder -= drainInt;

            if (debugLogs)
                Debug.Log($"[Water] Offline drain -{drainInt} over {dtSeconds:F0}s (snapGrow={growingSnapshot}). Now {current}/{max}");
        }

        d.waterLastUpdateTicks = now;
        d.waterGrowingCountSnapshot = GetGrowingPlotsCountSafe();

        SaveToGameData(saveNow: true);
    }

    // ---------- PlantManager hook ----------
    private int GetGrowingPlotsCountSafe()
    {
        if (PlantManager.Instance == null) return 0;
        return PlantManager.Instance.GetGrowingPlotsCount(); 
    }

    // ---------- GameData sync ----------
    private void LoadFromGameData()
    {
        var d = GameManager.Instance?.Data;
        if (d == null)
        {
            max = 20;
            current = 20;
            return;
        }

        max = Mathf.Max(0, d.waterMax);
        current = Mathf.Clamp(d.waterCurrent, 0, max);
    }

    private void SaveToGameData(bool saveNow)
    {
        var gm = GameManager.Instance;
        var d = gm?.Data;
        if (d == null) return;

        d.waterMax = max;
        d.waterCurrent = current;

        if (d.waterLastUpdateTicks == 0)
            d.waterLastUpdateTicks = DateTime.UtcNow.Ticks;

        if (saveNow)
            gm.SaveGame();
    }

    private void RaiseChanged()
    {
        onChanged?.Invoke();
    }
}
