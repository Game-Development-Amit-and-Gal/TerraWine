using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System; // FIX 1: Added the semicolon here!

public class JudgeSystem : MonoBehaviour
{
    [Header("Configurations")]
    [Tooltip("The judges participating in this event")]
    // Ensure this matches your script name exactly (JudgeSO or JudgeOS)
    [SerializeField] private List<JudgeSO> activeJudges;

    [Tooltip("Master list of all the wine ItemSOs available to choose randomly")]
    // You changed this to ItemSO (Good move!), just make sure to drag the Item assets in the Inspector.
    [SerializeField] private List<ItemSO> masterWineIDList;

    [Header("Balancing")]
    [SerializeField] private int preferenceCountPerJudge = 3;
    [SerializeField] private int minPreferenceBonus = 5;
    [SerializeField] private int maxPreferenceBonus = 15;

    [Tooltip("Score range for random opponents scores")]
    public Vector2Int opponentsRange = new Vector2Int(50, 100);

    // FIX 2: Fixed spelling (Competition)
    public static event Action<bool, int, int> OnCompetitionFinished;

    public void Start()
    {
        GenerateJudgePreference();
    }

    public void GenerateJudgePreference()
    {
        if (masterWineIDList == null || masterWineIDList.Count == 0)
        {
            Debug.LogError("Master Wine ID List is empty or null!");
            return;
        }

        foreach (var judge in activeJudges)
        {
            List<ItemSO> randomWines = new List<ItemSO>(masterWineIDList);
            List<string> selectedWines = new List<string>();

            for (int i = 0; i < preferenceCountPerJudge; i++)
            {
                if (randomWines.Count == 0) break;

                int randomIndex = UnityEngine.Random.Range(0, randomWines.Count);

                // Ensure your ItemSO script actually has the public string 'id';
                selectedWines.Add(randomWines[randomIndex].id);
                randomWines.RemoveAt(randomIndex);
            }
            judge.InitializePreferences(selectedWines, minPreferenceBonus, maxPreferenceBonus);
        }
    }

    public void EvaluateSubmission(ItemSO submittedWine, int baseQualityScore)
    {
        if (submittedWine == null) return;

        string wineID = submittedWine.id;
        int totalScore = baseQualityScore;

        foreach (var judge in activeJudges)
        {
            // FIX 3: Changed 'GetBonusScore' to 'GetPreferenceBonus' 
            // to match your JudgeOS.cs script
            int bonus = judge.GetBonusScore(wineID);
            totalScore += bonus;
        }

        DetermineWinner(totalScore);
    }

    private void DetermineWinner(int playerScore)
    {
        // Opponent Logic
        int opponent1score = UnityEngine.Random.Range(opponentsRange.x, opponentsRange.y);
        int opponent2score = UnityEngine.Random.Range(opponentsRange.x, opponentsRange.y);
        int opponent3score = UnityEngine.Random.Range(opponentsRange.x, opponentsRange.y);

        // Check who has the highest
        int highestOpponent = Mathf.Max(opponent1score, Mathf.Max(opponent2score, opponent3score));

        // Player wins if they beat OR tie the highest opponent
        bool playerWins = (playerScore >= highestOpponent);

        Debug.Log($"Player: {playerScore} vs Best Opponent: {highestOpponent}");

        // Invoke the event for the UI to hear
        OnCompetitionFinished?.Invoke(playerWins, playerScore, highestOpponent);
    }
}
