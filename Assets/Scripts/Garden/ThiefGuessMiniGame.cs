using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThiefGuessMiniGame : MonoBehaviour
{
    private enum QuestionType
    {
        ApronColor,
        LeftHandYesNo,
        RightHandItem,
        ShirtPattern
    }

    [Header("Suspects UI")]
    [SerializeField] private Transform suspectsContainer;
    [SerializeField] private SuspectCardUI suspectCardPrefab;
    [SerializeField] private string resourcesFolder = "enemy"; // Assets/Resources/enemy

    [Header("Questions UI (4 buttons)")]
    [SerializeField] private Button[] questionButtons;          // size 4
    [SerializeField] private TMP_Text[] questionButtonLabels;   // size 4 (optional but recommended)

    [Header("Text UI")]
    [SerializeField] private TMP_Text infoText;     // shows instructions + results
    [SerializeField] private TMP_Text answersText;  // shows Q&A log (2 answers)

    [Header("Close UI")]
    [SerializeField] private Button closeButton;    // כפתור Close
    [SerializeField] private GameObject rootPanelToClose; // אם ריק -> יסגור את הגייםאובג'קט הזה

    private readonly List<EnemyItemSO> suspects = new();
    private readonly List<SuspectCardUI> cards = new();

    private EnemyItemSO thief;
    private int questionsAsked = 0;
    private bool gameEnded = false;

    private readonly QuestionType[] questionOrder =
    {
        QuestionType.ApronColor,
        QuestionType.LeftHandYesNo,
        QuestionType.RightHandItem,
        QuestionType.ShirtPattern
    };

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
            closeButton.interactable = false; // רק בסוף המשחק
        }
    }

    private void Start()
    {
        StartNewRound();
    }

    public void StartNewRound()
    {
        gameEnded = false;
        if (closeButton != null) closeButton.interactable = false;

        LoadSuspects();
        SpawnSuspectCards();
        PickRandomThief();
        SetupQuestionsUI();

        questionsAsked = 0;
        answersText.text = "";
        infoText.text = "Ask questions, then pick the thief (max 2).";
    }

    private void LoadSuspects()
    {
        suspects.Clear();

        EnemyItemSO[] loaded = Resources.LoadAll<EnemyItemSO>(resourcesFolder);

        foreach (var s in loaded)
        {
            var runtimeCopy = Instantiate(s);
            runtimeCopy.isCaught = false;
            suspects.Add(runtimeCopy);
        }
    }

    private void SpawnSuspectCards()
    {
        for (int i = suspectsContainer.childCount - 1; i >= 0; i--)
            Destroy(suspectsContainer.GetChild(i).gameObject);

        cards.Clear();

        foreach (var suspect in suspects)
        {
            var card = Instantiate(suspectCardPrefab, suspectsContainer);
            card.Bind(suspect, this);
            cards.Add(card);
        }
    }

    private void PickRandomThief()
    {
        if (suspects.Count == 0)
        {
            thief = null;
            infoText.text = "No suspects found in Resources/enemy.";
            return;
        }

        thief = suspects[Random.Range(0, suspects.Count)];
    }

    private void SetupQuestionsUI()
    {
        if (questionButtonLabels != null && questionButtonLabels.Length >= 4)
        {
            questionButtonLabels[0].text = "What apron color?";
            questionButtonLabels[1].text = "Left hand holding something?";
            questionButtonLabels[2].text = "Right hand item?";
            questionButtonLabels[3].text = "Shirt pattern?";
        }

        for (int i = 0; i < questionButtons.Length; i++)
        {
            int index = i;
            questionButtons[i].onClick.RemoveAllListeners();
            questionButtons[i].onClick.AddListener(() => OnQuestionChosen(index));
            questionButtons[i].interactable = true;
        }
    }

    private void OnQuestionChosen(int index)
    {
        if (gameEnded) return;
        if (thief == null) return;
        if (questionsAsked >= 2) return;
        if (index < 0 || index >= questionOrder.Length) return;

        if (!questionButtons[index].interactable) return;

        QuestionType q = questionOrder[index];
        string answer = GetAnswerForThief(q);

        questionsAsked++;
        questionButtons[index].interactable = false;

        answersText.text += $"A{questionsAsked}: {answer}\n\n";

        if (questionsAsked < 2)
            infoText.text = "You can guess now, or ask 1 more question.";
        else
            infoText.text = "Now pick the thief.";
    }

    public void OnSuspectChosen(EnemyItemSO chosen)
    {
        if (gameEnded) return;
        if (thief == null || chosen == null) return;

        if (questionsAsked < 1)
        {
            infoText.text = "Ask at least 1 question (or guess immediately).";
            return;
        }

        bool correct = (chosen.enemyName == thief.enemyName && chosen.wineryName == thief.wineryName);

        if (correct)
        {
            chosen.isCaught = true;
            infoText.text = "Correct! You caught the thief.";
        }
        else
        {
            infoText.text = $"Wrong! The thief was: {thief.enemyName} ({thief.wineryName}).";
        }

        foreach (var c in cards)
            c.RefreshPortrait();

        EndGame(); // <--- כאן נגמר המשחק
    }

    private void EndGame()
    {
        gameEnded = true;

        // אפשר לסגור רק עכשיו
        if (closeButton != null)
            closeButton.interactable = true;

        // אם את רוצה גם לנעול את המשחק:
        for (int i = 0; i < questionButtons.Length; i++)
            questionButtons[i].interactable = false;
    }

    private void OnCloseClicked()
    {
        if (!gameEnded) return;

        GameObject panel = rootPanelToClose != null ? rootPanelToClose : gameObject;
        panel.SetActive(false);
    }

    private string GetAnswerForThief(QuestionType q)
    {
        return q switch
        {
            QuestionType.ApronColor => $"Apron color: {thief.apronColor}.",
            QuestionType.LeftHandYesNo => $"Left hand holding something: {thief.leftHandItem}.",
            QuestionType.RightHandItem => $"Right hand item: {thief.rightHandItem}.",
            QuestionType.ShirtPattern => $"Shirt pattern: {thief.shirtPattern}.",
            _ => "No answer."
        };
    }
}
