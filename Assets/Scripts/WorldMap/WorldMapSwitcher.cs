using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class WorldMapVisualController : MonoBehaviour
{
    [Header("Dependencies")]
    private SeasonManager seasonManager;
    [SerializeField] private SpriteRenderer mapImage;

    // Drag your "Competition_Hotspot" (the invisible collider object) here
    [SerializeField] private GameObject competition_object;

    [Header("Map Visuals")]
    public Sprite standardMapSprite;
    public Sprite competitionMapSprite;

    [Header("Configuration")]
    public string competitionSeasonName = "Winery";

    private bool isDebugCompetitionMode = false;

    private void Awake()
    {
        mapImage = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        seasonManager = FindAnyObjectByType<SeasonManager>();

        // Default to off if we haven't checked yet
        if (competition_object != null) competition_object.SetActive(false);

        UpdateMapVisuals();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            ToggleDebugMap();
        }
    }

    private void ToggleDebugMap()
    {
        isDebugCompetitionMode = !isDebugCompetitionMode;

        if (isDebugCompetitionMode)
        {
            // SWITCH TO COMPETITION
            mapImage.sprite = competitionMapSprite;
            Debug.Log("DEBUG: Swapped to COMPETITION Map");

            // FIX: Turn the object ON so the tooltip script can run!
            if (competition_object != null) competition_object.SetActive(true);
        }
        else
        {
            // SWITCH TO STANDARD
            mapImage.sprite = standardMapSprite;
            Debug.Log("DEBUG: Swapped to STANDARD Map");

            // FIX: Turn the object OFF
            if (competition_object != null) competition_object.SetActive(false);
        }
    }

    public void UpdateMapVisuals()
    {
        if (seasonManager == null) return;

        string currentSeason = seasonManager.GetCurrentSeason();

        if (currentSeason == competitionSeasonName)
        {
            // IT IS WINERY SEASON
            if (competitionMapSprite != null) mapImage.sprite = competitionMapSprite;
            isDebugCompetitionMode = true;

            // FIX: Turn Object ON
            if (competition_object != null) competition_object.SetActive(true);
        }
        else
        {
            // IT IS NORMAL SEASON
            if (standardMapSprite != null) mapImage.sprite = standardMapSprite;
            isDebugCompetitionMode = false;

            // FIX: Turn Object OFF
            if (competition_object != null) competition_object.SetActive(false);
        }
    }
}