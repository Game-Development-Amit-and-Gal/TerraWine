using UnityEngine;

public class SeasonManager : MonoBehaviour
{
    private static SeasonManager _instance;

    [SerializeField] private string[] seasons = { "Earth", "Vine", "Winery" };

    [SerializeField] private int totalDaysInSeason = 15;

    private int currentDay = 1;
    private int currentSeasonIndex = 0;

    private const string KEY_SEASON_INDEX = "TW_SeasonIndex";
    private const string KEY_CURRENT_DAY = "TW_CurrentDay";
    public string KEY_LAST_DATE = "TW_LastDate";

    private void Awake()
    {
        Debug.Log("[SeasonManager] Awake – I EXIST in this scene!");
        if (_instance != null && _instance != this)
        {
            // We already have a SeasonManager – destroy the new one
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAndUpdateByRealTime();
        SaveState();
    }

    private void LoadAndUpdateByRealTime()
    {
        Debug.Log("[SeasonManager] Loading saved state and updating by real time.");
        if (!PlayerPrefs.HasKey(KEY_LAST_DATE))
        {
            SaveState();
            return;
        }

        currentSeasonIndex = PlayerPrefs.GetInt(KEY_SEASON_INDEX, 0);
        currentDay = PlayerPrefs.GetInt(KEY_CURRENT_DAY, 1);

        
        string lastDateStr = PlayerPrefs.GetString(KEY_LAST_DATE, System.DateTime.Now.ToString());

       
        if (System.DateTime.
            TryParse(lastDateStr, out System.DateTime lastDate)) // parse last saved date to DateTime
        {
            System.TimeSpan timePassed = System.DateTime.Now - lastDate;
            int daysPassed = (int)timePassed.TotalDays;
            Debug.Log($"[SeasonManager] Real time passed since last save: {daysPassed} days.");
            for (int i = 0; i < daysPassed; i++)
            {
                Debug.Log($"[SeasonManager] Advancing day for real time passage. Day {i + 1} of {daysPassed}.");
                AdvanceDay();
            }
        } else { Debug.LogWarning("[SeasonManager] Failed to parse last saved date."); }
        Debug.Log("[SeasonManager] Real time update complete.");
    }

    public void AdvanceDay()
    {
        currentDay++;
        Debug.Log($"Advancing to Day {currentDay} of Season {GetCurrentSeason()}.");

        if (currentDay > totalDaysInSeason)
        {
            string oldSeason = GetCurrentSeason();

            // move to next season
            currentSeasonIndex = (currentSeasonIndex + 1) % seasons.Length;
            currentDay = 1; 

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
        if (pause)
        {

            SaveState();
        }
    }

    private void SaveState()
    {
        Debug.Log($"[SeasonManager] Saving state.");
        PlayerPrefs.SetInt(KEY_SEASON_INDEX, currentSeasonIndex);
        PlayerPrefs.SetInt(KEY_CURRENT_DAY, currentDay);
        PlayerPrefs.SetString(KEY_LAST_DATE, System.DateTime.Now.ToString());
        PlayerPrefs.Save();
        Debug.Log($"[SeasonManager] State saved.");
    }

    public string GetCurrentSeason() => seasons[currentSeasonIndex];
    public int GetCurrentDay() => currentDay;
    public int GetTotalDaysInSeason() => totalDaysInSeason;
}
