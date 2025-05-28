using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MovingPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    [SerializeField] private Transform platformTransform;
    [SerializeField] private LayerMask playerLayerMask = -1;
    [SerializeField] private float detectionRadius = 1f;
    
    private List<Transform> playersOnPlatform = new List<Transform>();
    private Vector3 lastPlatformPosition;
    
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
        
        if (platformMovement.magnitude > 0.001f)
        {
            // Move apenas players válidos (vivos)
            MoveValidPlayers(platformMovement);
        }
        
        lastPlatformPosition = platformTransform.position;
        
        // Limpa lista de players inválidos periodicamente
        CleanupInvalidPlayers();
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (IsPlayerLayer(other.gameObject) && IsPlayerValid(other.transform))
        {
            if (!playersOnPlatform.Contains(other.transform))
            {
                playersOnPlatform.Add(other.transform);
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (IsPlayerLayer(other.gameObject))
        {
            playersOnPlatform.Remove(other.transform);
        }
    }
    
    private void MoveValidPlayers(Vector3 movement)
    {
        // Cria uma lista temporária para evitar modificar durante iteração
        var validPlayers = playersOnPlatform.Where(IsPlayerValid).ToList();
        
        foreach (Transform player in validPlayers)
        {
            // Verifica se ainda é válido antes de mover
            if (IsPlayerValid(player))
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null && controller.enabled)
                {
                    controller.Move(movement);
                }
            }
        }
    }
    
    private bool IsPlayerValid(Transform player)
    {
        if (player == null) return false;
        
        // Verifica se o GameObject está ativo
        if (!player.gameObject.activeInHierarchy) return false;
        
        // Verifica CharacterController - PRINCIPAL INDICADOR DE MORTE
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null || !controller.enabled) return false;
        
        // Verifica estado do player diretamente (sem reflection)
        var playerScript = player.GetComponent<PlayerScript>();
        if (playerScript != null)
        {
            // Acesso direto ao enum PlayerState.Death
            if (playerScript.State == PlayerState.Death) return false;
        }
        
        return true;
    }
    
    // Método adicional para debug - opcional
    
    private void CleanupInvalidPlayers()
    {
        // Remove players inválidos da lista
        playersOnPlatform.RemoveAll(player => !IsPlayerValid(player));
    }
    
    private bool IsPlayerLayer(GameObject obj)
    {
        return ((1 << obj.layer) & playerLayerMask) != 0;
    }
    
    // Método público para forçar remoção de um player específico
    public void RemovePlayer(Transform player)
    {
        playersOnPlatform.Remove(player);
    }
    
    // Método público para limpar todos os players
    public void ClearAllPlayers()
    {
        playersOnPlatform.Clear();
    }
}