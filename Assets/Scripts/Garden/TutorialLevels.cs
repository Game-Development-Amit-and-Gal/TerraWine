using System;
using UnityEngine;

public class TutorialLevels : MonoBehaviour
{

    [SerializeField] private GameObject arrow;
    [SerializeField] private TMPro.TextMeshProUGUI tutorialText;
    [SerializeField] private RectTransform bagIcon; // UI element of the bag

    public enum GardenTutorialStep
    {
        None,
        OpenBag,
        PlantSeed,
        EnterHouse,
        Done
    }

    private GardenTutorialStep currentStep;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (!TutorialManager.tutorialIsRunningGardenScene)
        {
            enabled = false; // Not Needed
            return;
        }
        arrow.SetActive(false);

    }



    // Update is called once per frame
    void Update()
    {
        if (!TutorialManager.tutorialIsRunningGardenScene)
            return;


        switch (currentStep)
        {
            case GardenTutorialStep.OpenBag:
                ShowOpenBagStep();
                break;

            case GardenTutorialStep.PlantSeed:
                // wait for event
                break;

            case GardenTutorialStep.EnterHouse:
                // wait for event
                break;
        }
    }


    public void OnBagOpened()
    {
        if (currentStep != GardenTutorialStep.OpenBag) return;

        currentStep = GardenTutorialStep.PlantSeed;
        ShowPlantSeedStep();
    }

    private void ShowPlantSeedStep()
    {
        throw new NotImplementedException();
    }

    void EndTutorial()
    {
        TutorialManager.tutorialIsRunningGardenScene = false;
        currentStep = GardenTutorialStep.Done;
        enabled = false;
    }

    void ShowOpenBagStep()
    {
        tutorialText.gameObject.SetActive(true);
        tutorialText.text = "Fetch some seeds from your bag";

        arrow.SetActive(true);
        arrow.transform.position = bagIcon.position;
    }

    void OnEnable()
    {
        TutorialManager.GrandpaStoppedTalking += StartGardenTutorial;
    }

    void OnDisable()
    {
        TutorialManager.GrandpaStoppedTalking -= StartGardenTutorial;
    }

    void StartGardenTutorial()
    {
        currentStep = GardenTutorialStep.OpenBag;
        arrow.SetActive(true);
    }





}
