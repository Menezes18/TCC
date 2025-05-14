
using Mirror;
using UnityEngine;
using Steamworks;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[System.Serializable]
public class PlayerData : NetworkBehaviour{
   private PlayerList playerList => PlayerList.singleton;
   
   [SerializeField] PlayerDataSO PlayerDataSO;
   
   [SyncVar (hook = nameof(HookOnAliasUpdated))] public string alias; // name steam
   [SyncVar (hook = nameof(HookOnColorUpdated))] public int color = -1;
   [SyncVar] public int score;

   public UnityEvent<string> OnAliasUpdated;
   public UnityEvent<int> OnColorUpdated;

   private void Start()
   {
      PlayerDataSO.EventOnColorRequest += PlayerDataSOOnEventOnColorRequest;
      
      //
      if (base.isServer == true)
      {
         PlayerList.singleton.AddToList(this);
         
      }
      
      //
      CmdNetworkAlias();
      if(base.isOwned == false) return;
 
      
      //
      int lastColor = PlayerPrefs.GetInt("lastcolor", 1);
      CmdRequestColor(lastColor);
   }
   private void OnDestroy()
   {
      if (base.isServer == true)
         PlayerList.singleton.RemoveFromList(this);
      
      PlayerDataSO.EventOnColorRequest -= PlayerDataSOOnEventOnColorRequest;

   }


   [Command]
   void CmdNetworkAlias()
   {
      string chosenName;
      if (SteamManager.Initialized && SteamUser.BLoggedOn())
      {
         CSteamID myId = SteamUser.GetSteamID();
         chosenName = SteamFriends.GetFriendPersonaName(myId);
      }
      else
      {
         chosenName = "Menezes";
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
   }
   
   //
   void HookOnAliasUpdated(string oldVal, string newVal)
   {
      this.OnAliasUpdated?.Invoke(newVal);
   }

   void HookOnColorUpdated(int oldVal, int newVal)
   {
      this.OnColorUpdated?.Invoke(newVal);
   }
   
   
   void PlayerDataSOOnEventOnColorRequest(int obj)
   {
      if(!isOwned) return;
      Debug.LogError(this.gameObject.name + ": PlayerDataSOOnEventOnColorRequest");
      CmdRequestColor(obj);
   }
  
}
