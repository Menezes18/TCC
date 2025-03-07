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
    
    private Camera cameraJogador;
    private float ultimoTiroTempo;

    private void Awake()
    {
        if (origemTiro == null)
            origemTiro = transform;

        cameraJogador = Camera.main;
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
        
        CmdAtirar();
        
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
    private void CmdAtirar()
    {
        if (!isServer) return;

        // 1) Calcula direção base no XZ (ignora a inclinação vertical da camera).
        Vector3 dirXZ = cameraJogador.transform.forward;
        dirXZ.y = 0f;
        dirXZ.Normalize();

        // 2) Converte angulo de graus para radianos.
        float rad = _projetil.anguloTiro * Mathf.Deg2Rad;

        // 3) Decompoe velocidade inicial em horizontal (XZ) e vertical (Y).
        float vXZ = _projetil.velocidadeInicial * Mathf.Cos(rad);
        float vY  = _projetil.velocidadeInicial * Mathf.Sin(rad);

        // 4) Monta o vetor de velocidade inicial => (vXZ na horizontal, vY na vertical).
        Vector3 velocityInicial = dirXZ * vXZ;
        velocityInicial.y = vY;

        // 5) Instancia o projétil e inicializa com a velocity calculada
        if (_projetil.projetilPrefab != null)
        {
            GameObject projetil = Instantiate(_projetil.projetilPrefab, origemTiro.position, Quaternion.identity);
            NetworkServer.Spawn(projetil);

            ProjetilParabolico projParabolico = projetil.GetComponent<ProjetilParabolico>();
            if (projParabolico != null)
            {
                projParabolico.Inicializar(velocityInicial, _projetil.gravidade);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (origemTiro == null) return;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(origemTiro.position, 0.1f);
        
        if (cameraJogador != null && Application.isPlaying)
        {
            DesenharTrajetoriaParabolica();
        }
    }

    private void DesenharTrajetoriaParabolica()
    {
        // 1) Direção base no XZ
        Vector3 dirXZ = cameraJogador.transform.forward;
        dirXZ.y = 0f;
        dirXZ.Normalize();

        // 2) Angulo -> radianos
        float rad = _projetil.anguloTiro * Mathf.Deg2Rad;

        // 3) Decompoe
        float vXZ = _projetil.velocidadeInicial * Mathf.Cos(rad);
        float vY  = _projetil.velocidadeInicial * Mathf.Sin(rad);

        // 4) Monta velocity
        Vector3 velInicial = dirXZ * vXZ;
        velInicial.y = vY;

        // 5) Desenha
        Gizmos.color = Color.red;

        Vector3 posAnterior = origemTiro.position;
        float step = _projetil.tempoMaximoTrajetoria / _projetil.passosTrajetoria;
        for (int i = 1; i <= _projetil.passosTrajetoria; i++)
        {
            float t = i * step;

            // S(t) = S0 + v0*t + 1/2*g*t^2 (apenas g no Y).
            Vector3 posAtual = origemTiro.position 
                + velInicial * t
                + 0.5f * new Vector3(0, _projetil.gravidade, 0) * (t * t);

            Gizmos.DrawLine(posAnterior, posAtual);
            posAnterior = posAtual;
        }
        
        Gizmos.DrawWireSphere(posAnterior, 0.1f);
    }
}