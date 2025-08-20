using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(CreditEntry))]
public class CreditEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty entryTypeProp = property.FindPropertyRelative("entryType");
        SerializedProperty titleTextProp = property.FindPropertyRelative("titleText");
        SerializedProperty titleColorProp = property.FindPropertyRelative("titleTextColor");
        SerializedProperty sectionTitleProp = property.FindPropertyRelative("sectionTitles");
        SerializedProperty namesProp = property.FindPropertyRelative("names");

        float h = EditorGUIUtility.singleLineHeight;
        float s = EditorGUIUtility.standardVerticalSpacing;
        var lineRect = new Rect(position.x, position.y, position.width, h);

        string tipo = entryTypeProp.enumDisplayNames[entryTypeProp.enumValueIndex];
        property.isExpanded = EditorGUI.Foldout(lineRect, property.isExpanded,
            $"{property.displayName} ({tipo})", true);
        lineRect.y += h + s;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            EditorGUI.PropertyField(lineRect, entryTypeProp);
            lineRect.y += h + s;

            switch ((CreditEntryType)entryTypeProp.enumValueIndex)
            {
                case CreditEntryType.Title:
                    EditorGUI.PropertyField(lineRect, titleTextProp);
                    lineRect.y += h + s;
                    EditorGUI.PropertyField(lineRect, titleColorProp);
                    lineRect.y += h + s;
                    break;

                case CreditEntryType.Section:
                    EditorGUI.PropertyField(lineRect, sectionTitleProp);
                    lineRect.y += h + s;

                    float namesHeight = EditorGUI.GetPropertyHeight(namesProp, true);
                    var namesRect = new Rect(lineRect.x, lineRect.y, lineRect.width, namesHeight);
                    EditorGUI.PropertyField(namesRect, namesProp, true);
                    lineRect.y += namesHeight + s;
                    break;

                case CreditEntryType.Empty:
                    break;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUIUtility.singleLineHeight;
        float s = EditorGUIUtility.standardVerticalSpacing;
        float height = h + s;

        if (!property.isExpanded)
            return height;

        height += h + s;

        var entryTypeProp = property.FindPropertyRelative("entryType");
        switch ((CreditEntryType)entryTypeProp.enumValueIndex)
        {
            case CreditEntryType.Title:
                height += (h + s) * 2;
                break;

            case CreditEntryType.Section:
                height += h + s;
                var namesProp = property.FindPropertyRelative("names");
                height += EditorGUI.GetPropertyHeight(namesProp, true) + s;
                break;

            case CreditEntryType.Empty:
                break;
        }

        return height;
    }
}
