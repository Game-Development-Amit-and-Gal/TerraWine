using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using System.Threading.Tasks;

/// <summary>
/// Wrapper class used to store a list of plot saves as JSON.
/// JSON cannot directly serialize Lists unless wrapped, so we use this.
/// </summary>
[Serializable]
class PlantPlotsSaveWrapper
{
    public List<PlantPlotSave> plots = new();  // creates an empty List by default
}

/// <summary>
/// Responsible for saving, loading, and resetting ALL plant plots in the scene.
/// Uses PlayerPrefs as storage.
/// </summary>
public class PlantManager : MonoBehaviour
{
    public bool HasLoaded { get; private set; }
    // ------------- STATIC SINGLETON ------------- //

    public static PlantManager Instance { get; private set; }   // global access point

    // Key used in PlayerPrefs storage (acts like a file name)
    const string Key = "PROFILE::DEFAULT::PLANTS";
    private const string CloudKey = "plants_v1";
    /// <summary>
    /// Ensures only ONE PlantManager exists (singleton pattern).
    /// </summary>
    void Awake()
    {
        // If a second instance appears, destroy it to maintain one manager
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        HasLoaded = false;
        // Otherwise, this becomes the real instance
        Instance = this;
    }

    // -------------------------------------------------- //
    // --------------------- SAVE ----------------------- //
    // -------------------------------------------------- //

    /// <summary>
    /// Saves every PlantPlot in the current scene into PlayerPrefs.
    /// Does NOT overwrite if no plots exist (prevents clearing previous saves).
    /// </summary>
    public async void SaveAll()
    {
        PlantPlot[] plots = FindObjectsByType<PlantPlot>(FindObjectsSortMode.None);

        if (plots == null || plots.Length == 0)
        {
            Debug.Log("[PlantManager] SaveAll: no PlantPlots in this scene, skipping save.");
            return;
        }

        var wrapper = new PlantPlotsSaveWrapper();
        foreach (var p in plots)
            wrapper.plots.Add(p.GetSave());

        string json = JsonUtility.ToJson(wrapper);

        // 1) שמירה מקומית (כמו שיש לך)
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();

        // 2) שמירה לענן (רק אם מחוברת)
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
                await CloudSaveSystem.SaveStringAsync(CloudKey, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[PlantManager] Cloud Save failed: " + e.Message);
        }

        Debug.Log("[PlantManager] SaveAll: saved " + wrapper.plots.Count + " plots.");
    }


    // -------------------------------------------------- //
    // --------------------- LOAD ----------------------- //
    // -------------------------------------------------- //

    /// <summary>
    /// Loads saved PlantPlot data and restores their state visually.
    /// deltaSeconds = Time passed while the game was closed (offline growth).
    /// </summary>
    public async void LoadAll(float deltaSeconds)
    {
        HasLoaded = false;

        PlantPlot[] plotsInScene = FindObjectsByType<PlantPlot>(FindObjectsSortMode.None);

        // 0) קודם לנסות להביא JSON מהענן
        string json = null;

        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
                json = await CloudSaveSystem.LoadStringAsync(CloudKey);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[PlantManager] Cloud Load failed, fallback to local: " + e.Message);
        }

        // 1) fallback: אם אין בענן, לוקחים מקומי
        if (string.IsNullOrEmpty(json))
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                foreach (var p in plotsInScene)
                    if (p != null) p.ResetToEmpty();

                HasLoaded = true;
                return;
            }

            json = PlayerPrefs.GetString(Key);
        }

        var wrapper = JsonUtility.FromJson<PlantPlotsSaveWrapper>(json);

        if (wrapper == null || wrapper.plots == null)
        {
            foreach (var p in plotsInScene)
                if (p != null) p.ResetToEmpty();

            HasLoaded = true;
            return;
        }

        var map = new Dictionary<string, PlantPlot>(plotsInScene.Length);
        foreach (var p in plotsInScene)
        {
            if (p == null) continue;
            if (string.IsNullOrEmpty(p.PlotId)) continue;
            map[p.PlotId] = p;
        }

        foreach (var p in plotsInScene)
            if (p != null) p.ResetToEmpty();

        foreach (var saved in wrapper.plots)
        {
            if (saved == null || string.IsNullOrEmpty(saved.id)) continue;

            if (map.TryGetValue(saved.id, out var plot) && plot != null)
                plot.LoadFrom(saved, deltaSeconds);
        }

        HasLoaded = true;
    }


    // -------------------------------------------------- //
    // ------------------- RESET ALL -------------------- //
    // -------------------------------------------------- //

    /// <summary>
    /// Completely wipes all plant progress:
    /// - Deletes saved data from PlayerPrefs.
    /// - Resets all PlantPlots currently in the scene.
    /// </summary>
    public void ResetAll()
    {
        // Delete saved data under our key
        PlayerPrefs.DeleteKey(Key);

        // Find all plots present in the scene
        PlantPlot[] plots = FindObjectsByType<PlantPlot>(FindObjectsSortMode.None);

        // Reset each plot visually and internally
        foreach (var p in plots)
            p.ResetToEmpty();
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
                _ = CloudSaveSystem.DeleteAsync(CloudKey);
        }
        catch { }
    }

    // -------------------------------------------------- //
    // ------------------- UTILITY ---------------------- //
    // -------------------------------------------------- //

    /// <summary>
    /// Returns true if this scene contains at least ONE plant plot.
    /// Useful to detect scenes without farming.
    /// </summary>
    public bool HasAnyPlotsInScene()
    {
        PlantPlot[] plots = FindObjectsByType<PlantPlot>(FindObjectsSortMode.None);
        return plots != null && plots.Length > 0;
    }
    public int GetGrowingPlotsCount()
    {
        if (!HasLoaded) return 0;

        PlantPlot[] plots = FindObjectsByType<PlantPlot>(FindObjectsSortMode.None);

        int count = 0;
        foreach (var p in plots)
        {
            if (p != null && p.IsGrowing)
                count++;
        }
        return count;
    }
    public bool EnemyRaid_TryStealRandomPlant(out string stolenInfo)
    {
        stolenInfo = null;

        PlantPlot[] plots = FindObjectsByType<PlantPlot>(FindObjectsSortMode.None);
        if (plots == null || plots.Length == 0) return false;

        List<PlantPlot> ready = new();
        foreach (var p in plots)
        {
            if (p != null && p.IsReady)
                ready.Add(p);
        }

        if (ready.Count == 0) return false;

        var pick = ready[UnityEngine.Random.Range(0, ready.Count)];
        return pick.EnemyRaid_TryStealHarvest(out stolenInfo);
    }

}