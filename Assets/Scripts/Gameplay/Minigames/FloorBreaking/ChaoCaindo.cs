using System.Collections;
using UnityEngine;
using Mirror;

public class ChaoCaindo : ChaoMae
{
    public float tempoPraCair = 0.5f;
    public Collider colisor;
    [SyncVar(hook = nameof(OnCollisionDisabledChanged))] private bool collisionDisabled;
    private Coroutine fallRoutine;

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            tiraChao();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnCollisionDisabledChanged(false, collisionDisabled);
    }

    private void OnCollisionDisabledChanged(bool oldValue, bool newValue)
    {
        if (colisor != null) colisor.enabled = !newValue;
    }

    [Server]
    public override void poeChao()
    {
        if (fallRoutine != null) StopCoroutine(fallRoutine);
        fallRoutine = null;
        transform.position = posIncial;
        collisionDisabled = false;
        OnCollisionDisabledChanged(true, false);
        chaoTirado = false;
    }

    [Server]
    public override void tiraChao()
    {
        if (chaoTirado || dataChao == null) return;
        chaoTirado = true;
        fallRoutine = StartCoroutine(desceChao());
    }

    private IEnumerator desceChao()
    {
        yield return new WaitForSeconds(tempoPraCair);
        collisionDisabled = true;
        OnCollisionDisabledChanged(false, true);
        float elapsed = 0f;
        while (elapsed < dataChao.tempo)
        {
            transform.position -= Vector3.up * dataChao.speed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        fallRoutine = null;
    }
}
