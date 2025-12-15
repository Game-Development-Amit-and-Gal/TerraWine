using System.Collections.Generic;
using UnityEngine;

public static class WineRecipeLookup
{
    private static bool loaded = false;
    private static readonly Dictionary<string, WineRecipeSO> recipesById = new();

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;

        recipesById.Clear();

        // טוען את כל המתכונים מ-Resources/WineRecipes
        var all = Resources.LoadAll<WineRecipeSO>("WineRecipes");
        foreach (var r in all)
        {
            if (r == null) continue;
            if (string.IsNullOrWhiteSpace(r.id)) continue;

            if (!recipesById.ContainsKey(r.id))
                recipesById.Add(r.id, r);
        }
    }

    /// <summary>
    /// מחזיר את ה-rating של בקבוק לפי המתכונים שהשחקנית פתחה (unlockedRecipeIds).
    /// אם אותו bottle מופיע בכמה מתכונים/outputs -> ניקח את המקסימום (כדי לא “להפסיד” ניקוד).
    /// </summary>
    public static int GetRatingForBottle(string bottleItemId)
    {
        if (string.IsNullOrEmpty(bottleItemId)) return 0;

        EnsureLoaded();

        var gm = GameManager.Instance;
        if (gm == null || gm.Data == null || gm.Data.unlockedRecipeIds == null) return 0;

        int best = 0;

        foreach (var recipeId in gm.Data.unlockedRecipeIds)
        {
            if (string.IsNullOrWhiteSpace(recipeId)) continue;
            if (!recipesById.TryGetValue(recipeId, out var recipe) || recipe == null) continue;

            // SemiDry output
            if (recipe.semiDry.bottleItem != null &&
                recipe.semiDry.bottleItem.id == bottleItemId)
            {
                if (recipe.semiDry.rating > best) best = recipe.semiDry.rating;
            }

            // Dry output
            if (recipe.dry.bottleItem != null &&
                recipe.dry.bottleItem.id == bottleItemId)
            {
                if (recipe.dry.rating > best) best = recipe.dry.rating;
            }
        }

        return best;
    }
}
