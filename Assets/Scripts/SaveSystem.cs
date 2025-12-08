using System.IO;
using UnityEngine;

/// <summary>
/// Handles saving, loading, checking, and deleting game data as a JSON file.
/// Uses Unity's persistent data path for safe access on all platforms.
/// </summary>
public static class SaveSystem
{
    /// <summary>
    /// Full path to the save file (e.g., ".../AppData/LocalLow/Company/Game/savegame.json").
    /// Uses Application.persistentDataPath which works on Windows, Mac, Android, etc.
    /// </summary>
    private static string PathFile =>
        Path.Combine(Application.persistentDataPath, "savegame.json");

    /// <summary>
    /// Converts the GameData into JSON and writes it to disk.
    /// </summary>
    public static void Save(GameData data)
    {
        var json = JsonUtility.ToJson(data, true); // pretty-print for readability
        File.WriteAllText(PathFile, json);
    }

    /// <summary>
    /// Returns true if the save file exists.
    /// </summary>
    public static bool HasSave() => File.Exists(PathFile);

    /// <summary>
    /// Loads save data from the file.
    /// If no file exists, returns null instead of crashing.
    /// </summary>
    public static GameData Load()
    {
        if (!HasSave())
            return null;

        var json = File.ReadAllText(PathFile);
        return JsonUtility.FromJson<GameData>(json);
    }

    /// <summary>
    /// Deletes the save file from disk (if it exists).
    /// Useful for debugging or resetting progress.
    /// </summary>
    public static void Delete()
    {
        if (HasSave())
            File.Delete(PathFile);
    }
}
