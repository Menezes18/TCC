using System.Collections;
using UnityEngine;

class ChaoCaindo : ChaoMae
{
    public void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && !chaoTirado){
            chaoTirado = true;
            tiraChao();
        }
    }
    public override void poeChao()
    {
        transform.position = posIncial;
    }

    public override void tiraChao()
    {
        StartCoroutine(desceChao());
    }

    private IEnumerator desceChao()
    {
        float tempoDecorrido = 0f;
        yield return new WaitForSeconds(0.6f);
        while (tempoDecorrido < tempo)
        {
            transform.position -= Vector3.up * speed * Time.deltaTime;
            tempoDecorrido += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(5f);
        poeChao();
    }
}
