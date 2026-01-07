using TMPro;
using UnityEngine;

public class flashAura : MonoBehaviour
{
    [SerializeField] private SpriteRenderer auraSpriteRenderer;
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private float maxAlpha = 0.7f;
    [SerializeField] private float flashSpeed = 2f;
    [SerializeField] private float offset = 1f;
    private bool inRange = false;

    private void Awake()
    {
        if (auraSpriteRenderer == null)
        {
            auraSpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (inRange)
        {
            float pulse = ((Mathf.Sin(Time.time * flashSpeed)) + offset / 2*offset) * maxAlpha;
            Color color = auraSpriteRenderer.color;
            color.a = pulse;
            auraSpriteRenderer.color = color;
        }
        else
        {
            Color color = auraSpriteRenderer.color;
            color.a = 0f;
            auraSpriteRenderer.color = color;
        }

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inRange = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision) { 
        if (collision.CompareTag("Player"))
        {
            inRange = false;
            Color color = auraSpriteRenderer.color;
            color.a = 0f;
            auraSpriteRenderer.color = color;
        }
    }
}

