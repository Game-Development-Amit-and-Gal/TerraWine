using UnityEngine;
using UnityEngine.UI;

public class WaterMeterUI : MonoBehaviour
{
    public static WaterMeterUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject root;     // האובייקט שמדליקים/מכבים (WaterMeter)
    [SerializeField] private RectTransform panel; // ה-RectTransform של הפאנל של המד
    [SerializeField] private Image fillImage;     // Fill image (type Filled)

    [Header("Optional Offset (pixels)")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 0f);

    private Canvas canvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (root == null) root = gameObject;
        if (panel == null) panel = transform as RectTransform;

        canvas = GetComponentInParent<Canvas>();

        Hide();
    }

    public void Show(float normalized01, Vector2 screenPos)
    {
        if (root != null) root.SetActive(true);
        SetValue(normalized01);
        SetPosition(screenPos);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    public void SetValue(float normalized01)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = Mathf.Clamp01(normalized01);
    }

    private void SetPosition(Vector2 screenPos)
    {
        if (panel == null) return;

        Vector2 pos = screenPos + screenOffset;

        // אם הקנבס הוא Screen Space Overlay – אפשר לשים position במסך
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            panel.position = pos;
            return;
        }

        // אם הקנבס הוא Camera/World – צריך להמיר למסגרת של הקנבס
        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera cam = canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pos, cam, out Vector2 local))
            panel.anchoredPosition = local;
    }
}
