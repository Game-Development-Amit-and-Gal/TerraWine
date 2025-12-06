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
      
    }


    public void SaveAll()
    {
        PlantPlot[] plots = FindObjectsByType<PlantPlot>(FindObjectsSortMode.None);

        // אם אין בכלל ערוגות בסצנה הזאת – לא שומרים (כדי לא לדרוס שמירה קיימת)
        if (plots == null || plots.Length == 0)
        {
            Debug.Log("[PlantManager] SaveAll: no PlantPlots in this scene, skipping save.");
            return;
        }

        var wrapper = new PlantPlotsSaveWrapper();
        foreach (var p in plots)
            wrapper.plots.Add(p.GetSave());

        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();

        Debug.Log("[PlantManager] SaveAll: saved " + wrapper.plots.Count + " plots.");
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

    public bool HasAnyPlotsInScene()
    {
        PlantPlot[] plots = FindObjectsByType<PlantPlot>(FindObjectsSortMode.None);
        return plots != null && plots.Length > 0;
    }

}
