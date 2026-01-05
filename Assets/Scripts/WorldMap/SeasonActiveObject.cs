using UnityEngine;
using UnityEngine.InputSystem;

public class SeasonActiveObject : MonoBehaviour
{
    [SerializeField] private string activeSeasonName = "Winery"; // Set to "Winery" or "Competition"
    private SeasonManager seasonManager;

    // Drag your WineryMapZone script here in the Inspector
    [SerializeField] private MonoBehaviour uiObject;

    private void Start() // Use Start, not Awake, to ensure Managers are loaded
    {
        seasonManager = FindAnyObjectByType<SeasonManager>();

        if (seasonManager == null)
        {
            Debug.LogWarning("SeasonActiveObject: No SeasonManager found!");
            return;
        }

        CheckSeason(); // UNCOMMENTED THIS so it runs on start
    }
    bool toggle = false;
    private void Update()
    {
        // Optional: Keep checking in case season changes while playing
        //CheckSeason();

        if(Keyboard.current.tKey.wasPressedThisFrame)
        {
            toggle = !toggle;
            uiObject.enabled = toggle;
           
        }
    }

    public void CheckSeason()
    {
        if (uiObject == null) return;

        // Check if we are in the target season
        bool isActiveSeason = (seasonManager.GetCurrentSeason() == activeSeasonName);

        // ENABLE the script if it matches, DISABLE (cancel) it if it doesn't
        uiObject.enabled = isActiveSeason;
    }
}