using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.ProBuilder;

public class ChaoSumindo : ChaoMae
{
    [Server]
    public override void poeChao()
    {
        // TODO: Adicionar shader de aparecer
        transform.position = posIncial;
    }

    [Server]
    public override void tiraChao()
    {
        gameObject.SetActive(false);
    }
}
