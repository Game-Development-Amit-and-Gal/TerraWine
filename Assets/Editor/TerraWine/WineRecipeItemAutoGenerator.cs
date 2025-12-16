#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WineRecipeItemAutoGenerator
{
    private static bool _busy = false;

    public static void EnsureItemsForRecipe(WineRecipeSO recipe, AutoItemGenerationSettings settings)
    {
        if (_busy) return;
        if (recipe == null || settings == null) return;

        _busy = true;
        try
        {
            EnsureFolder(settings.itemsFolder);

            // 1) Grapes + seeds for each ingredient
            if (recipe.grapes != null)
            {
                foreach (var ia in recipe.grapes)
                {
                    if (string.IsNullOrWhiteSpace(ia.itemName))
                        continue;

                    string grapeId = SanitizeId(ia.itemName);
                    var grapeItem = GetOrCreateItem(settings, grapeId, grapeId, item =>
                    {
                        item.isSeed = false;
                        item.isWineBottle = false;
                        item.category = ItemCategory.Resources;
                        item.price = settings.defaultGrapePrice;
                        item.stackable = true;
                        item.maxStack = 99;
                    });

                    // Derive base name from grape id (remove _GRAPES / _GRAPE / _GRAP / _GRAP…)
                    string baseName = DeriveBaseFromGrapeId(grapeId);

                    // Seed
                    string seedId = $"{baseName}_Seed";
                    var seedItem = GetOrCreateItem(settings, seedId, seedId, item =>
                    {
                        item.isSeed = true;
                        item.isWineBottle = false;
                        item.category = ItemCategory.Resources;
                        item.price = settings.defaultSeedPrice;

                        item.growTimeSeconds = settings.defaultSeedGrowTimeSeconds;
                        item.harvestItem = grapeItem;
                        item.harvestAmount = settings.defaultHarvestAmount;

                        item.stackable = true;
                        item.maxStack = 99;
                    });

                    // Seed Sell (separate ItemSO if you insist on having one)
                    string seedSellId = $"{baseName}_Seed_Sell";
                    GetOrCreateItem(settings, seedSellId, seedSellId, item =>
                    {
                        // If you want it to still be plantable when bought, keep isSeed=true
                        item.isSeed = true;
                        item.isWineBottle = false;
                        item.category = ItemCategory.Resources;
                        item.price = settings.defaultSeedSellPrice;

                        item.growTimeSeconds = seedItem.growTimeSeconds;
                        item.harvestItem = seedItem.harvestItem;
                        item.harvestAmount = seedItem.harvestAmount;

                        item.stackable = true;
                        item.maxStack = 99;
                    });
                }
            }

            // 2) Bottles for Semi/Dry (and auto-assign into recipe if missing)
            string recipeBase = SanitizeId(string.IsNullOrWhiteSpace(recipe.id) ? recipe.name : recipe.id);

            // Semi
            {
                string bottleSemiId = $"{recipeBase}_Bottle_Semi";
                var semiOutput = recipe.semiDry;
                int rating = Mathf.Clamp(semiOutput.rating, 1, 10);

                var bottleSemi = GetOrCreateItem(settings, bottleSemiId,
                    $"{recipe.wineName} (Semi-Dry)",
                    item =>
                    {
                        item.isSeed = false;
                        item.isWineBottle = true;
                        item.category = ItemCategory.WineBottles;
                        item.price = settings.bottleBasePrice + rating * settings.bottlePricePerRating;
                        item.stackable = true;
                        item.maxStack = 99;
                    });

                if (semiOutput.bottleItem == null)
                {
                    semiOutput.bottleItem = bottleSemi;
                    recipe.semiDry = semiOutput;
                    EditorUtility.SetDirty(recipe);
                }
            }

            // Dry
            {
                string bottleDryId = $"{recipeBase}_Bottle_Dry";
                var dryOutput = recipe.dry;
                int rating = Mathf.Clamp(dryOutput.rating, 1, 10);

                var bottleDry = GetOrCreateItem(settings, bottleDryId,
                    $"{recipe.wineName} (Dry)",
                    item =>
                    {
                        item.isSeed = false;
                        item.isWineBottle = true;
                        item.category = ItemCategory.WineBottles;
                        item.price = settings.bottleBasePrice + rating * settings.bottlePricePerRating;
                        item.stackable = true;
                        item.maxStack = 99;
                    });

                if (dryOutput.bottleItem == null)
                {
                    dryOutput.bottleItem = bottleDry;
                    recipe.dry = dryOutput;
                    EditorUtility.SetDirty(recipe);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            _busy = false;
        }
    }

    private static ItemSO GetOrCreateItem(
        AutoItemGenerationSettings settings,
        string id,
        string displayName,
        Action<ItemSO> init)
    {
        string safeName = MakeSafeFileName(id);
        string path = $"{settings.itemsFolder}/{safeName}.asset".Replace("\\", "/");

        var existing = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
        if (existing != null)
            return existing;

        var item = ScriptableObject.CreateInstance<ItemSO>();
        item.id = id;
        item.displayName = displayName;

        init?.Invoke(item);

        AssetDatabase.CreateAsset(item, path);
        EditorUtility.SetDirty(item);
        return item;
    }

    private static void EnsureFolder(string folderPath)
    {
        folderPath = folderPath.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        // Create nested folders if needed
        string[] parts = folderPath.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string SanitizeId(string raw)
    {
        return raw.Trim();
    }

    private static string DeriveBaseFromGrapeId(string grapeId)
    {
        if (string.IsNullOrWhiteSpace(grapeId)) return grapeId;

        string upper = grapeId.ToUpperInvariant();
        string[] suffixes = { "_GRAPES", "_GRAPE", "_GRAP" };

        foreach (var s in suffixes)
        {
            if (upper.EndsWith(s))
                return grapeId.Substring(0, grapeId.Length - s.Length);
        }

        return grapeId;
    }

    private static string MakeSafeFileName(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c.ToString(), "");
        return s;
    }
}
#endif
