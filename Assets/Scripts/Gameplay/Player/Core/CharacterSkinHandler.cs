using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSkinHandler : MonoBehaviour
{
    public static CharacterSkinHandler instance;

    [Header("UI de celular (setar no Inspector)")]
    public CelularTag celularTag;

    [Header("Prefab e pontos de spawn")]
    [SerializeField] private GameObject characterSkinPrefab;
    [SerializeField] private Transform[] spawnPositions;

    // Arrays internos para controlar instâncias
    private CharacterSkinElement[] clientsCharacters;
    private GameObject[] spawnGameObjects;
    private readonly Dictionary<ulong, int> _slotByPlayerId = new Dictionary<ulong, int>();
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        int max = NetworkManager.singleton.maxConnections;
        clientsCharacters  = new CharacterSkinElement[max];
        spawnGameObjects   = new GameObject[max];

        StartCoroutine(SpawnExistingClientsWhenReady());
    }

    private IEnumerator SpawnExistingClientsWhenReady()
    {
       // Debug.Log("[CharacterSkinHandler] aguardando SteamManager.Initialized...");
        while (!SteamManager.Initialized)
            yield return null;

        //Debug.Log("[CharacterSkinHandler] Steam pronto, aguardando Mirror NetworkClient.isConnected...");
        while (!NetworkClient.isConnected)
            yield return null;
        
        // var clients = FindObjectsOfType<MyClient>();
        // Debug.Log($"[CharacterSkinHandler] encontrou {clients.Length} MyClient(s), spawnando agora.");

        // foreach (var client in clients)
        // {
        //     SpawnCharacterMesh(null);
        // }
    }
    public void SpawnCharacterMesh(PlayerData client)
    {
        if (client == null)
        {
            Debug.LogWarning("[SpawnCharacterMesh] client == null — IGNORANDO.");
            return;
        }

        ulong playerId = GetStablePlayerId(client);
        if (_slotByPlayerId.TryGetValue(playerId, out int existingIndex))
        {
            if (existingIndex >= 0 && existingIndex < clientsCharacters.Length && clientsCharacters[existingIndex] != null)
            {
                clientsCharacters[existingIndex].Initialize(client, client.IsReady);
                return;
            }
            _slotByPlayerId.Remove(playerId);
        }

        int index = GetNextPlatformIndex(client);
     //   Debug.Log($"[SpawnCharacterMesh] Cliente '{client.name}' vai usar slot #{index}");

        if (characterSkinPrefab == null)
        {
            Debug.LogError("[SpawnCharacterMesh] characterSkinPrefab NÃO está setado!");
            return;
        }
        if (spawnPositions == null || index >= spawnPositions.Length || spawnPositions[index] == null)
        {
          // Debug.LogError($"[SpawnCharacterMesh] spawnPositions[{index}] inválido ou null!");
            return;
        }

        

        for (int i = 0; i < clientsCharacters.Length; i++)
            if (clientsCharacters[i] != null && clientsCharacters[i].client == client) return;

        if (clientsCharacters[index] == null)
        {
            spawnGameObjects[index] = Instantiate(characterSkinPrefab, spawnPositions[index].position,
                spawnPositions[index].rotation, spawnPositions[index]);
            clientsCharacters[index] = spawnGameObjects[index].GetComponent<CharacterSkinElement>();
            if (clientsCharacters[index] == null)
            {
                Debug.LogError("[SpawnCharacterMesh] Prefab não tem CharacterSkinElement!");
                Destroy(spawnGameObjects[index]);
                spawnGameObjects[index] = null;
                return;
            }
        }

        _slotByPlayerId[playerId] = index;
        clientsCharacters[index].Initialize(client, client.IsReady);
    }

    private static ulong GetStablePlayerId(PlayerData client)
    {
        ulong steamId = client.playerInfo.steamId;
        return steamId != 0 ? steamId : ulong.MaxValue - client.netId;
    }

    /// <summary>
    /// Retorna o próximo slot livre (0 reservado pro localPlayer).
    /// </summary>
    public int GetNextPlatformIndex(PlayerData client)
    {
        if (client.isLocalPlayer)
            return 0;

        for (int i = 1; i < clientsCharacters.Length; i++)
        {
            if (clientsCharacters[i] == null)
                return i;
        }

        // fallback
        return 0;
    }

    public void DestroyMesh()
    {
        foreach (var character in clientsCharacters)
        {
            if (character != null)
                Destroy(character.gameObject);
        }
        System.Array.Clear(clientsCharacters, 0, clientsCharacters.Length);
        System.Array.Clear(spawnGameObjects, 0, spawnGameObjects.Length);
        _slotByPlayerId.Clear();
    }

    [Server]
    public void DestroyCharacterMesh(PlayerData client)
    {
        for (int i = 0; i < clientsCharacters.Length; i++)
        {
            var ch = clientsCharacters[i];
            if (ch != null && ch.client == client)
            {
                NetworkServer.Destroy(ch.gameObject);
                clientsCharacters[i] = null;
                spawnGameObjects[i] = null;
                _slotByPlayerId.Remove(GetStablePlayerId(client));
                Debug.Log($"[DestroyCharacterMesh] Slot {i} limpo para client {client.name}");
                break;
            }
        }
    }
}
