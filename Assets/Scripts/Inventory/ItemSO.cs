// Assets/Scripts/Inventory/ItemSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "TerraWine/Item", fileName = "NewItem")]
public class ItemSO : ScriptableObject
{
    [Header("זהות")]
    public string id;
    public string displayName;

    [Header("נראות / חנות")]
    public Sprite icon;
    public bool stackable = true;
    [Min(1)] public int maxStack = 99;
    [Min(0)] public int price = 0;

    [Header("סוג האייטם")]
    public bool isSeed;   

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id)) id = name.ToUpper().Replace(' ', '_');
        if (!stackable) maxStack = 1;
    }
}
