using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour
{
    private static PlayerManager _instance;
    public static PlayerManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<PlayerManager>();
            return _instance;
        }
    }

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private bool _randomizeSpawnPoints = true;
    
    [Header("Hit Kill Settings")]
    [SerializeField] private bool _hitKillEnabled = true;
    [SerializeField] private float _respawnDelay = 3f;
    
    [Header("Debug Controls")]
    [SerializeField] private bool _killAllPlayers = false;
    [SerializeField] private int _forceSpawnPointIndex = -1; // -1 = usar regras normais
    
    [Header("Player Search")]
    [SerializeField] private bool _autoSearchPlayers = true;
    [SerializeField] private float _searchInterval = 2f;
    private float _searchTimer = 0f;
    
    // Classe para representar cada jogador na lista
    [Serializable]
    public class PlayerEntry
    {
        public PlayerScript player;
        public bool killPlayer;
         public string playerName;
         public uint netId;
         public bool isActive;
         public Vector3 lastSpawnPosition;
    }
    
    // Lista de jogadores com controle individual
    [Header("Player List")]
    public List<PlayerEntry> playerList = new List<PlayerEntry>();
    
    // Lista de jogadores ativos (interna)
    private List<PlayerScript> _activePlayers = new List<PlayerScript>();
    
    // Jogadores aguardando respawn
    private Dictionary<uint, float> _respawnTimers = new Dictionary<uint, float>();
    
    // Armazenar posições de respawn para cada jogador
    private Dictionary<uint, Vector3> _respawnPositions = new Dictionary<uint, Vector3>();
    private Dictionary<uint, Quaternion> _respawnRotations = new Dictionary<uint, Quaternion>();
    
    // Eventos
    public event Action<PlayerScript> OnPlayerRegistered;
    public event Action<PlayerScript> OnPlayerUnregistered;
    public event Action<PlayerScript, PlayerScript> OnPlayerKilled;
    public event Action<PlayerScript, Transform> OnPlayerRespawned;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Update()
    {
        if (!isServer) return;
        
        // Busca automática de jogadores
        if (_autoSearchPlayers)
        {
            _searchTimer -= Time.deltaTime;
            if (_searchTimer <= 0)
            {
                SearchForPlayers();
                _searchTimer = _searchInterval;
            }
        }
        
        // Debug: Matar todos os jogadores quando o bool é ativado
        if (_killAllPlayers)
        {
            _killAllPlayers = false; // Reset automaticamente
            KillAllPlayers();
        }
        
        // Verificar os botões de kill na lista de jogadores
        CheckKillButtons();
        
        // Processar timers de respawn
        ProcessRespawnTimers();
    }
    
    // Buscar jogadores na cena
    [Server]
    private void SearchForPlayers()
    {
        // Encontrar todos os PlayerScript na cena
        PlayerScript[] foundPlayers = FindObjectsOfType<PlayerScript>();
        
        foreach (var player in foundPlayers)
        {
            if (player != null && !_activePlayers.Contains(player))
            {
                RegisterPlayer(player);
                Debug.Log($"Auto-found player: {player.name}");
            }
        }
        
        // Limpar jogadores que não existem mais
        List<PlayerScript> playersToRemove = new List<PlayerScript>();
        foreach (var player in _activePlayers)
        {
            if (player == null)
            {
                playersToRemove.Add(player);
            }
        }
        
        foreach (var player in playersToRemove)
        {
            _activePlayers.Remove(player);
        }
        
        // Limpar entradas nulas na lista
        playerList.RemoveAll(entry => entry.player == null);
        
        Debug.Log($"Player search complete. Found {foundPlayers.Length} players, active list has {_activePlayers.Count} players");
    }
    
    // Processar timers de respawn - VERSÃO CORRIGIDA
    private void ProcessRespawnTimers()
    {
        // Criar uma cópia das entradas do dicionário para iterar com segurança
        var timerEntries = new Dictionary<uint, float>(_respawnTimers);
        
        foreach (var kvp in timerEntries)
        {
            uint playerId = kvp.Key;
            float timer = kvp.Value - Time.deltaTime;
            
            if (timer <= 0)
            {
                // Tempo acabou, respawnar o jogador
                RespawnPlayer(playerId);
                _respawnTimers.Remove(playerId);
            }
            else
            {
                // Atualizar o timer
                _respawnTimers[playerId] = timer;
            }
        }
    }
    
    // Verificar e processar os botões de kill na lista
    [Server]
    private void CheckKillButtons()
    {
        // Criar uma cópia da lista para evitar problemas de modificação durante iteração
        List<PlayerEntry> entries = new List<PlayerEntry>(playerList);
        
        foreach (var entry in entries)
        {
            if (entry.killPlayer && entry.player != null)
            {
                entry.killPlayer = false; // Reset automaticamente
                KillPlayer(entry.player);
                Debug.Log($"Killed player {entry.playerName} via UI button");
            }
        }
    }
    
    // Registrar um jogador no sistema
    [Server]
    public void RegisterPlayer(PlayerScript player)
    {
        if (player == null)
        {
            Debug.LogError("Attempted to register null player");
            return;
        }
        
        if (!_activePlayers.Contains(player))
        {
            _activePlayers.Add(player);
            Debug.Log($"Player registered: {player.name} (NetID: {player.netId})");
            
            // Obter ponto de spawn para o jogador
            Transform spawnPoint = GetSpawnPointForPlayer();
            Vector3 spawnPosition = spawnPoint.position;
            Quaternion spawnRotation = spawnPoint.rotation;
            
            // Armazenar posição de spawn para uso futuro
            _respawnPositions[player.netId] = spawnPosition;
            _respawnRotations[player.netId] = spawnRotation;
            
            // Adicionar à lista de controle
            bool found = false;
            foreach (var entry in playerList)
            {
                if (entry.player == player)
                {
                    found = true;
                    entry.isActive = true;
                    entry.playerName = player.name;
                    entry.netId = player.netId;
                    entry.lastSpawnPosition = spawnPosition;
                    break;
                }
            }
            
            if (!found)
            {
                PlayerEntry newEntry = new PlayerEntry
                {
                    player = player,
                    killPlayer = false,
                    playerName = player.name,
                    netId = player.netId,
                    isActive = true,
                    lastSpawnPosition = spawnPosition
                };
                
                playerList.Add(newEntry);
                Debug.Log($"Added player {player.name} to player list during registration");
            }
            
            // Posicionar o jogador no ponto de spawn
            player.transform.position = spawnPosition;
            player.transform.rotation = spawnRotation;
            player.RpcTeleport(spawnPosition, spawnRotation);
            
            // Notificar clientes
            RpcPlayerRegistered(player.netId);
            
            OnPlayerRegistered?.Invoke(player);
        }
    }
    
    // Remover um jogador do sistema
    [Server]
    public void UnregisterPlayer(PlayerScript player)
    {
        if (player == null) return;
        
        if (_activePlayers.Contains(player))
        {
            _activePlayers.Remove(player);
            
            // Remover posições de respawn armazenadas
            if (_respawnPositions.ContainsKey(player.netId))
            {
                _respawnPositions.Remove(player.netId);
            }
            
            if (_respawnRotations.ContainsKey(player.netId))
            {
                _respawnRotations.Remove(player.netId);
            }
            
            // Atualizar lista de controle
            foreach (var entry in playerList)
            {
                if (entry.player == player)
                {
                    entry.isActive = false;
                    break;
                }
            }
            
            // Notificar clientes
            RpcPlayerUnregistered(player.netId);
            
            OnPlayerUnregistered?.Invoke(player);
        }
    }
    
    // Matar todos os jogadores (útil para debug ou eventos de jogo)
    [Server]
    public void KillAllPlayers()
    {
        // Criar uma cópia da lista para evitar problemas de modificação durante iteração
        List<PlayerScript> players = new List<PlayerScript>(_activePlayers);
        
        foreach (var player in players)
        {
            if (player != null)
            {
                KillPlayer(player);
            }
        }
        
        Debug.Log("Killed all players");
    }
    
    // Matar um jogador e iniciar o timer de respawn
    [Server]
    public void KillPlayer(PlayerScript victim, PlayerScript killer = null)
    {
        if (victim == null) return;
        
        if (!_activePlayers.Contains(victim))
            return;
        
        if (!_hitKillEnabled && killer != null)
            return; // Hit kill desativado, mas permite suicídio
        
        // Desativar o jogador temporariamente
        victim.gameObject.SetActive(false);
        
        // Atualizar lista de controle
        foreach (var entry in playerList)
        {
            if (entry.player == victim)
            {
                entry.isActive = false;
                break;
            }
        }
        
        // Iniciar timer de respawn
        _respawnTimers[victim.netId] = _respawnDelay;
        
        // Notificar clientes
        RpcPlayerKilled(victim.netId, killer != null ? killer.netId : 0);
        
        OnPlayerKilled?.Invoke(victim, killer);
    }
    
    // Respawnar um jogador
    [Server]
    private void RespawnPlayer(uint playerId)
    {
        PlayerScript player = GetPlayerById(playerId);
        if (player == null)
            return;
        
        // Obter posição de respawn armazenada ou gerar uma nova
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        
        if (_respawnPositions.ContainsKey(playerId) && _respawnRotations.ContainsKey(playerId))
        {
            spawnPosition = _respawnPositions[playerId];
            spawnRotation = _respawnRotations[playerId];
        }
        else
        {
            // Se não tiver posição armazenada, obter um novo ponto de spawn
            Transform spawnPoint = GetSpawnPointForPlayer();
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
            
            // Armazenar para uso futuro
            _respawnPositions[playerId] = spawnPosition;
            _respawnRotations[playerId] = spawnRotation;
        }
        
        // Reativar o jogador
        player.gameObject.SetActive(true);
        
        // Atualizar lista de controle
        foreach (var entry in playerList)
        {
            if (entry.player == player)
            {
                entry.isActive = true;
                entry.lastSpawnPosition = spawnPosition;
                break;
            }
        }
        
        // Teleportar o jogador para o ponto de spawn
        player.transform.position = spawnPosition;
        player.transform.rotation = spawnRotation;
        player.RpcTeleport(spawnPosition, spawnRotation);
        
        Debug.Log($"Player {playerId} respawned at {spawnPosition}");
        
        // Notificar clientes
        RpcPlayerRespawned(playerId, spawnPosition, spawnRotation);
        
        OnPlayerRespawned?.Invoke(player, null);
    }
    
    // Obter um ponto de spawn apropriado para o jogador
    [Server]
    private Transform GetSpawnPointForPlayer()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points defined!");
            return transform; // Usar a posição do manager como fallback
        }
        
        // Se um spawn point específico foi forçado via inspetor
        if (_forceSpawnPointIndex >= 0 && _forceSpawnPointIndex < _spawnPoints.Length)
        {
            return _spawnPoints[_forceSpawnPointIndex];
        }
        
        // Spawn padrão
        if (_randomizeSpawnPoints)
        {
            int randomIndex = UnityEngine.Random.Range(0, _spawnPoints.Length);
            return _spawnPoints[randomIndex];
        }
        else
        {
            return _spawnPoints[0];
        }
    }
    
    // Obter um jogador pelo ID
    private PlayerScript GetPlayerById(uint playerId)
    {
        foreach (var player in _activePlayers)
        {
            if (player != null && player.netId == playerId)
            {
                return player;
            }
        }
        
        return null;
    }
    
    // Obter todos os jogadores ativos
    public List<PlayerScript> GetAllPlayers()
    {
        return new List<PlayerScript>(_activePlayers);
    }
    
    // Forçar busca de jogadores (pode ser chamado por botão no Inspector)
    [ContextMenu("Force Player Search")]
    public void ForcePlayerSearch()
    {
        SearchForPlayers();
    }
    
    // Habilitar/desabilitar hit kill
    [Server]
    public void SetHitKillEnabled(bool enabled)
    {
        _hitKillEnabled = enabled;
        RpcHitKillStateChanged(enabled);
    }
    
    // Definir delay de respawn
    [Server]
    public void SetRespawnDelay(float delay)
    {
        _respawnDelay = Mathf.Max(0, delay);
    }
    
    #region ClientRpc Methods
    
    [ClientRpc]
    private void RpcPlayerRegistered(uint playerId)
    {
        Debug.Log($"Player registered: {playerId}");
    }
    
    [ClientRpc]
    private void RpcPlayerUnregistered(uint playerId)
    {
        Debug.Log($"Player unregistered: {playerId}");
    }
    
    [ClientRpc]
    private void RpcPlayerKilled(uint victimId, uint killerId)
    {
        Debug.Log($"Player {victimId} killed by {killerId}");
        
        // Efeitos visuais de morte
        PlayerScript victim = GetPlayerById(victimId);
        if (victim != null && !isServer) // Servidor já desativou o objeto
        {
            victim.gameObject.SetActive(false);
        }
    }
    
    [ClientRpc]
    private void RpcPlayerRespawned(uint playerId, Vector3 position, Quaternion rotation)
    {
        Debug.Log($"Player {playerId} respawned at {position}");
        
        // Reativar jogador
        PlayerScript player = GetPlayerById(playerId);
        if (player != null && !isServer) // Servidor já fez isso
        {
            player.gameObject.SetActive(true);
            
            // Garantir que a posição seja correta no cliente também
            if (!player.isLocalPlayer) // Não teleportar o jogador local, o RPC já fará isso
            {
                player.transform.position = position;
                player.transform.rotation = rotation;
            }
        }
    }
    
    [ClientRpc]
    private void RpcHitKillStateChanged(bool enabled)
    {
        Debug.Log($"Hit Kill {(enabled ? "enabled" : "disabled")}");
    }
    
    #endregion
}