using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class HoverToggleObject_ES : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject targetObject; // הילד Outline

    private void Awake()
    {
        if (targetObject != null)
            targetObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (targetObject != null)
            targetObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetObject != null)
            targetObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetObject != null)
            targetObject.SetActive(false);
    }
}
