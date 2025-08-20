using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MovingPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    [SerializeField] private Transform platformTransform;
    [SerializeField] private LayerMask playerLayerMask = -1;
    [SerializeField] private float detectionRadius = 1f; // Raio para OverlapSphere
    [SerializeField] private float playerStayTime = 0.5f; // Aumentar para 0.5f ou mais
[SerializeField] private float verticalOffsetDetection = 1.0f; // Aumentar para 1.0f ou mais
    private List<PlayerOnPlatformData> playersOnPlatform = new List<PlayerOnPlatformData>();
    private Vector3 lastPlatformPosition;

    // Classe para armazenar informações do player na plataforma
    private class PlayerOnPlatformData
    {
        public Transform playerTransform;
        public float timeExitedTrigger; // Tempo em que o player saiu do trigger (para gerenciar o "playerStayTime")
        public bool currentlyInTrigger; // Indica se o player está atualmente dentro do trigger
    }
    
    void Start()
    {
        if (platformTransform == null)
            platformTransform = transform;
            
        lastPlatformPosition = platformTransform.position;
    }
    
    void LateUpdate()
    {
        // Calcula o movimento da plataforma
        Vector3 platformMovement = platformTransform.position - lastPlatformPosition;
        
        if (platformMovement.magnitude > 0.0001f) // Usar um valor pequeno para evitar jitter
        {
            MovePlayers(platformMovement);
        }
        
        lastPlatformPosition = platformTransform.position;
        
        // limpa e att a lista de macacos
        UpdateAndCleanupPlayers();
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (IsPlayerLayer(other.gameObject))
        {
            PlayerOnPlatformData existingPlayer = playersOnPlatform.FirstOrDefault(p => p.playerTransform == other.transform);
            if (existingPlayer != null)
            {
                // Player já estava na lista, apenas marca como dentro do trigger de novo
                existingPlayer.currentlyInTrigger = true;
            }
            else
            {
                // Adiciona novo player
                playersOnPlatform.Add(new PlayerOnPlatformData {
                    playerTransform = other.transform,
                    currentlyInTrigger = true,
                    timeExitedTrigger = 0f // Não saiu do trigger ainda
                });
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (IsPlayerLayer(other.gameObject))
        {
            PlayerOnPlatformData existingPlayer = playersOnPlatform.FirstOrDefault(p => p.playerTransform == other.transform);
            if (existingPlayer != null)
            {
                // quando macaco sai do trigger, marca o tempo e flag
                existingPlayer.currentlyInTrigger = false;
                existingPlayer.timeExitedTrigger = Time.time;
            }
        }
    }

    private void MovePlayers(Vector3 movement)
    {
        var playersToMove = playersOnPlatform.ToList(); 

        foreach (PlayerOnPlatformData playerData in playersToMove)
        {
            if (IsPlayerValid(playerData.playerTransform))
            {
                CharacterController controller = playerData.playerTransform.GetComponent<CharacterController>();
                if (controller != null && controller.enabled)
                {
                    

                    // Aplica o movimento da plataforma
                    controller.Move(movement); 

                    if (!controller.isGrounded) // Se o player não está "grudado" por conta própria
                    {
                        // Tenta forçar o player um pouco para baixo para ele "grudar" na plataforma ( se ficar musgo agora desisto)
                        

                        controller.Move(Vector3.down * 0.5f * Time.deltaTime); 
                    }
                }
            }
        }
    }

    private void UpdateAndCleanupPlayers()
    {
        // Remove players que podem tar desativados ou mortos 
        playersOnPlatform.RemoveAll(p => !IsPlayerValid(p.playerTransform));

        // Para os jogadores que saíram do trigger, verifica se eles ainda devem ser considerados na plataforma
        // Isso lida com o pulo: o player saiu do trigger mas está próximo e dentro do "tempo de tolerância"
        playersOnPlatform.RemoveAll(playerData => 
            !playerData.currentlyInTrigger && // Se ele saiu do trigger
            Time.time - playerData.timeExitedTrigger > playerStayTime && // E o tempo de tolerância acabou
            !IsPlayerNearPlatform(playerData.playerTransform) // E ele não está mais perto da plataforma (verificação de fallback)
        );
    }

    // Nova função para verificar se o player está perto da plataforma, mesmo fora do trigger
    // No método IsPlayerNearPlatform:
    private bool IsPlayerNearPlatform(Transform playerTransform)
    {
        if (playerTransform == null) return false;

        Vector2 platformFlatPos = new Vector2(platformTransform.position.x, platformTransform.position.z);
        Vector2 playerFlatPos = new Vector2(playerTransform.position.x, playerTransform.position.z);
        float horizontalDistance = Vector2.Distance(platformFlatPos, playerFlatPos);

        
        return horizontalDistance <= detectionRadius &&
            playerTransform.position.y >= platformTransform.position.y - 0.1f && // Permite um pouco abaixo
            playerTransform.position.y <= platformTransform.position.y + verticalOffsetDetection; 
    }
    
    private bool IsPlayerValid(Transform player)
    {
        if (player == null) return false;
        
        if (!player.gameObject.activeInHierarchy) return false;
        
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null || !controller.enabled) return false;
        
        // Caso seja pra fazer por playerscript, mas aí Gustavo infarta cmg
        // var playerScript = player.GetComponent<PlayerScript>();
        // if (playerScript != null)
        // {
        //     // Remova ou adapte esta linha se você não tiver PlayerState no PlayerScript
        //     // if (playerScript.State == PlayerState.Death) return false;
        // }
        
        return true;
    }
    
    private bool IsPlayerLayer(GameObject obj)
    {
        return ((1 << obj.layer) & playerLayerMask) != 0;
    }
    
    // Métodos para forçar tirar a galera 
    public void RemovePlayer(Transform player)
    {
        playersOnPlatform.RemoveAll(p => p.playerTransform == player);
    }
    
    public void ClearAllPlayers()
    {
        playersOnPlatform.Clear();
    }

    void OnDrawGizmos()
    {
        if (platformTransform == null) platformTransform = transform;
        Gizmos.color = Color.cyan;
        // Desenha a esfera de detecção horizontal
        Gizmos.DrawWireSphere(platformTransform.position, detectionRadius);

        // Desenha uma representação da altura de detecção
        Vector3 topDetectionPoint = platformTransform.position + Vector3.up * verticalOffsetDetection;
        Gizmos.DrawWireSphere(topDetectionPoint, detectionRadius * 0.1f); // Pequena esfera no topo
        Gizmos.DrawLine(platformTransform.position, topDetectionPoint); // Linha para indicar a altura
    }
}