using Mirror;
using Steamworks;
using System.Collections;
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
        Debug.Log("[CharacterSkinHandler] aguardando SteamManager.Initialized...");
        while (!SteamManager.Initialized)
            yield return null;

        Debug.Log("[CharacterSkinHandler] Steam pronto, aguardando Mirror NetworkClient.isConnected...");
        while (!NetworkClient.isConnected)
            yield return null;
        
        // var clients = FindObjectsOfType<MyClient>();
        // Debug.Log($"[CharacterSkinHandler] encontrou {clients.Length} MyClient(s), spawnando agora.");

        // foreach (var client in clients)
        // {
        //     SpawnCharacterMesh(null);
        // }
    }
    public void SpawnCharacterMesh(MyClient client)
    {
        if (client == null)
        {
            Debug.LogWarning("[SpawnCharacterMesh] client == null — IGNORANDO.");
            return;
        }

        int index = GetNextPlatformIndex(client);
        Debug.Log($"[SpawnCharacterMesh] Cliente '{client.name}' vai usar slot #{index}");

        if (client.isLocalPlayer)
        {
            if (clientsCharacters[0] == null)
            {
                Debug.LogError("[SpawnCharacterMesh] Slot 0 vazio para local player!");
                return;
            }
            clientsCharacters[0].Initialize(client, client.IsReady);
            return;
        }

        if (characterSkinPrefab == null)
        {
            Debug.LogError("[SpawnCharacterMesh] characterSkinPrefab NÃO está setado!");
            return;
        }
        if (index >= spawnPositions.Length || spawnPositions[index] == null)
        {
            Debug.LogError($"[SpawnCharacterMesh] spawnPositions[{index}] inválido ou null!");
            return;
        }

        // Instancia e inicializa
        GameObject go = Instantiate(characterSkinPrefab, spawnPositions[index]);
        spawnGameObjects[index]   = go;
        clientsCharacters[index]  = go.GetComponent<CharacterSkinElement>();

        if (clientsCharacters[index] == null)
        {
            Debug.LogError("[SpawnCharacterMesh] Prefab não tem CharacterSkinElement!");
            return;
        }

        clientsCharacters[index].Initialize(client, client.IsReady);
        client.characterInstance = clientsCharacters[index];
    }

    /// <summary>
    /// Retorna o próximo slot livre (0 reservado pro localPlayer).
    /// </summary>
    public int GetNextPlatformIndex(MyClient client)
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
    }

    [Server]
    public void DestroyCharacterMesh(MyClient client)
    {
        for (int i = 0; i < clientsCharacters.Length; i++)
        {
            var ch = clientsCharacters[i];
            if (ch != null && ch.client == client)
            {
                NetworkServer.Destroy(ch.gameObject);
                clientsCharacters[i] = null;
                Debug.Log($"[DestroyCharacterMesh] Slot {i} limpo para client {client.name}");
                break;
            }
        }
    }
}
