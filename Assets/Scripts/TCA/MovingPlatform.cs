using UnityEngine;
using System.Collections.Generic;

public class MovingPlatform : MonoBehaviour
{
     [Header("Platform Settings")]
    [SerializeField] private Transform platformTransform;
    [SerializeField] private LayerMask playerLayerMask = -1;
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private float maxHeightDifference = 2f;
    
    private Vector3 lastPosition;
    private Vector3 lastRotation;
    private List<Transform> passengersOnPlatform = new List<Transform>();
    
    private void Start()
    {
        if (platformTransform == null)
            platformTransform = transform;
            
        lastPosition = platformTransform.position;
        lastRotation = platformTransform.eulerAngles;
    }
    
    private void Update()
    {
        // Calcula o movimento da plataforma
        Vector3 deltaPosition = platformTransform.position - lastPosition;
        Vector3 deltaRotation = platformTransform.eulerAngles - lastRotation;
        
        // Move todos os passageiros junto com a plataforma
        foreach (Transform passenger in passengersOnPlatform)
        {
            if (passenger != null)
            {
                // Move o passageiro junto com a plataforma
                CharacterController controller = passenger.GetComponent<CharacterController>();
                if (controller != null)
                {
                    controller.Move(deltaPosition);
                }
                else
                {
                    passenger.position += deltaPosition;
                }
                
                // Aplica rotação se necessário
                if (deltaRotation.magnitude > 0.1f)
                {
                    passenger.RotateAround(platformTransform.position, Vector3.up, deltaRotation.y);
                }
            }
        }
        
        lastPosition = platformTransform.position;
        lastRotation = platformTransform.eulerAngles;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AddPassenger(other.transform);
            Debug.Log(other.tag + " Entrou");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RemovePassenger(other.transform);
            Debug.Log(other.tag + " Saiu");
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log(other.tag + " Está");
            // Verifica se o jogador está realmente em cima da plataforma
            float heightDifference = other.transform.position.y - transform.position.y;
            
            if (heightDifference > 0.1f && heightDifference < maxHeightDifference)
            {
                if (!passengersOnPlatform.Contains(other.transform))
                {
                    AddPassenger(other.transform);
                }
            }
            else if (heightDifference <= 0.1f)
            {
                RemovePassenger(other.transform);
            }
        }
    }
    
    private void AddPassenger(Transform passenger)
    {
        if (!passengersOnPlatform.Contains(passenger))
        {
            passengersOnPlatform.Add(passenger);
            Debug.Log($"Passageiro {passenger.name} adicionado à plataforma");
        }
    }
    
    private void RemovePassenger(Transform passenger)
    {
        if (passengersOnPlatform.Contains(passenger))
        {
            passengersOnPlatform.Remove(passenger);
            Debug.Log($"Passageiro {passenger.name} removido da plataforma");
        }
    }
    
    // Limpa referências nulas
    private void LateUpdate()
    {
        passengersOnPlatform.RemoveAll(passenger => passenger == null);
    }
    
    // Visualização no editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up * maxHeightDifference/2, 
                           new Vector3(detectionRadius * 2, maxHeightDifference, detectionRadius * 2));
    }
}