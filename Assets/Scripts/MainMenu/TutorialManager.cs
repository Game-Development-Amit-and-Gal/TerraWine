using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages in-game tutorials across multiple scenes.
/// Each scene can have multiple "steps", where each step:
/// - shows a text message,
/// - optionally points an arrow at a target object (UI or world),
/// - can optionally hide the scene UI while this step is active.
/// Completion state is stored in GameData (via GameManager).
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial UI (Panel 1)")]
    [SerializeField] private GameObject panel;          // Root panel of the tutorial UI.
    [SerializeField] private TMP_Text tutorialText;     // Text component for the current step.

    [Header("Arrow (Panel 1)")]
    [SerializeField] private RectTransform arrow;       // Arrow that points to the target object (optional).

    [Header("Got It Button (Panel 1)")]
    [SerializeField] private RectTransform gotItButton; // "Got It" button for Panel 1 (optional but recommended).

    [Header("Tutorial UI (Panel 2)")]
    [SerializeField] private GameObject panel2;
    [SerializeField] private TMP_Text tutorialText2;

    [Header("Arrow (Panel 2)")]
    [SerializeField] private RectTransform arrow2;

    [Header("Got It Button (Panel 2)")]
    [SerializeField] private RectTransform gotItButton2; // "Got It" button for Panel 2 (optional but recommended).

    [Header("Got It Position (per panel)")]
    [SerializeField] private bool overrideGotItPosition = false;
    [SerializeField] private Vector2 gotItAnchoredPosPanel1 = Vector2.zero;
    [SerializeField] private Vector2 gotItAnchoredPosPanel2 = Vector2.zero;

    [Header("Per-scene guides")]
    [SerializeField] private SceneGuide[] guides;       // All guides for all scenes.

    [Header("Scene UI root")]
    [SerializeField] private string sceneUiRootName = "UI";
    // Name of the root GameObject that holds the scene's UI.
    // This object can be disabled per-step and re-enabled afterwards.
    [Header("Scene WORLD root")]
    [SerializeField] private string worldRootName = "World"; 

    private GameObject currentWorldRoot;
    // Runtime state
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

    public enum PanelChoice { Panel1, Panel2 }

    private GameObject activePanel;
    private TMP_Text activeText;
    private RectTransform activeArrow;
    private RectTransform activeGotItButton;

    // Auto-next state (timer/flag)
    private Coroutine autoAdvanceRoutine = null;
    private readonly HashSet<string> tutorialFlags = new HashSet<string>();

    /// <summary>
    /// Defines how the tutorial should advance from this step to the next one.
    /// </summary>
    public enum NextMode
    {
        OnClick,        // Next happens only when the user clicks "Got It"
        OnTime,         // Next happens after a delay (seconds)
        OnFlag,         // Next happens when a flag is set
        OnFlagThenTime  // Next happens when a flag is set, THEN after a delay
    }

    /// <summary>
    /// Configuration for a single scene's tutorial.
    /// Each scene can have multiple steps.
    /// </summary>
    [Serializable]
    public class SceneGuide
    {
        /// <summary>
        /// Name of the scene this guide belongs to.
        /// Must match the scene name in Build Settings.
        /// </summary>
        public string sceneName;

        /// <summary>
        /// Choose which tutorial panel to use in this scene (Panel 1 or Panel 2).
        /// NOTE: This is a DEFAULT. Each Step can override it (see Step.overridePanelChoice).
        /// </summary>
        public PanelChoice panelChoice = PanelChoice.Panel1;

        /// <summary>
        /// Ordered steps that will be shown in this scene's tutorial.
        /// </summary>
        public Step[] steps;
    }

    /// <summary>
    /// A single tutorial step:
    /// - message shown to the player,
    /// - optional target object name,
    /// - optional arrow offset,
    /// - optional flag to hide the scene UI for this step.
    /// </summary>
    [Serializable]
    public class Step
    {
        [Header("Per-step UI visibility (relative to Scene UI root)")]
        public List<string> hideUiPaths = new List<string>(); // מה להסתיר בצעד הזה
        public List<string> showUiPaths = new List<string>(); // אופציונלי: מה להכריח להציג בצעד הזה
        [Header("Per-step WORLD visibility (relative to World root)")]
        public List<string> hideWorldPaths = new List<string>();
        public List<string> showWorldPaths = new List<string>();


        /// <summary>
        /// Text to display for this step.
        /// </summary>
        [TextArea(3, 8)]
        public string message;

        /// <summary>
        /// Name of the target object in the scene hierarchy
        /// that the arrow should point to.
        /// If empty or null, the arrow will be hidden.
        /// </summary>
        public string targetObjectName;

        /// <summary>
        /// Additional offset applied to the arrow's position.
        /// Useful to adjust the arrow so it does not overlap the target.
        /// </summary>
        public Vector2 arrowOffset;

        /// <summary>
        /// If true, the scene UI root will be hidden for this step.
        /// If false, the scene UI will stay visible.
        /// </summary>
        public bool hideSceneUI = false;

        /// <summary>
        /// If true, this step will override the scene's default panel choice.
        /// If false, this step will use SceneGuide.panelChoice.
        /// </summary>
        public bool overridePanelChoice = false;

        /// <summary>
        /// Choose which tutorial panel to use in this step (Panel 1 or Panel 2).
        /// Only used when overridePanelChoice = true.
        /// </summary>
        public PanelChoice panelChoice = PanelChoice.Panel1;

        /// <summary>
        /// Controls how this step advances to the next one:
        /// - OnClick: waits for button click
        /// - OnTime: waits for secondsToAutoNext
        /// - OnFlag: waits for requiredFlagName
        /// - OnFlagThenTime: waits for requiredFlagName, then waits secondsToAutoNext
        /// </summary>
        public NextMode nextMode = NextMode.OnClick;

        /// <summary>
        /// Seconds to wait before auto-advancing (used by OnTime and OnFlagThenTime).
        /// </summary>
        public float secondsToAutoNext = 2f;

        /// <summary>
        /// Flag name to wait for (used by OnFlag and OnFlagThenTime).
        /// Another script should call TutorialManager.Instance.SetFlag("FLAG_NAME");
        /// </summary>
        public string requiredFlagName = "";
        /// <summary>
        /// If true, player can move with arrow keys during THIS tutorial step.
        /// </summary>
        public bool allowMovementDuringThisStep = false;
    }

    #region Unity lifecycle

    private void Awake()
    {
        // Ensure there is only one TutorialManager instance.
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Make sure the tutorial panel starts hidden.
        if (panel != null)
            panel.SetActive(false);

        if (panel2 != null)
            panel2.SetActive(false);

        // Make sure the arrow starts hidden.
        if (arrow != null)
            arrow.gameObject.SetActive(false);

        if (arrow2 != null)
            arrow2.gameObject.SetActive(false);

        // Make sure the "Got It" button starts hidden (both panels).
        if (gotItButton != null)
            gotItButton.gameObject.SetActive(false);

        if (gotItButton2 != null)
            gotItButton2.gameObject.SetActive(false);

        // Clear active references at start.
        activePanel = null;
        activeText = null;
        activeArrow = null;
        activeGotItButton = null;
    }

    private void OnEnable()
    {
        // Subscribe to the sceneLoaded event to know when a new scene is loaded.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe from the sceneLoaded event to avoid memory leaks.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region Scene handling

    /// <summary>
    /// Called automatically whenever a new scene is loaded.
    /// Decides whether to show the tutorial for that scene.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;

        StopAutoAdvance();

        // Never show tutorials on the main menu.
        if (scene.name == "MainMenu")
        {
            if (panel != null)
                panel.SetActive(false);

            if (panel2 != null)
                panel2.SetActive(false);

            if (arrow != null)
                arrow.gameObject.SetActive(false);

            if (arrow2 != null)
                arrow2.gameObject.SetActive(false);

            if (gotItButton != null)
                gotItButton.gameObject.SetActive(false);

            if (gotItButton2 != null)
                gotItButton2.gameObject.SetActive(false);

            currentSceneUiRoot = null;
            currentGuide = null;
            currentWorldRoot = null;
            currentStepIndex = 0;
            return;
        }

        TryShowGuideForScene(scene.name);
    }

    /// <summary>
    /// Attempts to show a tutorial for the given scene name,
    /// if there is a guide configured and it was not completed yet.
    /// </summary>
    private void TryShowGuideForScene(string sceneName)
    {
        if (GameManager.Instance == null || GameManager.Instance.Data == null)
            return;

        var data = GameManager.Instance.Data;

        // If all tutorials are completed, do nothing.
        if (data.tutorialCompleted)
            return;

        // Find the guide for this scene.
        currentGuide = Array.Find(guides, g => g.sceneName == sceneName);
        if (currentGuide == null || currentGuide.steps == null || currentGuide.steps.Length == 0)
            return;

        // If this specific scene guide was already completed, do nothing.
        if (IsSceneGuideAlreadyDone(sceneName, data))
            return;

        // Find the scene UI root (do not disable yet; that is per-step).
        currentSceneUiRoot = GameObject.Find(sceneUiRootName);
        currentWorldRoot = GameObject.Find(worldRootName);

        // Start from the first step.
        currentStepIndex = 0;

        // Pick the correct UI panel for this guide's first step (Step can override scene default).
        SelectPanel(GetEffectivePanelChoiceForStep(currentGuide.steps[currentStepIndex]));

        // Show the tutorial UI for this guide.
        if (activePanel != null)
            activePanel.SetActive(true);

        ShowCurrentStep();
    }

    #endregion

    #region Step display

    /// <summary>
    /// Updates the tutorial text, scene UI visibility and arrow
    /// based on the current step.
    /// </summary>
    ///
    private float faded = 0.2f;
    private float originalAlpha = 1f;

    /// <summary>
    /// Returns which panel should be used for the given Step:
    /// - If step.overridePanelChoice == true => uses step.panelChoice
    /// - Else => uses currentGuide.panelChoice (scene default)
    /// </summary>
    private PanelChoice GetEffectivePanelChoiceForStep(Step step)
    {
        if (currentGuide == null || step == null)
            return PanelChoice.Panel1;

        return step.overridePanelChoice ? step.panelChoice : currentGuide.panelChoice;
    }

    /// <summary>
    /// Select which tutorial panel is active (Panel 1 / Panel 2).
    /// This sets activePanel / activeText / activeArrow / activeGotItButton.
    /// </summary>
    private void SelectPanel(PanelChoice choice)
    {
        // Hide both panels first.
        if (panel != null) panel.SetActive(false);
        if (panel2 != null) panel2.SetActive(false);

        // Hide both arrows first.
        if (arrow != null) arrow.gameObject.SetActive(false);
        if (arrow2 != null) arrow2.gameObject.SetActive(false);

        // Hide both "Got It" buttons first.
        if (gotItButton != null) gotItButton.gameObject.SetActive(false);
        if (gotItButton2 != null) gotItButton2.gameObject.SetActive(false);

        // Decide which set to activate. If Panel2 is not assigned, fallback to Panel1.
        bool usePanel2 = (choice == PanelChoice.Panel2) && panel2 != null && tutorialText2 != null;

        activePanel = usePanel2 ? panel2 : panel;
        activeText = usePanel2 ? tutorialText2 : tutorialText;
        activeArrow = usePanel2 ? arrow2 : arrow;
        activeGotItButton = usePanel2 ? gotItButton2 : gotItButton;

        // Optional: set "Got It" position per panel.
        if (overrideGotItPosition && activeGotItButton != null)
        {
            activeGotItButton.anchoredPosition = usePanel2 ? gotItAnchoredPosPanel2 : gotItAnchoredPosPanel1;
        }
    }

    private void ShowCurrentStep()
    {

        StopAutoAdvance();
        RestorePerStepVisibility();

        if (currentGuide == null || currentGuide.steps == null || currentGuide.steps.Length == 0)
            return;

        if (currentStepIndex < 0 || currentStepIndex >= currentGuide.steps.Length)
        {
            return;
        }

        Step step = currentGuide.steps[currentStepIndex];
        CachePlayer();
        if (playerMover != null)
            playerMover.SetAllowMoveDuringTutorial(step.allowMovementDuringThisStep);


        // Pick the correct UI panel for THIS step (Step can override scene default).
        SelectPanel(GetEffectivePanelChoiceForStep(step));

        if (activeText == null)
            return;

        // Make sure the active tutorial panel is visible.
        if (activePanel != null)
            activePanel.SetActive(true);

        // Button visibility depends on nextMode:
        // - OnClick => show "Got It"
        // - otherwise => hide "Got It"
        if (activeGotItButton != null)
            activeGotItButton.gameObject.SetActive(step.nextMode == NextMode.OnClick);

        // 1) Show or hide scene UI according to this step
        if (currentSceneUiRoot != null)
            currentSceneUiRoot.SetActive(!step.hideSceneUI);

        if (!step.hideSceneUI)
        {
            var fader = currentSceneUiRoot.GetComponent<UITransparencyGroup>();
            fader?.RestoreAlpha(faded);
        }
        else
        {
            var fader = currentSceneUiRoot?.GetComponent<UITransparencyGroup>();
            fader?.RestoreAlpha(originalAlpha);
        }

        // 2) Update text
        activeText.text = step.message;

        // 3) Update arrow
        if (activeArrow != null)
        {
            if (!string.IsNullOrEmpty(step.targetObjectName))
            {
                GameObject target = GameObject.Find(step.targetObjectName);
                if (target != null)
                {
                    // Try treating the target as UI (RectTransform).
                    RectTransform targetRect = target.GetComponent<RectTransform>();
                    if (targetRect != null)
                    {
                        activeArrow.gameObject.SetActive(true);
                        activeArrow.position = targetRect.position + (Vector3)step.arrowOffset;
                    }
                    else
                    {
                        // Treat the target as a world object and convert to screen position.
                        Vector3 worldPos = target.transform.position;
                        if (Camera.main != null)
                        {
                            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                            activeArrow.gameObject.SetActive(true);
                            activeArrow.position = screenPos + (Vector3)step.arrowOffset;
                        }
                        else
                        {
                            activeArrow.gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    // Target not found.
                    activeArrow.gameObject.SetActive(false);
                }
            }
            else
            {
                // No target for this step.
                activeArrow.gameObject.SetActive(false);
            }
        }
        if (!step.hideSceneUI)
        {
            ApplyPerStepVisibility(step);
        }
        ApplyPerStepWorldVisibility(step);



        // 4) Auto-advance logic (time / flag / flag->time)
        StartAutoAdvanceForStep(step);
    }

    #endregion

    #region Auto advance (time / flag)

    /// <summary>
    /// External scripts can turn on tutorial flags.
    /// Example: TutorialManager.Instance.SetFlag("PLANTED_FIRST_SEED");
    /// </summary>
    public void SetFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName))
            return;

        tutorialFlags.Add(flagName);
    }

    /// <summary>
    /// Optional: clear a specific flag.
    /// </summary>
    public void ClearFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName))
            return;

        tutorialFlags.Remove(flagName);
    }

    /// <summary>
    /// Optional: clear all flags.
    /// </summary>
    public void ClearAllFlags()
    {
        tutorialFlags.Clear();
    }

    private bool IsFlagSet(string flagName)
    {
        if (string.IsNullOrEmpty(flagName))
            return false;

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
        if (step == null)
            return;

        // Snapshot for safety: if step changes while waiting, we don't auto-advance the wrong step.
        int stepSnapshot = currentStepIndex;
        SceneGuide guideSnapshot = currentGuide;

        // Fallback behavior if data is missing
        if (step.nextMode == NextMode.OnFlag || step.nextMode == NextMode.OnFlagThenTime)
        {
            if (string.IsNullOrEmpty(step.requiredFlagName))
            {
                // No flag name => cannot wait. Do nothing (behaves like OnClick without button).
                return;
            }
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

        // If we changed guide/step during wait, do nothing.
        if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
            yield break;

        OnNextStep();
    }

    private IEnumerator AutoAdvanceWhenFlag(string flagName, SceneGuide guideSnapshot, int stepSnapshot)
    {
        while (!IsFlagSet(flagName))
        {
            // If we changed guide/step during wait, do nothing.
            if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
                yield break;

            yield return null;
        }

        // Final safety check
        if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
            yield break;

        OnNextStep();
    }

    private IEnumerator AutoAdvanceFlagThenTime(string flagName, float seconds, SceneGuide guideSnapshot, int stepSnapshot)
    {
        // 1) Wait for flag
        while (!IsFlagSet(flagName))
        {
            if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
                yield break;

            yield return null;
        }

        // 2) Only AFTER flag is set, start the timer
        if (seconds < 0f) seconds = 0f;

        float t = 0f;
        while (t < seconds)
        {
            if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
                yield break;

            t += Time.deltaTime;
            yield return null;
        }

        // Final safety check
        if (currentGuide != guideSnapshot || currentStepIndex != stepSnapshot)
            yield break;

        OnNextStep();
    }

    #endregion

    #region Completion flags

    /// <summary>
    /// Checks if the guide for the given scene was already completed,
    /// using flags stored in GameData.
    /// </summary>
    private bool IsSceneGuideAlreadyDone(string sceneName, GameData data)
    {
        switch (sceneName)
        {
            case "SampleScene": return data.sampleSceneGuideDone;
            case "WorldMap": return data.worldMapGuideDone;
            case "Cellar": return data.cellarGuideDone;
            default: return false;
        }
    }

    /// <summary>
    /// Marks the guide for the given scene as completed in GameData.
    /// </summary>
    private void MarkSceneGuideDone(string sceneName, GameData data)
    {
        switch (sceneName)
        {
            case "SampleScene":
                data.sampleSceneGuideDone = true;
                Debug.Log("Ended SampleScene");
                break;
            case "WorldMap":
                data.worldMapGuideDone = true;
                Debug.Log("Ended World Map Scene");
                break;
            case "Cellar":
                data.cellarGuideDone = true;
                Debug.Log("Ended CellarGuide");
                break;
        }
    }

    #endregion

    #region Public API (button callback)

    /// <summary>
    /// Called from the tutorial "Next" button.
    /// Advances to the next step, or closes the tutorial
    /// if this was the last step.
    /// </summary>
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
            EnemyStateMachine.EndTutorial();
            Debug.Log("Executing enemy patrol");
        }
        else
        {
            ShowCurrentStep();
        }
    }


    #endregion

    #region Closing & cleanup

    /// <summary>
    /// Closes the tutorial panel, restores the scene UI,
    /// and updates GameData with completion flags.
    /// </summary>
    private void CloseGuide()
    {
        RestorePerStepVisibility();

        StopAutoAdvance();

        if (GameManager.Instance != null && GameManager.Instance.Data != null)
        {
            var data = GameManager.Instance.Data;

            // Mark this scene's guide as completed.
            MarkSceneGuideDone(currentSceneName, data);

            // Check if all scene guides are done.
            bool allDone =
                data.sampleSceneGuideDone &&
                data.worldMapGuideDone &&
                data.cellarGuideDone;

            if (allDone)
                data.tutorialCompleted = true;

            // Save the updated data.
            GameManager.Instance.SaveGame();
        }

        // Ensure the scene UI is visible when tutorial closes.
        if (currentSceneUiRoot != null)
            currentSceneUiRoot.SetActive(true);

        // Hide both tutorial panels.
        if (panel != null)
            panel.SetActive(false);

        if (panel2 != null)
            panel2.SetActive(false);

        // Hide both arrows.
        if (arrow != null)
            arrow.gameObject.SetActive(false);

        if (arrow2 != null)
            arrow2.gameObject.SetActive(false);

        // Hide both "Got It" buttons.
        if (gotItButton != null)
            gotItButton.gameObject.SetActive(false);

        if (gotItButton2 != null)
            gotItButton2.gameObject.SetActive(false);

        // Reset runtime state.
        currentGuide = null;
        currentStepIndex = 0;
        CachePlayer();
        if (playerMover != null)
            playerMover.SetAllowMoveDuringTutorial(false);
        GrandpaStoppedTalking?.Invoke();
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

        // קודם show ואז hide כדי שיהיה עקבי
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




    #endregion
}
