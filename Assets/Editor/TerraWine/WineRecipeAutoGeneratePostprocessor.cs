#if UNITY_EDITOR
using UnityEditor;

public class WineRecipeAutoGeneratePostprocessor : AssetPostprocessor
{
    private static AutoItemGenerationSettings FindSettings()
    {
        var guids = AssetDatabase.FindAssets("t:AutoItemGenerationSettings");
        if (guids == null || guids.Length == 0) return null;
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<AutoItemGenerationSettings>(path);
    }

    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        var settings = FindSettings();
        if (settings == null || !settings.autoGenerateOnImport) return;

        foreach (var path in importedAssets)
        {
            var recipe = AssetDatabase.LoadAssetAtPath<WineRecipeSO>(path);
            if (recipe == null) continue;

            WineRecipeItemAutoGenerator.EnsureItemsForRecipe(recipe, settings);
        }
    }
}
#endif
