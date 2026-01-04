using UnityEngine;
using UnityEngine.EventSystems;

public class WellHoverWaterUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Where to place the meter above the well")]
    [SerializeField] private Transform anchor;              // אם ריק -> this.transform
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f); // מעל הבאר
    [SerializeField] private Camera worldCamera;            // אם ריק -> Camera.main

    private void Awake()
    {
        if (anchor == null) anchor = transform;
        if (worldCamera == null) worldCamera = Camera.main;
    }

    public void OnPointerEnter(PointerEventData eventData) => ShowAtWell();
    public void OnPointerMove(PointerEventData eventData) => ShowAtWell();   // אופציונלי
    public void OnPointerExit(PointerEventData eventData) => WaterMeterUI.Instance?.Hide();

    private void ShowAtWell()
    {
        var wm = WaterManager.Instance;
        if (wm == null || WaterMeterUI.Instance == null) return;
        if (worldCamera == null) return;

        float n = (wm.Max <= 0) ? 0f : (wm.Current / (float)wm.Max);

        Vector3 worldPos = anchor.position + worldOffset;
        Vector2 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        // צריך ש-WaterMeterUI יקבל גם מיקום מסך
        WaterMeterUI.Instance.Show(n, screenPos);
    }
}
