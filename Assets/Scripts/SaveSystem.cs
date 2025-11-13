using System.IO;
using UnityEngine;

public static class SaveSystem
{
    static string PathFile => System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");

    public static void Save(GameData data)
    {
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(PathFile, json);
    }
    public static bool HasSave() => File.Exists(PathFile);

    public static GameData Load()
    {
        if (!HasSave()) return null;
        var json = File.ReadAllText(PathFile);
        return JsonUtility.FromJson<GameData>(json);
    }
    public static void Delete() { if (HasSave()) File.Delete(PathFile); }
}
