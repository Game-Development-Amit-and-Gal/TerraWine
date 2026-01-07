using UnityEngine;
using UnityEngine.InputSystem; // Required for Keyboard.current
using UnityEngine.SceneManagement;

/// <summary>
/// Senior Logic: Handles entering the winery with an aura effect and 'F' key requirement.
/// This version ensures the tutorial progresses only when the door is actually used.
/// </summary>
public class EnterWineryDoor2 : MonoBehaviour
{
    [Header("Scene Transition")]
    [Tooltip("The scene to load when entering (e.g., WineryReception).")]
    public string sceneName = "WineryReception";

    [Tooltip("The position where the Player will spawn inside the new scene.")]
    public Vector2 playerSpawnPosition;

    [Header("Visual Settings")]
    [Tooltip("Drag the child SpriteRenderer (the grey frame) here.")]
    public SpriteRenderer auraRenderer;

    [Header("Aura Animation")]
    public float pulseSpeed = 4.0f;
    public float maxAlpha = 0.7f;
    public float offset = 1.0f; // Baseline visibility

    private bool playerInRange = false;

    private void Awake()
    {
        // Ensure the aura is hidden on startup
        if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerInRange)
        {
            // 1. Aura Logic: Sine-wave pulse for visual feedback
            if (auraRenderer != null)
            {
                float pulse = (((Mathf.Sin(Time.time * pulseSpeed)) / 2f) + offset) * maxAlpha;
                Color c = auraRenderer.color;
                c.a = pulse;
                auraRenderer.color = c;
            }

            // 2. Interaction Logic: Wait for 'F' key press
            // Using the New Input System (Keyboard.current)
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                ExecuteWineryTransition();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (auraRenderer != null) auraRenderer.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Executes the actual logic. Call this only when the player presses F.
    /// </summary>
    private void ExecuteWineryTransition()
    {
        // Debug to track if the input is registered
        Debug.Log($"[Door] Transitioning to {sceneName}...");

        // Tutorial Fix: We only set the flag here.
        // This prevents the tutorial from skipping ahead while the player is still standing outside.
        TutorialManager.Instance?.SetFlag("Winery");

        // Use the GameManager architecture to handle the technical scene swap
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeScene(sceneName, playerSpawnPosition);
        }
        else
        {
            // Fallback if GameManager is missing
            SceneManager.LoadScene(sceneName);
        }
    }
}