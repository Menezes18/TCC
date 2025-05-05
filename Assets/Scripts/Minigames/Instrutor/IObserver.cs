using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObserver
{
    abstract void Atualizacao(ISubject subject);
    
}
