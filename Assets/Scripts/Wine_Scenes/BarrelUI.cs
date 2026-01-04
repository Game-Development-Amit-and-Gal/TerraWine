using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BarrelUI : MonoBehaviour
{
    [Header("Root Panel")]
    [SerializeField] private GameObject panel;

    [Header("Recipe List")]
    [SerializeField] private Transform recipeListRoot;      // Should be: ScrollView/Viewport/Content
    [SerializeField] private Button recipeButtonPrefab;     // Recipe button prefab

    [Header("Details")]
    [SerializeField] private TMP_Text detailsText;

    [Header("Buttons")]
    [SerializeField] private Button semiDryButton;
    [SerializeField] private Button dryButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Barrel currentBarrel;
    private string selectedRecipeId;
    private WineDryness selectedDryness = WineDryness.SemiDry;

    private readonly List<Button> spawned = new();

    private void Start()
    {
        if (panel != null) panel.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (semiDryButton != null)
            semiDryButton.onClick.AddListener(() =>
            {
                selectedDryness = WineDryness.SemiDry;
                RefreshDetails();
            });

        if (dryButton != null)
            dryButton.onClick.AddListener(() =>
            {
                selectedDryness = WineDryness.Dry;
                RefreshDetails();
            });

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
    }

    public void OpenForBarrel(Barrel barrel)
    {
        currentBarrel = barrel;
        selectedRecipeId = null;
        selectedDryness = WineDryness.SemiDry;

        if (panel != null) panel.SetActive(true);

       
        StartCoroutine(DelayedBuildList());
    }

    private IEnumerator DelayedBuildList()
    {
        yield return null; // ממתין פריים אחד
        BuildList();
        RefreshDetails();
        ForceRebuildListLayout();
    }


    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        currentBarrel = null;
        selectedRecipeId = null;
    }

    private void BuildList()
    {
        // Clear previous buttons
        foreach (var b in spawned)
            if (b != null) Destroy(b.gameObject);
        spawned.Clear();

        if (confirmButton != null) confirmButton.interactable = false;

        // ---------- Logs ----------
        if (debugLogs)
        {
            Debug.Log("========== [BarrelUI] BuildList ==========");

            Debug.Log("[BarrelUI] currentBarrel = " + (currentBarrel != null ? currentBarrel.name : "NULL"));
            if (currentBarrel != null)
                Debug.Log("[BarrelUI] BarrelPrefabName = " + currentBarrel.BarrelPrefabName);

            var unlocked = GameManager.Instance?.Data?.unlockedRecipeIds;
            Debug.Log("[BarrelUI] unlockedRecipeIds = " +
                      (unlocked == null ? "NULL" : string.Join(", ", unlocked)));

            Debug.Log("[BarrelUI] RecipeManager.Instance = " + (RecipeManager.Instance != null ? "OK" : "NULL"));
            Debug.Log("[BarrelUI] recipeListRoot = " + (recipeListRoot != null ? recipeListRoot.name : "NULL"));
            Debug.Log("[BarrelUI] recipeButtonPrefab = " + (recipeButtonPrefab != null ? recipeButtonPrefab.name : "NULL"));
        }
        // --------------------------

        if (RecipeManager.Instance == null || currentBarrel == null) return;
        if (recipeListRoot == null || recipeButtonPrefab == null) return;

        var recipes = RecipeManager.Instance.GetUnlockedRecipesForBarrel(currentBarrel.BarrelPrefabName);

        if (debugLogs)
            Debug.Log("[BarrelUI] recipes after barrel filter = " + recipes.Count);

        if (recipes.Count == 0)
        {
            if (detailsText != null)
                detailsText.text = "You have no unlocked recipes that match this barrel.";

            if (debugLogs)
                Debug.LogWarning("[BarrelUI] No recipes to show (0 after filter).");

            return;
        }

        // Build buttons
        foreach (var r in recipes)
        {
            if (debugLogs)
                Debug.Log($"[BarrelUI] Show recipe id='{r.id}' name='{r.wineName}' barrelPrefab='{r.barrelPrefab?.name}'");

            var btn = Instantiate(recipeButtonPrefab, recipeListRoot);
            spawned.Add(btn);

            btn.gameObject.SetActive(true);
            btn.transform.localScale = Vector3.one;

            // Update button label (searches inactive children too)
            var label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = string.IsNullOrWhiteSpace(r.wineName) ? r.id : r.wineName;

            // Make sure listeners don't stack
            btn.onClick.RemoveAllListeners();

            string rid = r.id;
            btn.onClick.AddListener(() =>
            {
                if (debugLogs) Debug.Log("[BarrelUI] Click recipe id = " + rid);

                selectedRecipeId = rid;

                // ✅ Flag only when clicking the specific recipe
                if (rid == "Young_Cabernet")
                    TutorialManager.Instance?.SetFlag("Young Cabernet");

                RefreshDetails();
            });

            // Auto-select the first recipe (but DOES NOT trigger the flag)
            if (string.IsNullOrWhiteSpace(selectedRecipeId))
                selectedRecipeId = rid;
        }

        ForceRebuildListLayout();

        if (debugLogs)
            Debug.Log("========== [BarrelUI] BuildList END ==========");
    }


    private void RefreshDetails()
    {
        if (confirmButton != null) confirmButton.interactable = false;
        if (detailsText == null) return;

        if (currentBarrel == null)
        {
            detailsText.text = "";
            return;
        }

        if (RecipeManager.Instance == null)
        {
            detailsText.text = "RecipeManager is missing.";
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedRecipeId))
        {
            detailsText.text = "Please select a recipe.";
            return;
        }

        var recipe = RecipeManager.Instance.GetRecipe(selectedRecipeId);
        if (recipe == null)
        {
            detailsText.text = "Recipe was not loaded (check Resources/WineRecipes + id).";
            if (debugLogs) Debug.LogWarning("[BarrelUI] GetRecipe returned NULL for id=" + selectedRecipeId);
            return;
        }

        bool hasAll = true;
        System.Text.StringBuilder sb = new();

        sb.AppendLine($"{(string.IsNullOrWhiteSpace(recipe.wineName) ? recipe.id : recipe.wineName)}");
        sb.AppendLine($"{selectedDryness}");
        sb.AppendLine("");

        
        if (InventoryManager.Instance == null)
        {
            sb.AppendLine("No InventoryManager in the scene.");
            hasAll = false;
        }
        else
        {
            foreach (var ing in recipe.grapes)
            {
                string id = ing.itemName;
                if (string.IsNullOrWhiteSpace(id))
                {
                    hasAll = false;
                    sb.AppendLine("- (Missing itemName in recipe)");
                    continue;
                }

                int have = InventoryManager.Instance.CountOf(id);
                bool ok = have >= ing.amount;
                if (!ok) hasAll = false;

                var def = InventoryManager.Instance.GetDefinition(id);
                string nice = def != null ? def.displayName : null;

                if (!string.IsNullOrWhiteSpace(nice))
                {
                    sb.AppendLine($"{nice}");
                    sb.AppendLine($"need {ing.amount}, have {have}");
                }
                else
                {
                    sb.AppendLine($"need {ing.amount}, have {have}");
                }
            }
        }

        sb.AppendLine("");
        var outp = recipe.GetOutput(selectedDryness);

        string bottleId = (outp.bottleItem != null) ? outp.bottleItem.id : "(NO BOTTLE ITEM)";
        sb.AppendLine($"Time: {outp.timeSeconds} seconds");
        
       

        detailsText.text = sb.ToString();

        if (confirmButton != null)
            
            confirmButton.interactable = hasAll && outp.bottleItem != null;
    }

    private void OnConfirm()
    {
        TutorialManager.Instance?.SetFlag("confirm");
        if (currentBarrel == null || string.IsNullOrWhiteSpace(selectedRecipeId))
        {
            Close();
            return;
        }

        bool started = currentBarrel.TryStartRecipe(selectedRecipeId, selectedDryness);
        if (started) Close();
        else RefreshDetails();
    }

    private void ForceRebuildListLayout()
    {
        // Important especially when Content is inside a ScrollView
        if (recipeListRoot is RectTransform rt)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
