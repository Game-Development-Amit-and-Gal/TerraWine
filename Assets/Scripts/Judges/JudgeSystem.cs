using UnityEngine;
using System.Collections.Generic;
using System; // Fixed: Added the missing semicolon

public class JudgeSystem : MonoBehaviour
{
    [Header("Configurations")]
    [Tooltip("Drag your Judge Assets (JudgeSO) here")]
    [SerializeField] private List<JudgeSO> activeJudges;

    [Tooltip("Master list of all wine ItemSOs available (Drag from Project folder)")]
    [SerializeField] private List<ItemSO> masterWineIDList;

    [Header("Balancing")]
    [SerializeField] private int preferenceCountPerJudge = 3; // How many wines each judge likes
    [SerializeField] private int minPreferenceBonus = 5;      // Minimum extra points
    [SerializeField] private int maxPreferenceBonus = 15;     // Maximum extra points

    [Tooltip("Score range for random opponents (e.g., 50 to 100)")]
    public Vector2Int opponentsRange = new Vector2Int(50, 100);

    // Fixed: Corrected spelling from 'Compeitiotion' to 'Competition'
    public static event Action<bool, int, int> OnCompetitionFinished;

    private void Start()
    {
        GenerateJudgePreference();
    }

    /// <summary>
    /// Randomly decides what each judge likes for this specific competition.
    /// </summary>
    public void GenerateJudgePreference()
    {
        if (masterWineIDList == null || masterWineIDList.Count == 0)
        {
            Debug.LogError("JudgeSystem: Master Wine ID List is empty! Drag items in Inspector.");
            return;
        }

        foreach (var judge in activeJudges)
        {
            // Create a temporary copy of the list so we can remove items as we pick them
            // (This prevents picking the same wine twice for one judge)
            List<ItemSO> tempWineList = new List<ItemSO>(masterWineIDList);
            List<string> selectedWineIDs = new List<string>();

            for (int i = 0; i < preferenceCountPerJudge; i++)
            {
                if (tempWineList.Count == 0) break; // Stop if we run out of wines

                int randomIndex = UnityEngine.Random.Range(0, tempWineList.Count);

                // Add the ID to the judge's list
                selectedWineIDs.Add(tempWineList[randomIndex].id); // Ensure ItemSO has public string 'id'

                // Remove from temp list so it's not picked again
                tempWineList.RemoveAt(randomIndex);
            }

            // Send the data to the Judge Asset
            judge.InitializePreferences(selectedWineIDs, minPreferenceBonus, maxPreferenceBonus);
        }
    }

    /// <summary>
    /// Call this when the player submits a bottle.
    /// </summary>
    public void EvaluateSubmission(ItemSO submittedWine, int baseQualityScore)
    {
        if (submittedWine == null)
        {
            Debug.LogWarning("JudgeSystem: No wine submitted!");
            return;
        }

        string wineID = submittedWine.id;
        int totalScore = baseQualityScore;

        // Loop through all judges and ask for their bonus score
        foreach (var judge in activeJudges)
        {
            // Fixed: Matches the method name in JudgeSO.cs
            int bonus = judge.GetBonusScore(wineID);
            totalScore += bonus;

            if (bonus > 0) Debug.Log($"Judge {judge.judgeName} awarded bonus: +{bonus}");
        }

        DetermineWinner(totalScore);
    }

    private void DetermineWinner(int playerScore)
    {
        // 1. Generate random scores for 3 opponents
        int opponent1 = UnityEngine.Random.Range(opponentsRange.x, opponentsRange.y);
        int opponent2 = UnityEngine.Random.Range(opponentsRange.x, opponentsRange.y);
        int opponent3 = UnityEngine.Random.Range(opponentsRange.x, opponentsRange.y);

        // 2. Find the highest score to beat
        int highestOpponent = Mathf.Max(opponent1, Mathf.Max(opponent2, opponent3));

        // 3. Win Condition: Player needs to tie or beat the best opponent
        bool playerWins = (playerScore >= highestOpponent);

        Debug.Log($"RESULT: Player ({playerScore}) vs Best Opponent ({highestOpponent}) -> Win? {playerWins}");

        // 4. Notify the UI (or Game Manager)
        OnCompetitionFinished?.Invoke(playerWins, playerScore, highestOpponent);
    }
}