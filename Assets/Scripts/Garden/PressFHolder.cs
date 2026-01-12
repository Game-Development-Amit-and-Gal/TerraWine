using UnityEngine;

public class PressFHolder : MonoBehaviour
{
    [SerializeField] private float freq = 1.0f;
    [SerializeField] private float speed = 1.0f; // Acts as Amplitude (how high it floats)
    [SerializeField] private RectTransform rectTransform;

    // 1. Variable to store where you put it in the Editor
    private Vector3 _startPos;

    private void Start()
    {
        rectTransform ??= GetComponent<RectTransform>();

        // 2. Capture the initial local position (e.g., Y=2.5 above the door)
        if (rectTransform != null)
        {
            _startPos = rectTransform.localPosition;
        }
    }

    private void Update()
    {
        if (rectTransform == null) return;

        // Calculate the floating offset
        float yOffset = Mathf.Sin(freq * Time.time) * speed;

        // 3. Apply the offset relative to the original Start Position
        // This preserves your X, Y, and Z adjustments from the Editor.
        rectTransform.localPosition = _startPos + new Vector3(0, yOffset, 0);
    }
}