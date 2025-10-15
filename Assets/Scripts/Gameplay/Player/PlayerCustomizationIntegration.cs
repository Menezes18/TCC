using UnityEngine;
using Mirror;


public class PlayerCustomizationIntegration : NetworkBehaviour
{
    [Header("Customization")]
    [SerializeField] private CustomizationApplier customizationApplier;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        
        ApplyLocalPlayerCustomization();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (isLocalPlayer)
        {
            ApplyLocalPlayerCustomization();
        }
    }

    private void ApplyLocalPlayerCustomization()
    {
        if (customizationApplier == null)
        {
            customizationApplier = GetComponent<CustomizationApplier>();
            
            if (customizationApplier == null)
            {
                Debug.LogWarning("⚠️ [PlayerCustomizationIntegration] CustomizationApplier não encontrado");
                return;
            }
        }

        customizationApplier.ApplyCurrentCustomization();
        
        Debug.Log($"✅ [PlayerCustomizationIntegration] Customização aplicada no player {gameObject.name}");
    }

}
