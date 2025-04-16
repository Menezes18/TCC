# Banana Party

### Projeto Multiplayer com Steamworks e Mirror

## Visão Geral do Projeto

Este projeto implementa funcionalidades para um jogo multiplayer utilizando a biblioteca Mirror para a rede e a Steamworks para integração com recursos do Steam, como gerenciamento de amigos, lobbies e avatares. Além disso, o sistema conta com componentes para manipulação de interfaces (menus, popups) e gerenciamento de estados do jogo (como o status "Ready" dos jogadores).

## Documentação dos Arquivos

### FriendItem.cs
- **Objetivo:** Exibir um item da lista de amigos do Steam.
- **Funcionalidades:**
  - Inicializa o item com o nome, ID do Steam e status online/offline.
  - Carrega e converte o avatar do amigo para exibição.
  - Permite enviar um convite para o amigo para participar de um lobby.
- **Métodos Principais:**
  - `InitializeFriendItem(string name, ulong id, bool status)`
  - `InviteFriend()`
  - `OnAvatarImageLoaded(...)` e `GetIcon()`

### FriendListManager.cs
- **Objetivo:** Gerenciar e atualizar a lista de amigos obtida via Steam.
- **Funcionalidades:**
  - Solicita a lista de amigos e organiza os itens, priorizando os online.
  - Atualiza periodicamente a lista de amigos.
- **Métodos Principais:**
  - `Update()`
  - `GetSteamFriends()`

### LobbyController.cs
- **Objetivo:** Controlar a transição do lobby para o início do jogo.
- **Funcionalidades:**
  - Inicia partidas em modos party (multijogador) ou solo.
  - Verifica se todos os jogadores estão prontos antes de iniciar o jogo.
  - Realiza a transição para a cena do jogo ("MiniGame").
- **Métodos Principais:**
  - `StartGameWithParty()`
  - `StartGameSolo()`
  - `AllPlayersReady()`

### MainMenu.cs
- **Objetivo:** Gerenciar a interface principal do jogo, incluindo a criação de party e o gerenciamento do status "Ready".
- **Funcionalidades:**
  - Alterna entre o menu inicial e o menu de party.
  - Permite criar party, encontrar partidas, iniciar jogos e sair da party.
  - Atualiza o botão que indica se o jogador está "Ready" ou "Not Ready".
- **Métodos Principais:**
  - `CreateParty()`, `StartSinglePlayer()`, `LeaveParty()`, `FindMatch()`, `StartGame()`
  - `ToggleReady()` e `UpdateReadyButton(bool)`

### MenuController.cs
- **Objetivo:** Gerenciar a exibição de menus e a troca de câmeras para a interface do usuário.
- **Observação:** O código atual está comentado, indicando que a funcionalidade pode ter sido descontinuada ou está em manutenção.

### MyClient.cs
- **Objetivo:** Representar um jogador na sessão multiplayer.
- **Funcionalidades:**
  - Sincroniza dados do jogador (nome, SteamID, status "Ready") com o servidor usando SyncVars.
  - Carrega o avatar do Steam e atualiza a interface do personagem.
  - Permite alternar o estado "Ready" e atualizar a interface do MainMenu.
- **Métodos Principais e Regiões:**
  - `ToggleReady()` e `Cmd_ToggleReady()`
  - Hooks de SyncVar: `PlayerInfoUpdate()` e `IsReadyUpdate()`

### MyNetworkManager.cs
- **Objetivo:** Gerenciar as conexões de rede e os eventos relacionados à entrada e saída de jogadores.
- **Funcionalidades:**
  - Customiza o processo de adição de jogadores e mantém um scoreboard com as pontuações.
  - Define se o jogo está em modo multiplayer ou singleplayer.
  - Inicia a contagem regressiva quando o número mínimo de jogadores é atingido.
- **Métodos Principais:**
  - `OnServerAddPlayer()`
  - `AddPoints(ulong steamID, int pointsToAdd)`
  - `SetMultiplayer(bool)`
  - `iniciaContador()`

### PartyMenuUIManager.cs
- **Objetivo:** Gerenciar a interface do menu de party durante as sessões multiplayer.
- **Funcionalidades:**
  - Alterna a visibilidade dos painéis do menu de party com base em inputs do jogador (ex.: pressionamento da tecla "M").
  - Disponibiliza funções para iniciar a cena do jogo ou convidar outros jogadores.
- **Métodos Principais:**
  - `TogglePartyMenu()`
  - `SetLobbyPlayer(MyClient)`

### PopupManager.cs
- **Objetivo:** Gerenciar popups e notificações na interface do jogo.
- **Funcionalidades:**
  - Exibe mensagens e popups (por exemplo, "Creating Party", "Joining Party").
  - Permite fechar os popups conforme a necessidade do fluxo do jogo.
- **Métodos Principais:**
  - `Popup_Show(string title)`
  - `Popup_Close()`

### SteamLobby.cs
- **Objetivo:** Integrar os recursos de lobby do Steam no jogo.
- **Funcionalidades:**
  - Cria e gerencia lobbies públicos utilizando a API Steamworks.
  - Lida com callbacks de criação, entrada e pedidos de join em lobbies.
  - Implementa matchmaking para encontrar e ingressar em lobbies disponíveis.
- **Métodos Principais:**
  - `CreateLobby()`, `JoinLobby(CSteamID)`
  - Callbacks: `OnLobbyCreated()`, `OnJoinRequest()`, `OnLobbyEntered()`
  - `FindMatch()`

## Considerações Finais

Este projeto foi desenvolvido para oferecer uma experiência multiplayer integrada com a rede Mirror e a API Steamworks, proporcionando funcionalidades que vão desde a exibição de amigos até a criação e gerenciamento de lobbies e partidas. A documentação acima detalha cada componente e suas responsabilidades dentro do sistema, facilitando futuras manutenções e evoluções do código.

Para utilizar o projeto, certifique-se de ter as dependências necessárias instaladas (por exemplo, Mirror e Steamworks.NET) e siga as instruções de configuração do ambiente.

---

