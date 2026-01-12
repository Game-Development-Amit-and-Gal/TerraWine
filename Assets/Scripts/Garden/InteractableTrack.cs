using UnityEngine;
using UnityEngine.UIElements;

public class InteractableTrack : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private float intensity = 1f;
    [SerializeField] private float baseAlpha = 0.5f;
    [SerializeField] private float alphaRange = 0.5f;

    [Header("press E Image")]
    [SerializeField] private SpriteRenderer buttonSprite;


    private bool inRange = false;
    private string playerTag = "Player";

    // Get the SpriteRenderer if not assigned
    // And the BoxCollider2D for interaction detection
    private void Start()
    {
        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
        }
        // Disable the sprite renderer initially
        sr.enabled = false;

        if (buttonSprite != null)
        {
            buttonSprite.enabled = false;
        }
    }
    /// <summary>
    /// Update the alpha value for transparancy pulsing effect.
    /// utilizing a sine wave function in order to change the alpha
    /// value over a period of time.
    /// </summary>
    private void Update()
    {
      
        if (inRange)
        {
            float pulse = (Mathf.Sin(Time.time * intensity) * baseAlpha);
            Color color = sr.color;
            color.a = baseAlpha + (pulse * alphaRange);
            sr.color = color;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            inRange = true;
            sr.enabled = true;
            buttonSprite.enabled = true;
            Debug.Log("Player in range of interactable track.");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            inRange = false;
            // Reset alpha to baseAlpha when exiting
            Color color = sr.color;
            color.a = baseAlpha;
            sr.color = color;
            sr.enabled = false;
            buttonSprite.enabled = false;
            Debug.Log("Player out of range of interactable track.");
        }
    }
}


