using UnityEngine;
using System.Collections.Generic;

// 1. THIS ATTRIBUTE IS CRITICAL: It creates the "TerraWine > Judge" menu option.
[CreateAssetMenu(fileName = "NewJudge", menuName = "TerraWine/Judge", order = 1)]
public class JudgeSO : ScriptableObject
{
    [Header("Judge Identity")]
    public string judgeName = "Gordon";
    public Sprite portrait;

    [Header("Runtime Data (Do not edit in inspector)")]
    // We store the randomized preferences here at runtime
    // Key = Wine ID, Value = The Bonus Score (e.g., +10)
    private Dictionary<string, int> currentPreferences = new Dictionary<string, int>();

    /// <summary>
    /// Called by JudgeSystem at the start of the competition.
    /// It assigns random bonus scores to specific wines for this session.
    /// </summary>
    public void InitializePreferences(List<string> selectedWineIDs, int minBonus, int maxBonus)
    {
        // Clear old data so we don't keep preferences from the last game
        currentPreferences.Clear();

        foreach (string wineID in selectedWineIDs)
        {
            // Pick a random bonus (e.g., between 5 and 15)
            int randomBonus = Random.Range(minBonus, maxBonus + 1);

            // Add to dictionary
            currentPreferences[wineID] = randomBonus;

            Debug.Log($"Judge {judgeName} likes {wineID} (+{randomBonus} points)");
        }
    }

    /// <summary>
    /// Checks if the submitted wine is in the judge's "Loved List" and returns the bonus.
    /// </summary>
    public int GetBonusScore(string wineID)
    {
        if (currentPreferences.ContainsKey(wineID))
        {
            return currentPreferences[wineID];
        }
        return 0; // No bonus if they don't care about this wine
    }
}