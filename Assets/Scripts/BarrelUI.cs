// Assets/Scripts/Barrels/BarrelUI.cs
using UnityEngine;
using TMPro;

public class BarrelUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text grapesText;
    [SerializeField] private TMP_Text modeText;  // למשל "חצי יבש" / "יבש"

    Barrel currentBarrel;
    int maxGrapes;        // כמה ענבים אפשר להשתמש (מכפלה של 5)
    int selectedGrapes;   // כמה השחקן בחר
    bool makeDry = false; // false = חצי יבש, true = יבש

    public void OpenForBarrel(Barrel barrel, int grapesAvailable)
    {
        currentBarrel = barrel;

        // נשתמש רק בכמות שהיא כפולה של GrapesPerBottle
        maxGrapes = (grapesAvailable / barrel.GrapesPerBottle) * barrel.GrapesPerBottle;

        if (maxGrapes <= 0)
        {
            Debug.Log("[BarrelUI] No grapes to use");
            return;
        }

        // ברירת מחדל: 5 ענבים
        selectedGrapes = barrel.GrapesPerBottle;
        makeDry = false;

        RefreshUI();
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
        currentBarrel = null;
    }

    public void OnMorePressed()
    {
        if (currentBarrel == null) return;
        selectedGrapes = Mathf.Min(selectedGrapes + currentBarrel.GrapesPerBottle, maxGrapes);
        RefreshUI();
    }

    public void OnLessPressed()
    {
        if (currentBarrel == null) return;
        selectedGrapes = Mathf.Max(currentBarrel.GrapesPerBottle, selectedGrapes - currentBarrel.GrapesPerBottle);
        RefreshUI();
    }

    public void OnSemiDryPressed()
    {
        makeDry = false;
        RefreshUI();
    }

    public void OnDryPressed()
    {
        makeDry = true;
        RefreshUI();
    }

    public void OnConfirmPressed()
    {
        if (currentBarrel == null || selectedGrapes <= 0)
        {
            Close();
            return;
        }

        currentBarrel.StartAging(selectedGrapes, makeDry);
        Close();
    }

    void RefreshUI()
    {
        if (grapesText != null)
            grapesText.text = selectedGrapes.ToString();

        if (modeText != null)
            modeText.text = makeDry ? "יבש (5 דקות)" : "חצי יבש (2 דקות)";
    }
}
