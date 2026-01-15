using UnityEngine;
using UnityEngine.InputSystem; // ✅ Input System

public class HarvestIconUI : MonoBehaviour
{
    public static HarvestIconUI Instance { get; private set; }

    [SerializeField] private RectTransform rect;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector2 offset = new Vector2(24f, -24f);

    private RectTransform _canvasRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // ✅ Auto-fill
        rect ??= GetComponent<RectTransform>();
        canvas ??= GetComponentInParent<Canvas>();
        _canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!gameObject.activeSelf || rect == null || canvas == null || _canvasRect == null)
            return;

        // ✅ Input System mouse position (NO Input.mousePosition)
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Vector2 screenPos = mousePos + offset;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rect.position = screenPos;
            return;
        }

        var cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        if (cam == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, cam, out Vector2 local);
        rect.anchoredPosition = local;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
