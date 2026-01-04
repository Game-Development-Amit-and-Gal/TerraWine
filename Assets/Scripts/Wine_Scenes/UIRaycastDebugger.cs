using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIRaycastDebugger : MonoBehaviour
{
    [SerializeField] private bool logOnlyOnClick = true;
    [SerializeField] private int maxHitsToPrint = 10;

    private EventSystem _es;
    private readonly List<RaycastResult> _results = new();

    private void Awake()
    {
        _es = EventSystem.current;
    }

    private void Update()
    {
        if (_es == null) return;

        // Detect click with the NEW Input System
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (logOnlyOnClick && !click) return;

        Vector2 pos = Vector2.zero;

        if (Mouse.current != null)
        {
            pos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null)
        {
            var t = Touchscreen.current.primaryTouch;
            if (!t.press.isPressed) return;
            pos = t.position.ReadValue();
            click = true; // treat touch as click
        }
        else
        {
            return;
        }

        _results.Clear();
        var ped = new PointerEventData(_es) { position = pos };

        // Collect ALL UI raycast hits (top-most first)
        _es.RaycastAll(ped, _results);

        if (!click) return;

        if (_results.Count == 0)
        {
            Debug.Log("[UIRaycastDebugger] No UI hits. (UI is not blocking)");
            return;
        }

        Debug.Log("---- UIRaycastDebugger: TOP UI HITS ----");
        for (int i = 0; i < Mathf.Min(maxHitsToPrint, _results.Count); i++)
        {
            var r = _results[i];
            var canvas = r.gameObject.GetComponentInParent<Canvas>();
            string canvasInfo = canvas ? $"{canvas.name} (order={canvas.sortingOrder})" : "no-canvas";
            Debug.Log($"{i}. {r.gameObject.name} | canvas={canvasInfo} | depth={r.depth} | module={r.module.GetType().Name}");
        }
    }
}
