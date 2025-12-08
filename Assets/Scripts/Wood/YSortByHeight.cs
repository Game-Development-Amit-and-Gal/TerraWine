using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortByHeight : MonoBehaviour
{
    /// <summary>
    /// Extra manual adjustment to the sorting order.
    /// Useful when you want a specific object (like tall UI or props)
    /// to be drawn above or below others despite its Y-position.
    /// </summary>
    [SerializeField] private int offset = 0;

    /// <summary>
    /// Cached reference to this object's SpriteRenderer.
    /// Used to update its sortingOrder every frame.
    /// </summary>
    private SpriteRenderer sr;
    private const int multiplier = 100;


    /// <summary>
    /// Cache the SpriteRenderer on startup.
    /// RequireComponent ensures this component exists on the object.
    /// </summary>
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// LateUpdate runs after all movement updates,
    /// ensuring we calculate the correct sorting order
    /// based on the final Y position of this frame.
    /// Objects lower on screen (smaller Y value) appear in front.
    /// </summary>
    void LateUpdate()
    {
        // Multiply Y by 100 for more precision, then invert sign (-Y)
        // so lower objects get higher sortingOrder (drawn in front).
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * multiplier) + offset;
    }
}
