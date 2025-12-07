using System;
using System.Collections;
using Mirror;
using UnityEngine;
using Steamworks;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public struct PlayerInfoData 
{
   public string username;
   public ulong steamId;

   public PlayerInfoData(string username, ulong steamId)
   {
      this.username = username;
      this.steamId = steamId;
   }
}

[System.Serializable]
public class PlayerData : NetworkBehaviour{
   private PlayerList playerList => PlayerList.singleton;
   
   [SerializeField] PlayerDataSO PlayerDataSO;
   [SerializeField] Database db;
   [SerializeField] HUDSO HUDSO;
   
   
   [SyncVar(hook = nameof(PlayerInfoUpdate))] public PlayerInfoData playerInfo;
   [SyncVar (hook = nameof(HookOnAliasUpdated))] public string alias; // name steam
   [SyncVar (hook = nameof(HookOnColorUpdated))] public int color = -1;
   [SyncVar(hook = nameof(HookOnScoreUpdated))] public int score;
   // Estado de espectador (opcional) replicado para todos
   [SyncVar(hook = nameof(OnSpectatingChanged))] public bool isSpectating;
   
   // Customização (chapeu, oculos, blusa)
   [SyncVar(hook = nameof(OnHatChanged))] public int hatIndex = -1;
   [SyncVar(hook = nameof(OnGlassesChanged))] public int glassesIndex = -1;
   [SyncVar(hook = nameof(OnShirtChanged))] public int shirtIndex = -1;
   
   public UnityEvent<string> OnAliasUpdated;
   public UnityEvent<int> OnColorUpdated;

   // Steam e lobby
   [SyncVar(hook = nameof(IsReadyUpdate))]
   public bool IsReady;
   [SyncVar]
   public bool isPartyOwner = false;
   public CharacterSkinElement characterInstance { get; set; }
   protected Callback<AvatarImageLoaded_t> avatarImageLoaded;
   public Sprite icon { get; private set; }

   private void Awake()
   {
      if (base.isServer == true)
      {
         PlayerList.singleton.AddToList(this);
      }
   }
   public override void OnStartServer()
   {
      base.OnStartServer();
      if (color < 0 && PlayerList.singleton != null)
         color = PlayerList.singleton.RequestRandomColor(); 
   }
   public override void OnStartLocalPlayer()
   {
      base.OnStartLocalPlayer();
      StartCoroutine(InitializePlayerInfo());
      
      StartCoroutine(InitializeCustomization());
   }
   
   private IEnumerator InitializeCustomization()
   {
      while (CustomizationManager.Instance == null)
         yield return null;
      
      yield return new WaitForEndOfFrame();
      
      SendCustomizationToServer();
      
      Debug.Log("🎨 [PlayerData] Customization initialized and sent to server");
   }
   private IEnumerator InitializePlayerInfo()
   {
      yield return new WaitForEndOfFrame();
      
      if (SteamManager.Initialized)
      {
         CSteamID myId = SteamUser.GetSteamID();
         string steamName = SteamFriends.GetFriendPersonaName(myId);
         ulong steamIdValue = myId.m_SteamID;
         
         CmdSetPlayerInfo(steamName, steamIdValue);
      }

   }
   [Command]
   void CmdSetPlayerInfo(string steamName, ulong steamIdValue)
   {

      alias = steamName;
      playerInfo = new PlayerInfoData(steamName, steamIdValue);
      MyNetworkManager.manager.RegisterNewPlayer(this);

   }
   private void Start()
   {
      PlayerDataSO.EventOnColorRequest += PlayerDataSOOnEventOnColorRequest;
      
      SteamInitialization();

      //
      if (base.isServer == true)
      {
         PlayerList.singleton.AddToList(this);
      }
      
      //
      if(base.isOwned == false) return;
      

      // 
   }
   
   

   
   private void SteamInitialization()
   {
      if (NetworkManager.singleton != null)
         ((MyNetworkManager)NetworkManager.singleton).allClients.Add(this);
        

      if(CharacterSkinHandler.instance) CharacterSkinHandler.instance.SpawnCharacterMesh(this);
      avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
   }

   private void OnDestroy()
   {
      if (this && ((MyNetworkManager)NetworkManager.singleton))
         ((MyNetworkManager)NetworkManager.singleton).allClients.Remove(this);
      
      if (base.isServer == true)
         PlayerList.singleton.RemoveFromList(this);
      
      PlayerDataSO.EventOnColorRequest -= PlayerDataSOOnEventOnColorRequest;

   }


   [Command]
   void CmdNetworkAlias()
   {
      if (SteamManager.Initialized)
      {
         CSteamID myId = SteamUser.GetSteamID();
         string steamName = SteamFriends.GetFriendPersonaName(myId);
         ulong steamIdValue = myId.m_SteamID;
         
      }
      else
      {
         // fallback name could be set here if needed
      }


   }
   

   [Command]
   void CmdRequestAlias(string value)
   {
      bool duplicateAlias = playerList.CheckDuplicateAlias(value);

      if (duplicateAlias){
         alias = $"{value} ({Random.Range(0, 9999)}) ";
      }
      alias = value;
   }

   [Command]
   void CmdRequestColor(int value)
   {
      Debug.Log($"[ColorSystem] Cor alterada: {value}" );
      color = playerList.ServerRequestColor(color, value);

     
      var playerData = GetComponent<PlayerData>();
      MyNetworkManager.manager.pointsBoard[playerData.playerInfo.steamId].color = value;
   }
   
   //
   void HookOnAliasUpdated(string oldVal, string newVal)
   {
      this.OnAliasUpdated?.Invoke(newVal);
   }
   void HookOnScoreUpdated(int oldVal, int newVal)
   {
      if (!isOwned) return;

      // Atualiza score via HUDSO (padrão local)
      if (HUDSO != null)
         HUDSO.UpdateScore(newVal);
   }
   void HookOnColorUpdated(int oldVal, int newVal)
   {
      OnColorUpdated?.Invoke(newVal);

      if (isServer)
      {
         if (oldVal >= 0)
            PlayerList.singleton.ReturnColor(oldVal);

         if (newVal >= 0 && PlayerList.singleton.ColorsAvailable.Contains(newVal))
            PlayerList.singleton.ColorsAvailable.Remove(newVal);
         
         var steamId = playerInfo.steamId;
         if (MyNetworkManager.manager.pointsBoard.TryGetValue(steamId, out var dp))
         {
            dp.color = newVal;
            MyNetworkManager.manager.pointsBoard[steamId] = dp;
            var pl = MyNetworkManager.manager.scoreboard.players.Find(p => p.steamID == steamId);
            if (pl != null) pl.color = newVal;


         }
      }
   }
   void OnHatChanged(int oldVal, int newVal)
   {
      Debug.Log($"🎩 [PlayerData] Hat changed: {oldVal} → {newVal} (isLocalPlayer={isLocalPlayer}, name={name})");
      ApplyCustomizationToCharacter();
      ApplyCustomizationToPlayerScript();
   }
   
   void OnGlassesChanged(int oldVal, int newVal)
   {
      Debug.Log($"🕶️ [PlayerData] Glasses changed: {oldVal} → {newVal} (isLocalPlayer={isLocalPlayer}, name={name})");
      ApplyCustomizationToCharacter();
      ApplyCustomizationToPlayerScript();
   }
   
   void OnShirtChanged(int oldVal, int newVal)
   {
      Debug.Log($"👕 [PlayerData] Shirt changed: {oldVal} → {newVal} (isLocalPlayer={isLocalPlayer}, name={name})");
      ApplyCustomizationToCharacter();
      ApplyCustomizationToPlayerScript();
   }
   
   private void ApplyCustomizationToCharacter()
   {
      if (characterInstance == null)
      {
         return;
      }
      
      var characterPlayerData = characterInstance.client;
      if (characterPlayerData != this)
      {
         Debug.LogWarning($"⚠️ [PlayerData] characterInstance.client mismatch! Expected {name}, but characterInstance belongs to {characterPlayerData?.name ?? "null"}");
         return;
      }
      
      var applier = characterInstance.GetComponentInChildren<CustomizationApplier>();
      if (applier != null)
      {
         var customData = new PlayerCustomizationData("");
         customData.hatIndex = hatIndex;
         customData.glassesIndex = glassesIndex;
         customData.shirtIndex = shirtIndex;
         
         applier.ApplyCustomization(customData);
         Debug.Log($"✅ [PlayerData] Customization applied to characterInstance: Hat={hatIndex}, Glasses={glassesIndex}, Shirt={shirtIndex}");
      }
   }
   
   private void ApplyCustomizationToPlayerScript()
   {
      if (isLocalPlayer)
      {
         return;
      }
      
      var playerScript = GetComponent<PlayerScript>();
      if (playerScript != null)
      {
         playerScript.ApplyRemoteCustomization(hatIndex, glassesIndex, shirtIndex);
      }
   }
   
   [Command]
   public void CmdSetCustomization(int hat, int glasses, int shirt)
   {
      Debug.Log($"🎮 [PlayerData] Server received customization: Hat={hat}, Glasses={glasses}, Shirt={shirt}");
      hatIndex = hat;
      glassesIndex = glasses;
      shirtIndex = shirt;
   }
   

   public void SendCustomizationToServer()
   {
      if (!isLocalPlayer) return;
      
      var customization = CustomizationManager.Instance?.GetCurrentCustomization();
      if (customization != null)
      {
         CmdSetCustomization(customization.hatIndex, customization.glassesIndex, customization.shirtIndex);
         Debug.Log($"📤 [PlayerData] Sent customization to server: {customization}");
      }
   }
   void PlayerDataSOOnEventOnColorRequest(int obj)
   {
      if(!isOwned) return;
      
      CmdRequestColor(obj);
   }

   #region Steam e Lobby

   
   private void PlayerInfoUpdate(PlayerInfoData _, PlayerInfoData data)
   {

      if (SteamManager.Initialized)
      {
         SetIcon(new CSteamID(data.steamId));
      }
   }
   public void IsReadyUpdate(bool _, bool value) 
   {
      if (isLocalPlayer) 
      {
         MainMenu.instance.UpdateReadyButton(value);
      }
   }
   private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
   {
      if (!SteamManager.Initialized)
         return;
      Debug.Log("Avatar loaded " + callback.m_steamID);
      if (callback.m_steamID.m_SteamID != playerInfo.steamId) return;
      SetIcon(callback.m_steamID);
        
   }
   void SetIcon(CSteamID steamId)
   {
      if (!SteamManager.Initialized) 
         return;
      Texture2D tex = SteamHelper.GetAvatar(steamId);
      if (tex)
         icon = SteamHelper.ConvertTextureToSprite(tex);
   }
   
    public void ToggleReady() => Cmd_ToggleReady();

    [Command]
    private void Cmd_ToggleReady() 
    {
        IsReady = !IsReady;
        
        BriefingManager.singleton?.CheckAllReady();
        BriefingManager.singleton?.UpdateAllClientsSlots();
    }

    [Command]
    public void CmdSetSpectating(bool value)
    {
        isSpectating = value;
    }

    private void OnSpectatingChanged(bool _, bool newVal)
    {
       
         Debug.Log($"[SPEC] isSpectating => {newVal}");
    }

   [Command]
   public void CmdReportLoadProgress(string scene, float progress)
   {
      MyNetworkManager.manager?.ServerRecordClientLoadProgress(playerInfo.steamId, scene, progress);
   }
   #endregion
   
}
