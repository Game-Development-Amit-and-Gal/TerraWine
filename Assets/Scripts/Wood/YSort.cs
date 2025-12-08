using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    /// <summary>
    /// Manual override for the calculated sorting order.
    /// Use this if the sprite should appear slightly above/below others,
    /// regardless of its Y height.
    /// </summary>
    [SerializeField] private int offset = 0;

    /// <summary>
    /// Higher multiplier = more precise sorting between sprites.
    /// Lower multiplier = less precision but cheaper performance.
    /// </summary>
    private const int multiplier = 100;

    /// <summary>
    /// Cached reference to the SpriteRenderer on this object.
    /// This component’s sortingOrder will be updated at runtime.
    /// </summary>
    private SpriteRenderer sr;

    /// <summary>
    /// Cache the SpriteRenderer on object initialization.
    /// </summary>
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Update the sorting order after all movement in this frame,
    /// ensuring accurate sorting based on final Y position.
    /// </summary>
    void LateUpdate()
    {
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * multiplier) + offset;
    }
}
