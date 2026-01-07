using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ManagerDoor2 : MonoBehaviour
{
    [Header("Scene Transition")]
    [Tooltip("The scene to load when entering (e.g., basement).")]
    [SerializeField] private string sceneName = "Manager_Office";

    [Tooltip("Where the Player will spawn in the new scene.")]
    [SerializeField] private Vector2 playerSpawnPosition;

    [Header("Visual Prompt References")]
    [Tooltip("Drag the child SpriteRenderer (the aura) here.")]
    public SpriteRenderer auraRenderer;

    [Header("Aura Animation Settings")]
    public float pulseSpeed = 4.0f;
    public float maxAlpha = 0.7f;
    public float offset = 1.0f; // Controls the baseline visibility of the aura

    private bool playerInRange = false;

    private void Awake()
    {
        // Start with visuals hidden
        if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerInRange)
        {
            // 1. Aura Pulsing Logic
            if (auraRenderer != null)
            {
                // Calculate a smooth breathing pulse using Sine
                float pulse = (((Mathf.Sin(Time.time * pulseSpeed)) + offset) / 2f) * maxAlpha;
                Color c = auraRenderer.color;
                c.a = pulse;
                auraRenderer.color = c;
            }

            // 2. Interaction Logic
            // Checking Keyboard.current is safer for PC games using the New Input System
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                EnterBasement();
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

    private void EnterBasement()
    {
        Debug.Log($"Moving player to {sceneName}...");

        // Tutorial Logic: Only fires when the player actually enters
        TutorialManager.Instance?.SetFlag("Basement");

        // Use GameManager to handle the technical scene swap and spawn positioning
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeScene(sceneName, playerSpawnPosition);
        }
        else
        {
            Debug.LogError("BasementDoor: GameManager instance missing!");
            SceneManager.LoadScene(sceneName);
        }
    }
}
