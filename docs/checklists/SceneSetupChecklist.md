# Scene setup checklist

Use this quick pass before committing changes to any gameplay scene.

## Offline (menu boot)

- Ensure `NetworkManagerRoot` hosts `MyNetworkManager`, `SteamLobby`, and the active transport.
- Keep `PlayerList` and `SteamManager` on root objects marked `DontDestroyOnLoad`.
- Wire `MinigameCatalog`, `Database`, `HUDSO`, and UI references on the relevant components.

## RASCUNHO (hub)

- Confirm `BriefingManager`, `MatchManager`, and `ScoreboardUI` live in the scene.
- Populate spawn anchors referenced by `MatchManager._spawns`.
- Place interactable props and UI canvases under organized root parents (`Environment`, `UI`).

## Minigame scenes (`MN_*`)

- Include `MatchManager` and `BriefingManager` prefabs.
- Assign all `SettingsMiniGameData`, `HUDSO`, and Database assets.
- Populate spawn points and gameplay-specific controllers (checkpoints, goals, hazards).

## Victory

- Reuse the scoreboard canvas and confirm links to `MyNetworkManager.lastGameResults`.

## Validation

- Run **Tools → Scene Validation → Validate Current Scene** and fix reported issues before pushing.
