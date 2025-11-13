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
    private const string KEY_LAST_DATE = "TW_LastDate";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // We already have a SeasonManager – destroy the new one
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void LoadAndUpdateByRealTime()
    {
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
            for (int i = 0; i < daysPassed; i++)
            {
                AdvanceDay();
            }
        }
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
        SaveState();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveState();
        }
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(KEY_SEASON_INDEX, currentSeasonIndex);
        PlayerPrefs.SetInt(KEY_CURRENT_DAY, currentDay);
        PlayerPrefs.SetString(KEY_LAST_DATE, System.DateTime.Now.ToString());
        PlayerPrefs.Save();
    }

    public string GetCurrentSeason() => seasons[currentSeasonIndex];
    public int GetCurrentDay() => currentDay;
    public int GetTotalDaysInSeason() => totalDaysInSeason;
}
