using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeRowUI : MonoBehaviour
{
    [Header("3 cells in this row (same order)")]
    public TMP_InputField[] inputs = new TMP_InputField[3];
    public Image[] cellImages = new Image[3];

    public void SetInteractable(bool on)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i] != null) inputs[i].interactable = on;
        }
    }

    public void Clear()
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i] != null) inputs[i].text = "";
        }
    }

    public bool TryGetGuess(out int[] guess)
    {
        guess = new int[3];

        if (inputs == null || inputs.Length < 3) return false;

        for (int i = 0; i < 3; i++)
        {
            if (inputs[i] == null) return false;

            string t = inputs[i].text?.Trim();
            if (string.IsNullOrEmpty(t)) return false;

            if (!int.TryParse(t, out int v)) return false;
            if (v < 0 || v > 9) return false;

            guess[i] = v;
        }

        return true;
    }

    public void SetCellColor(int index, Color c)
    {
        if (cellImages == null || cellImages.Length <= index) return;
        if (cellImages[index] != null) cellImages[index].color = c;
    }

    public void ResetColors(Color baseColor)
    {
        for (int i = 0; i < 3; i++)
            SetCellColor(i, baseColor);
    }
}
