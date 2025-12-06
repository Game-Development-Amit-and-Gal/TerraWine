#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemSO))]
public class ItemSOEditor : Editor
{
    // Serialized properties
    SerializedProperty idProp;
    SerializedProperty displayNameProp;
    SerializedProperty iconProp;
    SerializedProperty stackableProp;
    SerializedProperty maxStackProp;
    SerializedProperty priceProp;

    SerializedProperty isSeedProp;
    SerializedProperty isWineBottleProp;
    SerializedProperty categoryProp;

    SerializedProperty growTimeSecondsProp;
    SerializedProperty harvestItemProp;
    SerializedProperty harvestAmountProp;
    SerializedProperty plantedPlotSpriteProp;
    SerializedProperty readyPlotSpriteProp;

    private void OnEnable()
    {
        idProp                 = serializedObject.FindProperty("id");
        displayNameProp        = serializedObject.FindProperty("displayName");
        iconProp               = serializedObject.FindProperty("icon");
        stackableProp          = serializedObject.FindProperty("stackable");
        maxStackProp           = serializedObject.FindProperty("maxStack");
        priceProp              = serializedObject.FindProperty("price");

        isSeedProp             = serializedObject.FindProperty("isSeed");
        isWineBottleProp       = serializedObject.FindProperty("isWineBottle");
        categoryProp           = serializedObject.FindProperty("category");

        growTimeSecondsProp    = serializedObject.FindProperty("growTimeSeconds");
        harvestItemProp        = serializedObject.FindProperty("harvestItem");
        harvestAmountProp      = serializedObject.FindProperty("harvestAmount");
        plantedPlotSpriteProp  = serializedObject.FindProperty("plantedPlotSprite");
        readyPlotSpriteProp    = serializedObject.FindProperty("readyPlotSprite");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Identity
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.PropertyField(displayNameProp);
        EditorGUILayout.Space();

        // Visuals / Shop
        EditorGUILayout.LabelField("Visuals / Shop", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(iconProp);
        EditorGUILayout.PropertyField(stackableProp);
        EditorGUILayout.PropertyField(maxStackProp);
        EditorGUILayout.PropertyField(priceProp);
        EditorGUILayout.Space();

        // Type (Logic)
        EditorGUILayout.LabelField("Type (Logic)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isSeedProp);
        EditorGUILayout.PropertyField(isWineBottleProp);
        EditorGUILayout.Space();

        // Category
        EditorGUILayout.LabelField("Inventory Category", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(categoryProp);
        EditorGUILayout.Space();

        // Seed Settings – רק כשיש V על isSeed
        EditorGUILayout.LabelField("Seed Settings", EditorStyles.boldLabel);

        bool isSeed = isSeedProp.boolValue;

        // אם אין V – השדות יופיעו אפורים ולא יהיה אפשר למלא אותם
        EditorGUI.BeginDisabledGroup(!isSeed);
        EditorGUILayout.PropertyField(growTimeSecondsProp);
        EditorGUILayout.PropertyField(harvestItemProp);
        EditorGUILayout.PropertyField(harvestAmountProp);
        EditorGUILayout.PropertyField(plantedPlotSpriteProp);
        EditorGUILayout.PropertyField(readyPlotSpriteProp);
        EditorGUI.EndDisabledGroup();

        // אופציונלי: הודעה קטנה
        if (!isSeed)
        {
            EditorGUILayout.HelpBox(
                "Enable 'Is Seed' to edit seed-specific settings.",
                MessageType.Info
            );
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
