using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;     // Needed for MaskableGraphic (to disable raycasts)
using TMPro;

/// <summary>
/// Represents a clickable area on the world map.
/// When hovered, it displays a tooltip.
/// When clicked, it changes the scene and moves the player to a specific position.
/// </summary>
[RequireComponent(typeof(Collider2D))]   // Ensures the object has a 2D collider for pointer detection
public class WorldMapZone : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // --------- SCENE CHANGE SETTINGS --------- //
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "Wood";    // The scene to load when zone is clicked
    [SerializeField] private Vector2 targetPlayerPos = Vector2.zero;  // Position where the player will spawn in the target scene

    // --------- TOOLTIP SETTINGS --------- //
    [Header("Tooltip Settings")]
    [SerializeField] private string tooltipText = "Wood Area";   // Name to display above this map icon
    [SerializeField] private RectTransform tooltipUI;            // Reference to the tooltip UI object
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0f, 50f); // Distance above icon where tooltip appears

    private TextMeshProUGUI tooltipLabel; // Internal reference to the tooltip text component

    private void Awake()
    {
        if (tooltipUI != null)
        {
            // Grab the TMP label inside the tooltip and apply our text
            tooltipLabel = tooltipUI.GetComponentInChildren<TextMeshProUGUI>();
            if (tooltipLabel != null)
                tooltipLabel.text = tooltipText;

            // Prevent tooltip from blocking pointer events (important!)
            foreach (var g in tooltipUI.GetComponentsInChildren<MaskableGraphic>())
                g.raycastTarget = false;

            // Tooltip should remain hidden until hovering
            tooltipUI.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Triggered when the mouse cursor enters this world area.
    /// Shows the tooltip at a screen position relative to the icon.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipUI != null)
        {
            tooltipUI.gameObject.SetActive(true);

            // Convert world position to screen and apply offset
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            tooltipUI.position = screenPos + (Vector3)tooltipOffset;
        }
    }

    /// <summary>
    /// Triggered when mouse leaves the world area.
    /// Hides the tooltip.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when the zone is clicked (left mouse button).
    /// Uses GameManager to load the selected scene and move the player.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeScene(targetSceneName, targetPlayerPos);
        }
        else
        {
            Debug.LogWarning("[WorldMapZone] GameManager.Instance is null — cannot change scene.");
        }
    }
}
