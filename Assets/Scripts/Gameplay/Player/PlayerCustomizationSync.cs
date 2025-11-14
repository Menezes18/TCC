using Mirror;
using UnityEngine;


public class PlayerCustomizationSync : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnCustomizationChanged))]
    private string customizationJson = "";

    [SyncVar]
    private int hatIndex = -1;

    [SyncVar]
    private int glassesIndex = -1;

    [SyncVar]
    private int shirtIndex = -1;

    private PlayerCustomizationData cachedCustomization;
    private bool customizationApplied = false;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        

        if (CustomizationManager.Instance != null)
        {
            var customization = CustomizationManager.Instance.GetCurrentCustomization();
            if (customization != null)
            {
                CmdSyncCustomization(
                    customization.hatIndex,
                    customization.glassesIndex,
                    customization.shirtIndex
                );
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isLocalPlayer)
        {
            Invoke(nameof(ApplyReceivedCustomization), 0.1f);
        }
    }

    [Command]
    private void CmdSyncCustomization(int hat, int glasses, int shirt)
    {
        hatIndex = hat;
        glassesIndex = glasses;
        shirtIndex = shirt;

        var customization = new PlayerCustomizationData(netId.ToString())
        {
            hatIndex = hat,
            glassesIndex = glasses,
            shirtIndex = shirt
        };
        customizationJson = JsonUtility.ToJson(customization);

        Debug.Log($"[Server] Customização sincronizada: {customization}");
    }

    private void OnCustomizationChanged(string oldJson, string newJson)
    {
        if (!string.IsNullOrEmpty(newJson))
        {
            cachedCustomization = null;
            
            if (!isLocalPlayer && !customizationApplied)
            {
                ApplyReceivedCustomization();
            }
        }
    }

    private void ApplyReceivedCustomization()
    {
        if (customizationApplied) return;

        var customization = GetCustomization();
        if (customization != null)
        {
            var applier = GetComponent<CustomizationApplier>();
            if (applier != null)
            {
                applier.ApplyCustomization(customization);
                customizationApplied = true;
                Debug.Log($"[Client] Customização aplicada no player remoto: {customization}");
            }
        }
    }

    public PlayerCustomizationData GetCustomization()
    {
        if (cachedCustomization == null)
        {
            if (!string.IsNullOrEmpty(customizationJson))
            {
                try
                {
                    cachedCustomization = JsonUtility.FromJson<PlayerCustomizationData>(customizationJson);
                }
                catch
                {
                    cachedCustomization = CreateFromSyncVars();
                }
            }
            else
            {
                cachedCustomization = CreateFromSyncVars();
            }
        }

        return cachedCustomization;
    }

    private PlayerCustomizationData CreateFromSyncVars()
    {
        return new PlayerCustomizationData(netId.ToString())
        {
            hatIndex = this.hatIndex,
            glassesIndex = this.glassesIndex,
            shirtIndex = this.shirtIndex
        };
    }

    public void UpdateCustomization(PlayerCustomizationData customization)
    {
        if (!isLocalPlayer)
        {
            Debug.LogWarning("UpdateCustomization só pode ser chamado no player local");
            return;
        }

        CmdSyncCustomization(
            customization.hatIndex,
            customization.glassesIndex,
            customization.shirtIndex
        );
    }

    public bool IsCustomizationSynced()
    {
        return hatIndex >= -1 && glassesIndex >= -1 && shirtIndex >= -1;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug - Print Customization")]
    private void DebugPrintCustomization()
    {
        var customization = GetCustomization();
        if (customization != null)
        {
            Debug.Log($"Player: {gameObject.name}\n{customization}");
        }
        else
        {
            Debug.LogWarning("Customização não disponível");
        }
    }

    [ContextMenu("Debug - Force Resync (Local Player Only)")]
    private void DebugForceResync()
    {
        if (!isLocalPlayer)
        {
            Debug.LogWarning("Só funciona no player local");
            return;
        }

        if (CustomizationManager.Instance != null)
        {
            var customization = CustomizationManager.Instance.GetCurrentCustomization();
            UpdateCustomization(customization);
            Debug.Log("Customização reenviada");
        }
    }
#endif
}
