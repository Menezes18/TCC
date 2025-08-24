using System.Collections;
using UnityEngine;
using Mirror;

public class ChaoCaindo : ChaoMae
{
    public float tempoPraCair = 0.5f;
    public Collider colisor;

    // cache para reduzir GC em muitas instâncias
    private WaitForSeconds _waitAntesDeCair;
    private WaitForSeconds _waitDepoisDeCair;
    private Coroutine _quedaRoutine;

    private void Awake()
    {
        _waitAntesDeCair = new WaitForSeconds(tempoPraCair);
        _waitDepoisDeCair = new WaitForSeconds(5f);
    }

    private void OnDisable()
    {
        if (_quedaRoutine != null)
        {
            StopCoroutine(_quedaRoutine);
            _quedaRoutine = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || chaoTirado)
            return;

        // servidor decide; cliente solicita
        if (isServer)
        {
            chaoTirado = true;
            tiraChao();
        }
        else
        {
            CmdTentarCair();
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdTentarCair()
    {
        if (!chaoTirado){
            chaoTirado = true;
            tiraChao();
        }
    }
    [Server]
    public override void poeChao()
    {
        transform.position = posIncial;
        if (colisor != null) colisor.enabled = true;
        chaoTirado = false;
    }

    [Server]
    public override void tiraChao()
    {
        if (_quedaRoutine != null) StopCoroutine(_quedaRoutine);
        _quedaRoutine = StartCoroutine(desceChao());
    }

    private IEnumerator desceChao()
    {
        float tempoDecorrido = 0f;
        yield return _waitAntesDeCair;
        while (tempoDecorrido < dataChao.tempo)
        {
            transform.position -= Vector3.up * dataChao.speed * Time.deltaTime;
            tempoDecorrido += Time.deltaTime;
            if (colisor != null) colisor.enabled = false;
            yield return null;
        }
        yield return _waitDepoisDeCair;
        // poeChao();
        _quedaRoutine = null;
    }
}
