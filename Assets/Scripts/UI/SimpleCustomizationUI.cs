using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class SimpleCustomizationUI : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private CustomizationDatabase database;
    
    [Header("UI Buttons")]
    [SerializeField] private Button hatButton;
    [SerializeField] private Button glassesButton;
    [SerializeField] private Button shirtButton;
    
    
    private int currentHatIndex = -1;
    private int currentGlassesIndex = -1;
    private int currentShirtIndex = -1;
    
    private void Start()
    {
        if (database == null)
        {
            database = Resources.Load<CustomizationDatabase>("CustomizationDatabase");
            if (database == null)
            {
                Debug.LogError("❌ [SimpleCustomizationUI] CustomizationDatabase não encontrado! Crie em Resources/CustomizationDatabase");
                return;
            }
        }
        

        if (hatButton != null)
        {
            hatButton.onClick.AddListener(OnHatButtonClick);
        }
        
        if (glassesButton != null)
        {
            glassesButton.onClick.AddListener(OnGlassesButtonClick);
        }
        
        if (shirtButton != null)
        {
            shirtButton.onClick.AddListener(OnShirtButtonClick);
        }
    
        LoadCurrentCustomization();

    }
    
    private void OnDestroy()
    {
        if (hatButton != null) hatButton.onClick.RemoveListener(OnHatButtonClick);
        if (glassesButton != null) glassesButton.onClick.RemoveListener(OnGlassesButtonClick);
        if (shirtButton != null) shirtButton.onClick.RemoveListener(OnShirtButtonClick);
    }
    
    private void LoadCurrentCustomization()
    {
        if (CustomizationManager.Instance == null) return;
        
        var customization = CustomizationManager.Instance.GetCurrentCustomization();
        if (customization != null)
        {
            currentHatIndex = customization.hatIndex;
            currentGlassesIndex = customization.glassesIndex;
            currentShirtIndex = customization.shirtIndex;
        }
    }
    
    private void OnHatButtonClick()
    {
        if (database == null || database.hats.Count == 0)
        {
            Debug.LogWarning("⚠️ [SimpleCustomizationUI] Nenhum chapéu configurado no database!");
            return;
        }
        
        currentHatIndex++;
        if (currentHatIndex >= database.hats.Count)
        {
            currentHatIndex = -1;
        }

        CustomizationManager.Instance.SetHat(currentHatIndex);
        ApplyToPlayer();
        
        string itemName = currentHatIndex >= 0 ? database.hats[currentHatIndex].name : "Nenhum";
        Debug.Log($"🎩 [SimpleCustomizationUI] Chapéu: {itemName}");
    }
    
    private void OnGlassesButtonClick()
    {
        if (database == null || database.glasses.Count == 0)
        {
            Debug.LogWarning("⚠️ [SimpleCustomizationUI] Nenhum óculos configurado no database!");
            return;
        }
        
        currentGlassesIndex++;
        if (currentGlassesIndex >= database.glasses.Count)
        {
            currentGlassesIndex = -1; 
        }
        
        CustomizationManager.Instance.SetGlasses(currentGlassesIndex);
        ApplyToPlayer();
        
        string itemName = currentGlassesIndex >= 0 ? database.glasses[currentGlassesIndex].name : "Nenhum";
        Debug.Log($"🕶️ [SimpleCustomizationUI] Óculos: {itemName}");
    }
    
    private void OnShirtButtonClick()
    {
        if (database == null || database.shirts.Count == 0)
        {
            Debug.LogWarning("⚠️ [SimpleCustomizationUI] Nenhuma blusa configurada no database!");
            return;
        }
        
        currentShirtIndex++;
        if (currentShirtIndex >= database.shirts.Count)
        {
            currentShirtIndex = -1; 
        }
        

        CustomizationManager.Instance.SetShirt(currentShirtIndex);
        ApplyToPlayer();
        
        string itemName = currentShirtIndex >= 0 ? database.shirts[currentShirtIndex].name : "Nenhum";
        Debug.Log($"👕 [SimpleCustomizationUI] Blusa: {itemName}");
    }
    
    private void ApplyToPlayer()
    {

        var playerScript = FindAnyObjectByType<PlayerScript>();
        if (playerScript != null && playerScript.isLocalPlayer)
        {
            playerScript.ApplyPlayerCustomization();
            return;
        }
        
        var appliers = FindObjectsByType<CustomizationApplier>(FindObjectsSortMode.None);
        foreach (var applier in appliers)
        {
            applier.ApplyCurrentCustomization();
        }
    }
    
    
}
