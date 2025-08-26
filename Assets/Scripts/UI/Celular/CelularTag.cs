using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
public class CelularTag : MonoBehaviour
{
    public TMP_Text username_text, money_text;
    public Image pfp_Image;
    public CharacterSkinElement currentSkinElement;
    public CSteamID steamId;
    [SerializeField] Sprite fallbackAvatar;

    // Cache estático para evitar trabalho repetido entre instâncias
    static readonly Dictionary<CSteamID, Sprite> AvatarCache = new Dictionary<CSteamID, Sprite>();
    static string CachedPersonaName;
    Sprite icon;
    void Start()
    {
        // Se Steam não estiver pronto, use placeholders e saia cedo
        if (!SteamManager.Initialized)
        {
            if (username_text != null && string.IsNullOrEmpty(CachedPersonaName))
                username_text.text = username_text.text; // mantém valor já definido na UI
            if (pfp_Image != null && fallbackAvatar != null)
                pfp_Image.sprite = fallbackAvatar;
            return;
        }

        steamId = SteamUser.GetSteamID();

        // Nome: usa cache ou busca uma vez
        if (string.IsNullOrEmpty(CachedPersonaName))
            CachedPersonaName = SteamFriends.GetPersonaName();
        if (username_text != null)
            username_text.text = CachedPersonaName;

        // Avatar: usa cache se existir, senão placeholder + carrega em coroutine (adiado 1 frame)
        if (pfp_Image != null)
        {
            if (AvatarCache.TryGetValue(steamId, out var cachedSprite) && cachedSprite != null)
            {
                pfp_Image.sprite = cachedSprite;
            }
            else
            {
                if (fallbackAvatar != null)
                    pfp_Image.sprite = fallbackAvatar;
                StartCoroutine(LoadAvatarDeferred(steamId));
            }
        }
    }

    public void UpdateTagCelular(string username, string money) 
    {
        // Evita chamadas desnecessárias à Steam aqui; apenas atualiza UI
        if (username_text != null)
            username_text.text = username;
        if (!string.IsNullOrEmpty(username))
            CachedPersonaName = username; // mantém cache coerente se atualizar pelo jogo
        if (money_text != null)
            money_text.text = money;
    }

    public void UpdatePFP(Sprite icon)
    {
        if (pfp_Image != null)
            pfp_Image.sprite = icon;
    }

    IEnumerator LoadAvatarDeferred(CSteamID id)
    {
        // Adia para o próximo frame para aliviar pico no Start
        yield return null;

        // Se já foi carregado nesse meio tempo, usa cache
        if (AvatarCache.TryGetValue(id, out var cached) && cached != null)
        {
            if (pfp_Image != null) pfp_Image.sprite = cached;
            yield break;
        }

        // Busca avatar e converte uma única vez
        Texture2D tex = SteamHelper.GetAvatar(id);
        if (tex != null)
        {
            var sprite = SteamHelper.ConvertTextureToSprite(tex);
            AvatarCache[id] = sprite;
            if (pfp_Image != null)
                pfp_Image.sprite = sprite;
        }
    }
}