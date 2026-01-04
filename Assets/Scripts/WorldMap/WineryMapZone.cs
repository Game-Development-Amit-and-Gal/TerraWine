// Assets/Scripts/WorldMap/WineryMapZone.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Clickable zone on the world map for a winery.
/// - Hover: shows tooltip
/// - Click: opens a UI panel (instead of changing scenes)
/// Works with the NEW Input System.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WineryMapZone : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Open Settings")]
    [SerializeField] private GameObject wineryPanel;
    [SerializeField] private bool closeOnSecondClick = true;
    [SerializeField] private bool closeWithEsc = true;

    [Header("Tooltip Settings")]
    [SerializeField] private string tooltipText = "Winery";
    [SerializeField] private RectTransform tooltipUI;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0f, 50f);

    private TextMeshProUGUI tooltipLabel;

#if ENABLE_INPUT_SYSTEM
    private InputAction escAction;
#endif

    private void Awake()
    {
        // Tooltip setup (do NOT block clicks)
        if (tooltipUI != null)
        {
            tooltipLabel = tooltipUI.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tooltipLabel != null)
                tooltipLabel.text = tooltipText;

            foreach (var g in tooltipUI.GetComponentsInChildren<MaskableGraphic>(true))
                g.raycastTarget = false; // tooltip must not block pointer

            tooltipUI.gameObject.SetActive(false);
        }

        if (wineryPanel != null)
            wineryPanel.SetActive(false);

#if ENABLE_INPUT_SYSTEM
        // ESC key action
        escAction = new InputAction("CloseWineryPanel", binding: "<Keyboard>/escape");
        escAction.performed += _ =>
        {
            if (!closeWithEsc) return;
            if (wineryPanel == null) return;
            if (wineryPanel.activeSelf) SetPanelOpen(false);
        };
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        escAction?.Enable();
    }

    private void OnDisable()
    {
        escAction?.Disable();
    }

    private void OnDestroy()
    {
        if (escAction != null)
        {
            escAction.performed -= _ => { };
            escAction.Dispose();
            escAction = null;
        }
    }
#endif

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipUI == null) return;

        tooltipUI.gameObject.SetActive(true);

        var cam = Camera.main;
        Vector3 screenPos = cam != null
            ? cam.WorldToScreenPoint(transform.position)
            : transform.position;

        tooltipUI.position = screenPos + (Vector3)tooltipOffset;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (wineryPanel == null)
        {
            Debug.LogWarning("[WineryMapZone] wineryPanel is null — cannot open UI.");
            return;
        }

        if (closeOnSecondClick)
            SetPanelOpen(!wineryPanel.activeSelf);
        else
            SetPanelOpen(true);
    }

    private void SetPanelOpen(bool open)
    {
        if (wineryPanel == null) return;
        wineryPanel.SetActive(open);
    }

    // Hook this to the UI Close button
    public void ClosePanel()
    {
        SetPanelOpen(false);
    }
}
