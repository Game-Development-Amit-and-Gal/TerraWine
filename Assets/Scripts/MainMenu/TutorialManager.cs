using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

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

    [Header("Tutorial UI")]
    [SerializeField] private GameObject panel;          // Root panel of the tutorial UI.
    [SerializeField] private TMP_Text tutorialText;     // Text component for the current step.

    [Header("Arrow")]
    [SerializeField] private RectTransform arrow;       // Arrow that points to the target object (optional).

    [Header("Per-scene guides")]
    [SerializeField] private SceneGuide[] guides;       // All guides for all scenes.

    [Header("Scene UI root")]
    [SerializeField] private string sceneUiRootName = "UI";
    // Name of the root GameObject that holds the scene's UI.
    // This object can be disabled per-step and re-enabled afterwards.

    // Runtime state
    private GameObject currentSceneUiRoot;
    private string currentSceneName;
    private SceneGuide currentGuide;
    private int currentStepIndex = 0;

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
        DontDestroyOnLoad(gameObject);

        // Make sure the tutorial panel starts hidden.
        if (panel != null)
            panel.SetActive(false);

        // Make sure the arrow starts hidden.
        if (arrow != null)
            arrow.gameObject.SetActive(false);
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

        // Never show tutorials on the main menu.
        if (scene.name == "MainMenu")
        {
            if (panel != null)
                panel.SetActive(false);

            if (arrow != null)
                arrow.gameObject.SetActive(false);

            currentSceneUiRoot = null;
            currentGuide = null;
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

        // Start from the first step.
        currentStepIndex = 0;

        if (panel != null)
            panel.SetActive(true);

        ShowCurrentStep();
    }

    #endregion

    #region Step display

    /// <summary>
    /// Updates the tutorial text, scene UI visibility and arrow
    /// based on the current step.
    /// </summary>
    private void ShowCurrentStep()
    {
        if (currentGuide == null || tutorialText == null)
            return;

        if (currentStepIndex < 0 || currentStepIndex >= currentGuide.steps.Length)
            return;

        Step step = currentGuide.steps[currentStepIndex];

        // 1) Show or hide scene UI according to this step
        if (currentSceneUiRoot != null)
            currentSceneUiRoot.SetActive(!step.hideSceneUI);

        // 2) Update text
        tutorialText.text = step.message;

        // 3) Update arrow
        if (arrow == null)
            return;

        if (!string.IsNullOrEmpty(step.targetObjectName))
        {
            GameObject target = GameObject.Find(step.targetObjectName);
            if (target != null)
            {
                // Try treating the target as UI (RectTransform).
                RectTransform targetRect = target.GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    arrow.gameObject.SetActive(true);
                    arrow.position = targetRect.position + (Vector3)step.arrowOffset;
                }
                else
                {
                    // Treat the target as a world object and convert to screen position.
                    Vector3 worldPos = target.transform.position;
                    if (Camera.main != null)
                    {
                        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                        arrow.gameObject.SetActive(true);
                        arrow.position = screenPos + (Vector3)step.arrowOffset;
                    }
                    else
                    {
                        arrow.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // Target not found.
                arrow.gameObject.SetActive(false);
            }
        }
        else
        {
            // No target for this step.
            arrow.gameObject.SetActive(false);
        }
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
                break;
            case "WorldMap":
                data.worldMapGuideDone = true;
                break;
            case "Cellar":
                data.cellarGuideDone = true;
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
        if (currentGuide == null)
        {
            CloseGuide();
            return;
        }

        currentStepIndex++;

        // If we reached or passed the last step, close the tutorial.
        if (currentStepIndex >= currentGuide.steps.Length)
        {
            CloseGuide();
        }
        else
        {
            // Otherwise, show the next step.
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

        // Hide the tutorial panel.
        if (panel != null)
            panel.SetActive(false);

        // Hide the arrow.
        if (arrow != null)
            arrow.gameObject.SetActive(false);

        // Reset runtime state.
        currentGuide = null;
        currentStepIndex = 0;
    }

    #endregion
}
