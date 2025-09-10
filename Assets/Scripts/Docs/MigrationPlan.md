Temporary migration plan (will be removed after refactor):

Core: Extensions -> Core/Extensions ; Utilis -> Core/Utilities
Data: Database.cs, HUDSO.cs, PlayerControlsSO.cs, PlayerDataSO.cs, SettingsMiniGameData (if exists)
Gameplay/Player: PlayerScript.cs and related (Respawn, Mesh, Controls input adapter)
Gameplay/Combat: ProjectileScript.cs, PrefabInstancer.cs
Gameplay/Match: MatchManager.cs, Score related, Time related
Gameplay/Lobby: PlayerList.cs, PlayerData.cs, PlayerNameplate.cs, LobbySlot.cs
Gameplay/Minigames: folder splitting 1-Rua -> Street, 2-Chao -> FloorBreaking, 3-Sumo -> Sumo, 5-Fut -> Soccer, 6-BatataQuente -> HotPotato
Infrastructure/Network: MyNetworkManager.cs, SteamLobby.cs, LobbyController.cs, NetworkStatsDisplay.cs
UI/HUD: HUDManager.cs, CooldownUI.cs, BlindPanel.cs, ColorChangePanel.cs
UI/Menus: MainMenu.cs, MenuController.cs, LoadingScreenUI.cs, LobbyUI.cs, PartyMenuUIManager.cs
UI/Chat: ChatManager.cs
UI/Phone: (Celular related prefabs/scripts later)
Steam: FriendListManager.cs, FriendItem.cs, Steam helpers
Obsolete: GameManager.cs (empty), legacy placeholders

Note: No namespaces yet; assembly definitions not created yet.