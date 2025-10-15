using UnityEngine;


public class CustomizationApplier : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CustomizationDatabase database;

    [Header("Attachment Points")]
    [SerializeField] private Transform hatAttachPoint;
    [SerializeField] private Transform glassesAttachPoint;
    [SerializeField] private Transform shirtAttachPoint; 

    [Header("Current Items")]
    private GameObject currentHat;
    private GameObject currentGlasses;
    private GameObject currentShirt;

    private void Start()
    {

        if (database == null)
        {
            database = Resources.Load<CustomizationDatabase>("CustomizationDatabase");
            if (database == null)
            {
                Debug.LogWarning("⚠️ [CustomizationApplier] CustomizationDatabase não encontrado em Resources");
            }
        }


        ApplyCurrentCustomization();
    }


    public void ApplyCurrentCustomization()
    {
        if (CustomizationManager.Instance == null)
        {
            Debug.LogWarning("⚠️ [CustomizationApplier] CustomizationManager não inicializado");
            return;
        }

        PlayerCustomizationData customization = CustomizationManager.Instance.GetCurrentCustomization();
        if (customization != null)
        {
            ApplyCustomization(customization);
        }
    }


    public void ApplyCustomization(PlayerCustomizationData customization)
    {
        if (database == null)
        {
            Debug.LogWarning("⚠️ [CustomizationApplier] Database não configurado");
            return;
        }

        ApplyHat(customization.hatIndex);
        ApplyGlasses(customization.glassesIndex);
        ApplyShirt(customization.shirtIndex);

        Debug.Log($"✅ [CustomizationApplier] Customização aplicada: {customization}");
    }


    private void ApplyHat(int hatIndex)
    {

        if (currentHat != null)
        {
            Destroy(currentHat);
            currentHat = null;
        }


        if (hatIndex >= 0 && hatIndex < database.hats.Count)
        {
            CustomizationItem item = database.hats[hatIndex];
            if (item.prefab != null && hatAttachPoint != null)
            {
                currentHat = Instantiate(item.prefab, hatAttachPoint);
                
                
                
                Debug.Log($"🎩 [CustomizationApplier] Chapéu aplicado: {item.name}");
            }
        }
    }


    private void ApplyGlasses(int glassesIndex)
    {

        if (currentGlasses != null)
        {
            Destroy(currentGlasses);
            currentGlasses = null;
        }

        if (glassesIndex >= 0 && glassesIndex < database.glasses.Count)
        {
            CustomizationItem item = database.glasses[glassesIndex];
            if (item.prefab != null && glassesAttachPoint != null)
            {
                currentGlasses = Instantiate(item.prefab, glassesAttachPoint);
                     
                Debug.Log($"🕶️ [CustomizationApplier] Óculos aplicados: {item.name}");
            }
        }
    }


    private void ApplyShirt(int shirtIndex)
    {

        if (currentShirt != null)
        {
            Destroy(currentShirt);
            currentShirt = null;
        }


        if (shirtIndex >= 0 && shirtIndex < database.shirts.Count)
        {
            CustomizationItem item = database.shirts[shirtIndex];
            if (item.prefab != null && shirtAttachPoint != null)
            {
                currentShirt = Instantiate(item.prefab, shirtAttachPoint);
                

                Debug.Log($"👕 [CustomizationApplier] Blusa aplicada: {item.name}");
            }
        }
    }

    public void ClearCustomization()
    {
        if (currentHat != null) Destroy(currentHat);
        if (currentGlasses != null) Destroy(currentGlasses);
        if (currentShirt != null) Destroy(currentShirt);

        currentHat = null;
        currentGlasses = null;
        currentShirt = null;

        Debug.Log("🧹 [CustomizationApplier] Customização removida");
    }

    private void OnDestroy()
    {
        ClearCustomization();
    }

    #if UNITY_EDITOR
    [ContextMenu("Apply Test Customization")]
    private void ApplyTestCustomization()
    {
        PlayerCustomizationData testData = new PlayerCustomizationData("test");
        testData.hatIndex = 0;
        testData.glassesIndex = 0;
        testData.shirtIndex = 0;
        ApplyCustomization(testData);
    }

    [ContextMenu("Clear Customization")]
    private void ClearCustomizationTest()
    {
        ClearCustomization();
    }
    #endif
}
