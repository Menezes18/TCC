#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(ResultsBannerModel))]
public class ResultsBannerModelEditor : Editor
{
    private ResultsBannerModel script;
    
    private SerializedProperty targetImage;
    private SerializedProperty modelPrefab;
    private SerializedProperty textureSize;
    private SerializedProperty testOnStart;
    private SerializedProperty livePreview;
    
    private SerializedProperty modelPosition;
    private SerializedProperty modelRotation;
    private SerializedProperty modelScale;
    
    private SerializedProperty cameraPosition;
    private SerializedProperty cameraRotation;
    private SerializedProperty cameraFOV;
    
    private SerializedProperty animationName;
    
    // Preview System
    private RenderTexture previewTexture;
    private Camera previewCamera;
    private GameObject previewModel;
    private Light previewLight;
    private bool showPreview = true;

    private void OnEnable()
    {
        script = (ResultsBannerModel)target;
        
        targetImage = serializedObject.FindProperty("targetImage");
        modelPrefab = serializedObject.FindProperty("modelPrefab");
        textureSize = serializedObject.FindProperty("textureSize");
        testOnStart = serializedObject.FindProperty("testOnStart");
        livePreview = serializedObject.FindProperty("livePreview");
        
        modelPosition = serializedObject.FindProperty("modelPosition");
        modelRotation = serializedObject.FindProperty("modelRotation");
        modelScale = serializedObject.FindProperty("modelScale");
        
        cameraPosition = serializedObject.FindProperty("cameraPosition");
        cameraRotation = serializedObject.FindProperty("cameraRotation");
        cameraFOV = serializedObject.FindProperty("cameraFOV");
        
        animationName = serializedObject.FindProperty("animationName");
    }
    
    private void OnDisable()
    {
        CleanupPreview();
    }
    
    private void CreatePreviewScene()
    {
        if (!Application.isPlaying)
        {
            CleanupPreview();
            
            if (previewTexture == null)
            {
                previewTexture = new RenderTexture(512, 512, 24);
                previewTexture.antiAliasing = 4;
                previewTexture.Create();
            }
            
            if (previewCamera == null)
            {
                GameObject camObj = new GameObject("EditorPreviewCamera");
                camObj.hideFlags = HideFlags.HideAndDontSave;
                previewCamera = camObj.AddComponent<Camera>();
                previewCamera.targetTexture = previewTexture;
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                previewCamera.enabled = false;
                
                GameObject lightObj = new GameObject("EditorPreviewLight");
                lightObj.hideFlags = HideFlags.HideAndDontSave;
                lightObj.transform.SetParent(camObj.transform);
                lightObj.transform.localPosition = Vector3.zero;
                lightObj.transform.localRotation = Quaternion.Euler(50, -30, 0);
                previewLight = lightObj.AddComponent<Light>();
                previewLight.type = LightType.Directional;
                previewLight.intensity = 1.5f;
            }
        }
    }
    
    private void UpdatePreview()
    {
        if (Application.isPlaying || modelPrefab.objectReferenceValue == null) return;
        
        if (previewTexture == null || previewCamera == null)
        {
            CreatePreviewScene();
        }
        
        if (previewModel != null)
        {
            DestroyImmediate(previewModel);
            previewModel = null;
        }
        
        EditorApplication.delayCall += () =>
        {
            if (this == null || modelPrefab.objectReferenceValue == null) return;
            
            GameObject prefab = modelPrefab.objectReferenceValue as GameObject;
            if (prefab != null && previewModel == null)
            {
                previewModel = Instantiate(prefab);
                previewModel.name = "EditorPreviewModel_SINGLE";
                previewModel.hideFlags = HideFlags.HideAndDontSave;
                previewModel.transform.position = modelPosition.vector3Value;
                previewModel.transform.eulerAngles = modelRotation.vector3Value;
                previewModel.transform.localScale = Vector3.one * modelScale.floatValue;
                
                var colliders = previewModel.GetComponentsInChildren<Collider>(true);
                foreach (var col in colliders)
                    DestroyImmediate(col);
                    
                var rigidbodies = previewModel.GetComponentsInChildren<Rigidbody>(true);
                foreach (var rb in rigidbodies)
                    DestroyImmediate(rb);
            }
            
            if (previewCamera != null)
            {
                previewCamera.transform.position = cameraPosition.vector3Value;
                previewCamera.transform.eulerAngles = cameraRotation.vector3Value;
                previewCamera.fieldOfView = cameraFOV.floatValue;
                
                previewCamera.Render();
            }
            
            Repaint();
        };
    }
    
    private void CleanupPreview()
    {
        if (previewModel != null)
        {
            DestroyImmediate(previewModel);
            previewModel = null;
        }
        
        if (previewLight != null && (previewCamera == null || previewLight.gameObject != previewCamera.gameObject))
        {
            if (previewLight.gameObject != null)
                DestroyImmediate(previewLight.gameObject);
            previewLight = null;
        }
        
        if (previewCamera != null)
        {
            if (previewCamera.gameObject != null)
                DestroyImmediate(previewCamera.gameObject);
            previewCamera = null;
        }
        
        if (previewTexture != null)
        {
            previewTexture.Release();
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        if (!Application.isPlaying && modelPrefab.objectReferenceValue != null)
        {
            EditorGUILayout.Space();
            
            showPreview = EditorGUILayout.Foldout(showPreview, "PREVIEW RENDERIZADO", true, EditorStyles.foldoutHeader);
            
            if (showPreview)
            {
                EditorGUILayout.BeginVertical("box");
                
                if (previewTexture != null)
                {
                    GUILayout.Label("Resultado Final:", EditorStyles.boldLabel);
                    Rect previewRect = GUILayoutUtility.GetRect(512, 512, GUILayout.MaxWidth(512), GUILayout.MaxHeight(512));
                    EditorGUI.DrawPreviewTexture(previewRect, previewTexture);
                    
                    if (GUILayout.Button("Atualizar Preview (criar apenas 1 modelo)", GUILayout.Height(30)))
                    {
                        CleanupPreview();
                        UpdatePreview();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Preview não disponível. Clique em 'Atualizar Preview' abaixo.", MessageType.Info);
                    
                    if (GUILayout.Button("▶️ Criar Preview", GUILayout.Height(30)))
                    {
                        UpdatePreview();
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space();
        }
        
        EditorGUILayout.LabelField("Essencial", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Abrir Preview em Janela Separada (Tempo Real!)", GUILayout.Height(35)))
        {
            ResultsModelPreviewWindow.ShowWindowFor(script);
        }
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.PropertyField(targetImage);
        EditorGUILayout.PropertyField(modelPrefab);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Qualidade", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(textureSize);
        EditorGUILayout.PropertyField(testOnStart, new GUIContent("Test On Start (Play Mode)"));
        EditorGUILayout.PropertyField(livePreview, new GUIContent("Live Preview (Edit Mode)"));
        
        if (livePreview.boolValue && !Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Preview ativo! Ajuste os controles abaixo e clique em 'Atualizar Preview'", MessageType.Info);
        }
        
        if (Application.isPlaying && GUILayout.Button("Recarregar Modelo", GUILayout.Height(30)))
        {
            script.LoadModel(null);
        }
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Posicionamento do Modelo", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("box");
        
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(modelPosition, new GUIContent("Posição"));
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("⬆️ Subir")) { modelPosition.vector3Value += Vector3.up * 0.1f; GUI.changed = true; }
        if (GUILayout.Button("⬇️ Descer")) { modelPosition.vector3Value += Vector3.down * 0.1f; GUI.changed = true; }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.PropertyField(modelRotation, new GUIContent("Rotação"));
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("↪️ Girar Esq")) { modelRotation.vector3Value += new Vector3(0, -15, 0); GUI.changed = true; }
        if (GUILayout.Button("↩️ Girar Dir")) { modelRotation.vector3Value += new Vector3(0, 15, 0); GUI.changed = true; }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.PropertyField(modelScale, new GUIContent("Escala"));
        
        modelScale.floatValue = EditorGUILayout.Slider("Tamanho", modelScale.floatValue, 0.5f, 2f);
        
        bool modelChanged = EditorGUI.EndChangeCheck();
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Câmera", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("box");
        
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(cameraPosition, new GUIContent("Posição"));
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Mais Perto")) { cameraPosition.vector3Value += new Vector3(0, 0, -0.2f); GUI.changed = true; }
        if (GUILayout.Button("Mais Longe")) { cameraPosition.vector3Value += new Vector3(0, 0, 0.2f); GUI.changed = true; }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.PropertyField(cameraRotation, new GUIContent("Rotação"));
        
        EditorGUILayout.PropertyField(cameraFOV, new GUIContent("Field of View"));
        
        cameraFOV.floatValue = EditorGUILayout.Slider("Zoom", cameraFOV.floatValue, 15f, 60f);
        
        bool cameraChanged = EditorGUI.EndChangeCheck();
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Animação", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(animationName);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Presets Rápidos", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Corpo Inteiro", GUILayout.Height(25)))
        {
            modelPosition.vector3Value = new Vector3(0, -0.5f, 0);
            modelScale.floatValue = 1f;
            cameraPosition.vector3Value = new Vector3(0, 1f, 3.5f);
            cameraFOV.floatValue = 40f;
            GUI.changed = true;
        }
        
        if (GUILayout.Button("Meio Corpo", GUILayout.Height(25)))
        {
            modelPosition.vector3Value = new Vector3(0, 0, 0);
            modelScale.floatValue = 1.2f;
            cameraPosition.vector3Value = new Vector3(0, 1f, 2.5f);
            cameraFOV.floatValue = 30f;
            GUI.changed = true;
        }
        
        if (GUILayout.Button("Rosto/Busto", GUILayout.Height(25)))
        {
            modelPosition.vector3Value = new Vector3(0, 0.5f, 0);
            modelScale.floatValue = 1.5f;
            cameraPosition.vector3Value = new Vector3(0, 1.5f, 2f);
            cameraFOV.floatValue = 25f;
            GUI.changed = true;
        }
        
        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
