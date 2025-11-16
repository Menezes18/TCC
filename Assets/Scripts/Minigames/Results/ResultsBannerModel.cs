using UnityEngine;
using UnityEngine.UI;

public class ResultsBannerModel : MonoBehaviour
{
    [Header("Essencial")]
    [SerializeField] private RawImage targetImage;
    
    [Tooltip("Prefab com apenas visual: mesh + animator + CustomizationApplier (SEM scripts de gameplay)")]
    [SerializeField] private GameObject modelPrefab;
    
    [Header("Qualidade (opcional)")]
    [SerializeField] private int textureSize = 512;
    [SerializeField] private bool testOnStart = false;
    [SerializeField] private bool livePreview = true;
    
    [Header("Posicionamento do Modelo")]
    [SerializeField] private Vector3 modelPosition = new Vector3(0, -0.5f, 0);
    [SerializeField] private Vector3 modelRotation = new Vector3(0, 0, 0);
    [SerializeField] private float modelScale = 1f;
    
    [Header("Câmera")]
    [SerializeField] private Vector3 cameraPosition = new Vector3(0, 1f, 2.5f);
    [SerializeField] private Vector3 cameraRotation = new Vector3(10, 180, 0);
    [SerializeField] private float cameraFOV = 30f;
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    
    [Header("Customização (Teste)")]
    [SerializeField] private bool useTestCustomization = false;
    [SerializeField] private int testHatIndex = 0;
    [SerializeField] private int testGlassesIndex = 0;
    [SerializeField] private int testShirtIndex = 0;
    
    [Header("Animação")]
    [SerializeField] private string animationName = "Idle";
    
    private RenderTexture renderTexture;
    private Camera renderCamera;
    private GameObject modelInstance;

    public void LoadModel(PlayerCustomizationData customization)
    {
        if (targetImage == null)
        {
            Debug.LogError("[ResultsBannerModel] Target Image está NULL!");
            return;
        }
        
        if (modelPrefab == null)
        {
            Debug.LogError("[ResultsBannerModel] Model Prefab está NULL!");
            return;
        }

        Debug.Log($"[ResultsBannerModel] Iniciando LoadModel. RawImage: {targetImage.name}");


        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(textureSize, textureSize, 24);
            renderTexture.antiAliasing = 4;
            renderTexture.Create();
            targetImage.texture = renderTexture;
            
            Debug.Log($"[ResultsBannerModel] RenderTexture criada: {textureSize}x{textureSize}. Atribuída ao RawImage.");
        }

        if (renderCamera == null)
        {
            GameObject camObj = new GameObject("ModelCamera");
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = cameraPosition;
            camObj.transform.localEulerAngles = cameraRotation;

            renderCamera = camObj.AddComponent<Camera>();
            renderCamera.targetTexture = renderTexture;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = backgroundColor;
            renderCamera.fieldOfView = cameraFOV;
            renderCamera.nearClipPlane = 0.1f;
            renderCamera.farClipPlane = 10f;
            renderCamera.cullingMask = -1; 
            renderCamera.depth = -10;

            // Luz
            GameObject lightObj = new GameObject("ModelLight");
            lightObj.transform.SetParent(camObj.transform);
            lightObj.transform.localPosition = Vector3.zero;
            lightObj.transform.localEulerAngles = new Vector3(50, -30, 0);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            
            Debug.Log($"[ResultsBannerModel] Câmera criada: {renderCamera.name}");
        }

        // Limpa modelo anterior
        if (modelInstance != null)
            Destroy(modelInstance);

        // Cria o modelo
        modelInstance = Instantiate(modelPrefab, transform);
        modelInstance.transform.localPosition = modelPosition;
        modelInstance.transform.localEulerAngles = modelRotation;
        modelInstance.transform.localScale = Vector3.one * modelScale;
        modelInstance.SetActive(true);
        
        Debug.Log($"[ResultsBannerModel] Modelo criado: {modelInstance.name} em {modelInstance.transform.position}");

        // Prefab já deve vir limpo, mas garante segurança (remove física se houver)
        foreach (var col in modelInstance.GetComponentsInChildren<Collider>(true))
            Destroy(col);
        foreach (var rb in modelInstance.GetComponentsInChildren<Rigidbody>(true))
            Destroy(rb);

        // Aplica customização
        PlayerCustomizationData dataToApply = customization;
        
        // Se customização de teste estiver ativa, usa ela
        if (useTestCustomization)
        {
            dataToApply = new PlayerCustomizationData
            {
                hatIndex = testHatIndex,
                glassesIndex = testGlassesIndex,
                shirtIndex = testShirtIndex
            };
        }
        
        if (dataToApply != null)
        {
            var applier = modelInstance.GetComponent<CustomizationApplier>();
            if (applier == null)
                applier = modelInstance.AddComponent<CustomizationApplier>();
            applier.ApplyCustomization(dataToApply);
            Debug.Log($"[ResultsBannerModel] Customização aplicada: Hat={dataToApply.hatIndex}, Glasses={dataToApply.glassesIndex}, Shirt={dataToApply.shirtIndex}");
        }

        // Reproduz animação
        var animator = modelInstance.GetComponentInChildren<Animator>();
        if (animator != null && !string.IsNullOrEmpty(animationName))
        {
            animator.Play(animationName);
            Debug.Log($"[ResultsBannerModel] Animação tocando: {animationName}");
        }
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying && modelInstance != null)
        {
            modelInstance.transform.localPosition = modelPosition;
            modelInstance.transform.localEulerAngles = modelRotation;
            modelInstance.transform.localScale = Vector3.one * modelScale;
        }
        
        if (Application.isPlaying && renderCamera != null)
        {
            renderCamera.transform.localPosition = cameraPosition;
            renderCamera.transform.localEulerAngles = cameraRotation;
            renderCamera.fieldOfView = cameraFOV;
            renderCamera.backgroundColor = backgroundColor;
        }
    }

    private void OnDestroy()
    {
        if (modelInstance != null)
            Destroy(modelInstance);

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (renderCamera != null)
            Destroy(renderCamera.gameObject);
    }
}
