using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
class PlantPlotsSaveWrapper
{
    public List<PlantPlotSave> plots = new();
}

public class PlantManager : MonoBehaviour
{
    public static PlantManager Instance { get; private set; }

    const string Key = "PROFILE::DEFAULT::PLANTS";

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ===== SAVE ALL PLOTS =====
    public void SaveAll()
    {
        // Unity 6 – במקום FindObjectsOfType
        PlantPlot[] plots = FindObjectsByType<PlantPlot>(FindObjectsSortMode.None);

        var wrapper = new PlantPlotsSaveWrapper();
        foreach (var p in plots)
            wrapper.plots.Add(p.GetSave());

        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    // ===== LOAD ALL PLOTS =====
    public void LoadAll(float deltaSeconds)
    {
        if (!PlayerPrefs.HasKey(Key)) return;

        string json = PlayerPrefs.GetString(Key);
        var wrapper = JsonUtility.FromJson<PlantPlotsSaveWrapper>(json);
        if (wrapper == null || wrapper.plots == null) return;

        // Unity 6 – במקום FindObjectsOfType
        PlantPlot[] plotsInScene = FindObjectsByType<PlantPlot>(FindObjectsSortMode.None);

        foreach (var saved in wrapper.plots)
        {
            foreach (var plot in plotsInScene)
            {
                if (plot.PlotId == saved.id)
                {
                    plot.LoadFrom(saved, deltaSeconds);
                    break;
                }
            }
        }
    }


    public void ResetAll()
    {
        PlayerPrefs.DeleteKey(Key);


        PlantPlot[] plots = FindObjectsByType<PlantPlot>(FindObjectsSortMode.None);

        foreach (var p in plots)
            p.ResetToEmpty();
    }
}
