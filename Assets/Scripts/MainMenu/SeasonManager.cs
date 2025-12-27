using System;
using UnityEngine;

public class SeasonManager : MonoBehaviour
{
    private static SeasonManager _instance;

    [SerializeField] private string[] seasons = { "Earth", "Vine", "Winery" };
    [SerializeField] private int totalDaysInSeason = 15;
    [SerializeField] private int maxYears = 3;

    private int currentDay = 1;
    private int currentSeasonIndex = 0;
    private int currentYear = 1;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        LoadAndUpdateByRealTimeFromJson();
    }

    private void LoadAndUpdateByRealTimeFromJson()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Data == null)
        {
            Debug.LogWarning("[SeasonManager] No GameManager/Data yet.");
            return;
        }

        // 1) Load current state from JSON
        currentYear = Mathf.Max(1, gm.Data.calendarYear);
        currentSeasonIndex = Mathf.Clamp(gm.Data.calendarSeasonIndex, 0, seasons.Length - 1);
        currentDay = Mathf.Clamp(gm.Data.calendarDay, 1, totalDaysInSeason);

        long nowTicks = DateTime.UtcNow.Ticks;
        long lastTicks = gm.Data.calendarLastUpdateTicks;

        Debug.Log($"[SeasonManager] BEFORE JSON: Y={currentYear} S={currentSeasonIndex} D={currentDay} last={TicksToLocal(lastTicks)} now={TicksToLocal(nowTicks)}");

        // 2) First time (or old save) -> set timestamp and save
        if (lastTicks <= 0)
        {
            gm.Data.calendarLastUpdateTicks = nowTicks;
            CommitToJsonAndSave();
            return;
        }

        // 3) Compute real time passed
        var passed = new TimeSpan(nowTicks - lastTicks);
        int daysPassed = Mathf.Max(0, (int)passed.TotalDays);

        Debug.Log($"[SeasonManager] Offline passed: {passed.TotalHours:F1}h -> daysPassed={daysPassed}");

        // 4) Advance in-game days
        for (int i = 0; i < daysPassed; i++)
            AdvanceDayInternal();

        // 5) Update timestamp + save
        gm.Data.calendarLastUpdateTicks = nowTicks;
        CommitToJsonAndSave();

        Debug.Log($"[SeasonManager] AFTER JSON: Y={gm.Data.calendarYear} S={gm.Data.calendarSeasonIndex} D={gm.Data.calendarDay} last={TicksToLocal(gm.Data.calendarLastUpdateTicks)}");
    }

    private void CommitToJsonAndSave()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Data == null) return;

        gm.Data.calendarYear = currentYear;
        gm.Data.calendarSeasonIndex = currentSeasonIndex;
        gm.Data.calendarDay = currentDay;

        gm.SaveGame(); // כותב savegame.json
    }

    private void AdvanceDayInternal()
    {
        currentDay++;

        if (currentDay > totalDaysInSeason)
        {
            currentDay = 1;

            int oldSeasonIndex = currentSeasonIndex;
            currentSeasonIndex = (currentSeasonIndex + 1) % seasons.Length;

            // Winery -> Earth => שנה עולה
            if (oldSeasonIndex == seasons.Length - 1 && currentSeasonIndex == 0)
            {
                currentYear++;
                if (currentYear > maxYears) currentYear = maxYears;
            }
        }
    }

    public void ResetForNewGame()
    {
        currentSeasonIndex = 0;
        currentDay = 1;
        currentYear = 1;

        var gm = GameManager.Instance;
        if (gm != null && gm.Data != null)
        {
            gm.Data.calendarYear = 1;
            gm.Data.calendarSeasonIndex = 0;
            gm.Data.calendarDay = 1;
            gm.Data.calendarLastUpdateTicks = DateTime.UtcNow.Ticks;
            gm.SaveGame();
        }
    }

    private string TicksToLocal(long ticks)
    {
        if (ticks <= 0) return "n/a";
        return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }

    // Public accessors
    public string GetCurrentSeason() => seasons[currentSeasonIndex];
    public int GetCurrentDay() => currentDay;
    public int GetCurrentSeasonIndex() => currentSeasonIndex;
    public int GetCurrentYear() => currentYear;
}
