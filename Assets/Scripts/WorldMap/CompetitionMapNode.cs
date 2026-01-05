using UnityEngine;

/// <summary>
/// Logic for the World Map node using a Collider.
/// Triggers when the player clicks within the radius of the collider.
/// </summary>
[RequireComponent(typeof(Collider2D))] // Ensures the object has a collider for clicking
public class CompetitionMapNode : MonoBehaviour
{
    [Header("Dependencies")]
    private SeasonManager seasonManager;
    private SceneLoader sceneLoader;

    [Header("Settings")]
    public int competitionDay = 3;
    public string targetSeason = "Winery";
    public string competitionSceneName = "  ";

    [Header("Visuals")]
    [SerializeField] private GameObject visualMarker; // The icon/tents/trophy

    [Header("Spawn Configuration")]
    [SerializeField] private Vector2 defaultSpawnPosition = Vector2.zero;

    private void Start()
    {
        seasonManager = FindAnyObjectByType<SeasonManager>();
        sceneLoader = FindAnyObjectByType<SceneLoader>();

        UpdateVisualState();
    }

    /// <summary>
    /// Checks the game state. If it's not the right time, the collider and visuals turn off.
    /// </summary>
    public void UpdateVisualState()
    {
        if (seasonManager == null) return;

        bool isCorrectSeason = (seasonManager.GetCurrentSeason() == targetSeason);
        bool isCorrectDay = (seasonManager.GetCurrentDay() == competitionDay);

        bool shouldBeActive = isCorrectSeason && isCorrectDay;

        // Toggle Visuals
        if (visualMarker != null) visualMarker.SetActive(shouldBeActive);

        // Toggle Collider (If disabled, OnMouseDown won't fire)
        GetComponent<Collider2D>().enabled = shouldBeActive;
    }

    /// <summary>
    /// Unity calls this automatically when the user clicks the Collider.
    /// Note: This requires the Main Camera to have a PhysicsRaycaster (if UI) 
    /// or just a standard collider (if 2D world object).
    /// </summary>
    private void OnMouseDown()
    {
        ExecuteSceneLoad();
    }

    private void ExecuteSceneLoad()
    {
        // Final safety check
        if (seasonManager.GetCurrentSeason() == targetSeason &&
            seasonManager.GetCurrentDay() == competitionDay)
        {
            Debug.Log($"Loading Competition: {competitionSceneName}");
            if (sceneLoader != null)
            {
                sceneLoader.LoadSceneAndPlacePlayer(competitionSceneName, defaultSpawnPosition);
            }
            else
            {
                // Fallback if sceneLoader isn't found
                UnityEngine.SceneManagement.SceneManager.LoadScene(competitionSceneName);
            }
        }
    }
}