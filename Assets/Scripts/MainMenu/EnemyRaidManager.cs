// Assets/Scripts/MainMenu/EnemyRaidManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyRaidManager : MonoBehaviour
{
    public static EnemyRaidManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    [Header("Where raids are allowed (ONLY here)")]
    [SerializeField] private string raidAllowedScene = "SampleScene";
    [SerializeField] private LoadSceneMode raidAllowedMode = LoadSceneMode.Single;
    [SerializeField] private bool clearPendingOnMainMenu = true;
    [Header("Raid UI (SampleScene)")]
    [SerializeField] private string stolerPanelPath = "UI/Stoler/Panel";
    [SerializeField] private bool showStolerPanelOnSuccessfulRaid = true;


    [Header("Raid start timing (important)")]
    [Tooltip("How many frames to wait after sceneLoaded before trying the raid. Helps PlantManager/plots finish loading.")]
    [SerializeField] private int waitFramesBeforeRaid = 2;

    [Tooltip("Extra small delay (seconds) after frames wait. Can be 0.")]
    [SerializeField] private float extraDelaySeconds = 0f;

    [Tooltip("Safety timeout while waiting for plots/managers (seconds).")]
    [SerializeField] private float waitTimeoutSeconds = 2f;

    [Header("Raid rules")]
    [Range(0f, 1f)] public float baseRaidChance = 0.18f;
    public float raidCooldownHours = 8f;
    public int maxStealsPerRaid = 2;

    [Header("ID patterns (because inventory has no categories)")]
    public string[] bottleIdHints = { "Bottle", "_Bottle", "Wine_" };
    public string[] seedIdHints = { "Seed", "_Seed" };
    public string[] grapeIdHints = { "Grape", "Cabernet", "Merlot", "Grenache", "Petit" };

    [Header("Recipe protection (optional)")]
    public string[] protectedRecipeIds = { "Merlot_Easygoing", "Young_Cabernet" };

    private string pendingReason = null;
    private bool raidRoutineRunning = false;

    private void Log(string msg)
    {
        if (debugLogs) Debug.Log(msg);
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        Log("[Raid] EnemyRaidManager Awake -> subscribed to SceneManager.sceneLoaded");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Log("[Raid] EnemyRaidManager OnDestroy -> unsubscribed from SceneManager.sceneLoaded");
        }
    }

    // Called from GameManager before changing scenes
    public void ScheduleRaidOnNextSceneLoad(string reason)
    {
        pendingReason = reason;
        Log($"[Raid] ScheduleRaidOnNextSceneLoad: reason='{reason}' (will try ONLY when allowed scene loads)");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Log($"[Raid] OnSceneLoaded: scene='{scene.name}', mode={mode}, pendingReason='{pendingReason}'");

        if (string.IsNullOrEmpty(pendingReason))
        {
            Log("[Raid] OnSceneLoaded -> no pendingReason, skipping raid attempt");
            return;
        }

        if (clearPendingOnMainMenu && string.Equals(scene.name, "MainMenu", StringComparison.OrdinalIgnoreCase))
        {
            Log("[Raid] OnSceneLoaded -> MainMenu, clearing pendingReason and skipping");
            pendingReason = null;
            return;
        }

        bool allowedScene = string.Equals(scene.name, raidAllowedScene, StringComparison.OrdinalIgnoreCase);
        bool allowedMode = (mode == raidAllowedMode);

        if (!allowedScene || !allowedMode)
        {
            Log($"[Raid] OnSceneLoaded -> raid not allowed here. Keeping pendingReason for later. (allowedScene='{raidAllowedScene}', allowedMode={raidAllowedMode})");
            return; // IMPORTANT: do NOT clear pendingReason
        }

        // We are in allowed scene. Start the delayed raid routine once.
        if (raidRoutineRunning)
        {
            Log("[Raid] OnSceneLoaded -> raid routine already running, skipping duplicate start");
            return;
        }

        string reasonCopy = pendingReason;
        pendingReason = null; // clear NOW so it won't run again on other loads
        raidRoutineRunning = true;

        StartCoroutine(TryRaidWhenSceneReady(reasonCopy));
    }

    private IEnumerator TryRaidWhenSceneReady(string reason)
    {
        Log($"[Raid] TryRaidWhenSceneReady('{reason}') -> waiting frames={waitFramesBeforeRaid}, extraDelay={extraDelaySeconds}s");

        // 1) Wait a few frames (lets Start()/LoadAll() settle)
        for (int i = 0; i < Mathf.Max(0, waitFramesBeforeRaid); i++)
            yield return null;

        if (extraDelaySeconds > 0f)
            yield return new WaitForSeconds(extraDelaySeconds);

        // 2) Wait until we have GameManager/Data (usually already exists) and (optionally) plots
        float t0 = Time.realtimeSinceStartup;

        while (true)
        {
            bool gmOk = (GameManager.Instance != null && GameManager.Instance.Data != null);

            bool pmOk = (PlantManager.Instance != null && PlantManager.Instance.HasLoaded);

            bool plotsOk = true;
            if (pmOk)
                plotsOk = PlantManager.Instance.HasAnyPlotsInScene(); // אם את רוצה גם לוודא שיש פלוטים

            if (gmOk && pmOk && plotsOk)
                break;

            if (Time.realtimeSinceStartup - t0 > waitTimeoutSeconds)
            {
                Log("[Raid] TryRaidWhenSceneReady -> timeout waiting for PlantManager.HasLoaded. Cancelling this raid attempt.");
                raidRoutineRunning = false;

                // אם את רוצה שינסה שוב בפעם הבאה שנכנסים לסצנה:
                pendingReason = reason;

                yield break;
            }

            yield return null;
        }

        // 3) Do the raid attempt
        TryRaid(reason);

        raidRoutineRunning = false;
        Log("[Raid] TryRaidWhenSceneReady -> finished, raidRoutineRunning=false");
    }

    public void TryRaid(string reason)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Data == null)
        {
            Log("[Raid] TryRaid -> no GameManager/Data, abort");
            return;
        }

        long now = DateTime.UtcNow.Ticks;
        long minTicks = TimeSpan.FromHours(raidCooldownHours).Ticks;

        if (gm.Data.lastRaidTicks != 0)
        {
            double minutesSince = (now - gm.Data.lastRaidTicks) / (double)TimeSpan.TicksPerMinute;
            Log($"[Raid] TryRaid('{reason}') -> minutes since lastRaid={minutesSince:F1} (cooldownHours={raidCooldownHours})");
        }
        else
        {
            Log($"[Raid] TryRaid('{reason}') -> lastRaidTicks=0 (no previous raid)");
        }

        // cooldown check
        if (gm.Data.lastRaidTicks != 0 && (now - gm.Data.lastRaidTicks) < minTicks)
        {
            double minutesLeft = (minTicks - (now - gm.Data.lastRaidTicks)) / (double)TimeSpan.TicksPerMinute;
            Log($"[Raid] TryRaid -> cooldown active, minutesLeft={minutesLeft:F1}. No attempt.");
            return;
        }

        float securityFactor = 1f - Mathf.Clamp01(gm.Data.securityLevel * 0.08f);
        float chance = baseRaidChance * securityFactor;
        float roll = UnityEngine.Random.value;

        Log($"[Raid] TryRaid -> roll={roll:F3}, chance={chance:F3}, securityLevel={gm.Data.securityLevel}, securityFactor={securityFactor:F3}");

        if (roll > chance)
        {
            Log("[Raid] TryRaid -> roll failed. No raid.");
            return;
        }

        Log("[Raid] TryRaid -> roll success! Raid begins.");
        gm.Data.lastRaidTicks = now;

        int stealsLeft = Mathf.Max(1, maxStealsPerRaid);
        int stolenCount = 0;

        while (stealsLeft-- > 0)
        {
            // 0) FIRST: try steal READY harvest from plots
            if (TryStealReadyHarvestFromPlots(out var harvestInfo))
            {
                stolenCount++;
                LogRaid($"נגנב יבול מוכן: {harvestInfo}", reason);
                continue;
            }

            // 1) bottles
            if (TryStealInventoryByHints(bottleIdHints, 1, out var b))
            {
                stolenCount++;
                LogRaid($"נגנב בקבוק/יין: {b}", reason);
                continue;
            }

            // 2) seeds
            if (TryStealInventoryByHints(seedIdHints, 1, out var s))
            {
                stolenCount++;
                LogRaid($"נגנבו זרעים: {s}", reason);
                continue;
            }

            // 3) grapes/resources
            if (TryStealInventoryByHints(grapeIdHints, 1, out var g))
            {
                stolenCount++;
                LogRaid($"נגנבו ענבים/משאב: {g}", reason);
                continue;
            }

            // 4) recipe
            if (TryStealRecipe(out var r))
            {
                stolenCount++;
                LogRaid($"נגנב מתכון: {r}", reason);
                continue;
            }

            Log("[Raid] TryRaid -> nothing left to steal (no candidates). Breaking.");
            break;
        }

        gm.SaveGame();
        if (stolenCount > 0)
        {
            TryShowStolerPanel(reason, stolenCount);
        }

        Log($"[Raid] Raid finished. stolenCount={stolenCount}, reason='{reason}', scene='{SceneManager.GetActiveScene().name}'");
    }

    // ===== READY harvest steal =====
    private bool TryStealReadyHarvestFromPlots(out string stolenInfo)
    {
        stolenInfo = null;

        if (PlantManager.Instance == null)
        {
            Log("[Raid] TryStealReadyHarvestFromPlots -> PlantManager.Instance is null");
            return false;
        }

        if (!PlantManager.Instance.HasAnyPlotsInScene())
        {
            Log("[Raid] TryStealReadyHarvestFromPlots -> no plots in this scene");
            return false;
        }

        bool ok = PlantManager.Instance.EnemyRaid_TryStealRandomPlant(out stolenInfo);
        if (!ok)
        {
            Log("[Raid] TryStealReadyHarvestFromPlots -> no READY plots to steal from");
            return false;
        }

        Log("[Raid] TryStealReadyHarvestFromPlots -> SUCCESS: " + stolenInfo);
        return true;
    }

    private bool TryStealInventoryByHints(string[] hints, int amount, out string stolenId)
    {
        stolenId = null;

        var im = InventoryManager.Instance;
        if (im != null)
        {
            var slots = im.Slots;
            if (slots == null || slots.Count == 0)
            {
                Log("[Raid] TryStealInventoryByHints -> InventoryManager slots empty");
                return false;
            }

            List<string> candidateIds = new();
            foreach (var s in slots)
            {
                if (s == null) continue;
                if (s.amount <= 0) continue;
                if (string.IsNullOrEmpty(s.id)) continue;

                if (IdMatchesAnyHint(s.id, hints))
                    candidateIds.Add(s.id);
            }

            if (candidateIds.Count == 0) return false;

            string pickId = candidateIds[UnityEngine.Random.Range(0, candidateIds.Count)];
            int available = im.CountOf(pickId);
            int stealAmount = Mathf.Clamp(amount, 1, available);

            bool ok = im.Remove(pickId, stealAmount);
            if (!ok)
            {
                Log("[Raid] TryStealInventoryByHints -> Remove failed unexpectedly");
                return false;
            }

            stolenId = $"{pickId} x{stealAmount}";
            Log($"[Raid] Stole from InventoryManager: {stolenId}");
            return true;
        }

        var gm = GameManager.Instance;
        var inv = gm?.Data?.inventory;
        if (inv == null || inv.Count == 0)
        {
            Log("[Raid] TryStealInventoryByHints -> GameData.inventory empty/null");
            return false;
        }

        List<int> candidates = new();
        for (int i = 0; i < inv.Count; i++)
        {
            if (inv[i] == null) continue;
            if (inv[i].amount <= 0) continue;
            if (string.IsNullOrEmpty(inv[i].id)) continue;

            if (IdMatchesAnyHint(inv[i].id, hints))
                candidates.Add(i);
        }

        if (candidates.Count == 0) return false;

        int pickIndex = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        var item = inv[pickIndex];

        int steal = Mathf.Clamp(amount, 1, item.amount);
        item.amount -= steal;

        stolenId = $"{item.id} x{steal}";
        if (item.amount <= 0) inv.RemoveAt(pickIndex);

        Log($"[Raid] Stole from GameData.inventory: {stolenId}");
        return true;
    }

    private bool IdMatchesAnyHint(string id, string[] hints)
    {
        if (string.IsNullOrEmpty(id) || hints == null) return false;

        for (int i = 0; i < hints.Length; i++)
        {
            var h = hints[i];
            if (string.IsNullOrEmpty(h)) continue;

            if (id.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private bool TryStealRecipe(out string stolenRecipeId)
    {
        stolenRecipeId = null;

        var gm = GameManager.Instance;
        var list = gm?.Data?.unlockedRecipeIds;
        if (list == null || list.Count == 0)
        {
            Log("[Raid] TryStealRecipe -> unlockedRecipeIds empty/null");
            return false;
        }

        List<int> candidates = new();
        for (int i = 0; i < list.Count; i++)
        {
            string id = list[i];
            if (string.IsNullOrEmpty(id)) continue;
            if (IsProtectedRecipe(id)) continue;
            candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            Log("[Raid] TryStealRecipe -> no candidates (all protected or empty)");
            return false;
        }

        int idx = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        stolenRecipeId = list[idx];

        list.RemoveAt(idx);

        if (gm.Data.stolenRecipeIds == null) gm.Data.stolenRecipeIds = new List<string>();
        gm.Data.stolenRecipeIds.Add(stolenRecipeId);

        return true;
    }

    private bool IsProtectedRecipe(string id)
    {
        if (protectedRecipeIds == null) return false;
        for (int i = 0; i < protectedRecipeIds.Length; i++)
        {
            if (protectedRecipeIds[i] == id) return true;
        }
        return false;
    }

    private void LogRaid(string msg, string reason)
    {
        var gm = GameManager.Instance;
        if (gm?.Data == null) return;

        if (gm.Data.raidLog == null) gm.Data.raidLog = new List<string>();
        gm.Data.raidLog.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [{reason}] {msg}");

        Debug.Log("[Raid] " + msg);
    }
    private void TryShowStolerPanel(string reason, int stolenCount)
    {
        if (!showStolerPanelOnSuccessfulRaid) return;

        var active = SceneManager.GetActiveScene();
        if (!string.Equals(active.name, raidAllowedScene, StringComparison.OrdinalIgnoreCase))
            return;

        var panel = FindInSceneByPath(stolerPanelPath);
        if (panel == null)
        {
            Log($"[Raid] Stoler panel NOT found at path '{stolerPanelPath}'.");
            return;
        }

        panel.SetActive(true);
        Log($"[Raid] Stoler panel ON. reason='{reason}', stolenCount={stolenCount}");
    }

    private GameObject FindInSceneByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        // תומך ב "UI/Stoler"
        var parts = path.Split('/');
        Transform current = null;

        // root object
        var rootGo = GameObject.Find(parts[0]);
        if (rootGo == null) return null;

        current = rootGo.transform;

        for (int i = 1; i < parts.Length; i++)
        {
            current = current.Find(parts[i]);
            if (current == null) return null;
        }

        return current.gameObject;
    }

}
