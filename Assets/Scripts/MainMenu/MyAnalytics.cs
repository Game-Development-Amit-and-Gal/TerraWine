using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public static class MyAnalytics
{
    public static bool Ready =>
        UnityServices.State == ServicesInitializationState.Initialized;

    public static void Send(string eventName, Dictionary<string, object> data = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogWarning("[Analytics] eventName is empty -> skip");
                return;
            }

            if (!Ready)
            {
                Debug.LogWarning($"[Analytics] NOT READY -> skip '{eventName}'");
                return;
            }

            if (!AuthenticationManagerWithPassword.AnalyticsReady)
            {
                Debug.LogWarning($"[Analytics] AnalyticsReady=false -> skip '{eventName}'");
                return;
            }

            data ??= new Dictionary<string, object>();
            data["t_utc"] = DateTime.UtcNow.ToString("o");
            data["signedIn"] = AuthenticationService.Instance.IsSignedIn;
            data["playerId"] = AuthenticationService.Instance.PlayerId ?? "null";

            // ✅ זה ה-API שקיים אצלך
            var ev = new CustomEvent(eventName);
            foreach (var kv in data)
            {
                // CustomEvent מקבל סוגים בסיסיים: string/int/float/bool וכו'
                // אם יש לך object מורכב, עדיף להפוך ל-string
                ev[kv.Key] = kv.Value ?? "null";
            }

            AnalyticsService.Instance.RecordEvent(ev);
            AnalyticsService.Instance.Flush();

            Debug.Log($"[Analytics] SENT '{eventName}' keys={data.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Analytics] Failed to send '{eventName}': {ex}");
        }
    }
    public static void SendCheckpointSaved() =>
    Send("checkpoint_saved");

    public static void SendTutorialCompleted() =>
        Send("tutorial_completed");

    public static void SendTutorialSceneCompleted(string sceneName) =>
        Send("tutorial_scene_completed", new Dictionary<string, object> { { "scene", sceneName ?? "unknown" } });

    public static void SendSceneEntered(string sceneName) =>
        Send("scene_entered", new Dictionary<string, object> { { "scene", sceneName ?? "unknown" } });
}
