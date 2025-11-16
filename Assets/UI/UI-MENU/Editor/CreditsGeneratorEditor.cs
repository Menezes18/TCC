using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(CreditsGenerator))]
public class CreditsGeneratorEditor : Editor
{
    private ReorderableList list;
    private SerializedProperty entriesProp;
    
    private void OnEnable()
    {
        entriesProp = serializedObject.FindProperty("entries");
        
        list = new ReorderableList(serializedObject, entriesProp, true, true, true, true);
        
        // Header customizado
        list.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Credits Entries", EditorStyles.boldLabel);
            
            var countRect = new Rect(rect.x + rect.width - 80, rect.y, 80, rect.height);
            EditorGUI.LabelField(countRect, $"Total: {entriesProp.arraySize}", EditorStyles.miniLabel);
        };
        
        // Desenhar cada elemento
        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = entriesProp.GetArrayElementAtIndex(index);
            var entryTypeProp = element.FindPropertyRelative("entryType");
            var entryType = (CreditEntryType)entryTypeProp.enumValueIndex;
            
            rect.y += 2;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            
            // Ícone e tipo no topo
            string icon = GetIconForType(entryType);
            Color iconColor = GetColorForType(entryType);
            
            var iconRect = new Rect(rect.x, rect.y, 20, lineHeight);
            var oldColor = GUI.color;
            GUI.color = iconColor;
            EditorGUI.LabelField(iconRect, icon, EditorStyles.boldLabel);
            GUI.color = oldColor;
            
            var typeRect = new Rect(rect.x + 25, rect.y, 80, lineHeight);
            EditorGUI.LabelField(typeRect, entryType.ToString(), EditorStyles.boldLabel);
            
            float yOffset = rect.y + lineHeight + spacing + 2;
            
            // Campos de edição inline baseados no tipo
            switch (entryType)
            {
                case CreditEntryType.Title:
                    var titleTextProp = element.FindPropertyRelative("titleText");
                    var titleColorProp = element.FindPropertyRelative("titleTextColor");
                    
                    var titleLabelRect = new Rect(rect.x + 10, yOffset, 60, lineHeight);
                    EditorGUI.LabelField(titleLabelRect, "Title:", EditorStyles.miniLabel);
                    
                    var titleFieldRect = new Rect(rect.x + 70, yOffset, rect.width - 140, lineHeight);
                    titleTextProp.stringValue = EditorGUI.TextField(titleFieldRect, titleTextProp.stringValue);
                    
                    var colorRect = new Rect(rect.x + rect.width - 60, yOffset, 60, lineHeight);
                    titleColorProp.colorValue = EditorGUI.ColorField(colorRect, GUIContent.none, titleColorProp.colorValue, false, false, false);
                    break;
                    
                case CreditEntryType.Section:
                    var sectionTitleProp = element.FindPropertyRelative("sectionTitles");
                    var namesProp = element.FindPropertyRelative("names");
                    
                    var sectionLabelRect = new Rect(rect.x + 10, yOffset, 60, lineHeight);
                    EditorGUI.LabelField(sectionLabelRect, "Section:", EditorStyles.miniLabel);
                    
                    var sectionFieldRect = new Rect(rect.x + 70, yOffset, rect.width - 70, lineHeight);
                    sectionTitleProp.stringValue = EditorGUI.TextField(sectionFieldRect, sectionTitleProp.stringValue);
                    
                    yOffset += lineHeight + spacing + 2;
                    
                    // Lista de nomes inline
                    var namesLabelRect = new Rect(rect.x + 10, yOffset, 60, lineHeight);
                    EditorGUI.LabelField(namesLabelRect, "Names:", EditorStyles.miniLabel);
                    
                    var addButtonRect = new Rect(rect.x + 70, yOffset, 60, lineHeight);
                    if (GUI.Button(addButtonRect, "+ Add", EditorStyles.miniButton))
                    {
                        namesProp.InsertArrayElementAtIndex(namesProp.arraySize);
                        namesProp.GetArrayElementAtIndex(namesProp.arraySize - 1).stringValue = "";
                    }
                    
                    var countRect = new Rect(rect.x + 135, yOffset, rect.width - 135, lineHeight);
                    EditorGUI.LabelField(countRect, $"({namesProp.arraySize} person(s))", EditorStyles.miniLabel);
                    
                    yOffset += lineHeight + spacing + 2;
                    
                    // Mostra cada nome
                    for (int i = 0; i < namesProp.arraySize; i++)
                    {
                        var nameProp = namesProp.GetArrayElementAtIndex(i);
                        var nameRect = new Rect(rect.x + 20, yOffset, rect.width - 70, lineHeight);
                        nameProp.stringValue = EditorGUI.TextField(nameRect, $"  {i + 1}.", nameProp.stringValue);
                        
                        var removeRect = new Rect(rect.x + rect.width - 45, yOffset, 45, lineHeight);
                        if (GUI.Button(removeRect, "×", EditorStyles.miniButtonRight))
                        {
                            namesProp.DeleteArrayElementAtIndex(i);
                        }
                        
                        yOffset += lineHeight + spacing;
                    }
                    break;
                    
                case CreditEntryType.Empty:
                    var emptyLabelRect = new Rect(rect.x + 10, yOffset, rect.width - 10, lineHeight);
                    EditorGUI.LabelField(emptyLabelRect, "- Empty Space for Separation -", EditorStyles.centeredGreyMiniLabel);
                    break;
            }
        };
        
        // Altura dinâmica baseada no conteúdo
        list.elementHeightCallback = (int index) => {
            var element = entriesProp.GetArrayElementAtIndex(index);
            var entryTypeProp = element.FindPropertyRelative("entryType");
            var entryType = (CreditEntryType)entryTypeProp.enumValueIndex;
            
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float baseHeight = lineHeight + spacing + 4; // Ícone e tipo
            
            switch (entryType)
            {
                case CreditEntryType.Title:
                    return baseHeight + lineHeight + spacing + 8;
                    
                case CreditEntryType.Section:
                    var namesProp = element.FindPropertyRelative("names");
                    float sectionHeight = baseHeight + (lineHeight + spacing + 2) * 2; // Section title + Names header
                    sectionHeight += (lineHeight + spacing) * namesProp.arraySize; // Cada nome
                    return sectionHeight + 8;
                    
                case CreditEntryType.Empty:
                    return baseHeight + lineHeight + spacing + 8;
                    
                default:
                    return baseHeight + 4;
            }
        };
        
        // Callback ao adicionar novo elemento
        list.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) => {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Empty Space"), false, () => AddEntry(CreditEntryType.Empty));
            menu.AddItem(new GUIContent("Title"), false, () => AddEntry(CreditEntryType.Title));
            menu.AddItem(new GUIContent("Section"), false, () => AddEntry(CreditEntryType.Section));
            menu.ShowAsContext();
        };
        
        // Callback ao selecionar
        list.onSelectCallback = (ReorderableList l) => {
            // Força o inspector a mostrar o elemento selecionado
        };
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Header estilizado
        EditorGUILayout.Space(10);
        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 14;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Credits Generator Editor", headerStyle);
        EditorGUILayout.Space(5);
        
        // Campos obrigatórios
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Required References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("titlePrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sectionPrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("empty"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("content"));
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Estatísticas rápidas
        DrawStatistics();
        
        EditorGUILayout.Space(10);
        
        // Lista de créditos
        list.DoLayoutList();
        
        EditorGUILayout.Space(10);
        
        // Botões de ação
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Preview in Game", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                var generator = (CreditsGenerator)target;
                generator.SendMessage("Generate", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                EditorUtility.DisplayDialog("Preview", "Enter Play Mode to preview credits.", "OK");
            }
        }
        
        if (GUILayout.Button("Clear All", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear All Entries", 
                "Are you sure you want to clear all credit entries?", "Yes", "Cancel"))
            {
                entriesProp.ClearArray();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawStatistics()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        
        int titleCount = 0, sectionCount = 0, emptyCount = 0, totalPeople = 0;
        
        for (int i = 0; i < entriesProp.arraySize; i++)
        {
            var element = entriesProp.GetArrayElementAtIndex(i);
            var entryTypeProp = element.FindPropertyRelative("entryType");
            var entryType = (CreditEntryType)entryTypeProp.enumValueIndex;
            
            switch (entryType)
            {
                case CreditEntryType.Title: titleCount++; break;
                case CreditEntryType.Section: 
                    sectionCount++; 
                    var namesProp = element.FindPropertyRelative("names");
                    totalPeople += namesProp.arraySize;
                    break;
                case CreditEntryType.Empty: emptyCount++; break;
            }
        }
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"📋 Titles: {titleCount}", GUILayout.Width(100));
        EditorGUILayout.LabelField($"👥 Sections: {sectionCount}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"📝 Total People: {totalPeople}", GUILayout.Width(130));
        EditorGUILayout.LabelField($"⬜ Spaces: {emptyCount}");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawDetailedEntry(SerializedProperty element)
    {
        var entryTypeProp = element.FindPropertyRelative("entryType");
        EditorGUILayout.PropertyField(entryTypeProp, new GUIContent("Entry Type"));
        
        var entryType = (CreditEntryType)entryTypeProp.enumValueIndex;
        
        switch (entryType)
        {
            case CreditEntryType.Title:
                var titleTextProp = element.FindPropertyRelative("titleText");
                var titleColorProp = element.FindPropertyRelative("titleTextColor");
                EditorGUILayout.PropertyField(titleTextProp, new GUIContent("Title Text"));
                EditorGUILayout.PropertyField(titleColorProp, new GUIContent("Text Color"));
                
                if (!string.IsNullOrEmpty(titleTextProp.stringValue))
                {
                    EditorGUILayout.Space(5);
                    var previewStyle = new GUIStyle(EditorStyles.label);
                    previewStyle.alignment = TextAnchor.MiddleCenter;
                    previewStyle.fontSize = 16;
                    previewStyle.fontStyle = FontStyle.Bold;
                    var oldColor = GUI.color;
                    GUI.color = titleColorProp.colorValue;
                    EditorGUILayout.LabelField(titleTextProp.stringValue, previewStyle);
                    GUI.color = oldColor;
                }
                break;
                
            case CreditEntryType.Section:
                var sectionTitleProp = element.FindPropertyRelative("sectionTitles");
                var namesProp = element.FindPropertyRelative("names");
                
                EditorGUILayout.PropertyField(sectionTitleProp, new GUIContent("Section Title"));
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(namesProp, new GUIContent("Names"), true);
                
                if (namesProp.arraySize > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"👥 {namesProp.arraySize} person(s) in this section", EditorStyles.miniLabel);
                }
                break;
                
            case CreditEntryType.Empty:
                EditorGUILayout.HelpBox("Empty space for visual separation", MessageType.Info);
                break;
        }
    }
    
    private void AddEntry(CreditEntryType type)
    {
        entriesProp.arraySize++;
        var newElement = entriesProp.GetArrayElementAtIndex(entriesProp.arraySize - 1);
        var entryTypeProp = newElement.FindPropertyRelative("entryType");
        entryTypeProp.enumValueIndex = (int)type;
        
        // Inicializa valores padrão
        switch (type)
        {
            case CreditEntryType.Title:
                var titleTextProp = newElement.FindPropertyRelative("titleText");
                var titleColorProp = newElement.FindPropertyRelative("titleTextColor");
                titleTextProp.stringValue = "New Title";
                titleColorProp.colorValue = Color.white;
                break;
            case CreditEntryType.Section:
                var sectionTitleProp = newElement.FindPropertyRelative("sectionTitles");
                var namesProp = newElement.FindPropertyRelative("names");
                sectionTitleProp.stringValue = "New Section";
                namesProp.ClearArray();
                break;
        }
        
        serializedObject.ApplyModifiedProperties();
        list.index = entriesProp.arraySize - 1;
    }
    
    private string GetIconForType(CreditEntryType type)
    {
        switch (type)
        {
            case CreditEntryType.Title: return "📋";
            case CreditEntryType.Section: return "👥";
            case CreditEntryType.Empty: return "⬜";
            default: return "•";
        }
    }
    
    private Color GetColorForType(CreditEntryType type)
    {
        switch (type)
        {
            case CreditEntryType.Title: return new Color(0.3f, 0.8f, 1f);
            case CreditEntryType.Section: return new Color(0.3f, 1f, 0.5f);
            case CreditEntryType.Empty: return new Color(0.6f, 0.6f, 0.6f);
            default: return Color.white;
        }
    }
    
    private string GetPreviewText(SerializedProperty element, CreditEntryType type)
    {
        switch (type)
        {
            case CreditEntryType.Title:
                var titleText = element.FindPropertyRelative("titleText").stringValue;
                return string.IsNullOrEmpty(titleText) ? "<No Title>" : titleText;
                
            case CreditEntryType.Section:
                var sectionTitle = element.FindPropertyRelative("sectionTitles").stringValue;
                var names = element.FindPropertyRelative("names");
                string title = string.IsNullOrEmpty(sectionTitle) ? "<No Section Title>" : sectionTitle;
                return $"{title} ({names.arraySize} person(s))";
                
            case CreditEntryType.Empty:
                return "<Empty Space>";
                
            default:
                return "";
        }
    }
}
