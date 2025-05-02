using UnityEngine;
using TMPro;
using Mirror;

public class RecordeDistanciaEndpoint : NetworkBehaviour
{
    [Header("UI")]
    public TMP_Text distanciaText;
    public Transform endpoint;
    
    [Tooltip("0 = X, 1 = Y, 2 = Z")]
    public int eixoMedicao = 2; 
    
    [SerializeField]
    private float posicaoInicial = -205.5f;
    private float maxDistancia = 0;
    private float distanciaAtual = 0f;
    
    private void Start()
    {
        if (endpoint == null)
        {
            endpoint = transform;
        }
        
        FindAndInitPlayer();
    }
    
    private void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            FindAndInitPlayer();
            return;
        }
        
        NetworkIdentity netId = player.GetComponent<NetworkIdentity>();
        if (netId != null && !netId.isLocalPlayer)
        {
            return;
        }
        
        float posicaoAtual = player.transform.position.x;
        
        distanciaAtual = posicaoAtual - posicaoInicial;
        
        distanciaAtual = Mathf.Max(0, distanciaAtual);
        
        if (distanciaAtual > maxDistancia)
        {
            maxDistancia = distanciaAtual;
            
            if (distanciaText != null)
            {
                distanciaText.text = "Recorde: " + Mathf.RoundToInt(maxDistancia).ToString();
            }
        }
    }
    
    private void FindAndInitPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            NetworkIdentity netId = player.GetComponent<NetworkIdentity>();
            if (netId != null && !netId.isLocalPlayer)
            {
                return;
            }
            
            posicaoInicial = gameObject.transform.position.x;
            maxDistancia = 0f;
            
            Debug.Log("RecordeDistanciaEndpoint: Inicializado no eixo " + 
                     (eixoMedicao == 0 ? "X" : eixoMedicao == 1 ? "Y" : "Z"));
        }
    }
}