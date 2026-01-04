using UnityEngine;

public enum ApronColor
{
    Brown,
    Blue
}

public enum RIGHTHandItem
{
    None,
    Shears
}

public enum LeftHandItem
{
    Yes,
    No
}

public enum ShirtPattern
{
    Plain,
    Plaid
}

[CreateAssetMenu(fileName = "EnemyItem", menuName = "TerraWine/Enemy Item", order = 1)]
public class EnemyItemSO : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Suspect";
    public string wineryName = "Unknown Winery";

    [Tooltip("Normal portrait shown in UI.")]
    public Sprite portrait;

    [Tooltip("Portrait shown when the suspect is caught (e.g., surprised face).")]
    public Sprite caughtPortrait;

    [Header("Status")]
    [Tooltip("Mark if this suspect has been caught.")]
    public bool isCaught = false;

    [Header("Traits")]
    public ApronColor apronColor = ApronColor.Brown;

    [Tooltip("Does the enemy hold something in the LEFT hand? (Yes/No)")]
    public LeftHandItem leftHandItem = LeftHandItem.Yes;

    [Tooltip("What the enemy holds in the RIGHT hand (Shears or None)")]
    public RIGHTHandItem rightHandItem = RIGHTHandItem.None;

    [Tooltip("What pattern is on the shirt (Plain or Plaid).")]
    public ShirtPattern shirtPattern = ShirtPattern.Plain;

    public Sprite GetDisplayPortrait()
    {
        if (isCaught && caughtPortrait != null)
            return caughtPortrait;

        return portrait;
    }

    public override string ToString()
    {
        return $"{enemyName} ({wineryName}) | Caught: {isCaught} | Apron: {apronColor}, LeftHand: {leftHandItem}, RightHand: {rightHandItem}, Shirt: {shirtPattern}";
    }
}
