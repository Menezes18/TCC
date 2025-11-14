#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ResultsModelPreviewWindow : EditorWindow
{
    private RenderTexture previewTexture;
    private Camera previewCamera;
    private GameObject previewModel;
    private Light previewLight;
    
    private ResultsBannerModel targetComponent;
    
    // Configurações
    private Vector3 modelPosition = new Vector3(0, -0.5f, 0);
    private Vector3 modelRotation = new Vector3(0, 0, 0);
    private float modelScale = 1f;
    
    private Vector3 cameraPosition = new Vector3(0, 1f, 2.5f);
    private Vector3 cameraRotation = new Vector3(10, 180, 0);
    private float cameraFOV = 30f;
    private Color backgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
    
    private GameObject modelPrefab;
    private bool autoUpdate = false;
    private float lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.1f;
    
    private bool useCustomization = false;
    private int hatIndex = 0;
    private int glassesIndex = 0;
    private int shirtIndex = 0;
    
    [MenuItem("Window/Results Model Preview")]
    public static void ShowWindow()
    {
        var window = GetWindow<ResultsModelPreviewWindow>("Model Preview");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }
    
    public static void ShowWindowFor(ResultsBannerModel component)
    {
        var window = GetWindow<ResultsModelPreviewWindow>("Model Preview");
        window.targetComponent = component;
        window.SyncWithComponent();
        window.minSize = new Vector2(400, 500);
        window.Show();
        window.UpdatePreview();
    }
    
    private void OnEnable()
    {
        CreatePreviewScene();
        EditorApplication.update += OnEditorUpdate;
    }
    
    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        CleanupPreview();
    }
    
    private void OnEditorUpdate()
    {
        if (Application.isPlaying) return;
        
        if (autoUpdate && Time.realtimeSinceStartup - lastUpdateTime > UPDATE_INTERVAL)
        {
            if (targetComponent != null)
            {
                SyncWithComponent();
            }
            
            if (modelPrefab != null)
            {
                UpdatePreview();
            }
            
            Repaint();
            lastUpdateTime = Time.realtimeSinceStartup;
        }
    }
    
    private void SyncWithComponent()
    {
        if (targetComponent == null) return;
        
        SerializedObject so = new SerializedObject(targetComponent);
        
        var prefabProp = so.FindProperty("modelPrefab");
        if (prefabProp.objectReferenceValue != null)
            modelPrefab = prefabProp.objectReferenceValue as GameObject;
            
        modelPosition = so.FindProperty("modelPosition").vector3Value;
        modelRotation = so.FindProperty("modelRotation").vector3Value;
        modelScale = so.FindProperty("modelScale").floatValue;
        
        cameraPosition = so.FindProperty("cameraPosition").vector3Value;
        cameraRotation = so.FindProperty("cameraRotation").vector3Value;
        cameraFOV = so.FindProperty("cameraFOV").floatValue;
        backgroundColor = so.FindProperty("backgroundColor").colorValue;
        
        useCustomization = so.FindProperty("useTestCustomization").boolValue;
        hatIndex = so.FindProperty("testHatIndex").intValue;
        glassesIndex = so.FindProperty("testGlassesIndex").intValue;
        shirtIndex = so.FindProperty("testShirtIndex").intValue;
    }
    
    private void CreatePreviewScene()
    {
        CleanupPreview();
        
        previewTexture = new RenderTexture(1024, 1024, 24);
        previewTexture.antiAliasing = 8;
        previewTexture.Create();
        
        GameObject camObj = new GameObject("PreviewWindowCamera");
        camObj.hideFlags = HideFlags.HideAndDontSave;
        previewCamera = camObj.AddComponent<Camera>();
        previewCamera.targetTexture = previewTexture;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = backgroundColor;
        previewCamera.enabled = false;
        
        GameObject lightObj = new GameObject("PreviewWindowLight");
        lightObj.hideFlags = HideFlags.HideAndDontSave;
        lightObj.transform.SetParent(camObj.transform);
        lightObj.transform.localPosition = Vector3.zero;
        lightObj.transform.localRotation = Quaternion.Euler(50, -30, 0);
        previewLight = lightObj.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.5f;
    }
    
    private void UpdatePreview()
    {
        if (Application.isPlaying) return;
        if (modelPrefab == null) return;
        
        if (previewModel != null)
        {
            DestroyImmediate(previewModel);
            previewModel = null;
        }
        
        previewModel = Instantiate(modelPrefab);
        previewModel.name = "PreviewWindowModel";
        previewModel.hideFlags = HideFlags.HideAndDontSave;
        previewModel.transform.position = modelPosition;
        previewModel.transform.eulerAngles = modelRotation;
        previewModel.transform.localScale = Vector3.one * modelScale;
        
        foreach (var col in previewModel.GetComponentsInChildren<Collider>(true))
            DestroyImmediate(col);
        foreach (var rb in previewModel.GetComponentsInChildren<Rigidbody>(true))
            DestroyImmediate(rb);
        
        if (useCustomization)
        {
            var applier = previewModel.GetComponent<CustomizationApplier>();
            if (applier == null)
                applier = previewModel.AddComponent<CustomizationApplier>();
                
            var customData = new PlayerCustomizationData
            {
                hatIndex = hatIndex,
                glassesIndex = glassesIndex,
                shirtIndex = shirtIndex
            };
            
            applier.ApplyCustomization(customData);
        }
        
        if (previewCamera != null)
        {
            previewCamera.transform.position = cameraPosition;
            previewCamera.transform.eulerAngles = cameraRotation;
            previewCamera.fieldOfView = cameraFOV;
            previewCamera.backgroundColor = backgroundColor;
            previewCamera.Render();
        }
    }
    
    private void CleanupPreview()
    {
        if (previewModel != null)
        {
            DestroyImmediate(previewModel);
            previewModel = null;
        }
        
        if (previewCamera != null)
        {
            DestroyImmediate(previewCamera.gameObject);
            previewCamera = null;
        }
        
        if (previewLight != null && previewLight.gameObject != null)
        {
            DestroyImmediate(previewLight.gameObject);
            previewLight = null;
        }
        
        if (previewTexture != null)
        {
            previewTexture.Release();
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        
        GUILayout.Label("PREVIEW EM TEMPO REAL", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        if (previewTexture != null)
        {
            Rect rect = GUILayoutUtility.GetRect(position.width - 20, position.width - 20);
            EditorGUI.DrawPreviewTexture(rect, previewTexture);
        }
        else
        {
            EditorGUILayout.HelpBox("Preview não disponível", MessageType.Info);
        }
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginVertical("box");
        
        GUILayout.Label("Configurações", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        targetComponent = (ResultsBannerModel)EditorGUILayout.ObjectField("Componente Alvo", targetComponent, typeof(ResultsBannerModel), true);
        
        autoUpdate = EditorGUILayout.Toggle("Auto Update", autoUpdate);
        
        EditorGUILayout.Space(5);
        
        modelPrefab = (GameObject)EditorGUILayout.ObjectField("Model Prefab", modelPrefab, typeof(GameObject), false);
        
        EditorGUILayout.LabelField("Modelo");
        modelPosition = EditorGUILayout.Vector3Field("Posição", modelPosition);
        modelRotation = EditorGUILayout.Vector3Field("Rotação", modelRotation);
        modelScale = EditorGUILayout.Slider("Escala", modelScale, 0.5f, 2f);
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.LabelField("Câmera");
        cameraPosition = EditorGUILayout.Vector3Field("Posição", cameraPosition);
        cameraRotation = EditorGUILayout.Vector3Field("Rotação", cameraRotation);
        cameraFOV = EditorGUILayout.Slider("FOV", cameraFOV, 15f, 60f);
        backgroundColor = EditorGUILayout.ColorField("Cor de Fundo", backgroundColor);
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.LabelField("Customização");
        useCustomization = EditorGUILayout.Toggle("Usar Customização", useCustomization);
        
        if (useCustomization)
        {
            EditorGUI.indentLevel++;
            hatIndex = EditorGUILayout.IntSlider("Chapéu", hatIndex, 0, 10);
            glassesIndex = EditorGUILayout.IntSlider("Óculos", glassesIndex, 0, 10);
            shirtIndex = EditorGUILayout.IntSlider("Camisa", shirtIndex, 0, 10);
            EditorGUI.indentLevel--;
        }
        
        bool changed = EditorGUI.EndChangeCheck();
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Atualizar Manual", GUILayout.Height(30)))
        {
            if (targetComponent != null) SyncWithComponent();
            UpdatePreview();
        }
        
        if (GUILayout.Button("Limpar", GUILayout.Height(30)))
        {
            CleanupPreview();
            CreatePreviewScene();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        GUILayout.Label("Presets");
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Corpo Inteiro"))
        {
            modelPosition = new Vector3(0, -0.5f, 0);
            modelScale = 1f;
            cameraPosition = new Vector3(0, 1f, 3.5f);
            cameraFOV = 40f;
            changed = true;
        }
        
        if (GUILayout.Button("Meio Corpo"))
        {
            modelPosition = new Vector3(0, 0, 0);
            modelScale = 1.2f;
            cameraPosition = new Vector3(0, 1f, 2.5f);
            cameraFOV = 30f;
            changed = true;
        }
        
        if (GUILayout.Button("Busto"))
        {
            modelPosition = new Vector3(0, 0.5f, 0);
            modelScale = 1.5f;
            cameraPosition = new Vector3(0, 1.5f, 2f);
            cameraFOV = 25f;
            changed = true;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        if (changed && !autoUpdate)
        {
            UpdatePreview();
        }
        
        EditorGUILayout.Space(5);
        string status = autoUpdate ? "Atualizando automaticamente" : "Atualização manual";
        EditorGUILayout.HelpBox(status, MessageType.Info);
    }
}
#endif
