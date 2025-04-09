using System.Collections;
using UnityEngine;
using Mirror;

public class ChaoCaindo : ChaoMae
{
    public float tempoPraCair = 0.5f;
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
        gameObject.GetComponent<BoxCollider> ().enabled = true;
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
        yield return new WaitForSeconds(tempoPraCair);
        while (tempoDecorrido < dataChao.tempo)
        {
            transform.position -= Vector3.up * dataChao.speed * Time.deltaTime;
            tempoDecorrido += Time.deltaTime;
            gameObject.GetComponent<BoxCollider> ().enabled = false;
            yield return null;
        }
        yield return new WaitForSeconds(5f);
        poeChao();
    }
}
