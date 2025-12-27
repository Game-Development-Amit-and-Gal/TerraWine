using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarDayCellUI : MonoBehaviour
{
    [SerializeField] private TMP_Text dayNumberText;
    [SerializeField] private GameObject xMark;
    [SerializeField] private GameObject todayCircle;
    [SerializeField] private Button button; // אופציונלי

    private int dayNumber;

    public void Set(int day, bool isPast, bool isToday, bool isFutureLocked)
    {
        dayNumber = day;

        if (dayNumberText != null)
            dayNumberText.text = day.ToString();

        if (xMark != null) xMark.SetActive(isPast);
        if (todayCircle != null) todayCircle.SetActive(isToday);

        // אם את רוצה "לנעול" ימים עתידיים (רק אם זה שימושי לך)
        if (button != null)
            button.interactable = !isFutureLocked;
    }
}
