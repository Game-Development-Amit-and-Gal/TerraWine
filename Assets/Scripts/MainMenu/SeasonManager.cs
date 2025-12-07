using UnityEngine;

public class SeasonManager : MonoBehaviour
{
    private static SeasonManager _instance;   // Singleton instance to prevent duplicates

    // Names of the seasons in the game (ordered cycle)
    [SerializeField] private string[] seasons = { "Earth", "Vine", "Winery" };

    // How many in-game days each season lasts
    [SerializeField] private int totalDaysInSeason = 15;

    private int currentDay = 1;               // Current day inside the active season
    private int currentSeasonIndex = 0;       // Index of current season from the 'seasons' array

    // PlayerPrefs keys used to persist season progress
    private const string KEY_SEASON_INDEX = "TW_SeasonIndex";
    private const string KEY_CURRENT_DAY = "TW_CurrentDay";
    public string KEY_LAST_DATE = "TW_LastDate";  // Saved last login in real-world time

    private void Awake()
    {
        Debug.Log("[SeasonManager] Awake – I EXIST in this scene!");

        // Enforce singleton → destroy duplicate season managers in other scenes
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Load saved season/day and adjust based on real-world time passed
        LoadAndUpdateByRealTime();

        // Always save again to establish/update timestamp
        SaveState();
    }

    /// <summary>
    /// Loads saved season data and updates the current season/day
    /// based on how many real-world days have passed.
    /// </summary>
    private void LoadAndUpdateByRealTime()
    {
        int getIntDefault = 0; // avoid magic numebrs
        int getintDefault_one = 1;
        Debug.Log("[SeasonManager] Loading saved state and updating by real time.");

        // If no date saved → this is the first time playing
        if (!PlayerPrefs.HasKey(KEY_LAST_DATE))
        {
            SaveState();
            return;
        }

        // Load last saved values

        currentSeasonIndex = PlayerPrefs.GetInt(KEY_SEASON_INDEX, getIntDefault);
        currentDay = PlayerPrefs.GetInt(KEY_CURRENT_DAY, getintDefault_one);

        // Last login date saved as a string
        string lastDateStr = PlayerPrefs.GetString(KEY_LAST_DATE, System.DateTime.Now.ToString());

        // Try converting back into a valid DateTime structure
        if (System.DateTime.TryParse(lastDateStr, out System.DateTime lastDate))
        {
            // Calculate real-world time passed since last login
            System.TimeSpan timePassed = System.DateTime.Now - lastDate;
            int daysPassed = (int)timePassed.TotalDays;

            Debug.Log($"[SeasonManager] Real time passed since last save: {daysPassed} days.");

            // For each day passed in real life, advance in-game time
            for (int i = 0; i < daysPassed; i++)
            {
                Debug.Log($"[SeasonManager] Advancing day for real time passage. Day {i + 1} of {daysPassed}.");
                AdvanceDay();
            }
        }
        else
        {
            Debug.LogWarning("[SeasonManager] Failed to parse last saved date.");
        }

        Debug.Log("[SeasonManager] Real time update complete.");
    }

    /// <summary>
    /// Move forward by one in-game day and transition to the next season if needed.
    /// </summary>
    public void AdvanceDay()
    {
        currentDay++;
        Debug.Log($"Advancing to Day {currentDay} of Season {GetCurrentSeason()}.");

        // If the season exceeded its end → reset day and rotate to next season
        if (currentDay > totalDaysInSeason)
        {
            int firstDay = 1; // avoid magic numbers
            string oldSeason = GetCurrentSeason();

            // Move to the next season in the list (looping with %)
            currentSeasonIndex = (currentSeasonIndex + 1) % seasons.Length;
            currentDay =firstDay;

            string newSeason = GetCurrentSeason();
            Debug.Log($"Season {oldSeason} has ended. Transferring to {newSeason}.");
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[SeasonManager] Application quitting, saving state.");
        SaveState();
    }

    private void OnApplicationPause(bool pause)
    {
        Debug.Log($"[SeasonManager] Application pause: {pause}");

        // Save only when the game is actually paused (not resumed)
        if (pause)
            SaveState();
    }

    /// <summary>
    /// Saves the current season/day and the last time the game was running.
    /// </summary>
    private void SaveState()
    {
        Debug.Log($"[SeasonManager] Saving state.");

        PlayerPrefs.SetInt(KEY_SEASON_INDEX, currentSeasonIndex);
        PlayerPrefs.SetInt(KEY_CURRENT_DAY, currentDay);
        PlayerPrefs.SetString(KEY_LAST_DATE, System.DateTime.Now.ToString());
        PlayerPrefs.Save();

        Debug.Log($"[SeasonManager] State saved.");
    }

    // Public accessors for other systems
    public string GetCurrentSeason() => seasons[currentSeasonIndex];
    public int GetCurrentDay() => currentDay;
    public int GetTotalDaysInSeason() => totalDaysInSeason;
}
