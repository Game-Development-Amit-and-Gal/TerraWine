using UnityEngine;
using UnityEngine.UI;

public class DailyActionsCounterImage : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image targetImage;

    [Header("Sprites (11 states: 0..10)")]
    [Tooltip("סדר חובה: states[0]=0 פעולות (מלא), ... states[10]=10 פעולות (ריק)")]
    [SerializeField] private Sprite[] states = new Sprite[11];

    [Header("Config")]
    [SerializeField] private int maxActionsPerDay = 10;

    private int _lastUsed = -1;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (states == null || states.Length != maxActionsPerDay + 1)
            Debug.LogWarning($"[DailyActionsCounterImage] Expected {maxActionsPerDay + 1} sprites (0..{maxActionsPerDay}), but got {(states == null ? 0 : states.Length)}");
    }

    private void OnEnable()
    {
        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    public void Refresh(bool force)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Data == null) return;
        if (targetImage == null) return;
        if (states == null || states.Length == 0) return;

        int used = Mathf.Clamp(gm.Data.dailyActionsUsed, 0, maxActionsPerDay);

        if (!force && used == _lastUsed) return;
        _lastUsed = used;

        int idx = used; // 0..10
        if (idx < 0 || idx >= states.Length) return;

        var spr = states[idx];
        if (spr == null) return;

        targetImage.sprite = spr;

       
    }
}
