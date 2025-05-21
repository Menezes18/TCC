using System;
using Mirror;
using UnityEngine;
using Steamworks;
using UnityEngine.Events;
using Random = UnityEngine.Random;

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
   
   
   [SyncVar(hook = nameof(PlayerInfoUpdate))] public PlayerInfoData playerInfo;
   [SyncVar (hook = nameof(HookOnAliasUpdated))] public string alias; // name steam
   [SyncVar (hook = nameof(HookOnColorUpdated))] public int color = -1;
   [SyncVar] public int score;
   
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
   private void Start()
   {
      PlayerDataSO.EventOnColorRequest += PlayerDataSOOnEventOnColorRequest;
      
      SteamInitialization();

      //
      if (base.isServer == true)
      {
         PlayerList.singleton.AddToList(this);
      }
        CmdNetworkAlias();
        //
        if (base.isOwned == false) return;
      

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
      //if (!isOwned) return;
      string chosenName;
      if (SteamManager.Initialized)
      {
         CSteamID myId = SteamUser.GetSteamID();
         chosenName = SteamFriends.GetFriendPersonaName(myId);
      }
      else
      {
         chosenName = "Mamaco";
      }

      alias = chosenName;
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
      Debug.LogError(value + " is not a valid color");
      color = playerList.ServerRequestColor(color, value);

     
      MyNetworkManager.manager.pointsBoard[this.playerInfo.steamId].color = value;
   }
   
   //
   void HookOnAliasUpdated(string oldVal, string newVal)
   {
      if (!isOwned) return;
      MyNetworkManager.manager.pointsBoard[this.playerInfo.steamId].playerName = newVal;
      this.OnAliasUpdated?.Invoke(newVal);
   }

   void HookOnColorUpdated(int oldVal, int newVal)
   {
      this.OnColorUpdated?.Invoke(newVal);
      if (isServer)
      {
         var steamId = playerInfo.steamId;
         if (MyNetworkManager.manager.pointsBoard.TryGetValue(steamId, out var dp))
         {
            dp.color = newVal;
            MyNetworkManager.manager.pointsBoard[steamId] = dp;
            // e no scoreboard sincronizado também
            var plr = MyNetworkManager.manager.scoreboard.players
               .Find(p => p.steamID == steamId);
            if (plr != null) plr.color = newVal;
         }
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

      if (SteamManager.Initialized && !MyNetworkManager.manager.testMode)
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
      if (!SteamManager.Initialized || MyNetworkManager.manager.testMode)
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
   }
   #endregion
   
}
