using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro; // Essential for controlling TextMeshPro via code

/// <summary>
/// Senior logic: Handles proximity detection, pulsing aura visuals, 
/// a "Press F" floating text prompt, and scene transition.
/// </summary>
public class WineryDoorAura : MonoBehaviour
{
    [Header("Visual Settings")]
    public SpriteRenderer auraRenderer; // Drag the 'Aura_Visual' child here
    public float pulseSpeed = 4.0f;
    public float maxAlpha = 0.7f;
    public float offset = 1f;

    [Header("Scene Settings")]
    public string targetSceneName;

    private bool playerInRange = false;

    private void Awake()
    {
        // Hide both the aura and the text prompt on start
        if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerInRange)
        {
            // 1. Visual Pulse Logic
            if (auraRenderer != null)
            {
                // Pulse formula ensures alpha stays within a nice visible range
                float pulse = ((Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f) + offset;
                Color c = auraRenderer.color;
                c.a = pulse * maxAlpha;
                auraRenderer.color = c;

            }

            // 2. Interaction Logic (New Input System)
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                LoadDoorScene();
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

    private void LoadDoorScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("WineryDoorAura: No scene name assigned in inspector!");
        }
    }
}