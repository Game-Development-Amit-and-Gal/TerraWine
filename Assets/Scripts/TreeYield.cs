using UnityEngine;

/// <summary>
/// Holds the yield value for a tree instance.
/// Attach this script to each tree prefab (Tree_Pine, Tree_Round, etc.).
/// When the tree is created in the scene, it gets a random value
/// between minYield and maxYield (inclusive),
/// with higher probability for numbers in the lower range.
/// </summary>
public class TreeYield : MonoBehaviour
{
    [Header("Yield Settings")]

    /// <summary>
    /// Minimum number this tree can give.
    /// </summary>
    [Range(0, 25)]
    public int minYield = 0;

    /// <summary>
    /// Maximum number this tree can give.
    /// </summary>
    [Range(0, 25)]
    public int maxYield = 25;

    /// <summary>
    /// Max value of the "low" range that we want to be more likely.
    /// Example: 10 means 0–10 will be chosen more often.
    /// </summary>
    [Range(0, 25)]
    public int lowRangeMax = 10;

    /// <summary>
    /// Probability (0–1) to pick from the low range [minYield .. lowRangeMax].
    /// Example: 0.7f = 70% chance to pick from 0–10, 30% for the rest.
    /// </summary>
    [Range(0f, 1f)]
    public float lowRangeWeight = 0.7f;

    [Header("Debug / Runtime Value")]
    [SerializeField]
    [Tooltip("The final random value chosen for this tree instance.")]
    private int yieldAmount;

    /// <summary>
    /// Public read-only access for other scripts.
    /// </summary>
    public int YieldAmount => yieldAmount;

    private void Awake()
    {
        // make sure min <= max
        if (minYield > maxYield)
        {
            int temp = minYield;
            minYield = maxYield;
            maxYield = temp;
        }

        // Clamp lowRangeMax into [minYield, maxYield]
        int lowMax = Mathf.Clamp(lowRangeMax, minYield, maxYield);

        bool hasLowRange = lowMax >= minYield;
        bool hasHighRange = maxYield > lowMax;

        float roll = Random.value; 

        if (hasLowRange && (!hasHighRange || roll < lowRangeWeight))
        {
           
            yieldAmount = Random.Range(minYield, lowMax + 1);
        }
        else
        {
          
            int highMin = Mathf.Max(lowMax + 1, minYield);
            yieldAmount = Random.Range(highMin, maxYield + 1);
        }

     
        Debug.Log($"{gameObject.name} yield = {yieldAmount}");
    }
}
