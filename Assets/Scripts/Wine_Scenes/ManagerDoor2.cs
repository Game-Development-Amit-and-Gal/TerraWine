using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ManagerDoor2 : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private string sceneName = "Manager_Office";
    [SerializeField] private Vector2 playerSpawnPosition;

    [Header("Tutorial Flag")]
    [Tooltip("Tutorial flag to set when the player enters this door (leave empty to disable).")]
    [SerializeField] private string tutorialFlag = "Basement";

    [Header("Aura")]
    public SpriteRenderer auraRenderer;
    public float pulseSpeed = 4.0f;
    public float maxAlpha = 0.7f;
    public float offset = 1.0f;

    [Header("Press F World Anchor")]
    public Transform pressFAnchor; // child Empty בשם PressF_Anchor מעל הדלת

    private bool playerInRange = false;

    private void Awake()
    {
        if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange) return;

        // Aura pulse
        if (auraRenderer != null)
        {
            float pulse = (((Mathf.Sin(Time.time * pulseSpeed)) / 2f) + offset) * maxAlpha;
            var c = auraRenderer.color;
            c.a = pulse;
            auraRenderer.color = c;
        }

        // Input
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            EnterOffice();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (auraRenderer != null) auraRenderer.gameObject.SetActive(true);

        PressFPrompt.Instance?.Show(pressFAnchor != null ? pressFAnchor : transform);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);

        PressFPrompt.Instance?.Hide();
    }

    private void EnterOffice()
    {
        // Hide prompt before transition
        PressFPrompt.Instance?.Hide();

        // Tutorial flag
        if (string.IsNullOrWhiteSpace(tutorialFlag))
        {
            Debug.LogWarning("[Door] tutorialFlag is empty");
        }
        else if (TutorialManager.Instance == null)
        {
            Debug.LogWarning($"[Door] TutorialManager.Instance is NULL, can't set flag '{tutorialFlag}'");
        }
        else
        {
            Debug.Log($"[Door] Setting tutorial flag: '{tutorialFlag}'");
            TutorialManager.Instance.SetFlag(tutorialFlag);
        }

        // Scene transition
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeScene(sceneName, playerSpawnPosition);
        else
            SceneManager.LoadScene(sceneName);
    }
}
