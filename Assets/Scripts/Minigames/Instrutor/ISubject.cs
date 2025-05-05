using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISubject
{
    void Adicionar(IObserver observer);

    void Retira(IObserver observer);

    void Notifica();
}
