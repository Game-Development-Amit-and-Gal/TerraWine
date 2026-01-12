using UnityEngine;



/// <summary>
/// A  simple script to make the sprite of the press button 
/// float up and down to attract attention.
/// </summary>
public class pressEFloater : MonoBehaviour
{

    [Header("Sprite Renderer References")]
    [SerializeField] private SpriteRenderer sr;


    [Header("Float Settings")]
    [SerializeField] private float floatAmplitude = 0.1f;
    [SerializeField] private float floatFrequency = 1f;


    private float baseY = 0f;


    // initialize the base Y position and get the SpriteRenderer if not assigned
    void Start()
    {
        sr ??= GetComponent<SpriteRenderer>();

        baseY = sr.transform.position.y;

    }

    // Each frame Update the Y position of the sprite to create a floating effect
    void Update()
    {
        if (sr == null) return;
        else
        {
            float newY = baseY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            sr.transform.position = new Vector3(sr.transform.position.x, newY, sr.transform.position.z);
        }

    }
}
