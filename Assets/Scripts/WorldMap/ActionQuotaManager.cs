using System;
using UnityEngine;

public class ActionQuotaManager : MonoBehaviour
{
    public static ActionQuotaManager Instance { get; private set; }

    [Header("Daily actions")]
    [SerializeField] private int dailyLimit = 10;
    [SerializeField] private int resetHour = 8;     // 08:00
    [SerializeField] private int resetMinute = 0;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private bool initialized = false;

    private void Log(string msg)
    {
        if (debugLogs) Debug.Log("[DailyActions] " + msg);
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // DontDestroyOnLoad רק על Root
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    // אל תרוצי על Start - כי ב-MainMenu עדיין אין Data טעון
    private void Start() { }

    /// <summary>
    /// חובה לקרוא לזה אחרי NewGame/Continue (אחרי ש-GameManager.Data כבר נקבע).
    /// </summary>
    public void InitializeForCurrentProfile()
    {
        initialized = true;
        EnsureResetUpToDate(saveIfChanged: true);
        Log("InitializedForCurrentProfile");
    }

    public int Remaining()
    {
        EnsureResetUpToDate(saveIfChanged: false);
        var data = GameManager.Instance?.Data;
        if (!initialized || data == null) return dailyLimit; // לפני init תראי מלא

        return Mathf.Clamp(dailyLimit - data.dailyActionsUsed, 0, dailyLimit);
    }

    public bool CanSpend(int amount = 1)
    {
        amount = Mathf.Max(1, amount);
        return Remaining() >= amount;
    }

    public bool TrySpend(int amount = 1)
    {
        amount = Mathf.Max(1, amount);

        var gm = GameManager.Instance;
        var data = gm?.Data;

        if (!initialized || data == null)
        {
            Log("TrySpend called before InitializeForCurrentProfile -> blocked");
            return false;
        }

        EnsureResetUpToDate(saveIfChanged: false);

        int left = dailyLimit - data.dailyActionsUsed;
        if (left < amount)
        {
            Log($"TrySpend failed. left={left}, requested={amount}");
            return false;
        }

        data.dailyActionsUsed += amount;
        Log($"Spent {amount}. used={data.dailyActionsUsed}/{dailyLimit}");

        // חשוב: לא gm.SaveGame() כדי לא לדרוס sceneName/position
        SaveSystem.Save(data);
        return true;
    }

    public DateTime GetNextResetLocalTime()
    {
        DateTime now = GetIsraelLocalNow();
        DateTime todayReset = new DateTime(now.Year, now.Month, now.Day, resetHour, resetMinute, 0);
        return (now >= todayReset) ? todayReset.AddDays(1) : todayReset;
    }

    private void EnsureResetUpToDate(bool saveIfChanged)
    {
        var gm = GameManager.Instance;
        var data = gm?.Data;
        if (!initialized || data == null) return;

        DateTime now = GetIsraelLocalNow();

        DateTime todayReset = new DateTime(now.Year, now.Month, now.Day, resetHour, resetMinute, 0);
        DateTime currentWindowStart = (now >= todayReset) ? todayReset : todayReset.AddDays(-1);

        long windowStartTicksUtc = currentWindowStart.ToUniversalTime().Ticks;

        if (data.dailyActionsResetTicks == 0 || data.dailyActionsResetTicks < windowStartTicksUtc)
        {
            data.dailyActionsResetTicks = windowStartTicksUtc;
            data.dailyActionsUsed = 0;

            Log($"RESET -> windowStart={currentWindowStart:yyyy-MM-dd HH:mm} (local). limit={dailyLimit}");

            if (saveIfChanged)
            {
                // לא gm.SaveGame()
                SaveSystem.Save(data);
            }
        }
    }

    private DateTime GetIsraelLocalNow()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time"); // Windows
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
        }
        catch
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem"); // Linux/macOS
                return TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
            }
            catch
            {
                return DateTime.Now;
            }
        }
    }
}
