using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class ChangeMaterialTool : EditorWindow
{
    private GameObject targetObject;
    private Material newMaterial;
    private string targetName = "ChaoCaindoGeloEscuro";
    private bool includeInactive = true;
    private bool includeChildren = true;
    private bool usePartialMatch = false;
    private Vector2 scrollPosition;
    private List<GameObject> foundObjects = new List<GameObject>();
    private List<GameObject> allAffectedObjects = new List<GameObject>();
    private Dictionary<GameObject, bool> objectSelection = new Dictionary<GameObject, bool>();

    [MenuItem("Tools/Change Material Tool")]
    public static void ShowWindow()
    {
        GetWindow<ChangeMaterialTool>("Change Material Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Change Material Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Campo para selecionar o objeto pai
        targetObject = (GameObject)EditorGUILayout.ObjectField(
            "Target Parent Object", 
            targetObject, 
            typeof(GameObject), 
            true
        );

        EditorGUILayout.Space();

        // Campo para o nome dos objetos a serem encontrados
        targetName = EditorGUILayout.TextField("Object Name to Find", targetName);

        // Checkbox para busca parcial
        usePartialMatch = EditorGUILayout.Toggle("Use Partial Match (Contains)", usePartialMatch);

        // Checkbox para incluir inativos
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

        // Checkbox para incluir filhos
        includeChildren = EditorGUILayout.Toggle("Include Children of Found Objects", includeChildren);

        EditorGUILayout.Space();

        // Campo para selecionar o novo material
        newMaterial = (Material)EditorGUILayout.ObjectField(
            "New Material", 
            newMaterial, 
            typeof(Material), 
            false
        );

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // Botão para buscar objetos
        if (GUILayout.Button("Find Objects", GUILayout.Height(30)))
        {
            FindTargetObjects();
        }

        EditorGUILayout.Space();

        // Mostrar objetos encontrados
        if (foundObjects.Count > 0)
        {
            int totalObjects = includeChildren ? allAffectedObjects.Count : foundObjects.Count;
            int selectedCount = objectSelection.Where(kvp => kvp.Value).Count();
            
            EditorGUILayout.LabelField($"Found {foundObjects.Count} parent objects", EditorStyles.boldLabel);
            if (includeChildren)
            {
                EditorGUILayout.LabelField($"Total objects (including children): {totalObjects}", EditorStyles.boldLabel);
            }
            EditorGUILayout.LabelField($"Selected: {selectedCount} objects", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                SelectAll(true);
            }
            if (GUILayout.Button("Deselect All"))
            {
                SelectAll(false);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
            List<GameObject> objectsToShow = includeChildren ? allAffectedObjects : foundObjects;
            
            foreach (var obj in objectsToShow)
            {
                if (obj != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Checkbox de seleção
                    bool isSelected = objectSelection.ContainsKey(obj) && objectSelection[obj];
                    bool newSelection = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                    if (newSelection != isSelected)
                    {
                        objectSelection[obj] = newSelection;
                    }
                    
                    EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                    
                    // Mostrar o material atual
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        EditorGUILayout.LabelField($"Current: {renderer.sharedMaterial.name}", GUILayout.Width(200));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No Renderer", GUILayout.Width(200));
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Botão para aplicar o material
            GUI.enabled = newMaterial != null;
            if (GUILayout.Button("Apply Material to All", GUILayout.Height(40)))
            {
                ApplyMaterial();
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space();

        // Informações
        EditorGUILayout.HelpBox(
            "1. Selecione o objeto pai (ou deixe vazio para buscar em toda a cena)\n" +
            "2. Digite o nome dos objetos que deseja encontrar\n" +
            "3. Marque 'Use Partial Match' para encontrar objetos que contenham o nome (ex: 'ChaoCaindoGeloEscuro' encontra 'ChaoCaindoGeloEscuro 1', 'ChaoCaindoGeloEscuro 2', etc.)\n" +
            "4. Marque 'Include Children' para também listar os filhos dos objetos encontrados\n" +
            "5. Clique em 'Find Objects' para listar todos os objetos\n" +
            "6. Marque/desmarque os checkboxes para escolher quais objetos modificar\n" +
            "7. Escolha o novo material\n" +
            "8. Clique em 'Apply Material to All' para aplicar apenas nos selecionados",
            MessageType.Info
        );
    }

    private void FindTargetObjects()
    {
        foundObjects.Clear();
        allAffectedObjects.Clear();
        objectSelection.Clear();

        if (targetObject != null)
        {
            // Buscar dentro do objeto selecionado
            Transform[] allTransforms = targetObject.GetComponentsInChildren<Transform>(includeInactive);
            
            foreach (Transform t in allTransforms)
            {
                bool matches = usePartialMatch ? 
                    t.gameObject.name.Contains(targetName) : 
                    t.gameObject.name == targetName;
                    
                if (matches)
                {
                    foundObjects.Add(t.gameObject);
                }
            }
        }
        else
        {
            // Buscar em toda a cena
            GameObject[] allObjects = includeInactive ? 
                Resources.FindObjectsOfTypeAll<GameObject>() : 
                GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                // Filtrar objetos que não estão na cena (assets, prefabs, etc)
                bool matches = usePartialMatch ? 
                    obj.name.Contains(targetName) : 
                    obj.name == targetName;
                    
                if (obj.scene.IsValid() && matches)
                {
                    foundObjects.Add(obj);
                }
            }
        }

        // Adicionar os objetos encontrados e seus filhos à lista de objetos afetados
        foreach (GameObject obj in foundObjects)
        {
            allAffectedObjects.Add(obj);
            objectSelection[obj] = true; // Selecionar por padrão
            
            if (includeChildren)
            {
                // Adicionar todos os filhos que tenham Renderer
                Renderer[] childRenderers = obj.GetComponentsInChildren<Renderer>(includeInactive);
                foreach (Renderer renderer in childRenderers)
                {
                    if (renderer.gameObject != obj && !allAffectedObjects.Contains(renderer.gameObject))
                    {
                        allAffectedObjects.Add(renderer.gameObject);
                        objectSelection[renderer.gameObject] = true; // Selecionar por padrão
                    }
                }
            }
        }

        Debug.Log($"Found {foundObjects.Count} objects with name '{targetName}'");
        if (includeChildren)
        {
            Debug.Log($"Total objects including children: {allAffectedObjects.Count}");
        }
    }

    private void SelectAll(bool select)
    {
        List<GameObject> objectsToSelect = includeChildren ? allAffectedObjects : foundObjects;
        foreach (GameObject obj in objectsToSelect)
        {
            if (objectSelection.ContainsKey(obj))
            {
                objectSelection[obj] = select;
            }
        }
    }

    private void ApplyMaterial()
    {
        if (newMaterial == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a material first!", "OK");
            return;
        }

        if (foundObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No objects found. Click 'Find Objects' first!", "OK");
            return;
        }

        // Obter apenas os objetos selecionados
        List<GameObject> selectedObjects = objectSelection.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();

        if (selectedObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No objects selected. Please select at least one object!", "OK");
            return;
        }

        int successCount = 0;
        int failCount = 0;

        Undo.RecordObjects(selectedObjects.ToArray(), "Change Materials");

        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Undo.RecordObject(renderer, "Change Material");
                    renderer.sharedMaterial = newMaterial;
                    EditorUtility.SetDirty(renderer);
                    successCount++;
                }
                else
                {
                    failCount++;
                    Debug.LogWarning($"Object '{obj.name}' doesn't have a Renderer component");
                }
            }
        }

        EditorUtility.DisplayDialog(
            "Complete", 
            $"Material applied successfully!\n\nSuccess: {successCount}\nFailed: {failCount}", 
            "OK"
        );

        Debug.Log($"Applied material '{newMaterial.name}' to {successCount} objects");
        
        // Atualizar a vista da cena
        SceneView.RepaintAll();
    }
}
