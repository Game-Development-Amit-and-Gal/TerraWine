using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TutorialLevels : MonoBehaviour
{

    [SerializeField] private RectTransform arrowRectTransform;
    [SerializeField] private GameObject arrow;
    [SerializeField] private Canvas arrowCanvas;
    [SerializeField] private TMPro.TextMeshProUGUI tutorialText;
    [SerializeField] private GameObject textImage;
    [SerializeField] private RectTransform bagIcon; // UI element of the bag
    [SerializeField] private GameObject gardenIcon;
    [SerializeField] private Vector2 arrowOffset = new Vector2(40f, 40f);

    private bool hasSeed = false;
    private bool collectedCrop = false;
    private float secondsForCropFinishes = 0;
    private float upperSecondsBound = 10;
    private bool hideUI = false;
    private bool showUI = true;


    public enum GardenTutorialStep
    {
        None,
        OpenBag,
        PlantSeed,
        waitForCrop,
        viewBag,
        TruckSells,
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
        arrow.SetActive(hideUI);
        SetActiveImageAndText(hideUI);
        
    }



    // Update is called once per frame
    void Update()
    {
        switch (currentStep)
        {
            case GardenTutorialStep.OpenBag:
                ShowOpenBagStep();
                break;

            case GardenTutorialStep.PlantSeed:
                ShowPlantSeedStep();
                break;

            case GardenTutorialStep.waitForCrop:
                WaitForTheCrop();
                break;

            case GardenTutorialStep.EnterHouse:
                EnterHouse();
                break;

            case GardenTutorialStep.viewBag:
                ViewBag();
                break;

            case GardenTutorialStep.TruckSells:
                break;

            default:
                break;
        }
    }


    public void OnBagOpened()
    {
        if (currentStep != GardenTutorialStep.OpenBag) return;
        InventoryManager.openedBagGardenTutorial = true;
        tutorialText.text = ""; // Reset the string in order to load new text.
        arrow.SetActive(hideUI);

        currentStep = GardenTutorialStep.PlantSeed;
        ShowPlantSeedStep();
    }

    private void ShowPlantSeedStep()
    {
        if (currentStep != GardenTutorialStep.PlantSeed) return;
        hasSeed = true;


        SetActiveImageAndText(showUI);
        tutorialText.text = "Nice! Head to your " +
            "               Garden And plant " +
            "               the seed using the left button of your mouse";

        bool isInsidePressed = CursorIsPressedInsideGarden();

        

        if (isInsidePressed && hasSeed) 
        {
            tutorialText.text = "Perfect!\nNow wait for the crop to grow.\n If you stand near the garden you will see the countdown\n";
            currentStep = GardenTutorialStep.waitForCrop;
            return;
        } 


    }

    private bool CursorIsPressedInsideGarden()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x,mouseScreen.y, Camera.main.nearClipPlane) );

        CircleCollider2D gardenCollider2D = gardenIcon.GetComponentInParent<CircleCollider2D>();
        bool isInside = gardenCollider2D.OverlapPoint(mouseWorld);

        return isInside && Mouse.current.leftButton.wasPressedThisFrame;
    }



    void ViewBag()
    {
        tutorialText.text = "Check your backbag you to see Your crop";
        arrow.SetActive(showUI);
        if (InventoryManager.openedBagGardenTutorial)
        {
            arrow.SetActive(hideUI);
            SetActiveImageAndText(hideUI);
            EndTutorial();
        }
    }

    void WaitForTheCrop()
    {
        if (secondsForCropFinishes >= upperSecondsBound)
        {
            tutorialText.text = "Fabulous!!\nYour Crop is ready Go ahead and collect it.\nYou will be able to see you new crop in your bag.\n";
            bool isInsideAndPressed = CursorIsPressedInsideGarden();

            if (isInsideAndPressed)
            {
                currentStep = GardenTutorialStep.viewBag;
            }
        }
        else secondsForCropFinishes += Time.deltaTime;
       
    }
    void EndTutorial()
    {
        bool StopRunning = false;
        TutorialManager.tutorialIsRunningGardenScene = StopRunning;
        currentStep = GardenTutorialStep.Done;
        enabled = false;
    }

    void EnterHouse()
    {
        return;
    }

    void ShowOpenBagStep()
    {

        SetActiveImageAndText(showUI);
        tutorialText.text = "Fetch some seeds from your bag";

        arrow.SetActive(showUI);
        arrowRectTransform.position = bagIcon.position - (Vector3)arrowOffset;
        if (InventoryManager.openedBagGardenTutorial)
        {
           
            arrow.SetActive(hideUI);
            OnBagOpened();
            
        }
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
        arrow.SetActive(showUI);
    }





    void SetActiveImageAndText(bool active)
    {
        textImage.SetActive(active);
        textImage.SetActive(active);
    }





}
