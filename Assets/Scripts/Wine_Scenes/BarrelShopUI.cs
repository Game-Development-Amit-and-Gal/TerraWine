using UnityEngine;
using UnityEngine.InputSystem; // חשוב לחדש!

public class BarrelShopUI : MonoBehaviour
{
    public static BarrelShopUI Instance { get; private set; }

    [SerializeField] private GameObject panel;   // הפאנל עם הכפתורים (לשייך באינספקטור)
    private BarrelPurchase currentBarrel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        // חדש: Keyboard/Gamepad מה-Input System
        bool pressedEsc =
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        if (pressedEsc && panel != null && panel.activeInHierarchy)
            Close();
    }

    public void Open(BarrelPurchase barrel)
    {
        currentBarrel = barrel;
        if (panel != null) panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        currentBarrel = null;
    }

    public void OnClickBuyNormal() { if (currentBarrel == null) return; currentBarrel.BuyNormal(); Close(); }
    public void OnClickBuyPremium() { if (currentBarrel == null) return; currentBarrel.BuyPremium(); Close(); }
}
