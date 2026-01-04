using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIArrowBob : MonoBehaviour
{
    [SerializeField] private Vector2 offset = new Vector2(0f, 12f); // כמה לזוז (פיקסלים)
    [SerializeField] private float speed = 4f;                      // מהירות “קפיצה”

    private RectTransform rt;
    private Vector2 basePos;

    private void Awake()
    {
        rt = (RectTransform)transform;
        basePos = rt.anchoredPosition; // מקומי בתוך ההורה (ArrowImage)
    }

    private void OnEnable()
    {
        // כדי שלא “יישאר” מיקום ישן אחרי disable/enable
        basePos = rt.anchoredPosition;
    }

    private void Update()
    {
        float t = Time.unscaledTime * speed;
        float s = Mathf.Sin(t) * 0.5f + 0.5f; // 0..1
        rt.anchoredPosition = basePos + offset * s;
    }
}
