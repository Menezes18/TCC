using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

[System.Serializable]
public class RenderMonkey
{
    public Renderer renderer;
    public int index;

    public RenderMonkey(Renderer renderer, int index)
    {
        this.renderer = renderer;
        this.index = index;
    }
    public void ApplyRenderMonkey(Color color)
    {
        renderer.materials[index].color = color;
    }
}


public class VictoryPodiumManager : NetworkBehaviour
{
    [Header("Model References")]
    [Tooltip("Modelos existentes na ordem: 1º, 2º, 3º, 4º lugar")]
    [SerializeField] private GameObject[] existingModels = new GameObject[4];
    
    [Header("Settings")]
    [SerializeField] private bool autoUpdateOnStart = true;
    [SerializeField] private float updateDelay = 1f;
    [SerializeField] private bool applyPlayerColor = true;
    [SerializeField] private Database database;
    
    [Header("Animation")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Color Configuration")]
    [Header("Posição 1º Lugar")]
    [Tooltip("RenderMonkeys para aplicar cor ao 1º lugar")]
    [SerializeField] private RenderMonkey[] position1RenderMonkeys;
    
    [Header("Posição 2º Lugar")]
    [Tooltip("RenderMonkeys para aplicar cor ao 2º lugar")]
    [SerializeField] private RenderMonkey[] position2RenderMonkeys;
    
    [Header("Posição 3º Lugar")]
    [Tooltip("RenderMonkeys para aplicar cor ao 3º lugar")]
    [SerializeField] private RenderMonkey[] position3RenderMonkeys;
    
    [Header("Posição 4º Lugar")]
    [Tooltip("RenderMonkeys para aplicar cor ao 4º lugar")]
    [SerializeField] private RenderMonkey[] position4RenderMonkeys;
    
    private VictoryRankingData _currentRankingData;
    
    
    private void Awake()
    {

        NetworkIdentity netIdentity = GetComponent<NetworkIdentity>();
        if (netIdentity == null)
        {
            Debug.LogError("❌ [VictoryPodiumManager] NetworkIdentity NÃO encontrado! Adicione manualmente ao GameObject.");
            Debug.LogError("   → O componente NetworkIdentity deve estar configurado no prefab ou GameObject na cena.");
        }

        Debug.Log($"✅ [VictoryPodiumManager] Awake - NetworkIdentity presente: {netIdentity != null}");
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"🚀 [VictoryPodiumManager] OnStartClient - isServer: {isServer}, isClient: {isClient}, netId: {netId}");
        
        InitializePodiumManager();
        
        
        if (isClient)
        {
            StartCoroutine(CheckForRankingDataOnClient());
        }
    }
    

    private System.Collections.IEnumerator CheckForRankingDataOnClient()
    {
        yield return new WaitForSeconds(0.5f);
        
        int attempts = 0;
        const int maxAttempts = 10;
        
        while (attempts < maxAttempts)
        {
            if (VictoryDataManager.Instance != null)
            {
                var rankingData = VictoryDataManager.Instance.GetRankingData();
                if (rankingData != null)
                {
                    int validCount = CountValidPlayers(rankingData);
                    if (validCount > 0)
                    {
                        Debug.Log($"✅ [VictoryPodiumManager] Dados do ranking encontrados no cliente após {attempts * 0.5f} segundos - processando");
                        CreatePodium(rankingData);
                        yield break;
                    }
                }
            }
            
            yield return new WaitForSeconds(0.5f);
            attempts++;
        }
        
        Debug.LogWarning($"⚠️ [VictoryPodiumManager] Dados do ranking não encontrados no cliente após {maxAttempts * 0.5f} segundos");
    }
    
    private void Start()
    {
        if (!isClient && !isServer)
        {
            InitializePodiumManager();
        }
    }
    
    private void InitializePodiumManager()
    {
        Debug.Log($"🚀 [VictoryPodiumManager] Inicializando - isServer: {isServer}, isClient: {isClient}");
        

        if (database == null)
        {
            database = Resources.Load<Database>("Database");
        }
        

        DeactivateAllModelsImmediately();
        
        DisableAutoUpdateOnVictoryDisplays();
        
        if (VictoryDataManager.Instance != null)
        {
            VictoryDataManager.Instance.OnRankingDataReady += OnRankingDataReceived;
            Debug.Log("✅ [VictoryPodiumManager] Inscrito no evento OnRankingDataReady");
            
            var existingRanking = VictoryDataManager.Instance.GetRankingData();
            if (existingRanking != null)
            {
                Debug.Log("📦 [VictoryPodiumManager] Dados do ranking já disponíveis - processando imediatamente");
                OnRankingDataReceived(existingRanking);
            }
        }
        else
        {
            Debug.LogWarning("⚠️ [VictoryPodiumManager] VictoryDataManager.Instance é null no Start - tentando novamente...");
            Invoke(nameof(SubscribeToRankingEvent), 1f);
        }
        
        if (autoUpdateOnStart)
        {
           
            Invoke(nameof(CreatePodium), updateDelay);
            InvokeRepeating(nameof(TryCreatePodiumPeriodically), updateDelay + 2f, 2f);
        }
    }
    
   
    private void SubscribeToRankingEvent()
    {
        if (VictoryDataManager.Instance != null)
        {
            VictoryDataManager.Instance.OnRankingDataReady += OnRankingDataReceived;
            Debug.Log("✅ [VictoryPodiumManager] Inscrito no evento OnRankingDataReady (tentativa tardia)");
            
            var existingRanking = VictoryDataManager.Instance.GetRankingData();
            if (existingRanking != null)
            {
                Debug.Log("📦 [VictoryPodiumManager] Dados do ranking encontrados na tentativa tardia - processando");
                OnRankingDataReceived(existingRanking);
            }
        }
        else
        {
            Debug.LogWarning("⚠️ [VictoryPodiumManager] VictoryDataManager.Instance ainda é null - tentando novamente em 1 segundo...");
            Invoke(nameof(SubscribeToRankingEvent), 1f);
        }
    }
    
   
    private void TryCreatePodiumPeriodically()
    {
        if (VictoryDataManager.Instance == null)
        {
            Debug.LogWarning("⚠️ [VictoryPodiumManager] VictoryDataManager.Instance ainda é null no polling");
            return;
        }
        
        var rankingData = VictoryDataManager.Instance.GetRankingData();
        if (rankingData != null)
        {
           
            Debug.Log($"🔄 [VictoryPodiumManager] Dados do ranking encontrados via polling - processando (isServer: {isServer}, isClient: {isClient})");
            CreatePodium(rankingData);
            
            CancelInvoke(nameof(TryCreatePodiumPeriodically));
            Debug.Log("✅ [VictoryPodiumManager] Dados processados - cancelando polling");
        }
        else
        {
            Debug.Log("⏳ [VictoryPodiumManager] Aguardando dados do ranking... (polling)");
        }
    }
    

    private void DeactivateAllModelsImmediately()
    {
        if (existingModels == null)
            return;
        
        foreach (var model in existingModels)
        {
            if (model != null)
            {
                model.SetActive(false);
                
                VictoryDisplay victoryDisplay = model.GetComponentInChildren<VictoryDisplay>();
                if (victoryDisplay != null)
                {
                    victoryDisplay.HideDisplay();
                }
            }
        }
        
        Debug.Log("🔇 [VictoryPodiumManager] Todos os modelos desativados no início");
    }
    

    private void DisableAutoUpdateOnVictoryDisplays()
    {
        if (existingModels == null)
            return;
        
        foreach (var model in existingModels)
        {
            if (model != null)
            {
                VictoryDisplay victoryDisplay = model.GetComponentInChildren<VictoryDisplay>();
                if (victoryDisplay != null)
                {
                    victoryDisplay.HideDisplay();
                }
            }
        }
    }
    
    private void Update()
    {
        if (enableRotation && existingModels != null)
        {
            foreach (var model in existingModels)
            {
                if (model != null && model.activeSelf)
                {
                    model.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        CancelInvoke();
        
        if (VictoryDataManager.Instance != null)
        {
            VictoryDataManager.Instance.OnRankingDataReady -= OnRankingDataReceived;
        }
        
        CleanupAllMannequins();
    }
    

    private void OnRankingDataReceived(VictoryRankingData rankingData)
    {
        if (rankingData != null)
        {
            Debug.Log($"📡 [VictoryPodiumManager] Dados do ranking recebidos via evento (isServer: {isServer}, isClient: {isClient}, netId: {netId}). Total de jogadores: {CountValidPlayers(rankingData)}");
            
            if (isClient && !isServer)
            {
                Debug.Log($"📥 [VictoryPodiumManager] CLIENTE recebeu dados do ranking:");
                for (int i = 0; i < 4; i++)
                {
                    var player = rankingData.GetPlayerAtPosition(i + 1);
                    if (player != null)
                    {
                        Debug.Log($"  Cliente - Posição {i + 1}: {player.playerName} (SteamID: {player.steamId}, Customização: Hat={player.customization?.hatIndex}, Glasses={player.customization?.glassesIndex}, Shirt={player.customization?.shirtIndex})");
                    }
                }
            }
            

            CreatePodium(rankingData);
        }
        else
        {
            Debug.LogWarning("⚠️ [VictoryPodiumManager] OnRankingDataReceived recebeu dados nulos");
        }
    }
    

    [ClientRpc]
    public void RpcUpdatePodium()
    {
        Debug.Log($"📡 [VictoryPodiumManager] RpcUpdatePodium recebido no cliente - buscando dados do ranking");
        
        if (VictoryDataManager.Instance != null)
        {
            var rankingData = VictoryDataManager.Instance.GetRankingData();
            if (rankingData != null)
            {
                Debug.Log($"✅ [VictoryPodiumManager] Dados encontrados via RPC - atualizando pódio");
                CreatePodium(rankingData);
            }
            else
            {
                Debug.LogWarning("⚠️ [VictoryPodiumManager] RpcUpdatePodium: Dados do ranking não disponíveis ainda");
            }
        }
    }
    
 
    public void CreatePodium()
    {
        if (VictoryDataManager.Instance == null)
        {
            Debug.LogWarning("⚠️ [VictoryPodiumManager] VictoryDataManager.Instance é null. Aguardando...");
            Invoke(nameof(CreatePodium), 2f);
            return;
        }
        
        var rankingData = VictoryDataManager.Instance.GetRankingData();
        if (rankingData != null)
        {
            CreatePodium(rankingData);
        }
        else
        {
            Debug.LogWarning("⚠️ [VictoryPodiumManager] Dados do ranking não disponíveis ainda. Tentando novamente...");
            Invoke(nameof(CreatePodium), 1f);
        }
    }
    

    public void CreatePodium(VictoryRankingData rankingData)
    {
        Debug.Log($"🏆 [VictoryPodiumManager] CreatePodium INICIADO");
        Debug.Log($"   → isServer: {isServer}");
        Debug.Log($"   → isClient: {isClient}");
        Debug.Log($"   → netId: {netId}");
        
        if (rankingData == null)
        {
            Debug.LogError("❌ [VictoryPodiumManager] Tentativa de criar pódio com rankingData NULL!");
            return;
        }
        
        if (rankingData.rankedPlayers == null)
        {
            Debug.LogError("❌ [VictoryPodiumManager] rankingData.rankedPlayers é NULL!");
            return;
        }
        
        if (existingModels == null || existingModels.Length == 0)
        {
            Debug.LogError("❌ [VictoryPodiumManager] existingModels não configurado! Configure os 4 modelos no Inspector.");
            return;
        }
        
        _currentRankingData = rankingData;
        
        int validCount = CountValidPlayers(rankingData);
        Debug.Log($"📊 [VictoryPodiumManager] Dados recebidos: {validCount} jogadores VÁLIDOS de 4 posições");
        

        Debug.Log("📋 [VictoryPodiumManager] RANKING RECEBIDO (antes de aplicar):");
        for (int i = 0; i < 4; i++)
        {
            var player = rankingData.GetPlayerAtPosition(i + 1);
            if (player != null)
            {
                bool isValid = IsValidPlayer(player);
                Debug.Log($"  Posição {i + 1}: {(isValid ? "✅ VÁLIDO" : "❌ INVÁLIDO")}");
                Debug.Log($"     → Nome: '{player.playerName}'");
                Debug.Log($"     → SteamID: {player.steamId}");
                Debug.Log($"     → Score: {player.finalScore}");
                Debug.Log($"     → Customização: Hat={player.customization?.hatIndex}, Glasses={player.customization?.glassesIndex}, Shirt={player.customization?.shirtIndex}");
            }
            else
            {
                Debug.Log($"  Posição {i + 1}: ❌ NULL");
            }
        }
        
        Debug.Log("🧹 [VictoryPodiumManager] Passo 1: Limpando modelos existentes...");
        if (existingModels != null)
        {
            for (int i = 0; i < existingModels.Length; i++)
            {
                if (existingModels[i] != null)
                {
                    existingModels[i].SetActive(false);
                    
                    VictoryDisplay victoryDisplay = existingModels[i].GetComponentInChildren<VictoryDisplay>();
                    if (victoryDisplay != null)
                    {
                        victoryDisplay.HideDisplay();
                    }
                    
                    Debug.Log($"   → Modelo posição {i + 1} desativado");
                }
                else
                {
                    Debug.LogWarning($"⚠️ [VictoryPodiumManager] existingModels[{i}] é NULL! Configure no Inspector.");
                }
            }
        }
        

        Debug.Log("🎨 [VictoryPodiumManager] Passo 2: Aplicando dados aos modelos...");
        int appliedCount = 0;
        
        for (int i = 0; i < 4; i++)
        {
            int position = i + 1; 
            VictoryPlayerData playerData = rankingData.GetPlayerAtPosition(position);
            

            if (i < existingModels.Length && existingModels[i] != null)
            {
                if (IsValidPlayer(playerData))
                {
                    Debug.Log($"✅ [VictoryPodiumManager] Aplicando jogador na posição {position}:");
                    Debug.Log($"   → Nome: {playerData.playerName}");
                    Debug.Log($"   → SteamID: {playerData.steamId}");
                    Debug.Log($"   → Score: {playerData.finalScore}");
                    
                    ApplyPlayerInfoToModel(existingModels[i], playerData, position);
                    appliedCount++;
                }
                else
                {
                    DeactivateModelAndDisplay(existingModels[i], position);
                    
                    if (playerData != null)
                    {
                        Debug.Log($"⚠️ [VictoryPodiumManager] Jogador inválido na posição {position} (SteamID: {playerData.steamId}, Nome: '{playerData.playerName}')");
                    }
                    else
                    {
                        Debug.Log($"ℹ️ [VictoryPodiumManager] Nenhum jogador na posição {position}");
                    }
                }
            }
            else
            {
                Debug.LogError($"❌ [VictoryPodiumManager] Modelo da posição {position} não configurado ou é NULL!");
            }
        }
        
        int activeCount = CountActiveModels();
        Debug.Log($"✅ [VictoryPodiumManager] CreatePodium CONCLUÍDO");
        Debug.Log($"   → Jogadores aplicados: {appliedCount}");
        Debug.Log($"   → Modelos ativos: {activeCount}/4");
    }
    

    private bool IsValidPlayer(VictoryPlayerData playerData)
    {
        if (playerData == null)
            return false;
        
        bool hasValidSteamId = playerData.steamId != 0;
        bool hasValidName = !string.IsNullOrEmpty(playerData.playerName) && !string.IsNullOrWhiteSpace(playerData.playerName);
        
        return hasValidSteamId && hasValidName;
    }
    

    private int CountValidPlayers(VictoryRankingData rankingData)
    {
        if (rankingData == null || rankingData.rankedPlayers == null)
            return 0;
        
        int count = 0;
        foreach (var player in rankingData.rankedPlayers)
        {
            if (IsValidPlayer(player))
                count++;
        }
        return count;
    }
    

    private int CountActiveModels()
    {
        if (existingModels == null)
            return 0;
        
        int count = 0;
        foreach (var model in existingModels)
        {
            if (model != null && model.activeSelf)
                count++;
        }
        return count;
    }
    

    private void ApplyPlayerInfoToModel(GameObject model, VictoryPlayerData playerData, int position)
    {
        Debug.Log($" [VictoryPodiumManager] ApplyPlayerInfoToModel - Posição {position}");
        
        // Validações
        if (model == null)
        {
            Debug.LogError($"❌ [VictoryPodiumManager] Modelo é NULL na posição {position}!");
            return;
        }
        
        if (playerData == null)
        {
            Debug.LogError($"❌ [VictoryPodiumManager] PlayerData é NULL na posição {position}!");
            return;
        }
        
        Debug.Log($"📋 [VictoryPodiumManager] Aplicando dados do jogador:");
        Debug.Log($"   → Nome: {playerData.playerName}");
        Debug.Log($"   → SteamID: {playerData.steamId}");
        Debug.Log($"   → Score: {playerData.finalScore}");
        Debug.Log($"   → Cor: {playerData.playerColor}");
        
        bool wasActive = model.activeSelf;
        model.SetActive(true);
        Debug.Log($"✅ [VictoryPodiumManager] Modelo ativado (estava: {wasActive}, agora: {model.activeSelf})");
        
        VictoryDisplay victoryDisplay = model.GetComponentInChildren<VictoryDisplay>();
        if (victoryDisplay != null)
        {

            victoryDisplay.ShowDisplay(playerData);
            
            Debug.Log($"✅ [VictoryPodiumManager] VictoryDisplay atualizado:");
            Debug.Log($"   → Nome exibido: {playerData.playerName}");
            Debug.Log($"   → Score exibido: {playerData.finalScore}");
            Debug.Log($"   → Cor exibida: {playerData.playerColor}");
        }
        else
        {
            Debug.LogWarning($"⚠️ [VictoryPodiumManager] VictoryDisplay não encontrado em '{model.name}'");
            Debug.LogWarning("   → A UI não será atualizada para esta posição");
        }
        

        Debug.Log($"🎨 [VictoryPodiumManager] Aplicando customização...");
        ApplyCustomization(model, playerData);
        
        if (applyPlayerColor)
        {
            Debug.Log($"🎨 [VictoryPodiumManager] Aplicando cor do jogador...");
            ApplyPlayerColor(model, playerData);
        }
        else
        {
            Debug.Log($"ℹ️ [VictoryPodiumManager] Aplicação de cor desabilitada (applyPlayerColor = false)");
        }
        
        Debug.Log($"✅ [VictoryPodiumManager] Todas as informações aplicadas com sucesso à posição {position}");

    }
    

    private void DeactivateModelAndDisplay(GameObject model, int position)
    {
        if (model == null)
            return;
        
        VictoryDisplay victoryDisplay = model.GetComponentInChildren<VictoryDisplay>();
        if (victoryDisplay != null)
        {
            victoryDisplay.HideDisplay();
        }
        
        model.SetActive(false);
    }
    

    private void ApplyCustomization(GameObject mannequin, VictoryPlayerData playerData)
    {
        Debug.Log($"🎨 [VictoryPodiumManager] ApplyCustomization para {playerData?.playerName ?? "NULL"}");
        
        if (mannequin == null)
        {
            Debug.LogError("❌ [VictoryPodiumManager] mannequin é NULL! Não é possível aplicar customização.");
            return;
        }
        
        if (playerData == null)
        {
            Debug.LogError("❌ [VictoryPodiumManager] playerData é NULL! Não é possível aplicar customização.");
            return;
        }
        
        CustomizationApplier customizationApplier = mannequin.GetComponentInChildren<CustomizationApplier>();
        
        if (customizationApplier == null)
        {
            Debug.LogError($"❌ [VictoryPodiumManager] CustomizationApplier NÃO encontrado no manequim '{mannequin.name}'!");
            Debug.LogError("   → Adicione o componente CustomizationApplier ao manequim no Inspector.");
            Debug.LogError("   → Configure os pontos de anexo (hatAttachPoint, glassesAttachPoint, shirtAttachPoint).");
            return;
        }
        
        Debug.Log($"✅ [VictoryPodiumManager] CustomizationApplier encontrado em '{mannequin.name}'");
        
        customizationApplier.SetVictoryPodiumMode(true);
        
        customizationApplier.ClearCustomization();
        Debug.Log("🧹 [VictoryPodiumManager] Customização anterior limpa");
        
        if (playerData.customization == null)
        {
            Debug.LogWarning($"⚠️ [VictoryPodiumManager] playerData.customization é NULL para {playerData.playerName}");
            Debug.LogWarning("   → Jogador aparecerá sem acessórios no pódio");
            Debug.LogWarning("   → Isso pode ser normal se o jogador não customizou nada");
            return;
        }
        
        try
        {
            customizationApplier.ApplyCustomization(playerData.customization);
            
            Debug.Log($"✅ [VictoryPodiumManager] Customização aplicada com sucesso para {playerData.playerName}:");
            Debug.Log($"   → Hat: {playerData.customization.hatIndex}");
            Debug.Log($"   → Glasses: {playerData.customization.glassesIndex}");
            Debug.Log($"   → Shirt: {playerData.customization.shirtIndex}");
            
            bool hasAnyCustomization = playerData.customization.hatIndex >= 0 ||
                                       playerData.customization.glassesIndex >= 0 ||
                                       playerData.customization.shirtIndex >= 0;
            
            if (!hasAnyCustomization)
            {
                Debug.Log($"ℹ️ [VictoryPodiumManager] {playerData.playerName} não tem nenhum item de customização (todos -1)");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [VictoryPodiumManager] EXCEÇÃO ao aplicar customização:");
            Debug.LogError($"   → Mensagem: {e.Message}");
            Debug.LogError($"   → Stack Trace: {e.StackTrace}");
        }
    }
    

    private void ApplyPlayerColor(GameObject mannequin, VictoryPlayerData playerData)
    {
        if (mannequin == null || playerData == null)
        {
            Debug.LogWarning("⚠️ [VictoryPodiumManager] ApplyPlayerColor: mannequin ou playerData é NULL");
            return;
        }

        Color playerColor = playerData.playerColor;
        Debug.Log($"🎨 [VictoryPodiumManager] ApplyPlayerColor: Aplicando cor {playerColor} ao manequim");
        
        int positionIndex = System.Array.IndexOf(existingModels, mannequin);
        if (positionIndex == -1)
        {
            Debug.LogWarning($"⚠️ [VictoryPodiumManager] Manequim não encontrado no array existingModels");
            positionIndex = 0;
        }
        
        RenderMonkey[] renderMonkeys = GetRenderMonkeysForPosition(positionIndex);
        
        if (renderMonkeys != null && renderMonkeys.Length > 0)
        {
            Debug.Log($"✅ [VictoryPodiumManager] Usando RenderMonkeys configurados para posição {positionIndex + 1} ({renderMonkeys.Length} renderers)");
            ApplyColorUsingRenderMonkeys(renderMonkeys, playerColor, playerData.playerColorIndex);
        }
        else
        {
            Debug.LogWarning($"⚠️ [VictoryPodiumManager] RenderMonkeys não configurados para posição {positionIndex + 1}. Configure no Inspector.");
        }
    }
    

    private RenderMonkey[] GetRenderMonkeysForPosition(int positionIndex)
    {
        switch (positionIndex)
        {
            case 0: return position1RenderMonkeys;
            case 1: return position2RenderMonkeys;
            case 2: return position3RenderMonkeys;
            case 3: return position4RenderMonkeys;
            default: return null;
        }
    }


    private void ApplyColorUsingRenderMonkeys(RenderMonkey[] renderMonkeys, Color playerColor, int colorIndex)
    {
        foreach (var renderMonkey in renderMonkeys)
        {
            if (renderMonkey == null || renderMonkey.renderer == null)
            {
                Debug.LogWarning("⚠️ [VictoryPodiumManager] RenderMonkey ou Renderer é NULL - pulando");
                continue;
            }
            
            try
            {
                renderMonkey.ApplyRenderMonkey(playerColor);
                Debug.Log($"✅ [VictoryPodiumManager] Cor {playerColor} aplicada ao renderer '{renderMonkey.renderer.name}[{renderMonkey.index}]'");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [VictoryPodiumManager] Erro ao aplicar cor ao RenderMonkey: {e.Message}");
            }
        }
    }
    

    public void CleanupAllMannequins()
    {
        if (existingModels != null)
        {
            for (int i = 0; i < existingModels.Length; i++)
            {
                if (existingModels[i] != null)
                {
                    DeactivateModelAndDisplay(existingModels[i], i + 1);
                }
            }
        }
    }
    
    public void RefreshPodium()
    {
        CreatePodium();
    }
    
   
}

