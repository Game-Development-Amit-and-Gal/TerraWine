using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform gridRoot;
    [SerializeField] private CalendarDayCellUI dayCellPrefab;

    [SerializeField] private TMP_Text seasonTitleText;
    [SerializeField] private TMP_Text yearText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Calendar Config")]
    [SerializeField] private int daysPerSeason = 15;
    [SerializeField] private int seasonsPerYear = 3;
    [SerializeField] private int maxYears = 3; // אם את רוצה 3 שנים בלבד

    // תצוגה נוכחית (דפדוף)
    private int viewYear = 1;          // 1..maxYears
    private int viewSeasonIndex = 0;   // 0..2

    // הנתונים הנוכחיים של המשחק
    private int currentYear = 1;
    private int currentSeasonIndex = 0;
    private int currentDay = 1;

    private readonly string[] seasonNames = { "Earth", "Vine", "Winery" };

    private void OnEnable()
    {
        BuildGridOnce();

        if (prevButton != null) prevButton.onClick.AddListener(PrevSeason);
        if (nextButton != null) nextButton.onClick.AddListener(NextSeason);

        JumpToCurrentPage();
        Refresh();
    }

    private void OnDisable()
    {
        if (prevButton != null) prevButton.onClick.RemoveListener(PrevSeason);
        if (nextButton != null) nextButton.onClick.RemoveListener(NextSeason);
    }

    private void BuildGridOnce()
    {
        if (gridRoot == null || dayCellPrefab == null) return;

        // אם כבר בנינו – לא לבנות שוב
        if (gridRoot.childCount > 0) return;

        for (int d = 1; d <= daysPerSeason; d++)
        {
            var cell = Instantiate(dayCellPrefab, gridRoot);
            cell.name = "Day_" + d;
        }
    }

    private void ReadCurrentFromGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.Data != null)
        {
            currentDay = Mathf.Clamp(GameManager.Instance.Data.calendarDay, 1, daysPerSeason);
            currentSeasonIndex = Mathf.Clamp(GameManager.Instance.Data.calendarSeasonIndex, 0, seasonsPerYear - 1);
            currentYear = Mathf.Max(1, GameManager.Instance.Data.calendarYear);
        }
    }

    private int NameToSeasonIndex(string seasonName)
    {
        for (int i = 0; i < seasonNames.Length; i++)
            if (string.Equals(seasonNames[i], seasonName, System.StringComparison.OrdinalIgnoreCase))
                return i;

        return 0;
    }

    private void JumpToCurrentPage()
    {
        ReadCurrentFromGame();

        viewYear = currentYear;
        viewSeasonIndex = currentSeasonIndex;
    }

    private void Refresh()
    {
        ReadCurrentFromGame();

        // כותרות
        if (seasonTitleText != null)
            seasonTitleText.text = seasonNames[Mathf.Clamp(viewSeasonIndex, 0, seasonNames.Length - 1)];

        if (yearText != null)
            yearText.text = "Year " + viewYear;

        // מצב כפתורים
        bool canPrev = !(viewYear == 1 && viewSeasonIndex == 0);
        bool canNext = !(viewYear == maxYears && viewSeasonIndex == seasonsPerYear - 1);

        if (prevButton != null) prevButton.interactable = canPrev;
        if (nextButton != null) nextButton.interactable = canNext;

        // ציור ימים
        if (gridRoot == null) return;

        bool viewingSameSeasonAsCurrent =
            (viewYear == currentYear && viewSeasonIndex == currentSeasonIndex);

        bool viewingPastSeason =
            IsSeasonBefore(viewYear, viewSeasonIndex, currentYear, currentSeasonIndex);

        bool viewingFutureSeason =
            IsSeasonAfter(viewYear, viewSeasonIndex, currentYear, currentSeasonIndex);

        for (int i = 0; i < gridRoot.childCount; i++)
        {
            var cell = gridRoot.GetChild(i).GetComponent<CalendarDayCellUI>();
            if (cell == null) continue;

            int day = i + 1;

            bool isPast = false;
            bool isToday = false;
            bool lockFuture = false;

            if (viewingPastSeason)
            {
                // כל הימים עברו
                isPast = true;
            }
            else if (viewingFutureSeason)
            {
                // עונות עתידיות - אפשר לנעול או להשאיר ריק
                lockFuture = true;
            }
            else if (viewingSameSeasonAsCurrent)
            {
                // אותה עונה ושנה כמו הנוכחי
                if (day < currentDay) isPast = true;
                if (day == currentDay) isToday = true;
            }
            else
            {
                // מצב "אותה שנה אבל עונה אחרת" לא יקרה כי זה נכנס ל-past/future
            }

            cell.Set(day, isPast, isToday, lockFuture);
        }
    }

    private bool IsSeasonBefore(int yA, int sA, int yB, int sB)
    {
        if (yA != yB) return yA < yB;
        return sA < sB;
    }

    private bool IsSeasonAfter(int yA, int sA, int yB, int sB)
    {
        if (yA != yB) return yA > yB;
        return sA > sB;
    }

    private void PrevSeason()
    {
        if (viewYear == 1 && viewSeasonIndex == 0) return;

        viewSeasonIndex--;
        if (viewSeasonIndex < 0)
        {
            viewYear--;
            viewSeasonIndex = seasonsPerYear - 1;
        }

        Refresh();
    }

    private void NextSeason()
    {
        if (viewYear == maxYears && viewSeasonIndex == seasonsPerYear - 1) return;

        viewSeasonIndex++;
        if (viewSeasonIndex >= seasonsPerYear)
        {
            viewYear++;
            viewSeasonIndex = 0;
        }

        Refresh();
    }

    // אם תרצי לקרוא לזה מבחוץ בכל פעם שסצנה נטענת
    public void SnapToCurrentAndRefresh()
    {
        JumpToCurrentPage();
        Refresh();
    }
}

