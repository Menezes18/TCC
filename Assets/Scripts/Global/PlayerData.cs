using System;
using Mirror;
using UnityEngine;
using Steamworks;
using Random = UnityEngine.Random;


public class PlayerData : NetworkBehaviour{
   private PlayerList playerList => PlayerList.singleton;
   
   [SyncVar] public string alias; // name steam
   [SyncVar] public int color;
   [SyncVar] public int score;

   private void Start()
   {
      //
      if (base.isServer == true)
         PlayerList.singleton.AddToList(this);
      
      //
      if(base.isOwned == false) return;
 
      
      CmdNetworkAlias();
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
   
   private void OnDestroy()
   {
      if (base.isServer == true)
         PlayerList.singleton.RemoveFromList(this);
   }

   [Command]
   void CmdSetAlias(string value)
   {
      bool duplicateAlias = playerList.CheckDuplicateAlias(value);

      if (duplicateAlias){
         alias = $"{value} ({Random.Range(0, 9999)}) ";
      }
      alias = value;
   }

   [Command]
   void CmdRequestColor(int color)
   {
      
   }

  
}
