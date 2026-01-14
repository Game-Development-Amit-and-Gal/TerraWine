using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EnterWineryDoor2 : MonoBehaviour
{
    [Header("Scene Transition")]
    public string sceneName = "WineryReception";
    public Vector2 playerSpawnPosition;

    [Header("Aura (optional)")]
    public SpriteRenderer auraRenderer;
    public float pulseSpeed = 4.0f;
    public float maxAlpha = 0.7f;
    public float offset = 1.0f;

    [Header("Press F World Anchor")]
    public Transform pressFAnchor;

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
            ExecuteWineryTransition();
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

    private void ExecuteWineryTransition()
    {
        Debug.Log($"[Door] Transitioning to {sceneName}...");

        PressFPrompt.Instance?.Hide();
        TutorialManager.Instance?.SetFlag("Winery");

        if (GameManager.Instance != null)
            GameManager.Instance.ChangeScene(sceneName, playerSpawnPosition);
        else
            SceneManager.LoadScene(sceneName);
    }
}
