using System.Collections.Generic;
using UnityEngine;

public class RecipeManager : MonoBehaviour
{
    public static RecipeManager Instance { get; private set; }

    private readonly Dictionary<string, WineRecipeSO> catalog = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        var all = Resources.LoadAll<WineRecipeSO>("WineRecipes");
        foreach (var r in all)
        {
            if (r == null || string.IsNullOrWhiteSpace(r.id)) continue;

            if (!catalog.ContainsKey(r.id))
                catalog.Add(r.id, r);
        }

        Debug.Log("[RecipeManager] Loaded recipes: " + catalog.Count);

        // לוג IDs שנטענו בפועל (כדי לזהות mismatch)
        foreach (var kv in catalog)
            Debug.Log("[RecipeManager] Catalog ID = " + kv.Key);
    }

    public WineRecipeSO GetRecipe(string recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId)) return null;
        return catalog.TryGetValue(recipeId, out var r) ? r : null;
    }

    public bool IsUnlocked(string recipeId)
    {
        if (GameManager.Instance == null || GameManager.Instance.Data == null) return false;
        var list = GameManager.Instance.Data.unlockedRecipeIds ??= new List<string>();
        return list.Contains(recipeId);
    }

    public void Unlock(string recipeId)
    {
        if (GameManager.Instance == null || GameManager.Instance.Data == null) return;

        var list = GameManager.Instance.Data.unlockedRecipeIds ??= new List<string>();
        if (!list.Contains(recipeId))
        {
            list.Add(recipeId);
            SaveSystem.Save(GameManager.Instance.Data);
        }
    }

    // מסנן לפי Barrel prefab name (אם במתכון הוגדר barrelPrefab)
    public List<WineRecipeSO> GetUnlockedRecipesForBarrel(string barrelPrefabName)
    {
        var result = new List<WineRecipeSO>();

        if (GameManager.Instance == null || GameManager.Instance.Data == null)
        {
            Debug.LogWarning("[RecipeManager] GameManager/Data is NULL");
            return result;
        }

        var unlocked = GameManager.Instance.Data.unlockedRecipeIds ??= new List<string>();

        string have = StripClone(barrelPrefabName);
        Debug.Log($"[RecipeManager] Filter for barrel='{have}', unlockedCount={unlocked.Count}");

        foreach (var id in unlocked)
        {
            var r = GetRecipe(id);

            if (r == null)
            {
                Debug.LogWarning($"[RecipeManager] Unlocked id '{id}' NOT found in catalog (GetRecipe=null). " +
                                 "=> בדקי שה-id ב-WineRecipeSO זהה בדיוק ל-id שנשמר ב-unlockedRecipeIds");
                continue;
            }

            Debug.Log($"[RecipeManager] Check recipe id='{r.id}', barrelPrefab='{r.barrelPrefab?.name}'");

            if (r.barrelPrefab != null)
            {
                string need = StripClone(r.barrelPrefab.name);

                // השוואה שלא נשברת מאותיות גדולות/קטנות
                bool match = string.Equals(have, need, System.StringComparison.OrdinalIgnoreCase);

                if (!match)
                {
                    Debug.LogWarning($"[RecipeManager] Recipe '{r.id}' SKIPPED: need barrel='{need}' but have='{have}'");
                    continue;
                }
            }

            Debug.Log($"[RecipeManager] Recipe '{r.id}' PASSED -> added");
            result.Add(r);
        }

        Debug.Log($"[RecipeManager] Result after filter = {result.Count}");
        return result;
    }

    private static string StripClone(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("(Clone)", "").Trim();
    }
}
