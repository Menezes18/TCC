using Mirror;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterSkinElement : MonoBehaviour
{
    [Header("Posição da nametag (setar no Inspector)")]
    public Transform nametagPos;

    public MyClient client { get; private set; }
    public CSteamID steamId { get; private set; }
    public NametagMarker nametagMarker { get; set; }

    private bool initialized = false;
    private Sprite icon;
    protected Callback<AvatarImageLoaded_t> avatarImageLoaded;

    private void Awake()
    {
        Debug.Log("[CharacterSkinElement.Awake] Awake chamado.");
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        Debug.Log($"[OnAvatarImageLoaded] callback.m_steamID={callback.m_steamID}, steamId atual={steamId}");
        if (callback.m_steamID != steamId)
        {
            Debug.Log("[OnAvatarImageLoaded] IDs não batem, ignorando.");
            return;
        }

        Texture2D tex = SteamHelper.GetAvatar(steamId);
        if (tex)
        {
            icon = SteamHelper.ConvertTextureToSprite(tex);
            Debug.Log("[OnAvatarImageLoaded] Avatar convertido em sprite com sucesso.");
        }
        else
        {
            Debug.LogWarning("[OnAvatarImageLoaded] SteamHelper.GetAvatar retornou null.");
        }

        if (nametagMarker != null)
        {
            nametagMarker.UpdatePFP(icon);
            Debug.Log("[OnAvatarImageLoaded] nametagMarker.UpdatePFP executado.");
        }
        else
        {
            Debug.LogError("[OnAvatarImageLoaded] nametagMarker é null!");
        }

        if (CharacterSkinHandler.instance != null && CharacterSkinHandler.instance.celularTag != null)
        {
            CharacterSkinHandler.instance.celularTag.UpdatePFP(icon);
            Debug.Log("[OnAvatarImageLoaded] celularTag.UpdatePFP executado.");
        }
        else
        {
            Debug.LogError("[OnAvatarImageLoaded] CharacterSkinHandler.instance.celularTag é null!");
        }
    }

    private string _name;
    public void Initialize(MyClient client, bool _isReady)
    {
        Debug.Log($"[Initialize] chamado com client={(client==null?"null":client.name)}, _isReady={_isReady}");

        // 1) Checagem de client
        if (client == null)
        {
            Debug.LogWarning("[Initialize] client é null — pulando inicialização.");
            return;
        }
        this.client = client;

        // 2) Checa nametagPos
        if (nametagPos == null)
        {
            Debug.LogError("[Initialize] nametagPos NÃO está setado no Inspector!");
        }

        // 3) Busca o CelularTag no cliente
        var celular = client.GetComponent<PlayerScript>()?
                           .GetComponentInChildren<CelularTag>(includeInactive: true);
        if (celular == null)
        {
            Debug.LogError("[Initialize] Não achei CelularTag via GetComponentInChildren!");
        }
        else
        {
            CharacterSkinHandler.instance.celularTag = celular;
            celular.currentSkinElement = this;
            Debug.Log("[Initialize] CelularTag atribuído a CharacterSkinHandler.instance.celularTag");
        }

        // 4) Define steamId e username
        string username = SteamFriends.GetPersonaName();
        steamId = client != null
            ? new CSteamID(client.playerInfo.steamId)
            : SteamUser.GetSteamID();
        Debug.Log($"[Initialize] steamId definido como {steamId}");

        // 5) Spawna o Marker na posição correta
        if (MarkerHandler.instance == null)
        {
            Debug.LogError("[Initialize] MarkerHandler.instance é null!");
        }
        else
        {
            nametagMarker = (NametagMarker)MarkerHandler.instance
                .SpawnMarker(0, nametagPos.position, null);
            if (nametagMarker == null)
                Debug.LogError("[Initialize] SpawnMarker retornou null!");
            else
                Debug.Log("[Initialize] nametagMarker instanciado com sucesso.");
        }

        // 6) Ajusta dados do client (username e icon)
        if (client != null)
        {
            username = client.playerInfo.username;
            icon = client.icon;
            Debug.Log($"[Initialize] usando dados do client: username={username}");
        }

        // 7) Configura callback apenas uma vez
        if (!initialized)
        {
            initialized = true;
            avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
            Debug.Log("[Initialize] Callback de AvatarImageLoaded criado.");

            // Carrega avatar imediatamente
            Texture2D tex = SteamHelper.GetAvatar(steamId);
            if (tex)
            {
                icon = SteamHelper.ConvertTextureToSprite(tex);
                Debug.Log("[Initialize] Avatar inicial carregado com sucesso.");
            }
            else
            {
                Debug.LogWarning("[Initialize] SteamHelper.GetAvatar inicial retornou null.");
            }

            if (nametagMarker != null)
            {
                nametagMarker.UpdatePFP(icon);
                Debug.Log("[Initialize] nametagMarker.UpdatePFP inicial executado.");
            }
        }

        // 8) Atualiza tag e PFP
        if (nametagMarker != null)
        {
            nametagMarker.UpdateTag(username, _isReady);
            Debug.Log("[Initialize] nametagMarker.UpdateTag executado.");
            nametagMarker.UpdatePFP(icon);
            Debug.Log("[Initialize] nametagMarker.UpdatePFP executado novamente.");
        }
        else
        {
            Debug.LogError("[Initialize] nametagMarker é null ao final da Initialize!");
        }

        // 9) Se for local, atualiza CelularTag
        if (steamId == SteamUser.GetSteamID())
        {
            Debug.Log("[Initialize] Atualizando celularTag do player local.");
            if (CharacterSkinHandler.instance?.celularTag != null)
            {
                CharacterSkinHandler.instance.celularTag.UpdatePFP(icon);
                CharacterSkinHandler.instance.celularTag.UpdateTagCelular(username, "4590");
                Debug.Log("[Initialize] celularTag atualizado para player local.");
            }
            else
            {
                Debug.LogError("[Initialize] celularTag é null ao tentar atualizar para local!");
            }
        }
    }

    private void OnDestroy()
    {
        if (nametagMarker != null)
        {
            nametagMarker.DestroyMarker();
            Debug.Log("[OnDestroy] nametagMarker.DestroyMarker executado.");
        }
        else
        {
            Debug.Log("[OnDestroy] nametagMarker era null, nada para destruir.");
        }
    }
}
