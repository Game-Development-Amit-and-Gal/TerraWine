using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;   // חשוב בשביל MaskableGraphic
using TMPro;

/// <summary>
/// Clickable zone on the world map that changes the scene
/// and shows a tooltip when the mouse is over it.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WorldMapZone : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "Wood";
    [SerializeField] private Vector2 targetPlayerPos = Vector2.zero;

    [Header("Tooltip Settings")]
    [SerializeField] private string tooltipText = "Wood Area";
    [SerializeField] private RectTransform tooltipUI;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0f, 50f);

    private TextMeshProUGUI tooltipLabel;

    private void Awake()
    {
        if (tooltipUI != null)
        {
            // set label text
            tooltipLabel = tooltipUI.GetComponentInChildren<TextMeshProUGUI>();
            if (tooltipLabel != null)
                tooltipLabel.text = tooltipText;

            // 🟢 make tooltip ignore all raycasts so it won't steal the pointer
            foreach (var g in tooltipUI.GetComponentsInChildren<MaskableGraphic>())
            {
                g.raycastTarget = false;
            }

            tooltipUI.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipUI != null)
        {
            tooltipUI.gameObject.SetActive(true);

            // place tooltip near this zone on screen
            Vector3 worldPos = transform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            tooltipUI.position = screenPos + (Vector3)tooltipOffset;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeScene(targetSceneName, targetPlayerPos);
        }
        else
        {
            Debug.LogWarning("[WorldMapZone] GameManager.Instance is null.");
        }
    }
}
