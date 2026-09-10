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
    private Transform _localPlayer;
    
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
        if (_localPlayer == null)
        {
            FindAndInitPlayer();
            return;
        }
        
        float posicaoAtual = _localPlayer.position.x;
        
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
        var localIdentity = NetworkClient.localPlayer;
        if (localIdentity != null)
        {
            _localPlayer = localIdentity.transform;
            
            posicaoInicial = gameObject.transform.position.x;
            maxDistancia = 0f;
            
            Debug.Log("RecordeDistanciaEndpoint: Inicializado no eixo " + 
                     (eixoMedicao == 0 ? "X" : eixoMedicao == 1 ? "Y" : "Z"));
        }
    }
}
