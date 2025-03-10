using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerScript))]
public class PlayerShootSystem : PlayerScriptBase
{
    [SerializeField] private PlayerControlSO _playerSO;
    [SerializeField] private Transform origemTiro;
    [SerializeField] private ProjetilData _projetil;
    
    [SerializeField]  private Camera cameraJogador;
    private float ultimoTiroTempo;

    private void Awake()
    {
        if (origemTiro == null)
            origemTiro = transform;
    }

    private void Start()
    {
        if (!isLocalPlayer) return;
        _playerSO.EventOnShoot += EventOnShoot;
    }

    private void OnDestroy()
    {
        if (isLocalPlayer)
            _playerSO.EventOnShoot -= EventOnShoot;
    }

private void EventOnShoot(InputAction.CallbackContext obj)
{
    if (!isLocalPlayer || !PodeTirarAgora()) return;
    
    ultimoTiroTempo = Time.time;
    State = PlayerStates.Shooting;
    
    CmdAtirar(cameraJogador.transform.forward);
    
    StartCoroutine(VoltarParaDefaultAposTiro());
}

    private bool PodeTirarAgora()
    {
        bool recargaCompleta = Time.time >= ultimoTiroTempo + _projetil.tempoRecarga;
        bool estadoValido = IsInState(PlayerStates.Default) || IsInState(PlayerStates.ShootCooldown);
        return recargaCompleta && estadoValido;
    }

    private IEnumerator VoltarParaDefaultAposTiro()
    {
        yield return new WaitForSeconds(0.2f);
        State = PlayerStates.ShootCooldown;

        float tempoRestante = _projetil.tempoRecarga - 0.2f;
        if (tempoRestante > 0)
            yield return new WaitForSeconds(tempoRestante);

        if (IsInState(PlayerStates.ShootCooldown))
            State = PlayerStates.Default;
    }

    [Command]
    private void CmdAtirar(Vector3 cameraForward)
    {
        if (!isServer) return;

        // Usa a direção da câmera completa, incluindo o componente Y
        Vector3 direcaoTiro = cameraForward.normalized;
    
        // Calcula a velocidade inicial diretamente na direção da câmera
        Vector3 velocityInicial = direcaoTiro * _projetil.velocidadeInicial;

        // Instancia o projétil e inicializa com a velocity calculada
        if (_projetil.projetilPrefab != null)
        {
            GameObject projetil = Instantiate(_projetil.projetilPrefab, origemTiro.position, origemTiro.rotation);
            NetworkServer.Spawn(projetil);

            ProjetilParabolico projParabolico = projetil.GetComponent<ProjetilParabolico>();
            if (projParabolico != null)
            {
                projParabolico.Inicializar(velocityInicial, _projetil.gravidade, _projetil.velocidadeProjetil);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (origemTiro == null) return;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(origemTiro.position, 0.1f);
        
        if (Application.isPlaying)
        {
            DesenharTrajetoriaParabolica();
        }
    }
    private void Update()
    {
        if (!isLocalPlayer) return;
        
        if (origemTiro != null && cameraJogador != null)
        {
            origemTiro.rotation = cameraJogador.transform.rotation;
        }
    }
    private void DesenharTrajetoriaParabolica()
    {
        if (cameraJogador == null) return;
    
        // Usa a direção completa da câmera
        Vector3 direcaoTiro = cameraJogador.transform.forward.normalized;
    
        // Calcula a velocidade inicial diretamente na direção da câmera
        Vector3 velInicial = direcaoTiro * _projetil.velocidadeInicial;

        // Desenha
        Gizmos.color = Color.red;

        Vector3 posAnterior = origemTiro.position;
        float step = _projetil.tempoMaximoTrajetoria / _projetil.passosTrajetoria;
    
        for (int i = 1; i <= _projetil.passosTrajetoria; i++)
        {
            float t = i * step;

            // S(t) = S0 + v0*t + 1/2*g*t^2 (apenas g no Y)
            Vector3 posAtual = origemTiro.position 
                               + velInicial * t
                               + 0.5f * new Vector3(0, _projetil.gravidade, 0) * (t * t);

            Gizmos.DrawLine(posAnterior, posAtual);
            posAnterior = posAtual;
        }
    
        Gizmos.DrawWireSphere(posAnterior, 0.1f);
    }
}