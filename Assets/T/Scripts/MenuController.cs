// using UnityEngine;
// using UnityEngine.InputSystem;
// using Cinemachine;
// using Mirror;
//
// public class MenuController : NetworkBehaviour
// {
//     public static MenuController instance;
//
//     [Header("UI Menus")]
//     [SerializeField] private GameObject mainMenu;
//     [SerializeField] private GameObject tabletUI;
//
//     [Header("Camera Settings")]
//     [SerializeField] private CinemachineVirtualCamera frontCamera;
//     [SerializeField] private CinemachineVirtualCamera defaultCamera;
//
//     private bool isMenuOpen = false;
//     private MainMenu mainMenuScript;
//
//     private void Awake()
//     {
//         if (instance == null) instance = this;
//         else Destroy(gameObject);
//     }
//
//     public override void OnStartAuthority()
//     {
//         base.OnStartAuthority();
//         enabled = true; // Ativa o script apenas para o jogador local
//     }
//
//     private void Start()
//     {
//         if (!isLocalPlayer) return; // Apenas o jogador local pode controlar a UI
//
//         mainMenuScript = MainMenu.instance;
//         mainMenu.SetActive(false);
//         tabletUI.SetActive(false);
//     }
//
//     [Command(requiresAuthority = false)]
//     private void CmdToggleMenu(uint playerId)
//     {
//         if (!IsPartyLeader(playerId)) return; // Verifica se o jogador é o líder da party
//
//         RpcToggleMenu();
//     }
//
//     [ClientRpc]
//     private void RpcToggleMenu()
//     {
//         isMenuOpen = !isMenuOpen;
//
//         if (mainMenuScript.state == MenuState.InParty)
//         {
//             tabletUI.SetActive(isMenuOpen);
//             // frontCamera.Priority = isMenuOpen ? 20 : 0; 
//             // defaultCamera.Priority = isMenuOpen ? 0 : 10;
//         }
//         else
//         {
//             mainMenu.SetActive(isMenuOpen);
//         }
//     }
//
//     private void Update()
//     {
//         if (!isLocalPlayer) return; // Apenas o dono do player pode interagir
//         
//         if (Keyboard.current.tabKey.wasPressedThisFrame)
//         {
//             if (mainMenuScript.IsPartyLeader) // Apenas o líder da party pode abrir o menu
//             {
//                 CmdToggleMenu(netId); // Envia o ID do jogador para o servidor
//             }
//         }
//     }
//
//     private bool IsPartyLeader(uint playerId)
//     {
//         // Aqui você pode checar se esse playerId é o líder da party na lógica do seu jogo
//         return mainMenuScript.IsPartyLeader; 
//     }
// }
