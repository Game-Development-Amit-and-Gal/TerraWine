using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tutorial manager that can show:
/// 1) UI tutorial panels (panel/panel2/panel3)
/// 2) UI arrows (RectTransform) pointing to UI targets
/// 3) WORLD arrows (Prefab with SpriteRenderer) that physically exists in the world above a world object
///
/// WORLD arrow selection:
/// - Global default: worldArrowPrefab
/// - Per-step override: Step.worldArrowPrefabOverride (use different arrow prefab per object)
///
/// Target resolution:
/// - If targetObjectName contains "/" -> treated as path under UI root ("UI") or World root ("World")
/// - Otherwise -> deep search by name under UI root then World root, then fallback GameObject.Find
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial UI (Panel 1)")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text tutorialText;

    [Header("Arrows (Panel 1)")]
    [SerializeField] private RectTransform[] arrows1;

    [Header("Got It Button (Panel 1)")]
    [SerializeField] private RectTransform gotItButton;

    [Header("Tutorial UI (Panel 2)")]
    [SerializeField] private GameObject panel2;
    [SerializeField] private TMP_Text tutorialText2;

    [Header("Arrows (Panel 2)")]
    [SerializeField] private RectTransform[] arrows2;

    [Header("Got It Button (Panel 2)")]
    [SerializeField] private RectTransform gotItButton2;

    [Header("Tutorial UI (Panel 3)")]
    [SerializeField] private GameObject panel3;
    [SerializeField] private TMP_Text tutorialText3;

    [Header("Arrows (Panel 3)")]
    [SerializeField] private RectTransform[] arrows3;

    [Header("Got It Button (Panel 3)")]
    [SerializeField] private RectTransform gotItButton3;

    [Header("Got It Position (per panel)")]
    [SerializeField] private bool overrideGotItPosition = false;
    [SerializeField] private Vector2 gotItAnchoredPosPanel1 = Vector2.zero;
    [SerializeField] private Vector2 gotItAnchoredPosPanel2 = Vector2.zero;
    [SerializeField] private Vector2 gotItAnchoredPosPanel3 = Vector2.zero;

    [Header("Per-scene guides")]
    [SerializeField] private SceneGuide[] guides;

    [Header("Scene UI root")]
    [SerializeField] private string sceneUiRootName = "UI";

    [Header("Scene WORLD root")]
    [SerializeField] private string worldRootName = "World";

    // -----------------------------
    // WORLD arrow (Prefab in the world, NOT UI)
    // -----------------------------
    [Header("WORLD Arrow (Prefab in the world, NOT UI)")]
    [Tooltip("Default world arrow prefab (SpriteRenderer + WorldArrowFollow). Can be overridden per step.")]
    [SerializeField] private GameObject worldArrowPrefab;

    [Tooltip("Default offset above the target object (world units).")]
    [SerializeField] private Vector3 defaultWorldArrowOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("If target isn't found immediately after scene load, wait N frames before giving up.")]
    [SerializeField, Min(1)] private int waitFramesForTarget = 60;

    private GameObject worldArrowInstance;
    private Coroutine worldArrowRoutine;

    private GameObject currentWorldRoot;
    private GameObject currentSceneUiRoot;
    private string currentSceneName;
    private SceneGuide currentGuide;
    private int currentStepIndex = 0;

    public static bool tutorialIsRunning = false;
    public static bool tutorialIsRunningGardenScene = false;
    public static Action GrandpaStoppedTalking;

    private PlayerMovement playerMover;
    private readonly Dictionary<GameObject, bool> _originalActive = new();
    private readonly HashSet<GameObject> _touchedThisStep = new();

    public enum PanelChoice { Panel1, Panel2, Panel3 }

    private GameObject activePanel;
    private TMP_Text activeText;
    private RectTransform activeGotItButton;

    // UI arrows (RectTransform inside panels)
    private RectTransform[] activeArrows;
    private RectTransform activeArrow;

    // Auto-next state (timer/flag)
    private Coroutine autoAdvanceRoutine = null;
    private readonly HashSet<string> tutorialFlags = new HashSet<string>();

    public enum NextMode { OnClick, OnTime, OnFlag, OnFlagThenTime }

    [Serializable]
    public class SceneGuide
    {
        public string sceneName;
        public PanelChoice panelChoice = PanelChoice.Panel1;
        public Step[] steps;
    }

    [Serializable]
    public class Step
    {
        [Header("Arrow Variant (UI arrows inside panel)")]
        [Tooltip("Which UI arrow to use inside the selected panel. 0 = first. -1 = no UI arrow (even if target exists).")]
        public int arrowVariantIndex = 0;

        [Header("WORLD Arrow Override (optional)")]
        [Tooltip("If set, this specific WORLD arrow prefab will be used for this step (instead of the global worldArrowPrefab).")]
        public GameObject worldArrowPrefabOverride;

        [Tooltip("Optional: override world offset for THIS step (world units).")]
        public bool overrideWorldOffset = false;

        public Vector3 worldOffsetOverride = new Vector3(0f, 1.5f, 0f);

        [Header("Per-step UI visibility (relative to Scene UI root)")]
        public List<string> hideUiPaths = new List<string>();
        public List<string> showUiPaths = new List<string>();

        [Header("Per-step WORLD visibility (relative to World root)")]
        public List<string> hideWorldPaths = new List<string>();
        public List<string> showWorldPaths = new List<string>();

        [TextArea(3, 8)]
        public string message;

        [Header("Target")]
        [Tooltip("Name or path. If includes '/', it's treated as a path under UI root or World root.")]
        public string targetObjectName;

        [Tooltip("For UI: screen-space offset. For World: extra offset added in world (x,y).")]
        public Vector2 arrowOffset;

        public bool hideSceneUI = false;

        public bool overridePanelChoice = false;
        public PanelChoice panelChoice = PanelChoice.Panel1;

        public NextMode nextMode = NextMode.OnClick;
        public float secondsToAutoNext = 2f;
        public string requiredFlagName = "";

        public bool allowMovementDuringThisStep = false;
    }

    private float faded = 0.2f;
    private float originalAlpha = 1f;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (panel != null) panel.SetActive(false);
        if (panel2 != null) panel2.SetActive(false);
        if (panel3 != null) panel3.SetActive(false);

        HideAllArrows();

        if (gotItButton != null) gotItButton.gameObject.SetActive(false);
        if (gotItButton2 != null) gotItButton2.gameObject.SetActive(false);
        if (gotItButton3 != null) gotItButton3.gameObject.SetActive(false);

        activePanel = null;
        activeText = null;
        activeGotItButton = null;
        activeArrows = null;
        activeArrow = null;

        HideWorldArrow();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        HideWorldArrow();
    }

   /* private void Update()
    {
        // נוח לסגור מדריך בלחיצה על ESC (אפשר להסיר אם לא רוצים)
        if (!tutorialIsRunning) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseGuide();
        }
    }*/

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;

        StopAutoAdvance();
        HideWorldArrow(); // חשוב: לא להשאיר חץ מהסצנה הקודמת
        HideAllArrows();  // חשוב: לנקות חצי UI

        if (scene.name == "MainMenu")
        {
            if (panel != null) panel.SetActive(false);
            if (panel2 != null) panel2.SetActive(false);
            if (panel3 != null) panel3.SetActive(false);

            if (gotItButton != null) gotItButton.gameObject.SetActive(false);
            if (gotItButton2 != null) gotItButton2.gameObject.SetActive(false);
            if (gotItButton3 != null) gotItButton3.gameObject.SetActive(false);

            tutorialIsRunning = false;

            currentSceneUiRoot = null;
            currentGuide = null;
            currentWorldRoot = null;
            currentStepIndex = 0;
            return;
        }

        TryShowGuideForScene(scene.name);
    }

    private void TryShowGuideForScene(string sceneName)
    {
        tutorialIsRunning = false;

        if (GameManager.Instance == null || GameManager.Instance.Data == null)
            return;

        var data = GameManager.Instance.Data;
        if (data.tutorialCompleted) return;

        currentGuide = Array.Find(guides, g => g.sceneName == sceneName);
        if (currentGuide == null || currentGuide.steps == null || currentGuide.steps.Length == 0)
            return;

        if (IsSceneGuideAlreadyDone(sceneName, data))
            return;

        currentSceneUiRoot = GameObject.Find(sceneUiRootName);
        currentWorldRoot = GameObject.Find(worldRootName);

        currentStepIndex = 0;
        SelectPanel(GetEffectivePanelChoiceForStep(currentGuide.steps[currentStepIndex]));

        if (activePanel != null)
            activePanel.SetActive(true);

        tutorialIsRunning = true;
        ShowCurrentStep();
    }

    private PanelChoice GetEffectivePanelChoiceForStep(Step step)
    {
        if (currentGuide == null || step == null) return PanelChoice.Panel1;
        return step.overridePanelChoice ? step.panelChoice : currentGuide.panelChoice;
    }

    // -----------------------------
    // UI Arrow helpers
    // -----------------------------
    private void HideAllArrows()
    {
        if (arrows1 != null) foreach (var a in arrows1) if (a != null) a.gameObject.SetActive(false);
        if (arrows2 != null) foreach (var a in arrows2) if (a != null) a.gameObject.SetActive(false);
        if (arrows3 != null) foreach (var a in arrows3) if (a != null) a.gameObject.SetActive(false);
    }

    private void HideActivePanelArrows()
    {
        if (activeArrows == null) return;
        foreach (var a in activeArrows)
            if (a != null) a.gameObject.SetActive(false);
    }

    private RectTransform GetArrowForStep(Step step)
    {
        if (step == null) return null;
        if (activeArrows == null || activeArrows.Length == 0) return null;

        if (step.arrowVariantIndex < 0) return null;

        int idx = Mathf.Clamp(step.arrowVariantIndex, 0, activeArrows.Length - 1);
        return activeArrows[idx];
    }

    private void SelectPanel(PanelChoice choice)
    {
        if (panel != null) panel.SetActive(false);
        if (panel2 != null) panel2.SetActive(false);
        if (panel3 != null) panel3.SetActive(false);

        HideAllArrows();

        if (gotItButton != null) gotItButton.gameObject.SetActive(false);
        if (gotItButton2 != null) gotItButton2.gameObject.SetActive(false);
        if (gotItButton3 != null) gotItButton3.gameObject.SetActive(false);

        bool canUsePanel2 = (choice == PanelChoice.Panel2) && panel2 != null && tutorialText2 != null;
        bool canUsePanel3 = (choice == PanelChoice.Panel3) && panel3 != null && tutorialText3 != null;

        if (canUsePanel3)
        {
            activePanel = panel3;
            activeText = tutorialText3;
            activeGotItButton = gotItButton3 != null ? gotItButton3 : gotItButton;
            activeArrows = arrows3;
        }
        else if (canUsePanel2)
        {
            activePanel = panel2;
            activeText = tutorialText2;
            activeGotItButton = gotItButton2 != null ? gotItButton2 : gotItButton;
            activeArrows = arrows2;
        }
        else
        {
            activePanel = panel;
            activeText = tutorialText;
            activeGotItButton = gotItButton;
            activeArrows = arrows1;
        }

        activeArrow = null;

        if (overrideGotItPosition && activeGotItButton != null)
        {
            switch (choice)
            {
                case PanelChoice.Panel2: activeGotItButton.anchoredPosition = gotItAnchoredPosPanel2; break;
                case PanelChoice.Panel3: activeGotItButton.anchoredPosition = gotItAnchoredPosPanel3; break;
                default: activeGotItButton.anchoredPosition = gotItAnchoredPosPanel1; break;
            }
        }
    }

    private void ShowCurrentStep()
    {
        StopAutoAdvance();
        RestorePerStepVisibility();
        HideWorldArrow(); // חשוב: לא להשאיר חץ עולם מסטפ קודם

        if (currentGuide == null || currentGuide.steps == null || currentGuide.steps.Length == 0) return;
        if (currentStepIndex < 0 || currentStepIndex >= currentGuide.steps.Length) return;

        Step step = currentGuide.steps[currentStepIndex];

        CachePlayer();
        if (playerMover != null)
            playerMover.SetAllowMoveDuringTutorial(step.allowMovementDuringThisStep);

        SelectPanel(GetEffectivePanelChoiceForStep(step));

        if (activeText == null) return;

        if (activePanel != null) activePanel.SetActive(true);
        if (activeGotItButton != null) activeGotItButton.gameObject.SetActive(step.nextMode == NextMode.OnClick);

        // 1) UI root
        if (currentSceneUiRoot != null)
            currentSceneUiRoot.SetActive(!step.hideSceneUI);

        if (!step.hideSceneUI)
        {
            var fader = currentSceneUiRoot != null ? currentSceneUiRoot.GetComponent<UITransparencyGroup>() : null;
            fader?.RestoreAlpha(faded);
        }
        else
        {
            var fader = currentSceneUiRoot != null ? currentSceneUiRoot.GetComponent<UITransparencyGroup>() : null;
            fader?.RestoreAlpha(originalAlpha);
        }

        // 2) Text
        activeText.text = step.message;

        // 3) Visibility first (so targets become active before arrow tries to attach)
        if (!step.hideSceneUI)
            ApplyPerStepVisibility(step);

        ApplyPerStepWorldVisibility(step);

        // 4) Arrow (UI or WORLD)
        SetupArrowForStep(step);

        StartAutoAdvanceForStep(step);
    }

    private void SetupArrowForStep(Step step)
    {
        HideActivePanelArrows();
        activeArrow = GetArrowForStep(step);

        if (string.IsNullOrEmpty(step.targetObjectName))
        {
            if (activeArrow != null) activeArrow.gameObject.SetActive(false);
            HideWorldArrow();
            return;
        }

        Transform targetT = ResolveTargetTransformSmart(step.targetObjectName);

        if (targetT == null || !targetT.gameObject.activeInHierarchy)
        {
            if (activeArrow != null) activeArrow.gameObject.SetActive(false);
            TrySpawnWorldArrowWithWait(step);
            return;
        }

        // If UI target -> use UI arrow (RectTransform)
        RectTransform targetRect = targetT.GetComponent<RectTransform>();
        if (targetRect != null)
        {
            if (activeArrow == null) return;

            activeArrow.gameObject.SetActive(true);
            activeArrow.position = targetRect.position + (Vector3)step.arrowOffset;

            HideWorldArrow();
            return;
        }

        // Otherwise it's a world target -> show WORLD arrow prefab
        if (activeArrow != null) activeArrow.gameObject.SetActive(false);
        ShowWorldArrowImmediate(step, targetT);
    }

    // -----------------------------
    // WORLD arrow logic
    // -----------------------------
    private void HideWorldArrow()
    {
        if (worldArrowRoutine != null)
        {
            StopCoroutine(worldArrowRoutine);
            worldArrowRoutine = null;
        }

        if (worldArrowInstance != null)
        {
            Destroy(worldArrowInstance);
            worldArrowInstance = null;
        }
    }

    private void TrySpawnWorldArrowWithWait(Step step)
    {
        // If no prefab at all -> nothing to do
        var prefabToUse = step != null && step.worldArrowPrefabOverride != null ? step.worldArrowPrefabOverride : worldArrowPrefab;
        if (prefabToUse == null) return;

        HideWorldArrow();

        int stepSnapshot = currentStepIndex;
        SceneGuide guideSnapshot = currentGuide;

        worldArrowRoutine = StartCoroutine(CoSpawnWorldArrowWhenReady(step, guideSnapshot, stepSnapshot));
    }

    private IEnumerator CoSpawnWorldArrowWhenReady(Step step, SceneGuide guideSnapshot, int stepSnapshot)
    {
        int frames = Mathf.Max(1, waitFramesForTarget);

        for (int i = 0; i < frames; i++)
        {
            if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
                yield break;

            Transform targetT = ResolveTargetTransformSmart(step.targetObjectName);

            if (targetT != null && targetT.gameObject.activeInHierarchy)
            {
                // If it becomes UI -> don't spawn world arrow
                if (targetT.GetComponent<RectTransform>() != null)
                    yield break;

                ShowWorldArrowImmediate(step, targetT);
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning($"[Tutorial] Target not found after waiting {frames} frames: '{step.targetObjectName}' in scene '{currentSceneName}'.");
    }

    private void ShowWorldArrowImmediate(Step step, Transform targetT)
    {
        if (targetT == null) return;

        GameObject prefabToUse = (step != null && step.worldArrowPrefabOverride != null) ? step.worldArrowPrefabOverride : worldArrowPrefab;
        if (prefabToUse == null) return;

        HideWorldArrow();

        Vector3 anchor = GetNiceWorldPoint(targetT);

        worldArrowInstance = Instantiate(prefabToUse);
        worldArrowInstance.name = "WorldArrow_Runtime";

        Vector3 baseOffset = defaultWorldArrowOffset;
        if (step != null && step.overrideWorldOffset) baseOffset = step.worldOffsetOverride;

        // step.arrowOffset used as extra X/Y in world
        Vector3 extra = step != null ? new Vector3(step.arrowOffset.x, step.arrowOffset.y, 0f) : Vector3.zero;

        // corrected offset: if anchor != target.position (Renderer bounds center), we compensate
        Vector3 correctedOffset = (anchor - targetT.position) + baseOffset + extra;

        var follow = worldArrowInstance.GetComponent<WorldArrowFollow>();
        if (follow != null)
        {
            follow.AttachTo(targetT, correctedOffset);
        }
        else
        {
            worldArrowInstance.transform.position = anchor + baseOffset + extra;
        }
    }

    private Vector3 GetNiceWorldPoint(Transform t)
    {
        if (t == null) return Vector3.zero;

        var r = t.GetComponentInChildren<Renderer>();
        if (r != null) return r.bounds.center;

        return t.position;
    }

    // Smart target resolution:
    // 1) If path under UI root -> Find(path)
    // 2) If path under World root -> Find(path)
    // 3) Deep name search under UI, then World
    // 4) fallback GameObject.Find
    private Transform ResolveTargetTransformSmart(string nameOrPath)
    {
        if (string.IsNullOrEmpty(nameOrPath)) return null;

        bool isPath = nameOrPath.Contains("/");

        if (isPath)
        {
            if (currentSceneUiRoot != null)
            {
                var tUi = currentSceneUiRoot.transform.Find(nameOrPath);
                if (tUi != null) return tUi;
            }

            if (currentWorldRoot != null)
            {
                var tW = currentWorldRoot.transform.Find(nameOrPath);
                if (tW != null) return tW;
            }
        }
        else
        {
            if (currentSceneUiRoot != null)
            {
                var deepUi = FindDeepChildByName(currentSceneUiRoot.transform, nameOrPath);
                if (deepUi != null) return deepUi;
            }

            if (currentWorldRoot != null)
            {
                var deepWorld = FindDeepChildByName(currentWorldRoot.transform, nameOrPath);
                if (deepWorld != null) return deepWorld;
            }
        }

        var go = GameObject.Find(nameOrPath);
        return go != null ? go.transform : null;
    }

    private Transform FindDeepChildByName(Transform root, string name)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c.name == name) return c;

            var found = FindDeepChildByName(c, name);
            if (found != null) return found;
        }

        return null;
    }

    // -----------------------------
    // Flags
    // -----------------------------
    public void SetFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName)) return;
        tutorialFlags.Add(flagName);
    }

    public void ClearFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName)) return;
        tutorialFlags.Remove(flagName);
    }

    public void ClearAllFlags() => tutorialFlags.Clear();

    private bool IsFlagSet(string flagName)
    {
        if (string.IsNullOrEmpty(flagName)) return false;
        return tutorialFlags.Contains(flagName);
    }

    private void StopAutoAdvance()
    {
        if (autoAdvanceRoutine != null)
        {
            StopCoroutine(autoAdvanceRoutine);
            autoAdvanceRoutine = null;
        }
    }

    private void StartAutoAdvanceForStep(Step step)
    {
        if (step == null) return;

        int stepSnapshot = currentStepIndex;
        SceneGuide guideSnapshot = currentGuide;

        if (step.nextMode == NextMode.OnFlag || step.nextMode == NextMode.OnFlagThenTime)
        {
            if (string.IsNullOrEmpty(step.requiredFlagName))
                return;
        }

        if (step.nextMode == NextMode.OnTime)
        {
            autoAdvanceRoutine = StartCoroutine(AutoAdvanceAfterSeconds(step.secondsToAutoNext, guideSnapshot, stepSnapshot));
        }
        else if (step.nextMode == NextMode.OnFlag)
        {
            autoAdvanceRoutine = StartCoroutine(AutoAdvanceWhenFlag(step.requiredFlagName, guideSnapshot, stepSnapshot));
        }
        else if (step.nextMode == NextMode.OnFlagThenTime)
        {
            autoAdvanceRoutine = StartCoroutine(AutoAdvanceFlagThenTime(step.requiredFlagName, step.secondsToAutoNext, guideSnapshot, stepSnapshot));
        }
    }

    private IEnumerator AutoAdvanceAfterSeconds(float seconds, SceneGuide guideSnapshot, int stepSnapshot)
    {
        if (seconds < 0f) seconds = 0f;

        yield return new WaitForSeconds(seconds);

        if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
            yield break;

        OnNextStep();
    }

    private IEnumerator AutoAdvanceWhenFlag(string flagName, SceneGuide guideSnapshot, int stepSnapshot)
    {
        while (!IsFlagSet(flagName))
        {
            if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
                yield break;

            yield return null;
        }

        if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
            yield break;

        OnNextStep();
    }

    private IEnumerator AutoAdvanceFlagThenTime(string flagName, float seconds, SceneGuide guideSnapshot, int stepSnapshot)
    {
        while (!IsFlagSet(flagName))
        {
            if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
                yield break;

            yield return null;
        }

        if (seconds < 0f) seconds = 0f;

        float t = 0f;
        while (t < seconds)
        {
            if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
                yield break;

            t += Time.deltaTime;
            yield return null;
        }

        if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
            yield break;

        OnNextStep();
    }

    // -----------------------------
    // Completion
    // -----------------------------
    private bool IsSceneGuideAlreadyDone(string sceneName, GameData data)
    {
        switch (sceneName)
        {
            case "SampleScene": return data.sampleSceneGuideDone;
            case "WorldMap": return data.worldMapGuideDone;
            case "wine": return data.wineGuideDone;
            case "basement": return data.basementGuideDone;
            case "WineryReception": return data.wineryReceptionGuideDone;
            default: return false;
        }
    }

    private void MarkSceneGuideDone(string sceneName, GameData data)
    {
        switch (sceneName)
        {
            case "SampleScene": data.sampleSceneGuideDone = true; break;
            case "WorldMap": data.worldMapGuideDone = true; break;
            case "basement": data.basementGuideDone = true; break;
            case "wine": data.wineGuideDone = true; break;
            case "WineryReception": data.wineryReceptionGuideDone = true; break;
        }
    }

    public void OnNextStep()
    {
        CachePlayer();
        if (playerMover != null)
            playerMover.SetAllowMoveDuringTutorial(false);

        StopAutoAdvance();
        ClearAllFlags();

        if (currentGuide == null)
        {
            CloseGuide();
            tutorialIsRunning = false;
            return;
        }

        currentStepIndex++;

        if (currentStepIndex >= currentGuide.steps.Length)
        {
            CloseGuide();
            tutorialIsRunning = false;
        }
        else
        {
            ShowCurrentStep();
        }
    }

    private void CloseGuide()
    {
        RestorePerStepVisibility();
        StopAutoAdvance();
        HideWorldArrow();
        HideAllArrows();

        if (GameManager.Instance != null && GameManager.Instance.Data != null)
        {
            var data = GameManager.Instance.Data;

            MarkSceneGuideDone(currentSceneName, data);
            MyAnalytics.SendTutorialSceneCompleted(currentSceneName);

            bool allDone =
                data.sampleSceneGuideDone &&
                data.worldMapGuideDone &&
                data.wineryReceptionGuideDone &&
                data.basementGuideDone &&
                data.wineGuideDone;

            if (allDone && !data.tutorialCompleted)
            {
                data.tutorialCompleted = true;
                MyAnalytics.SendTutorialCompleted();
            }

            GameManager.Instance.SaveGame();
        }

        if (currentSceneUiRoot != null)
            currentSceneUiRoot.SetActive(true);

        if (panel != null) panel.SetActive(false);
        if (panel2 != null) panel2.SetActive(false);
        if (panel3 != null) panel3.SetActive(false);

        if (gotItButton != null) gotItButton.gameObject.SetActive(false);
        if (gotItButton2 != null) gotItButton2.gameObject.SetActive(false);
        if (gotItButton3 != null) gotItButton3.gameObject.SetActive(false);

        currentGuide = null;
        currentStepIndex = 0;

        CachePlayer();
        if (playerMover != null)
            playerMover.SetAllowMoveDuringTutorial(false);

        GrandpaStoppedTalking?.Invoke();
        tutorialIsRunning = false;
    }

    private void CachePlayer()
    {
        if (playerMover != null) return;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            playerMover = p.GetComponent<PlayerMovement>();
    }

    private void RestorePerStepVisibility()
    {
        foreach (var go in _touchedThisStep)
        {
            if (go == null) continue;
            if (_originalActive.TryGetValue(go, out bool wasActive))
                go.SetActive(wasActive);
        }

        _touchedThisStep.Clear();
        _originalActive.Clear();
    }

    private void SetActiveByUiPath(string path, bool active)
    {
        if (currentSceneUiRoot == null) return;
        if (string.IsNullOrWhiteSpace(path)) return;

        var t = currentSceneUiRoot.transform.Find(path);
        if (t == null)
        {
            Debug.LogWarning($"[Tutorial] UI path not found under '{sceneUiRootName}': {path}");
            return;
        }

        var go = t.gameObject;

        if (!_originalActive.ContainsKey(go))
            _originalActive[go] = go.activeSelf;

        go.SetActive(active);
        _touchedThisStep.Add(go);
    }

    private void ApplyPerStepVisibility(Step step)
    {
        if (step == null) return;

        if (step.showUiPaths != null)
            foreach (var p in step.showUiPaths)
                SetActiveByUiPath(p, true);

        if (step.hideUiPaths != null)
            foreach (var p in step.hideUiPaths)
                SetActiveByUiPath(p, false);
    }

    private void SetActiveByWorldPath(string path, bool active)
    {
        if (currentWorldRoot == null) return;
        if (string.IsNullOrWhiteSpace(path)) return;

        var t = currentWorldRoot.transform.Find(path);
        if (t == null)
        {
            Debug.LogWarning($"[Tutorial] WORLD path not found under '{worldRootName}': {path}");
            return;
        }

        var go = t.gameObject;

        if (!_originalActive.ContainsKey(go))
            _originalActive[go] = go.activeSelf;

        go.SetActive(active);
        _touchedThisStep.Add(go);
    }

    private void ApplyPerStepWorldVisibility(Step step)
    {
        if (step == null) return;

        if (step.showWorldPaths != null)
            foreach (var p in step.showWorldPaths)
                SetActiveByWorldPath(p, true);

        if (step.hideWorldPaths != null)
            foreach (var p in step.hideWorldPaths)
                SetActiveByWorldPath(p, false);
    }
}
