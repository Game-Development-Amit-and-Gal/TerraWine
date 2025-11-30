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

    [Header("הגדרות זרעים")]
    [Tooltip("רק אם זה זרע: זמן גדילה בשניות")]
    [Min(1)] public float growTimeSeconds = 180f;

    [Tooltip("איזה אייטם מקבלים בקציר מהערוגה של זרע כזה")]
    public ItemSO harvestItem;      // לדוגמה: Cabernet_Grape_ItemSO
    [Min(1)] public int harvestAmount = 10;

    // (אופציונלי – אם תרצי ספרייטים שונים לערוגה לפי הזן)
    public Sprite plantedPlotSprite;
    public Sprite readyPlotSprite;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id)) id = name.ToUpper().Replace(' ', '_');
        if (!stackable) maxStack = 1;
    }
}
