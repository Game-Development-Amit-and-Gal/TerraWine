using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class WineryCodeGameAuto : MonoBehaviour
{
    [Header("Row Roots (each row contains 3 TMP_InputField)")]
    [SerializeField] private Transform row1;
    [SerializeField] private Transform row2;
    [SerializeField] private Transform row3;
    [SerializeField] private Transform row4;

    [Header("UI")]
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Game rules")]
    [SerializeField] private int maxAttempts = 4;     
    [SerializeField] private int digits = 3;          
    [SerializeField] private bool allowDuplicates = true;

    [Header("Colors")]
    [SerializeField] private Color colorGray = new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private Color colorPurple = new Color(0.65f, 0.35f, 0.90f, 1f);
    [SerializeField] private Color colorGreen = new Color(0.25f, 0.85f, 0.35f, 1f);
    [SerializeField] private WorldMapPanelSwitcher panelSwitcher;
    private bool canCloseWithEsc = false;

    private Transform[] rowRoots;
    private TMP_InputField[][] rows;
    private Image[][] rowBgs;

    private int[] secret;
    private int attemptIndex = 0;

    private void Awake()
    {
        rowRoots = new[] { row1, row2, row3, row4 };

        // אוספים אוטומטית את ה-InputFields מכל שורה
        rows = new TMP_InputField[maxAttempts][];
        rowBgs = new Image[maxAttempts][];

        for (int r = 0; r < maxAttempts; r++)
        {
            rows[r] = GetRowInputs(rowRoots[r]);
            rowBgs[r] = GetRowBackgrounds(rows[r]);

            // enforce: ספרה אחת בלבד + מעבר אוטומטי לתא הבא
            for (int i = 0; i < rows[r].Length; i++)
            {
                rows[r][i].contentType = TMP_InputField.ContentType.IntegerNumber;
                rows[r][i].characterLimit = 1;

                int rr = r; // חשוב
                int ii = i; // חשוב
                rows[r][i].onValueChanged.AddListener((val) => OnCellValueChanged(rr, ii, val));
            }
        }

        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmit);
    }

    private void Update()
    {
        if (!canCloseWithEsc) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseGamePanel();
    }

    private void OnEnable()
    {
        StartNewGame();
    }

    public void StartNewGame()
    {
        int start_row = 0;
        attemptIndex = 0;
        secret = GenerateSecret();
        canCloseWithEsc = false;
        // Debug - להסיר אחרי שבטוח עובד
        Debug.Log("[WineryGame] Secret = " + string.Join("", secret));

        // בהתחלה רק שורה ראשונה נראית
        for (int r = 0; r < maxAttempts; r++)
        {
            if (rowRoots[r] != null)
                rowRoots[r].gameObject.SetActive(r == 0);

            ClearRow(r);
            PaintRow(r, null);
            SetRowInteractable(r, r == 0);
        }

        if (statusText != null) statusText.text = "נחשי 3 ספרות (0-9) ואז Submit";
        submitButton.interactable = true;

        FocusRow(start_row);
    }

    private void OnSubmit()
    {
        if (attemptIndex >= maxAttempts) return;

        int[] guess = ReadGuess(attemptIndex);
        if (guess == null)
        {
            if (statusText != null) statusText.text = "מלאי 3 ספרות (0-9) — ספרה אחת בכל תא";
            return;
        }

        int[] states = EvaluateGuessPerSlot(guess, secret);
        PaintRow(attemptIndex, states);
        SetRowInteractable(attemptIndex, false);

        bool win = true;
        for (int i = 0; i < digits; i++)
            if (guess[i] != secret[i]) { win = false; break; }

        if (win)
        {
            GiveRandomRecipeReward();   // ✅ חדש
            submitButton.interactable = false;
            canCloseWithEsc = true;
            if (statusText) statusText.text += "\nלחצי ESC כדי לסגור.";
            return;
        }

        attemptIndex++;

        if (attemptIndex >= maxAttempts)
        {
            if (statusText != null) statusText.text = $"נגמרו הנסיונות. הקוד היה: {string.Join("", secret)}";
            submitButton.interactable = false;
            canCloseWithEsc = true;
            if (statusText) statusText.text += "\nלחצי ESC כדי לסגור.";
            return;
        }

        // חושפים את השורה הבאה
        if (rowRoots[attemptIndex] != null)
            rowRoots[attemptIndex].gameObject.SetActive(true);

        SetRowInteractable(attemptIndex, true);
        FocusRow(attemptIndex);

        if (statusText != null) statusText.text = $"נסיון {attemptIndex + 1}/{maxAttempts}";
    }

    // ---------------- Helpers ----------------

    private TMP_InputField[] GetRowInputs(Transform rowRoot)
    {
        if (rowRoot == null)
        {
            Debug.LogError("[WineryGame] Row root is null!");
            return new TMP_InputField[digits];
        }

        // מביא את כל ה-TMP_InputField מתחת לשורה (כולל ילדים)
        var list = new List<TMP_InputField>(rowRoot.GetComponentsInChildren<TMP_InputField>(true));

        // אם יש יותר/פחות מ-3 -> זו בעיה במבנה
        if (list.Count != digits)
            Debug.LogWarning($"[WineryGame] Row '{rowRoot.name}' has {list.Count} input fields (expected {digits}).");

        return list.ToArray();
    }

    private Image[] GetRowBackgrounds(TMP_InputField[] inputs)
    {
        // ברוב המקרים ה-Image של הרקע נמצא על אותו GameObject של TMP_InputField
        var bgs = new Image[inputs.Length];
        for (int i = 0; i < inputs.Length; i++)
        {
            var img = inputs[i].GetComponent<Image>();
            if (img == null)
                img = inputs[i].GetComponentInChildren<Image>(true);

            bgs[i] = img;
        }
        return bgs;
    }

    private void ClearRow(int r)
    {
        for (int i = 0; i < rows[r].Length; i++)
            rows[r][i].text = "";
    }

    private void SetRowInteractable(int r, bool enabled)
    {
        for (int i = 0; i < rows[r].Length; i++)
            rows[r][i].interactable = enabled;
    }

    private void FocusRow(int r)
    {
        if (rows[r].Length > 0)
            rows[r][0].Select();
    }

    private int[] ReadGuess(int row)
    {
        var g = new int[digits];

        for (int i = 0; i < digits; i++)
        {
            string t = rows[row][i].text;
            if (string.IsNullOrWhiteSpace(t)) return null;

            if (!int.TryParse(t, out int v)) return null;
            if (v < 0 || v > 9) return null;

            // אם מישהו הדביק "12" למרות limit (לפעמים קורה)
            if (t.Length > 1) v = t[0] - '0';

            g[i] = v;
        }

        return g;
    }

    // states: 0=gray, 1=purple, 2=green
    private int[] EvaluateGuessPerSlot(int[] guess, int[] sec)
    {
        int[] result = new int[digits];
        int[] count = new int[10];

        // ירוקים
        for (int i = 0; i < digits; i++)
        {
            if (guess[i] == sec[i]) result[i] = 2; // 2 is green in our case
            else count[sec[i]]++;
        }

        // סגולים
        for (int i = 0; i < digits; i++)
        {
            if (result[i] == 2) continue;
            int d = guess[i];
            if (count[d] > 0)
            {
                result[i] = 1;
                count[d]--;
            }
            else result[i] = 0;
        }

        return result;
    }

    private void PaintRow(int r, int[] states)
    {
        for (int i = 0; i < rowBgs[r].Length; i++)
        {
            if (rowBgs[r][i] == null) continue;

            if (states == null)
            {
                rowBgs[r][i].color = colorGray;
                continue;
            }

            rowBgs[r][i].color =
                states[i] == 2 ? colorGreen :
                states[i] == 1 ? colorPurple :
                colorGray;
        }
    }

    private int[] GenerateSecret()
    {
        int[] s = new int[digits];

        if (allowDuplicates)
        {
            for (int i = 0; i < digits; i++)
                s[i] = Random.Range(0, 10);
            return s;
        }

        var pool = new List<int>();
        for (int i = 0; i < 10; i++) pool.Add(i);

        for (int i = 0; i < digits; i++)
        {
            int idx = Random.Range(0, pool.Count);
            s[i] = pool[idx];
            pool.RemoveAt(idx);
        }

        return s;
    }
    private void GiveRandomRecipeReward()
    {
        var rm = RecipeManager.Instance;
        var gm = GameManager.Instance;

        if (rm == null || gm == null)
        {
            Debug.LogWarning("[WineryGame] Missing RecipeManager or GameManager.");
            if (statusText) statusText.text = "ניצחת! אבל אין RecipeManager/GameManager";
            return;
        }

        string id = rm.GetRandomLockedRecipeId();
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.Log("[WineryGame] WIN but no locked recipes left.");
            if (statusText) statusText.text = "ניצחת! אבל אין עוד מתכונים להוסיף.";
            return;
        }

        bool ok = gm.UnlockRecipe(id, saveImmediately: true);

        Debug.Log("[WineryGame] WIN reward recipe=" + id + " unlocked=" + ok);

        if (statusText) statusText.text = ok
            ? $"ניצחת! קיבלת מתכון חדש: {id}"
            : $"ניצחת! (המתכון {id} כבר היה פתוח)";
    }
    private void CloseGamePanel()
    {
        // כדי שלא יישאר פוקוס על InputField
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        // חוזרים לפאנל הפעולות
        if (panelSwitcher != null)
            panelSwitcher.OpenActionsPanel();
        else
            gameObject.SetActive(false); // fallback אם שכחת לשים רפרנס
    }
    private void OnCellValueChanged(int r, int i, string val)
    {
        // אם התא לא פעיל / השורה לא אינטראקטיבית - לא עושים כלום
        if (!rows[r][i].interactable) return;

        // אם יש בדיוק ספרה אחת -> קופצים לתא הבא באותה שורה
        if (!string.IsNullOrEmpty(val) && val.Length >= 1)
        {
            // תיקון למקרה של הדבקה/הקלדה מהירה
            if (val.Length > 1)
            {
                rows[r][i].SetTextWithoutNotify(val[0].ToString());
            }

            int next = i + 1;
            if (next < digits)
            {
                var nextField = rows[r][next];
                if (nextField != null && nextField.interactable)
                {
                    nextField.Select();
                    nextField.ActivateInputField(); // חשוב ל-TMP שייכנס לפוקוס באמת
                }
            }
        }
    }


}
