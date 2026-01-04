using TMPro;
using UnityEngine;

public class InventoryTooltipUI : MonoBehaviour
{
    public static InventoryTooltipUI Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private RectTransform root; // RectTransform של הפאנל
    [SerializeField] private TMP_Text label;     // TMP של הטקסט

    [Header("Position")]
    [SerializeField] private Vector2 offset = new Vector2(14f, -14f);

    private Canvas canvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        canvas = GetComponentInParent<Canvas>();

        // מתחילים מוסתר
        gameObject.SetActive(false);
    }

    public void Show(string text, Vector2 screenPos)
    {
        if (root == null || label == null) return;
        if (string.IsNullOrWhiteSpace(text)) return;

        label.text = text;
        gameObject.SetActive(true);

        SetPosition(screenPos);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetPosition(Vector2 screenPos)
    {
        Vector2 pos = screenPos + offset;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            RectTransform canvasRect = canvas.transform as RectTransform;
            Camera cam = canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pos, cam, out Vector2 local))
                root.anchoredPosition = local;
        }
        else
        {
            root.position = pos;
        }
    }
}
