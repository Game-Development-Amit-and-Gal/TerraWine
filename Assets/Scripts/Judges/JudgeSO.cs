using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a single Judge in the competition.
/// Stores static identity data and runtime dynamic preferences.
/// </summary>
[CreateAssetMenu(fileName = "NewJudge", menuName = "TerraWine/Judge")]
public class JudgeSO : ScriptableObject
{
    [Header("Identity")]
    public string judgeName;
    public Sprite portrait;

    [Header("Description")]
    [TextArea] public string bio;

    // Runtime Data: We don't save this in the asset, it is generated per competition
    // Dictionary mapping a Wine Name (ID) to a Score Bonus
    // We use [System.NonSerialized] so Unity doesn't try to save this dictionary between sessions
    [System.NonSerialized]
    public Dictionary<string, int> currentPreferences = new Dictionary<string, int>();

    /// <summary>
    /// Clears previous preferences and sets up new ones for the current competition.
    /// </summary>
    /// <param name="selectedPreferredWines">List of wine names this judge will like this round</param>
    /// <param name="minBonus">Minimum bonus points added</param>
    /// <param name="maxBonus">Maximum bonus points added</param>
    public void InitializePreferences(List<string> selectedPreferredWines, int minBonus, int maxBonus)
    {
        currentPreferences.Clear();

        foreach (var wineID in selectedPreferredWines)
        {
            // Assign a random bonus value for this specific wine
            int bonus = Random.Range(minBonus, maxBonus);

            // If the wineID already exists, we skip adding it again
            if (!currentPreferences.ContainsKey(wineID))
            {
                currentPreferences.Add(wineID, bonus);
            }
        }
    }

    /// <summary>
    /// Checks if the judge likes the wine and returns the bonus score.
    /// </summary>
    public int GetBonusScore(string wineID)
    {
        // Check if the wineID is in the judge's preferences

        if (currentPreferences.TryGetValue(wineID, out int bonus))
        {
            Debug.Log($"{judgeName} liked the {wineID}! Bonus: +{bonus}");
            return bonus;
        }
        return 0;
    }
}