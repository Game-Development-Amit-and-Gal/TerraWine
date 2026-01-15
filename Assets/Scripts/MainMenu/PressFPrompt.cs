using UnityEngine;

public class PressFPrompt : MonoBehaviour
{
    public static PressFPrompt Instance { get; private set; }

    [Header("Follow")]
    public RectTransform rect;
    public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    public Camera cam;

    [Header("Pulse (press-like)")]
    public float pulseSpeed = 6f;     // כמה מהר הוא "לוחץ"
    public float pulseAmount = 0.12f; // כמה חזק (0.1 = 10% גדילה)
    public float settleSpeed = 12f;   // כמה מהר חוזר לבייס

    private Transform _target;
    private float _t;
    private Vector3 _baseScale;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(transform.root.gameObject);

        rect ??= GetComponent<RectTransform>();
        if (cam == null) cam = Camera.main;

        if (rect != null) _baseScale = rect.localScale;

        gameObject.SetActive(false);
    }

    public void Show(Transform target)
    {
        _target = target;
        _t = 0f;

        if (rect != null)
        {
            if (_baseScale == Vector3.zero) _baseScale = rect.localScale;
            rect.localScale = _baseScale; // reset
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        _target = null;
        if (rect != null && _baseScale != Vector3.zero)
            rect.localScale = _baseScale;

        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_target == null || rect == null) return;

        // תמיד לתפוס MainCamera עדכני (מונע בעיות אחרי מעבר סצנות)
        var main = Camera.main;
        if (main != null) cam = main;
        if (cam == null) return;

        Vector3 worldPos = _target.position + worldOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0f) return; // לא לכבות את האובייקט

        rect.position = screenPos;

        // ---- Press-like pulse: scale up/down
        _t += Time.unscaledDeltaTime;

        float s = 1f + Mathf.Abs(Mathf.Sin(_t * pulseSpeed)) * pulseAmount;
        Vector3 targetScale = _baseScale * s;

        // החלקה כדי שזה ירגיש "לחיצה" ולא ריצוד
        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, settleSpeed * Time.unscaledDeltaTime);
    }
}
