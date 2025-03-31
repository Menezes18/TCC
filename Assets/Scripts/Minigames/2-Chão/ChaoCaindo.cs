using System.Collections;
using UnityEngine;
using Mirror;

public class ChaoCaindo : ChaoMae
{
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && !chaoTirado)
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
        chaoTirado = false;
    }

    [Server]
    public override void tiraChao()
    {
        StartCoroutine(desceChao());
    }

    private IEnumerator desceChao()
    {
        float tempoDecorrido = 0f;
        yield return new WaitForSeconds(0.5f);
        while (tempoDecorrido < dataChao.tempo)
        {
            transform.position -= Vector3.up * dataChao.speed * Time.deltaTime;
            tempoDecorrido += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(5f);
        poeChao();
    }
}
