using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public class ChaoSumindo : ChaoMae
{
    public override void poeChao()
    {
        //TODO: Adicionar shader de aparecer
        transform.position = posIncial;
    }

    public override void tiraChao()
    {
        gameObject.SetActive(false);
    }
}
