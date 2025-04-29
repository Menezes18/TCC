using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerScript))]
[RequireComponent(typeof(LineRenderer))]
public class PlayerShootSystem : PlayerScriptBase
{
    [SerializeField] private PlayerControlSO _playerSO;
    [SerializeField] private Transform origemTiro;
    [SerializeField] private ProjetilData _projetil;
    [SerializeField] private PlayerScript _player;

    private LineRenderer lineRenderer;
    public bool segurandoBotao = false;
    private float ultimoTiroTempo = -999f;

    public float alturaExtra = 5f;

    private void Awake()
    {
        if (origemTiro == null)
            origemTiro = transform;

        _player = GetComponent<PlayerScript>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
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
    private IEnumerator CooldownCounter()
    {
        float endTime = ultimoTiroTempo + _projetil.tempoRecarga;
        while (Time.time < endTime)
        {
            float restante = endTime - Time.time;
            //Debug.Log($"Cooldown restante: {restante:F2}s");
            yield return new WaitForSeconds(0.5f);
        }
    }
    private void Update()
    {
        if (!isLocalPlayer) return;

        if (segurandoBotao && origemTiro != null && _player.cameraJogador != null)
        {
            origemTiro.rotation = _player.cameraJogador.transform.rotation;
            AtualizarTrajetoriaParabolica();
        }
    }

    public void EventOnShoot(InputAction.CallbackContext ctx)
    {
        if (!isLocalPlayer) return;
        if (!PodeTirarAgora()) return;
        if (ctx.started)
        {
            segurandoBotao = true;
            lineRenderer.enabled = true;
            _player._animator.SetTrigger("StartHold");
            _player._animator.SetBool("IsHolding", true);
            CmdNotifyStartHold();
        }
        else if (ctx.canceled)
        {
            segurandoBotao = false;
            lineRenderer.enabled = false;
            _player._animator.SetBool("IsHolding", false);
            _player._animator.SetTrigger("Throw");
            CmdNotifyThrow();
            
            if (PodeTirarAgora())
            {
                ultimoTiroTempo = Time.time;
                State = PlayerStates.ShootCooldown;

                CmdAtirar(_player.cameraJogador.transform.forward);
                StartCoroutine(VoltarParaMovingAposCooldown());
                StartCoroutine(CooldownCounter());
                CmdNotifyThrow();
            }
        }
    }


    [Command]
    void CmdNotifyStartHold()
    {
        RpcOnStartHold();
    }

    [Command]
    void CmdNotifyThrow()
    {
        RpcOnThrow();
    }

    [ClientRpc]
    void RpcOnStartHold()
    {
        if (isLocalPlayer) return;  
        _player._animator.SetTrigger("StartHold");
        _player._animator.SetBool("IsHolding", true);
    }

    [ClientRpc]
    void RpcOnThrow()
    {
        if (isLocalPlayer) return;
        _player._animator.SetBool("IsHolding", false);
        _player._animator.SetTrigger("Throw");
    }

    private bool PodeTirarAgora()
    {
        bool recargaCompleta = Time.time >= ultimoTiroTempo + _projetil.tempoRecarga;
        bool estadoValido = IsInState(PlayerStates.Moving) ||
                            IsInState(PlayerStates.Idle) ||
                            IsInState(PlayerStates.Pushing) ||
                            IsInState(PlayerStates.PushCooldown);
        return recargaCompleta && estadoValido;
    }

    private IEnumerator VoltarParaMovingAposCooldown()
    {
        // aguarda TODO o tempo de recarga
        yield return new WaitForSeconds(_projetil.tempoRecarga);
        // só troca se ainda estivermos em cooldown
        if (IsInState(PlayerStates.ShootCooldown))
            State = PlayerStates.Idle;
    }

    [Command]
    private void CmdAtirar(Vector3 cameraForward)
    {
        if (!isServer) return;

        Vector3 dir = cameraForward.normalized;
        dir.y += alturaExtra;
        Vector3 velIni = dir * _projetil.velocidadeInicial;

        if (_projetil.projetilPrefab != null)
        {
            var proj = Instantiate(_projetil.projetilPrefab, origemTiro.position, origemTiro.rotation);
            NetworkServer.Spawn(proj);

            var pp = proj.GetComponent<ProjetilParabolico>();
            if (pp != null)
                pp.Inicializar(velIni, _projetil.gravidade, _projetil.velocidadeProjetil, netIdentity);
        }
    }
    
    private void AtualizarTrajetoriaParabolica()
    {
        if (_player.cameraJogador == null || lineRenderer == null) return;

        Vector3 dir = _player.cameraJogador.transform.forward.normalized;
        dir.y += alturaExtra;
        Vector3 velIni = dir * _projetil.velocidadeInicial;

        int passos = Mathf.Min(5, _projetil.passosTrajetoria);
        float step = _projetil.tempoMaximoTrajetoria / _projetil.passosTrajetoria;

        lineRenderer.positionCount = passos + 1;
        for (int i = 0; i <= passos; i++)
        {
            float t = i * step;
            Vector3 pos = origemTiro.position
                        + velIni * t
                        + 0.5f * new Vector3(0, _projetil.gravidade, 0) * (t * t);
            lineRenderer.SetPosition(i, pos);
        }
    }
    
    #region Gizmo
    private void OnDrawGizmos()
    {
        if (origemTiro == null) return;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(origemTiro.position, 0.1f);
        if (Application.isPlaying) DesenharTrajetoriaParabolica();
    }

    private void DesenharTrajetoriaParabolica()
    {
        if (_player.cameraJogador == null) return;
        Vector3 dir = _player.cameraJogador.transform.forward.normalized;
        dir.y += alturaExtra;
        Vector3 velIni = dir * _projetil.velocidadeInicial;

        Gizmos.color = Color.red;
        Vector3 prev = origemTiro.position;
        float step = _projetil.tempoMaximoTrajetoria / _projetil.passosTrajetoria;
        for (int i = 1; i <= _projetil.passosTrajetoria; i++)
        {
            float t = i * step;
            Vector3 pos = origemTiro.position
                        + velIni * t
                        + 0.5f * new Vector3(0, _projetil.gravidade, 0) * (t * t);
            Gizmos.DrawLine(prev, pos);
            prev = pos;
        }
        Gizmos.DrawWireSphere(prev, 0.1f);
    }
    #endregion
}