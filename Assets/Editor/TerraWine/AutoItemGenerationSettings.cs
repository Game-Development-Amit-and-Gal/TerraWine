#if UNITY_EDITOR
using UnityEngine;

[CreateAssetMenu(menuName = "TerraWine/Editor/Auto Item Generation Settings", fileName = "AutoItemGenerationSettings")]
public class AutoItemGenerationSettings : ScriptableObject
{
    [Header("Main")]
    public bool autoGenerateOnImport = true;

    [Tooltip("Where to create ItemSO assets.")]
    public string itemsFolder = "Assets/Resources/Items";

    [Header("Defaults (you can tweak later)")]
    public int defaultGrapePrice = 0;

    public int defaultSeedPrice = 10;
    public int defaultSeedSellPrice = 10;

    public float defaultSeedGrowTimeSeconds = 180f;
    public int defaultHarvestAmount = 10;

    [Header("Bottle pricing from rating")]
    public int bottleBasePrice = 20;
    public int bottlePricePerRating = 10;
}
#endif
