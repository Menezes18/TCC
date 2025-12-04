using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;


public class VictoryDataManager : NetworkBehaviour
{
    public static VictoryDataManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private Database database;
    
    [Header("Victory Data")]
    [SyncVar(hook = nameof(OnWinnerDataChanged))]
    private string winnerDataJson = "";
    
    [SyncVar(hook = nameof(OnRankingDataChanged))]
    private string rankingDataJson = "";
    
    private VictoryPlayerData _cachedWinnerData;
    private VictoryRankingData _cachedRankingData;
    
    private const string VICTORY_DATA_PREFS_KEY = "VictoryPlayerData";
    private const string VICTORY_RANKING_PREFS_KEY = "VictoryRankingData";
    

    public void ClearVictoryData()
    {
        if (PlayerPrefs.HasKey(VICTORY_DATA_PREFS_KEY))
        {
            PlayerPrefs.DeleteKey(VICTORY_DATA_PREFS_KEY);
            Debug.Log("🧹 [VictoryDataManager] Dados antigos do vencedor limpos de PlayerPrefs");
        }
        
        if (PlayerPrefs.HasKey(VICTORY_RANKING_PREFS_KEY))
        {
            PlayerPrefs.DeleteKey(VICTORY_RANKING_PREFS_KEY);
            Debug.Log("🧹 [VictoryDataManager] Dados antigos do ranking limpos de PlayerPrefs");
        }
        
        PlayerPrefs.Save();
        

        _cachedWinnerData = null;
        _cachedRankingData = null;
        
        Debug.Log("✅ [VictoryDataManager] Todos os dados de vitória limpos (PlayerPrefs + cache)");
    }
    
    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;

            Debug.Log("✅ [VictoryDataManager] Instância inicializada (Scene Object)");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"⚠️ [VictoryDataManager] Instância duplicada detectada e destruída (netId: {netId})");
            Destroy(gameObject);
            return;
        }
        

        if (database == null)
        {
            database = Resources.Load<Database>("Database");
            if (database == null)
            {
                Debug.LogError("❌ [VictoryDataManager] Database não encontrado em Resources! CRÍTICO para funcionamento do sistema.");
            }
            else
            {
                Debug.Log("✅ [VictoryDataManager] Database carregado de Resources");
            }
        }
    }
    

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"🌐 [VictoryDataManager] OnStartServer - Servidor inicializado (netId: {netId})");
        Debug.Log($"   → isServer: {isServer}");
        Debug.Log($"   → isClient: {isClient}");
        Debug.Log($"   → NetworkIdentity spawnado: {GetComponent<NetworkIdentity>()?.isServer ?? false}");
    }
    

    public bool IsProperlySpawned()
    {
        var netIdentity = GetComponent<NetworkIdentity>();
        if (netIdentity == null)
        {
            Debug.LogError("❌ [VictoryDataManager] NetworkIdentity não encontrado!");
            return false;
        }
        
        if (!NetworkServer.active)
        {
            Debug.LogWarning("⚠️ [VictoryDataManager] NetworkServer não está ativo!");
            return false;
        }
        
        if (!netIdentity.isServer)
        {
            Debug.LogWarning("⚠️ [VictoryDataManager] NetworkIdentity.isServer é FALSE!");
            return false;
        }
        
        return true;
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();

        Debug.Log($"🌐 [VictoryDataManager] OnStartClient CHAMADO");
        Debug.Log($"   → isServer: {isServer}");
        Debug.Log($"   → isClient: {isClient}");
        Debug.Log($"   → isLocalPlayer: {isLocalPlayer}");
        Debug.Log($"   → netId: {netId}");
        
        var netIdentity = GetComponent<NetworkIdentity>();
        if (netIdentity != null)
        {
            Debug.Log($"   → NetworkIdentity.isServer: {netIdentity.isServer}");
            Debug.Log($"   → NetworkIdentity.isClient: {netIdentity.isClient}");
            Debug.Log($"   → NetworkIdentity.connectionToClient: {netIdentity.connectionToClient != null}");
        }

        

        if (!string.IsNullOrEmpty(rankingDataJson))
        {
            Debug.Log($"📦 [VictoryDataManager] Dados do ranking JÁ PRESENTES no SyncVar!");
            Debug.Log($"   → Tamanho do JSON: {rankingDataJson.Length} caracteres");
            Debug.Log($"   → Primeiros 100 chars: {rankingDataJson.Substring(0, Mathf.Min(100, rankingDataJson.Length))}");
            Debug.Log($"📡 [VictoryDataManager] Processando dados existentes no cliente...");
            
            StartCoroutine(ProcessRankingDataDelayed(rankingDataJson));
        }
        else
        {
            Debug.LogWarning("⚠️ [VictoryDataManager] SyncVar rankingDataJson está VAZIO no OnStartClient!");
            Debug.LogWarning("   → Cliente vai aguardar sincronização do servidor");
            Debug.LogWarning("   → Isso pode indicar que:");
            Debug.LogWarning("      1. Servidor ainda não chamou DetectAndSyncWinner()");
            Debug.LogWarning("      2. Cliente conectou ANTES dos dados serem gerados");
            Debug.LogWarning("      3. SyncVar não está sincronizando (problema de NetworkIdentity)");
            
            // Tentar processar periodicamente caso os dados cheguem depois
            StartCoroutine(WaitForRankingData());
        }
    }
    

    private System.Collections.IEnumerator WaitForRankingData()
    {
        int attempts = 0;
        const int maxAttempts = 20; 
        
        while (attempts < maxAttempts && string.IsNullOrEmpty(rankingDataJson))
        {
            yield return new WaitForSeconds(0.5f);
            attempts++;
            
            if (!string.IsNullOrEmpty(rankingDataJson))
            {
                Debug.Log($"📦 [VictoryDataManager] Dados do ranking recebidos após {attempts * 0.5f} segundos - processando");
                StartCoroutine(ProcessRankingDataDelayed(rankingDataJson));
                yield break;
            }
        }
        
        if (string.IsNullOrEmpty(rankingDataJson))
        {
            Debug.LogWarning($"⚠️ [VictoryDataManager] Dados do ranking não recebidos após {maxAttempts * 0.5f} segundos");
        }
    }
    

    private IEnumerator ProcessRankingDataDelayed(string json)
    {
        yield return new WaitForSeconds(0.1f);
        OnRankingDataChanged("", json);
    }
    
    private void Start()
    {

        SceneManager.sceneLoaded += OnSceneLoaded;
        
        CheckIfInVictoryScene();
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        

        if (Instance == this)
        {
            Instance = null;
            Debug.Log("🧹 [VictoryDataManager] Instância destruída");
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckIfInVictoryScene();
    }
    
    private void CheckIfInVictoryScene()
    {
        if (isServer && IsVictoryScene(SceneManager.GetActiveScene().name))
        {
            StartCoroutine(DetectWinnerDelayed());
        }
    }
    
    private bool IsVictoryScene(string sceneName)
    {

        string lowerSceneName = sceneName.ToLower();
        

        if (lowerSceneName.Contains("Vitoria") )
        {
            return true;
        }
        

        return false;
    }
    
    private IEnumerator DetectWinnerDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        
        DetectAndSyncWinner();
    }
    

    [Server]
    public void DetectAndSyncWinner()
    {

        Debug.Log("🏆 [VictoryDataManager] DetectAndSyncWinner INICIADO");

        
        if (MyNetworkManager.manager == null)
        {
            Debug.LogError("❌ [VictoryDataManager] MyNetworkManager.manager é null! Não é possível detectar vencedor.");
            return;
        }
        
        var scoreboard = MyNetworkManager.manager.scoreboard;
        if (scoreboard == null || scoreboard.players == null || scoreboard.players.Count == 0)
        {
            Debug.LogWarning("⚠️ [VictoryDataManager] Scoreboard vazio ou não inicializado. Aguardando dados...");
            return;
        }
        
        Debug.Log($"📊 [VictoryDataManager] Scoreboard contém {scoreboard.players.Count} jogadores (BRUTO)");
        
        var validPlayers = scoreboard.players
            .Where(p => p.steamID != 0 && !string.IsNullOrWhiteSpace(p.playerName))
            .ToList();
        
        Debug.Log($"📊 [VictoryDataManager] Jogadores VÁLIDOS após filtro: {validPlayers.Count}");
        
        int filteredCount = scoreboard.players.Count - validPlayers.Count;
        if (filteredCount > 0)
        {
            Debug.LogWarning($"⚠️ [VictoryDataManager] {filteredCount} jogador(es) INVÁLIDO(S) removido(s) do scoreboard:");
            foreach (var invalid in scoreboard.players.Where(p => p.steamID == 0 || string.IsNullOrWhiteSpace(p.playerName)))
            {
                Debug.LogWarning($"   → SteamID: {invalid.steamID}, Nome: '{invalid.playerName}', Pontos: {invalid.points}");
            }
        }
        

        var sortedPlayers = validPlayers
            .OrderByDescending(p => p.points)
            .ThenBy(p => p.steamID)
            .ToList();
        
        if (sortedPlayers.Count == 0)
        {
            Debug.LogWarning("⚠️ [VictoryDataManager] Nenhum jogador encontrado após ordenação");
            return;
        }
        
        Debug.Log("📋 [VictoryDataManager] Ranking calculado (ordem determinística):");
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            Debug.Log($"  {i + 1}º - {sortedPlayers[i].playerName} | {sortedPlayers[i].points} pontos | SteamID: {sortedPlayers[i].steamID}");
        }
        
        VictoryPlayerData[] rankedPlayers = new VictoryPlayerData[4];
        
        HashSet<ulong> processedSteamIds = new HashSet<ulong>();
        
        int validPlayersCount = 0;
        for (int i = 0; i < sortedPlayers.Count && validPlayersCount < 4; i++)
        {
            var dataPlayer = sortedPlayers[i];
            

            if (dataPlayer.steamID == 0 || string.IsNullOrWhiteSpace(dataPlayer.playerName))
            {
                Debug.LogWarning($"⚠️ [VictoryDataManager] Jogador INVÁLIDO detectado no loop (SteamID: {dataPlayer.steamID}, Nome: '{dataPlayer.playerName}') - PULANDO");
                continue;
            }
            
            if (processedSteamIds.Contains(dataPlayer.steamID))
            {
                Debug.LogWarning($"⚠️ [VictoryDataManager] Jogador DUPLICADO detectado no scoreboard: {dataPlayer.playerName} (SteamID: {dataPlayer.steamID}) - PULANDO");
                continue;
            }
            
            processedSteamIds.Add(dataPlayer.steamID);
            
            PlayerData playerData = FindPlayerDataBySteamId(dataPlayer.steamID);
            
            if (playerData == null)
            {
                Debug.LogWarning($"⚠️ [VictoryDataManager] PlayerData não encontrado para {dataPlayer.playerName} (SteamID: {dataPlayer.steamID})");
                Debug.LogWarning($"   → Customização usará valores padrão (-1, -1, -1)");
            }
            else
            {
                Debug.Log($"✅ [VictoryDataManager] PlayerData encontrado para {dataPlayer.playerName}");
                Debug.Log($"   → Customização: Hat={playerData.hatIndex}, Glasses={playerData.glassesIndex}, Shirt={playerData.shirtIndex}");
            }
            
            rankedPlayers[validPlayersCount] = CreateWinnerData(dataPlayer, playerData);
            validPlayersCount++;
            
            Debug.Log($"🎯 [VictoryDataManager] Posição {validPlayersCount}: {dataPlayer.playerName} ({dataPlayer.points} pts)");
        }
        
        Debug.Log($"✅ [VictoryDataManager] Total de {validPlayersCount} jogadores válidos coletados para o pódio");
        

        VictoryRankingData rankingData = new VictoryRankingData(rankedPlayers);

        Debug.Log("📡 [VictoryDataManager] Sincronizando ranking via SyncVar...");
        SyncRankingData(rankingData);
        

        Debug.Log("📡 [VictoryDataManager] Enviando ranking via RPC (fallback)...");
        string rankingJson = JsonUtility.ToJson(rankingData);
        RpcSyncRankingToClients(rankingJson);
        
        if (rankedPlayers[0] != null)
        {
            Debug.Log($"🥇 [VictoryDataManager] Vencedor: {rankedPlayers[0].playerName}");
            SyncWinnerData(rankedPlayers[0]);
            SaveWinnerDataLocal(rankedPlayers[0]);
        }
        
        SaveRankingDataLocal(rankingData);
        
        Debug.Log("✅ [VictoryDataManager] DetectAndSyncWinner CONCLUÍDO");
        Debug.Log($"📦 Ranking completo sincronizado via SyncVar + RPC:\n{rankingData}");
    }
    

    [ClientRpc]
    private void RpcSyncRankingToClients(string rankingJson)
    {

        Debug.Log($"📡 [VictoryDataManager] RpcSyncRankingToClients RECEBIDO");
        Debug.Log($"   → isServer: {isServer}");
        Debug.Log($"   → isClient: {isClient}");
        Debug.Log($"   → JSON length: {rankingJson?.Length ?? 0}");

        
        if (string.IsNullOrEmpty(rankingJson))
        {
            Debug.LogError("❌ [VictoryDataManager] RPC recebeu JSON vazio!");
            return;
        }
        
        try
        {
            var rankingData = JsonUtility.FromJson<VictoryRankingData>(rankingJson);
            
            if (rankingData == null || rankingData.rankedPlayers == null)
            {
                Debug.LogError("❌ [VictoryDataManager] Falha ao deserializar ranking via RPC!");
                return;
            }
            
            Debug.Log($"✅ [VictoryDataManager] Ranking recebido via RPC com sucesso!");
            
            if (database != null)
            {
                foreach (var player in rankingData.rankedPlayers)
                {
                    if (player != null && player.playerColorIndex >= 0)
                    {
                        player.playerColor = database.GetColor(player.playerColorIndex);
                    }
                }
            }
            

            _cachedRankingData = rankingData;
            

            SaveRankingDataLocal(rankingData);
            

            Debug.Log($"📢 [VictoryDataManager] Disparando OnRankingDataReady via RPC");
            OnRankingDataReady?.Invoke(rankingData);
            

            for (int i = 0; i < rankingData.rankedPlayers.Length; i++)
            {
                var player = rankingData.rankedPlayers[i];
                if (player != null && player.steamId != 0)
                {
                    Debug.Log($"  {i + 1}º lugar via RPC: {player.playerName} ({player.finalScore} pts)");
                }
            }
            
            Debug.Log("✅ [VictoryDataManager] RPC processado com sucesso!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [VictoryDataManager] Erro ao processar RPC: {e.Message}");
            Debug.LogError($"   → Stack: {e.StackTrace}");
        }
    }
    

    [Server]
    private PlayerData FindPlayerDataBySteamId(ulong steamId)
    {
        if (MyNetworkManager.manager == null || MyNetworkManager.manager.allClients == null)
        {
            Debug.LogWarning("⚠️ [VictoryDataManager] MyNetworkManager.manager.allClients é null");
            return null;
        }
        
        foreach (var pd in MyNetworkManager.manager.allClients)
        {
            if (pd != null && pd.playerInfo.steamId == steamId)
            {
                return pd;
            }
        }
        
        if (PlayerList.singleton != null && PlayerList.singleton.players != null)
        {
            foreach (var pd in PlayerList.singleton.players)
            {
                if (pd != null && pd.playerInfo.steamId == steamId)
                {
                    Debug.Log($"ℹ️ [VictoryDataManager] PlayerData encontrado via PlayerList.singleton para SteamID: {steamId}");
                    return pd;
                }
            }
        }
        
        return null;
    }
    

    [Server]
    private VictoryPlayerData CreateWinnerData(DataPlayer dataPlayer, PlayerData playerData)
    {
        Color playerColor = Color.white;
        if (database != null && dataPlayer.color >= 0)
        {
            playerColor = database.GetColor(dataPlayer.color);
            Debug.Log($"🎨 [VictoryDataManager] Cor obtida do Database para {dataPlayer.playerName}: {playerColor} (index: {dataPlayer.color})");
        }
        else
        {
            Debug.LogWarning($"⚠️ [VictoryDataManager] Database null ou colorIndex inválido para {dataPlayer.playerName} - usando cor branca");
        }
        
        PlayerCustomizationData customization = new PlayerCustomizationData();
        
        if (playerData != null)
        {
            customization.hatIndex = playerData.hatIndex;
            customization.glassesIndex = playerData.glassesIndex;
            customization.shirtIndex = playerData.shirtIndex;
            
            Debug.Log($"✅ [VictoryDataManager] Customização coletada para {dataPlayer.playerName}:");
            Debug.Log($"   → Hat: {customization.hatIndex}");
            Debug.Log($"   → Glasses: {customization.glassesIndex}");
            Debug.Log($"   → Shirt: {customization.shirtIndex}");
            
            bool hasValidCustomization = customization.hatIndex >= 0 || 
                                          customization.glassesIndex >= 0 || 
                                          customization.shirtIndex >= 0;
            
            if (!hasValidCustomization)
            {
                Debug.LogWarning($"⚠️ [VictoryDataManager] Customização de {dataPlayer.playerName} está vazia (todos -1). Isso é esperado se o jogador não customizou nada.");
            }
        }
        else
        {
            customization.hatIndex = -1;
            customization.glassesIndex = -1;
            customization.shirtIndex = -1;
            
            Debug.LogWarning($"⚠️ [VictoryDataManager] PlayerData é null para {dataPlayer.playerName}");
            Debug.LogWarning($"   → Usando customização padrão: Hat=-1, Glasses=-1, Shirt=-1");
            Debug.LogWarning($"   → Jogador aparecerá sem acessórios no pódio");
        }
        
        var winnerData = new VictoryPlayerData(
            dataPlayer.steamID,
            dataPlayer.playerName,
            dataPlayer.points,
            dataPlayer.color,
            customization
        )
        {
            playerColor = playerColor
        };
        
        Debug.Log($"📦 [VictoryDataManager] VictoryPlayerData criado com sucesso:");
        Debug.Log($"   → Nome: {winnerData.playerName}");
        Debug.Log($"   → SteamID: {winnerData.steamId}");
        Debug.Log($"   → Score: {winnerData.finalScore}");
        Debug.Log($"   → Cor: {winnerData.playerColor}");
        Debug.Log($"   → Customização: {winnerData.customization}");
        
        return winnerData;
    }
    

    [Server]
    private void SyncWinnerData(VictoryPlayerData winnerData)
    {
        if (winnerData == null)
        {
            Debug.LogError("🏆 [VictoryDataManager] Tentativa de sincronizar dados nulos");
            return;
        }
        
        string json = JsonUtility.ToJson(winnerData);
        winnerDataJson = json;
        
        Debug.Log($"🏆 [VictoryDataManager] Dados do vencedor sincronizados: {json}");
    }
    

    [Server]
    private void SyncRankingData(VictoryRankingData rankingData)
    {
        if (rankingData == null)
        {
            Debug.LogError("❌ [VictoryDataManager] Tentativa de sincronizar ranking nulo");
            return;
        }
        
        var netIdentity = GetComponent<NetworkIdentity>();
        if (netIdentity == null)
        {
            Debug.LogError("❌ [VictoryDataManager] NetworkIdentity é NULL! SyncVar NÃO será sincronizado!");
            Debug.LogError("   → Adicione NetworkIdentity ao GameObject manualmente no Inspector!");
            return;
        }
        
        if (!netIdentity.isServer)
        {
            Debug.LogError("❌ [VictoryDataManager] NetworkIdentity.isServer é FALSE! SyncVar NÃO será sincronizado!");
            Debug.LogError("   → O objeto não está spawnado corretamente na rede!");
            return;
        }
        
        Debug.Log("✅ [VictoryDataManager] NetworkIdentity verificado - OK para sincronizar");
        Debug.Log($"   → netId: {netId}");
        Debug.Log($"   → isServer: {isServer}");
        Debug.Log($"   → observers count: {netIdentity.observers?.Count ?? 0}");
        
        string json = JsonUtility.ToJson(rankingData);
        
        Debug.Log($"📦 [VictoryDataManager] JSON serializado - Tamanho: {json.Length} caracteres");
        Debug.Log($"📡 [VictoryDataManager] Atualizando SyncVar rankingDataJson...");
        
        rankingDataJson = json;
        
        Debug.Log($"✅ [VictoryDataManager] SyncVar rankingDataJson atualizado!");
        Debug.Log($"   → Valor atual: {rankingDataJson.Substring(0, Mathf.Min(100, rankingDataJson.Length))}...");
        Debug.Log($"📡 [VictoryDataManager] Mirror vai sincronizar automaticamente para {netIdentity.observers?.Count ?? 0} observers");
    }
    

    private void SaveWinnerDataLocal(VictoryPlayerData winnerData)
    {
        if (winnerData == null) return;
        
        string json = JsonUtility.ToJson(winnerData);
        PlayerPrefs.SetString(VICTORY_DATA_PREFS_KEY, json);
        PlayerPrefs.Save();
        
        Debug.Log($"💾 [VictoryDataManager] Dados do vencedor salvos localmente");
    }
    

    private void SaveRankingDataLocal(VictoryRankingData rankingData)
    {
        if (rankingData == null) return;
        
        string json = JsonUtility.ToJson(rankingData);
        PlayerPrefs.SetString(VICTORY_RANKING_PREFS_KEY, json);
        PlayerPrefs.Save();
        
        Debug.Log($"💾 [VictoryDataManager] Dados do ranking salvos localmente");
    }
    
 
    public VictoryPlayerData LoadWinnerDataLocal()
    {
        if (!PlayerPrefs.HasKey(VICTORY_DATA_PREFS_KEY))
            return null;
        
        string json = PlayerPrefs.GetString(VICTORY_DATA_PREFS_KEY);
        if (string.IsNullOrEmpty(json))
            return null;
        
        try
        {
            var data = JsonUtility.FromJson<VictoryPlayerData>(json);
            Debug.Log($"📖 [VictoryDataManager] Dados do vencedor carregados localmente: {data}");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [VictoryDataManager] Erro ao carregar dados locais: {e.Message}");
            return null;
        }
    }
    
    public VictoryRankingData LoadRankingDataLocal()
    {
        if (!PlayerPrefs.HasKey(VICTORY_RANKING_PREFS_KEY))
            return null;
        
        string json = PlayerPrefs.GetString(VICTORY_RANKING_PREFS_KEY);
        if (string.IsNullOrEmpty(json))
            return null;
        
        try
        {
            var data = JsonUtility.FromJson<VictoryRankingData>(json);
            Debug.Log($"📖 [VictoryDataManager] Dados do ranking carregados localmente: {data}");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [VictoryDataManager] Erro ao carregar dados do ranking: {e.Message}");
            return null;
        }
    }
    private void OnWinnerDataChanged(string oldJson, string newJson)
    {
        if (string.IsNullOrEmpty(newJson))
        {
            _cachedWinnerData = null;
            return;
        }
        
        try
        {
            _cachedWinnerData = JsonUtility.FromJson<VictoryPlayerData>(newJson);
            
            if (_cachedWinnerData != null && database != null && _cachedWinnerData.playerColorIndex >= 0)
            {
                _cachedWinnerData.playerColor = database.GetColor(_cachedWinnerData.playerColorIndex);
            }
            
            Debug.Log($"🏆 [VictoryDataManager] Dados do vencedor recebidos no cliente: {_cachedWinnerData}");
            
            OnWinnerDataReady?.Invoke(_cachedWinnerData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [VictoryDataManager] Erro ao processar dados do vencedor: {e.Message}");
            _cachedWinnerData = null;
        }
    }
    
    private void OnRankingDataChanged(string oldJson, string newJson)
    {
        Debug.Log($"🔄 [VictoryDataManager] OnRankingDataChanged HOOK DISPARADO");
        Debug.Log($"   → isServer: {isServer}");
        Debug.Log($"   → isClient: {isClient}");
        Debug.Log($"   → oldJson length: {oldJson?.Length ?? 0}");
        Debug.Log($"   → newJson length: {newJson?.Length ?? 0}");
        
        if (string.IsNullOrEmpty(newJson))
        {
            Debug.LogWarning("⚠️ [VictoryDataManager] OnRankingDataChanged recebeu JSON vazio ou null");
            _cachedRankingData = null;
            return;
        }
        
        try
        {
            _cachedRankingData = JsonUtility.FromJson<VictoryRankingData>(newJson);
            
            if (_cachedRankingData == null)
            {
                Debug.LogError("❌ [VictoryDataManager] Falha ao deserializar VictoryRankingData do JSON!");
                Debug.LogError($"   → JSON recebido: {newJson}");
                return;
            }
            
            Debug.Log($"✅ [VictoryDataManager] Ranking deserializado com sucesso");
            
            if (_cachedRankingData.rankedPlayers != null && database != null)
            {
                int coloredPlayers = 0;
                foreach (var playerData in _cachedRankingData.rankedPlayers)
                {
                    if (playerData != null && playerData.playerColorIndex >= 0)
                    {
                        playerData.playerColor = database.GetColor(playerData.playerColorIndex);
                        coloredPlayers++;
                    }
                }
                Debug.Log($"🎨 [VictoryDataManager] Cores aplicadas a {coloredPlayers} jogadores do ranking");
            }
            else if (database == null)
            {
                Debug.LogWarning("⚠️ [VictoryDataManager] Database é null - cores não serão aplicadas");
            }
            
            Debug.Log($"📋 [VictoryDataManager] RANKING COMPLETO RECEBIDO NO CLIENTE:");
            if (_cachedRankingData.rankedPlayers != null)
            {
                for (int i = 0; i < _cachedRankingData.rankedPlayers.Length; i++)
                {
                    var player = _cachedRankingData.rankedPlayers[i];
                    if (player != null && player.steamId != 0) // Validar jogador válido
                    {
                        Debug.Log($"  🏅 Posição {i + 1}:");
                        Debug.Log($"     → Nome: {player.playerName}");
                        Debug.Log($"     → SteamID: {player.steamId}");
                        Debug.Log($"     → Score: {player.finalScore}");
                        Debug.Log($"     → Cor: {player.playerColor}");
                        Debug.Log($"     → Hat: {player.customization?.hatIndex ?? -999}");
                        Debug.Log($"     → Glasses: {player.customization?.glassesIndex ?? -999}");
                        Debug.Log($"     → Shirt: {player.customization?.shirtIndex ?? -999}");
                    }
                }
            }
            
            if (OnRankingDataReady != null)
            {
                int listenerCount = OnRankingDataReady.GetInvocationList().Length;
                Debug.Log($"📢 [VictoryDataManager] Disparando evento OnRankingDataReady");
                Debug.Log($"   → Listeners inscritos: {listenerCount}");
                Debug.Log($"   → isServer: {isServer}, isClient: {isClient}");
                
                OnRankingDataReady.Invoke(_cachedRankingData);
                
                Debug.Log($"✅ [VictoryDataManager] Evento OnRankingDataReady disparado com sucesso para {listenerCount} listeners");
            }
            else
            {
                Debug.LogWarning("⚠️ [VictoryDataManager] OnRankingDataReady é NULL - nenhum listener inscrito!");
                Debug.LogWarning("   → VictoryPodiumManager pode não estar inicializado ainda.");
                Debug.LogWarning("   → Os dados estão salvos em cache e podem ser acessados via GetRankingData()");
            }
            
            SaveRankingDataLocal(_cachedRankingData);
            Debug.Log("💾 [VictoryDataManager] Ranking salvo localmente como backup");
            
            if (isServer)
            {
                VictoryPodiumManager podiumManager = FindAnyObjectByType<VictoryPodiumManager>();
                if (podiumManager != null)
                {
                    Debug.Log("📡 [VictoryDataManager] Chamando RpcUpdatePodium no VictoryPodiumManager (redundância)");
                    podiumManager.RpcUpdatePodium();
                }
                else
                {
                    Debug.LogWarning("⚠️ [VictoryDataManager] VictoryPodiumManager não encontrado na cena");
                }
            }
            
            Debug.Log("✅ [VictoryDataManager] OnRankingDataChanged CONCLUÍDO");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [VictoryDataManager] EXCEÇÃO ao processar dados do ranking:");
            Debug.LogError($"   → Mensagem: {e.Message}");
            Debug.LogError($"   → Stack Trace: {e.StackTrace}");
            Debug.LogError($"   → JSON recebido: {newJson}");
            _cachedRankingData = null;
        }
    }
    
    public VictoryPlayerData GetWinnerData()
    {
        if (_cachedWinnerData != null)
            return _cachedWinnerData.Clone();
        
        return LoadWinnerDataLocal();
    }
    

    public VictoryRankingData GetRankingData()
    {
        if (_cachedRankingData != null)
            return _cachedRankingData;
        
        return LoadRankingDataLocal();
    }
    

    public VictoryPlayerData GetPlayerAtPosition(int position)
    {
        var ranking = GetRankingData();
        if (ranking == null)
            return null;
        
        return ranking.GetPlayerAtPosition(position);
    }
    

    public System.Action<VictoryPlayerData> OnWinnerDataReady;
    

    public System.Action<VictoryRankingData> OnRankingDataReady;
    

    [Server]
    public void ForceDetectWinner()
    {
        DetectAndSyncWinner();
    }
    

}


