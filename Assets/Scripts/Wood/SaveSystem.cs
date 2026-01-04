using System.IO;
using UnityEngine;

/// <summary>
/// Handles saving, loading, checking, and deleting game data as a JSON file.
/// Uses Unity's persistent data path for safe access on all platforms.
/// </summary>
public static class SaveSystem
{
    private static string PathFile =>
        Path.Combine(Application.persistentDataPath, "savegame.json");

    public static void Save(GameData data)
    {
        Debug.Log($"[SaveSystem] SAVE -> {PathFile}");
        Debug.Log("[SaveSystem] SAVE unlockedRecipeIds = " +
                  (data?.unlockedRecipeIds == null ? "NULL" : string.Join(", ", data.unlockedRecipeIds)));

        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(PathFile, json);
    }

    public static bool HasSave() => File.Exists(PathFile);

    public static GameData Load()
    {
        Debug.Log($"[SaveSystem] LOAD -> {PathFile}");

        if (!HasSave())
        {
            Debug.Log("[SaveSystem] LOAD: no file");
            return null;
        }

        var json = File.ReadAllText(PathFile);
        var d = JsonUtility.FromJson<GameData>(json);

        Debug.Log("[SaveSystem] LOAD unlockedRecipeIds = " +
                  (d?.unlockedRecipeIds == null ? "NULL" : string.Join(", ", d.unlockedRecipeIds)));

        return d;
    }

    public static void Delete()
    {
        Debug.Log($"[SaveSystem] DELETE -> {PathFile}");
        if (HasSave())
            File.Delete(PathFile);
    }
}
