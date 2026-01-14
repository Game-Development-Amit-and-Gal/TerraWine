using UnityEngine;

public class PressFPrompt : MonoBehaviour
{
    public static PressFPrompt Instance { get; private set; }

    [Header("Follow")]
    public RectTransform rect;
    public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    public Camera cam;

    [Header("Float")]
    public float amplitude = 10f;
    public float freq = 1.5f;

    private Transform _target;
    private float _t;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // להשאיר את ה-Canvas/Root בחיים בין סצנות
        DontDestroyOnLoad(transform.root.gameObject);

        rect ??= GetComponent<RectTransform>();
        if (cam == null) cam = Camera.main;

        gameObject.SetActive(false);
    }

    public void Show(Transform target)
    {
        _target = target;
        _t = 0f;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        _target = null;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_target == null || rect == null) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = _target.position + worldOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        
        if (screenPos.z < 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        rect.position = screenPos;

        _t += Time.unscaledDeltaTime;
        float y = Mathf.Sin(_t * freq) * amplitude;
        rect.position += new Vector3(0, y, 0);
    }
}
