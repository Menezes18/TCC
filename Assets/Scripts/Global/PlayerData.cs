using System;
using Mirror;
using UnityEngine;

public class PlayerData : NetworkBehaviour{
   
   [SyncVar] string alias;
   [SyncVar] int color;
   [SyncVar] int score;

   private void Start()
   {
      if(base.isOwned == false) return;
      
   }
}
