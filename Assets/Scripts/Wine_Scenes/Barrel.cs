using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Barrel : MonoBehaviour, IPointerDownHandler
{
    [Header("Identity")]
    [SerializeField] private string barrelId = "";

    [Header("Barrel Prefab Name For Matching Recipes")]
    [Tooltip("If empty -> uses this GameObject name (without (Clone)).")]
    [SerializeField] private string barrelPrefabNameOverride = "";

    public string BarrelPrefabName => string.IsNullOrWhiteSpace(barrelPrefabNameOverride)
        ? StripClone(gameObject.name)
        : StripClone(barrelPrefabNameOverride);

    [Header("UI")]
    [SerializeField] private BarrelUI ui;

    // state
    private bool isAging;
    private bool isReady;

    private string recipeId;
    private WineDryness dryness;

    private long agingStartTicks;
    private long agingEndTicks;

    private string bottleItemId;
    private int bottleAmount;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(barrelId))
        {
            barrelId = Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    private void Awake()
    {
        if (ui == null)
            ui = UnityEngine.Object.FindFirstObjectByType<BarrelUI>();
    }

    private void Start()
    {
        LoadStateFromSave();
        ResumeOrFinishIfNeeded();
    }

    // ✅ במקום Click - פותחים על PointerDown (מבטל כמעט תמיד את הבעיה של "צריך 2 קליקים")
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[Barrel] OnPointerDown hit {name}. ui={(ui ? ui.name : "NULL")}");

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // אם מוכן – איסוף
        if (isReady)
        {
            HarvestBottles();
            return;
        }

        // אם בתהליך יישון – לא לפתוח UI (רק לוג)
        if (isAging)
        {
            Debug.Log($"[Barrel] Aging... remaining {GetRemainingSeconds():0.0}s");
            return;
        }

        if (ui != null)
        {
            TutorialManager.Instance?.SetFlag("barrel");
            ui.OpenForBarrel(this);
        }
        else
        {
            Debug.LogWarning("[Barrel] No BarrelUI in scene.");
        }
    }

    public bool TryStartRecipe(string selectedRecipeId, WineDryness selectedDryness)
    {
        if (isAging || isReady) return false;

        if (RecipeManager.Instance == null)
        {
            Debug.LogWarning("[Barrel] No RecipeManager.");
            return false;
        }

        if (!RecipeManager.Instance.IsUnlocked(selectedRecipeId))
        {
            Debug.LogWarning("[Barrel] Recipe is locked: " + selectedRecipeId);
            return false;
        }

        var recipe = RecipeManager.Instance.GetRecipe(selectedRecipeId);
        if (recipe == null)
        {
            Debug.LogWarning("[Barrel] Recipe not found: " + selectedRecipeId);
            return false;
        }

        // Match barrel prefab if recipe defines it
        if (recipe.barrelPrefab != null)
        {
            string need = StripClone(recipe.barrelPrefab.name);
            if (need != BarrelPrefabName)
            {
                Debug.LogWarning($"[Barrel] Recipe needs barrel '{need}' but this barrel is '{BarrelPrefabName}'");
                return false;
            }
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[Barrel] No InventoryManager.");
            return false;
        }

        // Check ingredients
        foreach (var ing in recipe.grapes)
        {
            string id = ing.itemName;
            if (string.IsNullOrWhiteSpace(id)) return false;

            int have = InventoryManager.Instance.CountOf(id);
            if (have < ing.amount)
            {
                Debug.LogWarning($"[Barrel] Missing {id}. Need {ing.amount}, have {have}");
                return false;
            }
        }

        // Remove ingredients
        foreach (var ing in recipe.grapes)
        {
            bool ok = InventoryManager.Instance.Remove(ing.itemName, ing.amount);
            if (!ok)
            {
                Debug.LogWarning($"[Barrel] Failed removing {ing.itemName} x{ing.amount}");
                return false;
            }
        }

        // Output
        var outp = recipe.GetOutput(selectedDryness);
        if (outp.bottleItem == null)
        {
            Debug.LogWarning("[Barrel] Recipe output has no bottleItem.");
            return false;
        }

        recipeId = selectedRecipeId;
        dryness = selectedDryness;

        bottleItemId = outp.bottleItem.id;
        bottleAmount = outp.bottleAmount;

        agingStartTicks = DateTime.UtcNow.Ticks;
        agingEndTicks = agingStartTicks + (long)(outp.timeSeconds * TimeSpan.TicksPerSecond);

        isAging = true;
        isReady = false;

        SaveState();

        StopAllCoroutines();
        StartCoroutine(AgingRoutine());

        Debug.Log($"[Barrel] Start recipe={recipeId} dryness={dryness} -> {bottleItemId} x{bottleAmount}");
        return true;
    }

    private IEnumerator AgingRoutine()
    {
        while (isAging)
        {
            if (DateTime.UtcNow.Ticks >= agingEndTicks) break;
            yield return null;
        }

        if (!isAging) yield break;

        isAging = false;
        isReady = true;
        SaveState();

        Debug.Log("[Barrel] Ready!");
    }

    private void HarvestBottles()
    {
        if (!isReady) return;
        if (InventoryManager.Instance == null) return;

        if (string.IsNullOrWhiteSpace(bottleItemId) || bottleAmount <= 0)
        {
            Debug.LogWarning("[Barrel] Invalid bottle output.");
            return;
        }

        bool added = InventoryManager.Instance.Add(bottleItemId, bottleAmount);
        Debug.Log($"[Barrel] Harvested {bottleItemId} x{bottleAmount}, success={added}");
        TutorialManager.Instance?.SetFlag("Readybarrel");

        // reset
        isReady = false;
        isAging = false;

        recipeId = null;
        bottleItemId = null;
        bottleAmount = 0;

        agingStartTicks = 0;
        agingEndTicks = 0;

        SaveState();
    }

    private float GetRemainingSeconds()
    {
        if (!isAging) return 0f;
        long left = agingEndTicks - DateTime.UtcNow.Ticks;
        return Mathf.Max(0f, left / (float)TimeSpan.TicksPerSecond);
    }

    // ---------------- Save/Load using GameData ----------------

    private void SaveState()
    {
        if (GameManager.Instance == null || GameManager.Instance.Data == null) return;

        var data = GameManager.Instance.Data;
        data.barrelAging ??= new List<BarrelAgingSave>();

        var s = data.barrelAging.Find(x => x.barrelId == barrelId);
        if (s == null)
        {
            s = new BarrelAgingSave();
            data.barrelAging.Add(s);
        }

        s.barrelId = barrelId;

        s.isAging = isAging;
        s.isReady = isReady;

        s.recipeId = recipeId;
        s.dryness = dryness;

        s.agingStartTicks = agingStartTicks;
        s.agingEndTicks = agingEndTicks;

        s.bottleItemId = bottleItemId;
        s.bottleAmount = bottleAmount;

        SaveSystem.Save(data);
    }

    private void LoadStateFromSave()
    {
        if (GameManager.Instance == null || GameManager.Instance.Data == null) return;

        var list = GameManager.Instance.Data.barrelAging;
        if (list == null) return;

        var s = list.Find(x => x.barrelId == barrelId);
        if (s == null) return;

        isAging = s.isAging;
        isReady = s.isReady;

        recipeId = s.recipeId;
        dryness = s.dryness;

        agingStartTicks = s.agingStartTicks;
        agingEndTicks = s.agingEndTicks;

        bottleItemId = s.bottleItemId;
        bottleAmount = s.bottleAmount;
    }

    private void ResumeOrFinishIfNeeded()
    {
        if (!isAging) return;

        if (DateTime.UtcNow.Ticks >= agingEndTicks)
        {
            isAging = false;
            isReady = true;
            SaveState();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(AgingRoutine());
    }

    private static string StripClone(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("(Clone)", "").Trim();
    }
}
